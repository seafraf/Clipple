using Clipple.Util;
using Clipple.Util.ISOBMFF;
using Clipple.Wpf.View;
using Clipple.Wpf.ViewModel;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using System.Xml.Serialization;

namespace Clipple.ViewModel
{
    public class RootViewModel : ObservableObject
    {

        public RootViewModel()
        {
            updateViewModel      = new UpdateViewModel();
            VideoPlayerViewModel = new VideoPlayerViewModel();

            var stopwatch = Stopwatch.StartNew();

            // Create commands
            OpenVideosFlyout   = new RelayCommand(() => IsVideosFlyoutOpen = !IsVideosFlyoutOpen);
            OpenSettingsFlyout = new RelayCommand(() => IsSettingsFlyoutOpen = !IsSettingsFlyoutOpen);
            OpenUpdateDialog   = new RelayCommand(() =>
            {
                if (UpdateViewModel.LatestVersion == null)
                    return;

                UpdateDialog? dialog = null;
                dialog = new UpdateDialog()
                {
                    DataContext = new UpdateDialogViewModel(UpdateViewModel,
                        new RelayCommand(async () =>
                        {
                            dialog?.Close();

                            // start download
                            var manager     = UpdateViewModel.Manager;
                            var updateInfo  = UpdateViewModel.UpdateInfo;
                            if (manager != null && updateInfo != null)
                            {
                                App.VideoPlayerVisible = false;

                                var progressDialog = await App.Window.ShowProgressAsync("Please wait...", "Fetching updates");
                                await manager.DownloadReleases(updateInfo.ReleasesToApply, (progress) =>
                                {
                                    var downloadedBytes = (long)Math.Floor(UpdateViewModel.UpdateSize * (progress / 100.0));

                                    progressDialog.SetTitle("Downloading...");
                                    progressDialog.SetMessage($"Downloaded {Formatting.ByteCountToString(downloadedBytes)}/{UpdateViewModel.UpdateSizeString}");
                                    progressDialog.SetProgress(progress / 200.0);
                                });

                                await manager.ApplyReleases(updateInfo, (progress) =>
                                {
                                    progressDialog.SetTitle("Installing...");
                                    progressDialog.SetMessage("Installing updates");
                                    progressDialog.SetProgress(0.5 + (progress / 200.0));
                                });
                                await progressDialog.CloseAsync();

                                // Make sure all settings are saved before exiting as the below function calls Environment.Exit
                                await Save();

                                // No need to bring back the video player since as we're restarting the app
                                UpdateManager.RestartApp();
                            }
                        }),
                        new RelayCommand(() =>
                        {
                            dialog?.Close();
                        }))
                };
                dialog.ShowDialog();
            });
            ProcessAllVideos = new RelayCommand(async () => await ClipProcessor.Process());

            AddVideoCommand = new RelayCommand(() =>
            {
                using var dialog = new OpenFileDialog();
                var result = dialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.FileName))
                    AddVideo(dialog.FileName);
            });

            AddFolderCommand = new RelayCommand(() =>
            {
                using var dialog = new FolderBrowserDialog();
                var result = dialog.ShowDialog();

                if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(dialog.SelectedPath))
                    AddVideosFromFolder(dialog.SelectedPath);
            });

            Videos.CollectionChanged += (s, e) =>
            {
                // If the user has not selected a video yet, or if the selected video was removed, select a new video for the user if possible
                if (SelectedVideo == null)
                {
                    SelectedVideo = Videos.FirstOrDefault();
                }
                else if (e.OldItems != null && e.Action == NotifyCollectionChangedAction.Remove && e.OldItems.Contains(SelectedVideo))
                {
                    // Try to select a video closest to where the last selected index 
                    SelectedVideo = Videos.ElementAtOrDefault(Math.Min(videos.Count - 1, e.OldStartingIndex));
                }

                OnPropertyChanged(nameof(HasClips));
            };

            var applicationData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Application.ProductName);

            var settingsFile = Path.Combine(applicationData, SettingsFileName);
            var videosFile   = Path.Combine(applicationData, VideosFileName);
            var presetsFile  = Path.Combine(applicationData, ClipPresetsFileName);

            try
            {
                stopwatch.Start();
                var settingsFileReader = new FileStream(settingsFile, FileMode.Open);
                SettingsViewModel = JsonSerializer.Deserialize<SettingsViewModel>(settingsFileReader) ?? throw new Exception();

                stopwatch.Stop();
                App.Logger.Log($"Deserialized settings in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                if (File.Exists(settingsFile))
                    App.Logger.LogError($"Failed to deserialize settings", e);

                // Use default settings if disk settings failed to load
                SettingsViewModel ??= new SettingsViewModel();
            }

            try
            {
                stopwatch.Start();
                var videosFileReader = new FileStream(videosFile, FileMode.Open);
                var videos = JsonSerializer.Deserialize<ObservableCollection<VideoViewModel>>(videosFileReader) ?? throw new Exception();

                foreach (var video in videos)
                {
                    Videos.Add(video);
                }

                stopwatch.Stop();
                App.Logger.Log($"Deserialized videos in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                if (File.Exists(videosFile))
                    App.Logger.LogError($"Failed to deserialize videos", e);
            }

            try
            {
                stopwatch.Start();
                var presetsFileReader = new FileStream(presetsFile, FileMode.Open);
                clipPresetsViewModel = JsonSerializer.Deserialize<ClipPresetsViewModel>(presetsFileReader) ?? throw new Exception();


                stopwatch.Stop();
                App.Logger.Log($"Deserialized clip presets in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                if (File.Exists(presetsFile))
                    App.Logger.LogError($"Failed to deserialize clip presets", e);

                // Use default presets if disk settings failed to load
                clipPresetsViewModel ??= new ClipPresetsViewModel(true);
            }

            // Update timer now that settings are loaded
            App.AutoSaveTimer.Interval = SettingsViewModel.AutoSaveFrequency * 1000;
            App.AutoSaveTimer.Start();

            var ingestResource = SettingsViewModel.IngestAutomatically ? SettingsViewModel.IngestFolder : null;

            // Handle CLI input path/video
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
                ingestResource = args[1];

            if (ingestResource != null)
            {
                if (Directory.Exists(ingestResource))
                {
                    AddVideosFromFolder(ingestResource);
                }
                else if (File.Exists(ingestResource))
                    AddVideo(ingestResource);
            }

            // Check for updates every 5 minutes
            var updateTimer = new DispatcherTimer();
            updateTimer.Tick += async (s, e) => await UpdateViewModel.CheckForUpdate();
            updateTimer.Interval = TimeSpan.FromMinutes(5.0);
            updateTimer.Start();

            // Check for an update immediately after launch
            App.Current.Dispatcher.Invoke(async () => await UpdateViewModel.CheckForUpdate());
        }

        #region Methods
        /// <summary>
        /// Using JSON serialization to save the following data to file:
        /// - The entire SettingsViewModel
        /// - The currently loaded videos and their respective clips
        /// </summary>
        public async Task Save()
        {
            var applicationData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Application.ProductName);

            if (!Directory.Exists(applicationData))
                Directory.CreateDirectory(applicationData);

            var settingsFile = Path.Combine(applicationData, SettingsFileName);
            var videosFile   = Path.Combine(applicationData, VideosFileName);
            var presetsFile  = Path.Combine(applicationData, ClipPresetsFileName);

            var stopwatch = Stopwatch.StartNew();

            try
            {
                stopwatch.Start();
                using var settingsWriter = new FileStream(settingsFile, FileMode.Create);
                await JsonSerializer.SerializeAsync(settingsWriter, SettingsViewModel);

                stopwatch.Stop();
                App.Logger.Log($"Serialized settings in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                App.Logger.LogError($"Failed to serialize settings", e);
            }

            try
            {
                stopwatch.Start();
                using var videosWriter = new FileStream(videosFile, FileMode.Create);
                await JsonSerializer.SerializeAsync(videosWriter, Videos);

                stopwatch.Stop();
                App.Logger.Log($"Serialized videos in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                App.Logger.LogError($"Failed to serialize videos", e);
            }

            try
            {
                stopwatch.Start();
                using var presetsWriter = new FileStream(presetsFile, FileMode.Create);
                await JsonSerializer.SerializeAsync(presetsWriter, ClipPresetsViewModel);

                stopwatch.Stop();
                App.Logger.Log($"Serialized clip presets in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception e)
            {
                App.Logger.LogError($"Failed to serialize clip presets", e);
            }
        }

        /// <summary>
        /// Attempts to add every file in a directory as a video
        /// </summary>
        /// <param name="folder">The folder to add videos from</param>
        /// <returns>True if every file in the specified folder was added successfully, false otherwise</returns>
        public bool AddVideosFromFolder(string folder)
        {
            if (!Directory.Exists(folder))
                return false;

            var failed = false;
            foreach (var file in Directory.GetFiles(folder))
            {
                if (!AddVideo(file))
                    failed = true;
            }

            return !failed;
        }

        /// <summary>
        /// Add a video a video file to the videos list
        /// </summary>
        /// <param name="file">The full file name of the video file to add</param>
        /// <returns>True if the video was added, false otherwise</returns>
        public bool AddVideo(string file)
        {
            // File doesn't exist
            if (!File.Exists(file))
                return false;

            // We already have this video in the list
            if (Videos.Where((x) => x.FileInfo.FullName == file).Any())
                return false;

            try
            {
                var video = new VideoViewModel(file);
                Videos.Add(video);
            }
            catch (Exception e)
            {
                App.Logger.LogError($"Failed to import {file}", e);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Selects the next video in the videos list
        /// </summary>
        internal void NextVideo()
        {
            if (SelectedVideo == null)
                return;

            var idx = Videos.IndexOf(SelectedVideo);
            if (idx == -1 || idx == (Videos.Count - 1))
                return;

            SelectedVideo = Videos[idx + 1];
        }

        /// <summary>
        /// Selects the previous video in the videos list
        /// </summary>
        internal void PreviousVideo()
        {
            if (SelectedVideo == null)
                return;

            var idx = Videos.IndexOf(SelectedVideo);
            if (idx == -1 || idx == 0)
                return;

            SelectedVideo = Videos[idx - 1];
        }

        /// <summary>
        /// Called by various other view models to notify the root view model that clips have changed, we need to know
        /// this to enable the "process all clips" command
        /// </summary>
        public void NotifyClipsChanged()
        {
            OnPropertyChanged(nameof(HasClips));
            OnPropertyChanged(nameof(HasSelectedVideoClips));
        }
        #endregion

        #region Properties
        private VideoViewModel? selectedVideo;
        public VideoViewModel? SelectedVideo
        {
            get => selectedVideo;
            set
            {
                if (selectedVideo != null)
                    selectedVideo.IsSelected = false;

                if (value != null)
                    value.IsSelected = true;

                SetProperty(ref selectedVideo, value);
                OnPropertyChanged(nameof(HasSelectedVideo));
                OnPropertyChanged(nameof(HasSelectedVideoClips));

                // Set the VideoPlayer's video too so they have easier access to it
                VideoPlayerViewModel.Video = value;
            }
        }

        private ObservableCollection<VideoViewModel> videos = new();
        public ObservableCollection<VideoViewModel> Videos
        {
            get { return videos; }
            set { videos = value; }
        }

        private bool isVideosFlyoutOpen;
        public bool IsVideosFlyoutOpen
        {
            get => isVideosFlyoutOpen;
            set
            {
                if (value)
                    VideoPlayerViewModel.VideoVisibility = System.Windows.Visibility.Hidden;

                SetProperty(ref isVideosFlyoutOpen, value);
            }
        }

        private bool isSettingsFlyoutOpen;
        public bool IsSettingsFlyoutOpen
        {
            get => isSettingsFlyoutOpen;
            set
            {
                if (value)
                    VideoPlayerViewModel.VideoVisibility = System.Windows.Visibility.Hidden;

                SetProperty(ref isSettingsFlyoutOpen, value);
            }
        }

        private UpdateViewModel updateViewModel;
        public UpdateViewModel UpdateViewModel
        {
            get => updateViewModel;
            set => SetProperty(ref updateViewModel, value);
        }

        private ClipPresetsViewModel clipPresetsViewModel;
        public ClipPresetsViewModel ClipPresetsViewModel
        {
            get => clipPresetsViewModel;
            set => SetProperty(ref clipPresetsViewModel, value);
        }

        /// <summary>
        /// Does any video have any clips?
        /// </summary>
        public bool HasClips
        {
            get => Videos.Any(video => video.Clips.Any());
        }

        /// <summary>
        /// Has the user selected a video?
        /// </summary>
        public bool HasSelectedVideo => SelectedVideo != null;

        /// <summary>
        /// Does the selected video have any clips?
        /// </summary>
        public bool HasSelectedVideoClips => SelectedVideo != null && SelectedVideo.Clips.Count > 0;

        /// <summary>
        /// Reference to the video player view model
        /// </summary>
        public VideoPlayerViewModel VideoPlayerViewModel { get; }

        /// <summary>
        /// Reference to the settings
        /// </summary>
        public SettingsViewModel SettingsViewModel { get; }

        /// <summary>
        /// Title for the main window
        /// </summary>
        public string Title => $"Clipple ({UpdateViewModel.CurrentVersion})";
        #endregion

        #region Commands
        public ICommand OpenVideosFlyout { get; }
        public ICommand OpenUpdateDialog { get; }
        public ICommand OpenSettingsFlyout { get; }
        public ICommand ProcessAllVideos { get; }
        public ICommand AddVideoCommand { get; }
        public ICommand AddFolderCommand { get; }
        
        #endregion

        #region Member
        private const string SettingsFileName = "settings.json";
        private const string VideosFileName = "videos.json";
        private const string ClipPresetsFileName = "clip-presets.json";
        #endregion
    }
}

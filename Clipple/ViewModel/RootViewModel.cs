using Clipple.Util;
using Clipple.Util.ISOBMFF;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
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
using System.Xml.Serialization;

namespace Clipple.ViewModel
{
    public class RootViewModel : ObservableObject
    {

        public RootViewModel()
        {
            VideoPlayerViewModel = new VideoPlayerViewModel();

            // Create commands
            OpenVideosFlyout = new RelayCommand(() => IsVideosFlyoutOpen = !IsVideosFlyoutOpen);
            OpenSettingsFlyout = new RelayCommand(() => IsSettingsFlyoutOpen = !IsSettingsFlyoutOpen);
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

            ProcessClipsCommand = new RelayCommand(async () =>
            {
                if (SelectedVideo != null)
                    await ClipProcessor.Process(SelectedVideo);
            });

            ClearClipsCommand = new RelayCommand(() =>
            {
                if (SelectedVideo != null)
                    SelectedVideo.Clips.Clear();
            });

            RemoveVideoCommand = new RelayCommand(() =>
            {
                if (SelectedVideo != null)
                    Videos.Remove(SelectedVideo);
            });

            // Change HasClips if the videos property changes
            Videos.CollectionChanged += (s, e) =>
            {
                if (SelectedVideo == null || !Videos.Contains(SelectedVideo))
                {
                    if (Videos.Count > 0)
                        SelectedVideo = Videos.First();
                }

                OnPropertyChanged(nameof(HasClips));
            };

            //Load settings and videos, or create new instances of the respective types
            try
            {
                var applicationData = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    Application.ProductName);

                var settingsFile = Path.Combine(applicationData, SettingsFileName);
                var videosFile = Path.Combine(applicationData, VideosFileName);

                var settingsFileReader = new FileStream(settingsFile, FileMode.Open);
                SettingsViewModel = JsonSerializer.Deserialize<SettingsViewModel>(settingsFileReader) ?? throw new Exception();

                var videosFileReader = new FileStream(videosFile, FileMode.Open);
                var videos = JsonSerializer.Deserialize<ObservableCollection<VideoViewModel>>(videosFileReader) ?? throw new Exception();
                foreach (var video in videos)
                    Videos.Add(video);
            }
            catch (Exception)
            {
                // Use default settings if disk settings failed to load
                SettingsViewModel = new SettingsViewModel();
            }

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
        }

        #region Methods
        /// <summary>
        /// Using JSON serialization to save the following data to file:
        /// - The entire SettingsViewModel
        /// - The currently loaded videos and their respective clips
        /// </summary>
        public async void Save()
        {
            var applicationData = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Application.ProductName);

            if (!Directory.Exists(applicationData))
                Directory.CreateDirectory(applicationData);

            var settingsFile = Path.Combine(applicationData, SettingsFileName);
            var videosFile = Path.Combine(applicationData, VideosFileName);

            using var settingsWriter = new FileStream(settingsFile, FileMode.Create);
            await JsonSerializer.SerializeAsync(settingsWriter, SettingsViewModel);

            using var videosWriter = new FileStream(videosFile, FileMode.Create);
            await JsonSerializer.SerializeAsync(videosWriter, Videos);
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
                Videos.Add(new VideoViewModel(file));
            }
            catch (Exception)
            {
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
                SetProperty(ref selectedVideo, value);
                OnPropertyChanged(nameof(HasSelectedVideo));
                OnPropertyChanged(nameof(HasSelectedVideoClips));

                // Set the VideoPlayer's video too so they have easier access to it
                VideoPlayerViewModel.Video = value;

                // Schedule an open for this video
                if (value != null)
                {
                    Trace.WriteLine(value.FileInfo.FullName);
                    AppCommands.OpenCommand.Execute(value.FileInfo.FullName);
                }
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
            set => SetProperty(ref isVideosFlyoutOpen, value);
        }

        private bool isSettingsFlyoutOpen;
        public bool IsSettingsFlyoutOpen
        {
            get => isSettingsFlyoutOpen;
            set => SetProperty(ref isSettingsFlyoutOpen, value);
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
        [field: XmlIgnore]
        public VideoPlayerViewModel VideoPlayerViewModel { get; }

        /// <summary>
        /// Reference to the settings
        /// </summary>
        public SettingsViewModel SettingsViewModel { get; }
        #endregion

        #region Commands
        public ICommand OpenVideosFlyout { get; }
        public ICommand OpenSettingsFlyout { get; }
        public ICommand ProcessAllVideos { get; }
        public ICommand AddVideoCommand { get; }
        public ICommand AddFolderCommand { get; }
        public ICommand ProcessClipsCommand { get; }
        public ICommand ClearClipsCommand { get; }
        public ICommand RemoveVideoCommand { get; }
        #endregion

        #region Member
        private const string SettingsFileName = "settings.json";
        private const string VideosFileName = "videos.json";
        #endregion
    }
}

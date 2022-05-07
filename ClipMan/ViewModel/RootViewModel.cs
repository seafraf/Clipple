using ClipMan.Util;
using ClipMan.Util.ISOBMFF;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace ClipMan.ViewModel
{
    public class RootViewModel : ObservableObject
    {
        public RootViewModel()
        {
            VideoPlayerViewModel    = new VideoPlayerViewModel();
            Settings                = new SettingsViewModel();

            // Create commands
            SetupCommands();

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

            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                string open = args[1];
                if (Directory.Exists(open))
                {
                    AddVideosFromFolder(open);
                }
                else if (File.Exists(open))
                    AddVideo(open);
            }
        }
       
        private void SetupCommands()
        {
            OpenVideosFlyout = new RelayCommand(() => IsVideoFlyoutOpen = !IsVideoFlyoutOpen);
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

                NotifyClipsChanged();
            });

            RemoveVideoCommand = new RelayCommand(() =>
            {
                if (SelectedVideo != null)
                    Videos.Remove(SelectedVideo);
            });
        }

        public bool AddVideosFromFolder(string folder)
        {
            if (!Directory.Exists(folder))
                return false;

            foreach (var file in Directory.GetFiles(folder))
            {
                try
                {
                    AddVideo(file);
                }
                catch
                {
                    // Ignore exception for now.. this generally means the input file was not recognised was not an ISOBMFF container
                }
            }

            return true;
        }

        public bool AddVideo(string file)
        {
            if (!File.Exists(file))
                return false;

            try
            {
                var info = new FileInfo(file);
                var parser = new SimpleParser(file);
                parser.Parse();

                Videos.Add(new VideoViewModel(info, parser.Tracks.Select(track => track.Name).ToArray()));
            }
            catch (Exception ex)
            {
                // Notify them somehow that a video failed to parse?
            }

            return true;
        } 

        public void NotifyClipsChanged()
        {
            OnPropertyChanged(nameof(HasClips));
        }

        public VideoPlayerViewModel VideoPlayerViewModel { get; }

        #region Properties
        private VideoViewModel? selectedVideo;
        public VideoViewModel? SelectedVideo
        {
            get => selectedVideo;
            set
            {
                SetProperty(ref selectedVideo, value);

                // Set the VideoPlayer's video too so they have easier access to it
                VideoPlayerViewModel.Video = value;

                // Schedule an open for this video
                if (value != null)
                    AppCommands.OpenCommand.Execute(value.FileInfo.FullName);
            }
        }

        private SettingsViewModel settings;
        public SettingsViewModel Settings
        {
            get => settings;
            set => SetProperty(ref settings, value);
        }

        private ObservableCollection<VideoViewModel> videos = new();
        public ObservableCollection<VideoViewModel> Videos
        {
            get { return videos; }
            set { videos = value; }
        }

        private bool isVideoFlyoutOpen;
        public bool IsVideoFlyoutOpen
        {
            get => isVideoFlyoutOpen;
            set => SetProperty(ref isVideoFlyoutOpen, value);
        }

        public bool HasClips
        {
            get => Videos.Any(video => video.Clips.Any());
        }

        #endregion

        #region Commands
        public ICommand OpenVideosFlyout { get; set; }
        public ICommand ProcessAllVideos { get; set; }
        public ICommand AddVideoCommand { get; set; }
        public ICommand AddFolderCommand { get; set; }
        public ICommand ProcessClipsCommand { get; set; }
        public ICommand ClearClipsCommand { get; set; }
        public ICommand RemoveVideoCommand { get; set; }
        #endregion
    }
}

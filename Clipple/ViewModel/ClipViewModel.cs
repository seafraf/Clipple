using Clipple.Util;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Clipple.ViewModel
{
    public enum ClipExportMode
    {
        AudioOnly,
        VideoOnly,
        Both
    }

    public enum VideoClipPreset
    {
        YT_2160_60, // 68
        YT_2160_30, // 45

        YT_1440_60, // 24
        YT_1440_30, // 16

        YT_1080_60, // 12
        YT_1080_30, // 8

        YT_720_60,  // 7.5
        YT_720_30,  // 5

        YT_480_60,  // 4 
        YT_480_30, // 2.5

        YT_360_60,  // 1.5
        YT_360_30, // 1

        NoPreset
    }

    public enum AudioClipPreset
    {
        YT_Mono,        // 128
        YT_Stereo,      // 384
        YT_5_1,         // 512
        NoPreset,
    }

    public class ClipViewModel : ObservableObject, INotifyDataErrorInfo
    {
        public ClipViewModel(double fps, int width, int height, long startFrame, long endFrame, string title, string folder)
        {
            SourceFPS = (int)Math.Round(fps);

            VideoPreset     = VideoClipPreset.NoPreset;
            
            SourceWidth     = width;
            SourceHeight    = height;
            TargetWidth     = width;
            TargetHeight    = height;
            TargetFPS       = SourceFPS;
            this.title      = title;
            this.folder     = folder;

            // End frame has to be set first so that it doesn't get clamped
            EndFrame    = endFrame;
            StartFrame  = startFrame;

            SetStartFrameCommand    = new RelayCommand(() => StartFrame = App.ViewModel.VideoPlayerViewModel.CurrentFrame);
            SetEndFrameCommand      = new RelayCommand(() => EndFrame = App.ViewModel.VideoPlayerViewModel.CurrentFrame);
            GoToStartFrameCommand   = new RelayCommand(() => App.ViewModel.VideoPlayerViewModel.SeekFrame(StartFrame));
            GoToEndFrameCommand     = new RelayCommand(() => App.ViewModel.VideoPlayerViewModel.SeekFrame(EndFrame));

            DeleteCommand = new RelayCommand(async () =>
            {
                if (await App.Window.ShowMessageAsync($"Delete {title}?", 
                    "This action cannot be undone.", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                {
                    App.ViewModel.SelectedVideo?.Clips.Remove(this);
                    App.ViewModel.NotifyClipsChanged();
                }
            });

            ProcessCommand = new RelayCommand(async () =>
            {
                var video = App.ViewModel.VideoPlayerViewModel.Video;
                if (video != null)
                    await ClipProcessor.Process(video, this);
            });
        }

        #region Properties
        private long startFrame;
        public long StartFrame
        {
            get => startFrame;
            set
            {
                value = Math.Min(EndFrame, value);

                SetProperty(ref startFrame, value);
                OnPropertyChanged(nameof(EstimatedMaxSize));
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(StartTime));
            }
        }

        private long endFrame;
        public long EndFrame
        {
            get => endFrame;
            set
            {
                value = Math.Max(StartFrame, value);

                SetProperty(ref endFrame, value);
                OnPropertyChanged(nameof(EstimatedMaxSize));
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(EndTime));
            }
        }

        public TimeSpan StartTime
        {
            get => TimeSpan.FromSeconds((double)StartFrame / SourceFPS);
            set => StartFrame = (long)Math.Round(value.TotalSeconds * SourceFPS);
        }

        public TimeSpan EndTime
        {
            get => TimeSpan.FromSeconds((double)EndFrame / SourceFPS);
            set => EndFrame = (long)Math.Round(value.TotalSeconds * SourceFPS);
        }

        private int sourceFPS;
        public int SourceFPS
        {
            get => sourceFPS;
            set
            {
                SetProperty(ref sourceFPS, value);
                OnPropertyChanged(nameof(EstimatedMaxSize));
                OnPropertyChanged(nameof(Duration));
            }
        }

        private ClipExportMode exportMode = ClipExportMode.Both;
        public ClipExportMode ExportMode
        {
            get => exportMode;
            set
            {
                SetProperty(ref exportMode, value);
                OnPropertyChanged(nameof(VideoSettingsEnabled));
                OnPropertyChanged(nameof(AudioSettingsEnabled));
                OnPropertyChanged(nameof(MergeAudio));
                OnPropertyChanged(nameof(CanCopyPackets));
                OnPropertyChanged(nameof(CopyPackets));
            }
        }

        private double videoBitrate = 100;
        public double VideoBitrate
        {
            get => videoBitrate;
            set
            {
                SetProperty(ref videoBitrate, value);
                OnPropertyChanged(nameof(EstimatedMaxSize));
            }
        }

        private int fps = 60;
        public int TargetFPS
        {
            get => fps;
            set
            {
                SetProperty(ref fps, Math.Max(1, Math.Min(value, SourceFPS)));
                OnPropertyChanged(nameof(EstimatedMaxSize));
            }
        }

        private int sourceWidth;
        public int SourceWidth
        {
            get => sourceWidth;
            set => SetProperty(ref sourceWidth, value);
        }

        private int sourceHeight;
        public int SourceHeight
        {
            get => sourceHeight;
            set => SetProperty(ref sourceHeight, value);
        }

        private int targetWidth;
        public int TargetWidth
        {
            get => targetWidth;
            set => SetProperty(ref targetWidth, value);
        }

        private int targetHeight;
        public int TargetHeight
        {
            get => targetHeight;
            set => SetProperty(ref targetHeight, value);
        }

        private string title;
        public string Title
        {
            get => title;
            set
            {
                SetProperty(ref title, value);
                ValidateTitle();
            }
        }

        private string folder;
        public string Folder
        {
            get => folder;
            set
            {
                SetProperty(ref folder, value);
                ValidateFolder();
            }
        }

        private bool mergeAudio;
        public bool MergeAudio
        {
            get => ExportMode == ClipExportMode.AudioOnly || mergeAudio;
            set => SetProperty(ref mergeAudio, value);
        }

        private bool copyPackets;
        public bool CopyPackets
        {
            get
            {
                return CanCopyPackets && copyPackets;
            }
            set
            {
                SetProperty(ref copyPackets, value);

                OnPropertyChanged(nameof(VideoSettingsEnabled));
                OnPropertyChanged(nameof(AudioSettingsEnabled));
            }
        }

        private bool showInPlayer = true;
        public bool ShowInPlayer
        {
            get => showInPlayer;
            set => SetProperty(ref showInPlayer, value);
        }

        private bool pauseAtClipStart = true;
        public bool PauseAtClipStart
        {
            get => pauseAtClipStart;
            set => SetProperty(ref pauseAtClipStart, value);
        }

        private bool pauseAtClipEnd = true;
        public bool PauseAtClipEnd
        {
            get => pauseAtClipEnd;
            set => SetProperty(ref pauseAtClipEnd, value);
        }

        private ObservableCollection<AudioSettingsModel> audioSettings = new ();
        public ObservableCollection<AudioSettingsModel> AudioSettings
        {
            get => audioSettings;
            set => SetProperty(ref audioSettings, value);
        }

        private VideoClipPreset videoPreset;

        public VideoClipPreset VideoPreset
        {
            get => videoPreset;
            set
            {
                SetProperty(ref videoPreset, value);

                switch (value)
                {
                    case VideoClipPreset.YT_2160_60:
                        VideoBitrate = 68.0;
                        TargetFPS    = Math.Min(60, SourceFPS);
                        TargetWidth  = Math.Min(3840, SourceWidth);
                        TargetHeight = Math.Min(2160, SourceHeight);
                        break;
                    case VideoClipPreset.YT_2160_30:
                        VideoBitrate = 45.0;
                        TargetFPS    = Math.Min(30, SourceFPS);
                        TargetWidth  = Math.Min(3840, SourceWidth);
                        TargetHeight = Math.Min(2160, SourceHeight);
                        break;
                    case VideoClipPreset.YT_1440_60:
                        VideoBitrate = 24.0;
                        TargetFPS    = Math.Min(60, SourceFPS);
                        TargetWidth  = Math.Min(2560, SourceWidth);
                        TargetHeight = Math.Min(1440, SourceHeight);
                        break;
                    case VideoClipPreset.YT_1440_30:
                        VideoBitrate = 16.0;
                        TargetFPS          = Math.Min(30, SourceFPS);
                        TargetWidth  = Math.Min(2560, SourceWidth);
                        TargetHeight = Math.Min(1440, SourceHeight);
                        break;
                    case VideoClipPreset.YT_1080_60:
                        VideoBitrate = 12.0;
                        TargetFPS          = Math.Min(60, SourceFPS);
                        TargetWidth  = Math.Min(1920, SourceWidth);
                        TargetHeight = Math.Min(1080, SourceHeight);
                        break;
                    case VideoClipPreset.YT_1080_30:
                        VideoBitrate = 8.0;
                        TargetFPS          = Math.Min(30, SourceFPS);
                        TargetWidth  = Math.Min(1920, SourceWidth);
                        TargetHeight = Math.Min(1080, SourceHeight);
                        break;
                    case VideoClipPreset.YT_720_60:
                        VideoBitrate = 7.5;
                        TargetFPS          = Math.Min(60, SourceFPS);
                        TargetWidth  = Math.Min(1280, SourceWidth);
                        TargetHeight = Math.Min(720, SourceHeight);
                        break;
                    case VideoClipPreset.YT_720_30:
                        VideoBitrate = 5;
                        TargetFPS          = Math.Min(30, SourceFPS);
                        TargetWidth  = Math.Min(1280, SourceWidth);
                        TargetHeight = Math.Min(720, SourceHeight);
                        break;
                    case VideoClipPreset.YT_480_60:
                        VideoBitrate = 4;
                        TargetFPS          = Math.Min(60, SourceFPS);
                        TargetWidth  = Math.Min(852, SourceWidth);
                        TargetHeight = Math.Min(480, SourceHeight);
                        break;
                    case VideoClipPreset.YT_480_30:
                        VideoBitrate = 2.5;
                        TargetFPS          = Math.Min(30, SourceFPS);
                        TargetWidth  = Math.Min(852, SourceWidth);
                        TargetHeight = Math.Min(480, SourceHeight);
                        break;
                    case VideoClipPreset.YT_360_60:
                        VideoBitrate = 1.5;
                        TargetFPS          = Math.Min(60, SourceFPS);
                        TargetWidth  = Math.Min(480, SourceWidth);
                        TargetHeight = Math.Min(360, SourceHeight);
                        break;
                    case VideoClipPreset.YT_360_30:
                        VideoBitrate = 1;
                        TargetFPS          = Math.Min(30, SourceFPS);
                        TargetWidth  = Math.Min(480, SourceWidth);
                        TargetHeight = Math.Min(360, SourceHeight);
                        break;
                    case VideoClipPreset.NoPreset:
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Enumeration of all video clip presets
        /// </summary>
        public IEnumerable<VideoClipPreset> VideoClipPresetValues
        {
            get => Enum.GetValues(typeof(VideoClipPreset)).Cast<VideoClipPreset>();
        }

        /// <summary>
        /// True when export mode is video only or both
        /// </summary>
        public bool VideoSettingsEnabled => 
            (ExportMode == ClipExportMode.VideoOnly || ExportMode == ClipExportMode.Both) && !CopyPackets;

        /// <summary>
        /// True when export mode is audio only or both
        /// </summary>
        public bool AudioSettingsEnabled => 
            (ExportMode == ClipExportMode.AudioOnly || ExportMode == ClipExportMode.Both) && !CopyPackets;

        /// <summary>
        /// Estimated, human-readable maximum size for the output of this clip.  Calculated by using maximum bitrates for
        /// video and audio
        /// </summary>
        public string EstimatedMaxSize
        {
            get
            {
                var frames = EndFrame - StartFrame;
                var seconds = frames / SourceFPS;
                var megabits = VideoBitrate * seconds;
                return Formatting.ByteCountToString((long)(megabits * 125000));
            }
        }

        /// <summary>
        /// Duration of the clip
        /// </summary>
        public TimeSpan Duration
        {
            get => TimeSpan.FromSeconds((EndFrame - StartFrame) / SourceFPS);
        }

        /// <summary>
        /// Returns a full file name for this clip, including absolute directory and extension.
        /// </summary>
        public string FullFileName
        {
            get
            {
                string fileName = Title;
                string ext = ExportMode == ClipExportMode.AudioOnly ? ".mp3" : ".mp4";
                if (!fileName.EndsWith(ext))
                    fileName += ext;

                return Path.Combine(Folder, fileName);
            }
        }

        /// <summary>
        /// Whether or not the copy packets can be used
        /// </summary>
        public bool CanCopyPackets => ExportMode != ClipExportMode.AudioOnly;
        #endregion

        #region INotifyDataErrorInfo implementation

        /// <summary>
        /// Property-keyed dictionary containing list of errors for each propety
        /// </summary>
        private Dictionary<string, List<string>> propertyErrors = new();

        /// <summary>
        /// True if propertyErrors contains any entries
        /// </summary>
        public bool HasErrors => propertyErrors.Any();

        /// <summary>
        /// Implementation
        /// </summary>
        public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

        public IEnumerable GetErrors(string? propertyName)
        {
#pragma warning disable CS8603 // Possible null reference return.
            if (propertyName == null)
                return null;
                
            return propertyErrors.ContainsKey(propertyName) ? propertyErrors[propertyName] : null;
#pragma warning restore CS8603 // Possible null reference return.
        }

        /// <summary>
        /// Add an error for a property
        /// </summary>
        /// <param name="propertyName">The property's name</param>
        /// <param name="error">The error to add</param>
        private void AddError(string propertyName, string error)
        {
            if (!propertyErrors.ContainsKey(propertyName))
                propertyErrors.Add(propertyName, new List<string>());

            if (!propertyErrors[propertyName].Contains(error))
            {
                propertyErrors[propertyName].Add(error);
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (propertyErrors.ContainsKey(propertyName) && propertyErrors[propertyName].Count > 0)
            {
                propertyErrors[propertyName].Clear();
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            }
        }
        #endregion

        #region Validators
        private void ValidateTitle()
        {
            ClearErrors(nameof(Title));

            // Check empty
            if (string.IsNullOrEmpty(Title))
            {
                AddError(nameof(Title), "Title cannot be empty");
            }

            // Check characters
            if (Title.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                AddError(nameof(Title), "Contains invalid characters");
            }

            // Check unique
            foreach (var video in App.ViewModel.Videos)
            {
                foreach (var clip in video.Clips)
                {
                    if (clip != this && clip.Title == Title)
                    {
                        AddError(nameof(Title), $"Clip name in use by {video.FileInfo.FullName}");
                        break;
                    }
                }
            }
        }

        private void ValidateFolder()
        {
            ClearErrors(nameof(Folder));

            // Check empty
            if (string.IsNullOrEmpty(Folder))
            {
                AddError(nameof(Folder), "Folder cannot be empty");
            }

            // Check characters
            if (Folder.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                AddError(nameof(Folder), "Contains invalid characters");
            }

            // Check exists
            if (!Directory.Exists(folder))
            {
                AddError(nameof(Folder), "Folder does not exist");
            }
        }
        #endregion

        #region Commands
        public ICommand SetStartFrameCommand { get; }
        public ICommand GoToStartFrameCommand { get; }
        public ICommand SetEndFrameCommand { get; }
        public ICommand GoToEndFrameCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand ProcessCommand { get; }
        #endregion
    }
}

using Clipple.Types;
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
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

namespace Clipple.ViewModel
{
    public partial class ClipViewModel : ObservableObject, INotifyDataErrorInfo, IJsonOnDeserialized, IJsonOnDeserializing
    {
#pragma warning disable CS8618
        /// <summary>
        /// Used by the deserializer
        /// </summary>
        public ClipViewModel()
#pragma warning restore CS8618
        {
            InitialiseVideoViews();

            // Events
            AudioSettings.CollectionChanged += OnAudioSettingsChanged;
            PropertyChanged += OnPropertyChanged;

            SetStartTimeCommand     = new RelayCommand(() => StartTicks = App.MediaPlayer.CurTime);
            SetEndTimeCommand       = new RelayCommand(() => EndTicks = App.MediaPlayer.CurTime);
            GoToStartTimeCommand    = new RelayCommand(() => App.ViewModel.VideoPlayerViewModel.SeekTicks(StartTicks));
            GoToEndTimeCommand      = new RelayCommand(() => App.ViewModel.VideoPlayerViewModel.SeekTicks(EndTicks));

            DeleteCommand = new RelayCommand(async () =>
            {
                var oldVisiblity = App.VideoPlayerVisible;
                App.VideoPlayerVisible = false;

                if (await App.Window.ShowMessageAsync($"Delete {title}?",
                    "This action cannot be undone.", MessageDialogStyle.AffirmativeAndNegative) == MessageDialogResult.Affirmative)
                {
                    App.ViewModel.SelectedVideo?.Clips.Remove(this);
                }

                App.VideoPlayerVisible = oldVisiblity;
            });

            ProcessCommand = new RelayCommand(async () =>
            {
                var video = App.ViewModel.VideoPlayerViewModel.Video;
                if (video != null)
                    await ClipProcessor.Process(video, this);
            });

            SupportedOutputFormats.GroupDescriptions.Clear();
            SupportedOutputFormats.GroupDescriptions.Add(new PropertyGroupDescription("Category"));

            TranscodingPresets.GroupDescriptions.Clear();
            TranscodingPresets.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
        }

        public ClipViewModel(long startTicks, long endTicks, string title, string folder) :
            this()
        {
            this.title      = title;
            this.folder     = folder;

            // End time has to be set first so that it doesn't get clamped
            EndTicks    = endTicks;
            StartTicks  = startTicks;

            // Select default output format
            OutputFormatIndex = 6; // mp4
        }

        #region Members
        private bool isDeserializing = false;
        #endregion

        #region Properties
        /// <summary>
        /// Reference to the parent view model
        /// </summary>
        private VideoViewModel? parent;
        [JsonIgnore]
        public VideoViewModel? Parent
        {
            get => parent;
            set
            {
                SetProperty(ref parent, value);

                OnPropertyChanged(nameof(SourceWidth));
                OnPropertyChanged(nameof(SourceHeight));
                OnPropertyChanged(nameof(SourceFPS));

                // Set defaults
                if (value != null)
                {
                    TargetWidth     = value.VideoWidth;
                    TargetHeight    = value.VideoHeight;
                    TargetFPS       = value.VideoFPS;
                }
            }
        }

        /// <summary>
        /// Clip start time in ticks (native format for the video player)
        /// </summary>
        private long startTicks;
        public long StartTicks
        {
            get => Math.Min(endTicks, startTicks);
            set
            {
                SetProperty(ref startTicks, value);
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(StartTime));

                if (UseTargetSize)
                    OnPropertyChanged(nameof(VideoBitrate));
            }
        }

        /// <summary>
        /// Clip end time in ticks (native format for the video player)
        /// </summary>
        private long endTicks;
        public long EndTicks
        {
            get => Math.Max(startTicks, endTicks);
            set
            {
                SetProperty(ref endTicks, value);
                OnPropertyChanged(nameof(Duration));
                OnPropertyChanged(nameof(EndTime));

                if (UseTargetSize)
                    OnPropertyChanged(nameof(VideoBitrate));
            }
        }

        /// <summary>
        /// Clip start time
        /// </summary>
        [JsonIgnore]
        public TimeSpan StartTime
        {
            get => TimeSpan.FromTicks(StartTicks);
            set => StartTicks = value.Ticks;
        }

        /// <summary>
        /// Clip end time
        /// </summary>
        [JsonIgnore]
        public TimeSpan EndTime
        {
            get => TimeSpan.FromTicks(EndTicks);
            set => EndTicks = value.Ticks;
        }

        /// <summary>
        /// Duration of the clip
        /// </summary>
        [JsonIgnore]
        public TimeSpan Duration
        {
            get => TimeSpan.FromTicks(EndTicks - StartTicks);
        }

        /// <summary>
        /// Input video FPS (rounded)
        /// </summary>
        [JsonIgnore]
        public int SourceFPS
        {
            get => Parent?.VideoFPS ?? -1;
        }

        /// <summary>
        /// Input video width in pixels
        /// </summary>
        [JsonIgnore]
        public int SourceWidth
        {
            get => Parent?.VideoWidth ?? -1;
        }

        /// <summary>
        /// Input video height in pixels
        /// </summary>
        [JsonIgnore]
        public int SourceHeight
        {
            get => Parent?.VideoHeight ?? -1;
        }

        /// <summary>
        /// Clip title and output name
        /// </summary>
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

        /// <summary>
        /// Clip output folder
        /// </summary>
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

        /// <summary>
        /// Whether or not this clip should show in the player, note if this is not turned on, neither PauseAtClipStart 
        /// nor PauseAtClipEnd will be activated
        /// </summary>
        private bool showInPlayer = true;
        public bool ShowInPlayer
        {
            get => showInPlayer;
            set => SetProperty(ref showInPlayer, value);
        }

        /// <summary>
        /// Whether or not the player should pause when it hits the start of this clip
        /// </summary>
        private bool pauseAtClipStart = true;
        public bool PauseAtClipStart
        {
            get => pauseAtClipStart;
            set => SetProperty(ref pauseAtClipStart, value);
        }

        /// <summary>
        /// Whether or not the player should pause when it hits the end of this clip
        /// </summary>
        private bool pauseAtClipEnd = true;
        public bool PauseAtClipEnd
        {
            get => pauseAtClipEnd;
            set => SetProperty(ref pauseAtClipEnd, value);
        }

        /// <summary>
        /// Currently selected output format
        /// </summary>
        private OutputFormat outputFormat;
        [JsonIgnore]
        public OutputFormat OutputFormat
        {
            get => outputFormat;
            set
            {
                SetProperty(ref outputFormat, value);

                // Merge audio is forced on for audio only formats
                OnPropertyChanged(nameof(MergeAudio));
            }
        }

        /// <summary>
        /// Index of , for serialization
        /// </summary>
        private int outputFormatIndex = -1;
        public int OutputFormatIndex
        {
            get => outputFormatIndex;
            set => SetProperty(ref outputFormatIndex, value);
        }

        /// <summary>
        /// List of supported output formats, ffmpeg supports many more but these are the most popular + tested to work
        /// </summary>
        [JsonIgnore]
        public ListCollectionView SupportedOutputFormats { get; } = new(new ObservableCollection<OutputFormat>()
        {
            // Audio only
            new OutputFormat("wav", ".wav", "Waveform Audio", true, false),
            new OutputFormat("adts", ".aac", "Advanced Audio Coding", true, false),
            new OutputFormat("mp3", ".mp3", "MPEG audio layer 3", true, false),

            // Video and audio
            new OutputFormat("webm", ".webm", "WebM"),
            new OutputFormat("avi", ".avi", "Audio Video Interleaved"),
            new OutputFormat("mov", ".mov", "QuickTime"),
            new OutputFormat("mp4", ".mp4", "MPEG-4 Part 14"),
            new OutputFormat("matroska", ".mkv", "Matroska"),
        });

        /// <summary>
        /// Returns a full file name for this clip, including absolute directory and extension.
        /// </summary>
        [JsonIgnore]
        public string FullFileName
        {
            get
            {
                string fileName = Title;
                string ext = outputFormat.Extension;
                if (!fileName.EndsWith(ext))
                    fileName += ext;

                return Path.Combine(Folder, fileName);
            }
        }

        /// <summary>
        /// Current transcoding preset
        /// </summary>
        private TranscodingPreset? transcodingPreset;
        [JsonIgnore]
        public TranscodingPreset? TranscodingPreset
        {
            get => transcodingPreset;
            set
            {
                if (value != null)
                {
                    VideoBitrate     = value.VideoBitrate ?? VideoBitrate;
                    TargetWidth      = value.TargetWidth ?? TargetWidth;
                    TargetHeight     = value.TargetHeight ?? TargetHeight;
                    TargetFPS        = value.FPS ?? TargetFPS;
                    VideoCodec       = value.VideoCodec ?? VideoCodec;
                    AudioBitrate     = value.AudioBitrate ?? AudioBitrate;
                    UseTargetSize    = value.UseTargetSize ?? UseTargetSize;
                    OutputTargetSize = value.TargetSize ?? OutputTargetSize;
                }

                SetProperty(ref transcodingPreset, value);
            }
        }

        /// <summary>
        /// Index of transcoding preset, for serialization
        /// </summary>
        private int transcodingPresetIndex = -1;
        public int TranscodingPresetIndex
        {
            get => transcodingPresetIndex;
            set => SetProperty(ref transcodingPresetIndex, value);
        }

        /// <summary>
        /// List of transcoding presets
        /// </summary>
        [JsonIgnore]
        public ListCollectionView TranscodingPresets { get; } = new(new ObservableCollection<TranscodingPreset>()
        {
            new TranscodingPreset("10MB (no nitro)", "Discord", useTargetSize: true, targetSize: 10, videoCodec: "libx264"),
            new TranscodingPreset("50MB (nitro classic)", "Discord", useTargetSize: true, targetSize: 50, videoCodec: "libx264"),
            new TranscodingPreset("100MB (nitro)", "Discord", useTargetSize: true, targetSize: 100, videoCodec: "libx264"),

            new TranscodingPreset("10MB (no nitro)", "Discord, VP9 (Slow)", useTargetSize: true, targetSize: 10, videoCodec: "libvpx-vp9"),
            new TranscodingPreset("50MB (nitro classic)", "Discord, VP9 (Slow)", useTargetSize: true, targetSize: 50, videoCodec: "libvpx-vp9"),
            new TranscodingPreset("100MB (nitro)", "Discord, VP9 (Slow)", useTargetSize: true, targetSize: 100, videoCodec: "libvpx-vp9"),

            new TranscodingPreset("2160p@60", "YouTube SDR recommendations", videoBitrate: 680000, targetWidth: 3840, targetHeight: 2160, fps: 60, videoCodec: "libx264"),
            new TranscodingPreset("2160p@30", "YouTube SDR recommendations", videoBitrate: 450000, targetWidth: 3840, targetHeight: 2160, fps: 30, videoCodec: "libx264"),
            new TranscodingPreset("1440p@60", "YouTube SDR recommendations", videoBitrate: 240000, targetWidth: 2560, targetHeight: 1440, fps: 60, videoCodec: "libx264"),
            new TranscodingPreset("1440p@30", "YouTube SDR recommendations", videoBitrate: 160000, targetWidth: 2560, targetHeight: 1440, fps: 30, videoCodec: "libx264"),
            new TranscodingPreset("1080p@60", "YouTube SDR recommendations", videoBitrate: 120000, targetWidth: 1920, targetHeight: 1080, fps: 60, videoCodec: "libx264"),
            new TranscodingPreset("1080p@30", "YouTube SDR recommendations", videoBitrate: 80000, targetWidth: 1920, targetHeight: 1080, fps: 30, videoCodec: "libx264"),
            new TranscodingPreset("720p@60", "YouTube SDR recommendations", videoBitrate: 75000, targetWidth: 1280, targetHeight: 720, fps: 60, videoCodec: "libx264"),
            new TranscodingPreset("720p@30", "YouTube SDR recommendations", videoBitrate: 50000, targetWidth: 1280, targetHeight: 720, fps: 30, videoCodec: "libx264"),
            new TranscodingPreset("480p@60", "YouTube SDR recommendations", videoBitrate: 40000, targetWidth: 852, targetHeight: 480, fps: 60, videoCodec: "libx264"),
            new TranscodingPreset("480p@30", "YouTube SDR recommendations", videoBitrate: 25000, targetWidth: 852, targetHeight: 480, fps: 30, videoCodec: "libx264"),
            new TranscodingPreset("360p@60", "YouTube SDR recommendations", videoBitrate: 15000, targetWidth: 480, targetHeight: 360, fps: 60, videoCodec: "libx264"),
            new TranscodingPreset("360p@30", "YouTube SDR recommendations", videoBitrate: 10000, targetWidth: 480, targetHeight: 360, fps: 30, videoCodec: "libx264"),
        });
        #endregion

        #region INotifyDataErrorInfo implementation

        /// <summary>
        /// Property-keyed dictionary containing list of errors for each propety
        /// </summary>
        private Dictionary<string, List<string>> propertyErrors = new();

        /// <summary>
        /// True if propertyErrors contains any entries
        /// </summary>
        [JsonIgnore]
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

                OnPropertyChanged(nameof(HasErrors));
            }
        }

        private void ClearErrors(string propertyName)
        {
            if (propertyErrors.ContainsKey(propertyName) && propertyErrors[propertyName].Count > 0)
            {
                propertyErrors[propertyName].Clear();
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

                OnPropertyChanged(nameof(HasErrors));
            }
        }
        #endregion

        #region Validators
        private void ValidateTitle()
        {
            // Don't run validators when deserializing
            if (isDeserializing)
                return;

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
            // Don't run validators when deserializing
            if (isDeserializing)
                return;

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

        public void OnDeserialized()
        {
            isDeserializing = false;
        }

        public void OnDeserializing()
        {
            isDeserializing = true;
        }
        #endregion

        #region Commands
        [JsonIgnore]
        public ICommand SetStartTimeCommand { get; }

        [JsonIgnore]
        public ICommand GoToStartTimeCommand { get; }

        [JsonIgnore]
        public ICommand SetEndTimeCommand { get; }

        [JsonIgnore]
        public ICommand GoToEndTimeCommand { get; }

        [JsonIgnore]
        public ICommand DeleteCommand { get; }

        [JsonIgnore]
        public ICommand ProcessCommand { get; }
        #endregion

        #region Event handlers
        private void OnAudioSettingsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems == null)
                return;

            foreach (var audioSetting in e.NewItems)
            {
                ((AudioSettingsModel)audioSetting).PropertyChanged -= OnAudioSettingPropertyChanged;
                ((AudioSettingsModel)audioSetting).PropertyChanged += OnAudioSettingPropertyChanged;
            }
        }

        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // If any of the properties set by transcoding presets change, unselect the transcoding preset
            // to indicate the current settings don't reflect the previously selected preset any more
            if (e.PropertyName == "VideoBitrate" || e.PropertyName == "TargetWidth"
                || e.PropertyName == "TargetHeight" || e.PropertyName == "TargetFPS"
                || e.PropertyName == "VideoCodec" || e.PropertyName == "AudioBitrate"
                || e.PropertyName == "AudioBitrate" || e.PropertyName == "UseTargetSize" ||
                e.PropertyName == "OutputTargetSize")
            {
                TranscodingPreset = null;
            }
        }
        #endregion
    }
}

using Clipple.DataModel;
using Clipple.Types;
using Clipple.Util;
using Clipple.Util.ISOBMFF;
using ControlzEx.Theming;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Windows.Input;
using System.Windows.Media;

namespace Clipple.ViewModel
{
    public partial class VideoViewModel : ObservableObject, IJsonOnDeserialized
    {

        /// <summary>
        /// This constructor only exists for the deserializer to call it, the OnDeserialized method will initiate 
        /// all of the missing properties and fields
        /// </summary>
#pragma warning disable CS8618
        public VideoViewModel()
#pragma warning restore CS8618

        {
            Clips.CollectionChanged += (s, e) => App.ViewModel.NotifyClipsChanged();

            RemoveClipsCommand = new RelayCommand(() => Clips.Clear());
            RemoveVideoCommand = new RelayCommand(() => App.ViewModel.Videos.Remove(this));
            ProcessVideoCommand = new RelayCommand(async () => await ClipProcessor.Process(this));
        }

        public VideoViewModel(string filePath) : this()
        {
            FilePath = filePath;
            InitializeFromFilePath();
        }

        #region Methods
        private void InitializeFromFilePath()
        {
            fileInfo = new FileInfo(FilePath);

            try
            {
                // mp4 only
                var parser = new SimpleParser(FilePath);
                parser.Parse();

                trackNames = parser.Tracks.Select(x => x.Name).ToArray();
            }
            catch (Exception)
            {
                // Temp fix for other container formats..
                trackNames = Array.Empty<string>();
            }

            // Load properties that are sourced by parsing the video with ffmpeg (libav*)
            InitialiseFFMPEG();
        }

        public void OnDeserialized()
        {
            InitializeFromFilePath();
        }
        #endregion

        #region Properties
        /// <summary>
        /// A reference to the video's file info. This can be used to determine the path, file size, etc
        /// </summary>
        private FileInfo fileInfo;
        [JsonIgnore]
        public FileInfo FileInfo
        {
            get => fileInfo;
            set
            {
                SetProperty(ref fileInfo, value);
                OnPropertyChanged(nameof(FileSize));
            }
        }

        /// <summary>
        /// A list of clips this video owns
        /// </summary>
        private ObservableCollection<ClipViewModel> clips = new();
        public ObservableCollection<ClipViewModel> Clips
        {
            get => clips;
            set => SetProperty(ref clips, value);
        }

        /// <summary>
        /// Video width in pixels
        /// </summary>
        private int videoWidth = -1;
        public int VideoWidth
        {
            get => videoWidth;
            set => SetProperty(ref videoWidth, value);
        }

        /// <summary>
        /// Video height in pixels
        /// </summary>
        private int videoHeight = -1;
        public int VideoHeight
        {
            get => videoHeight;
            set => SetProperty(ref videoHeight, value);
        }

        /// <summary>
        /// Rounded video FPS
        /// </summary>
        private int videoFPS = -1;
        public int VideoFPS
        {
            get => videoFPS;
            set => SetProperty(ref videoFPS, value);
        }

        /// <summary>
        /// Video duration
        /// </summary>
        private TimeSpan videoDuration;
        public TimeSpan VideoDuration
        {
            get => videoDuration;
            set => SetProperty(ref videoDuration, value);
        }

        /// <summary>
        /// Names of audio tracks if provided.  Currently only supported by MP4 containers
        /// </summary>
        private string?[] trackNames;
        [JsonIgnore]
        public string?[] TrackNames
        {
            get => trackNames;
            set => SetProperty(ref trackNames, value);
        }

        /// <summary>
        /// Various video player state properties.  These are set and read by the player when playing and loading videos.  These are serialized to disk 
        /// so that player state is persistent across sessions.
        /// </summary>
        private VideoState videoState = new();
        public VideoState VideoState
        {
            get => videoState;
            set => SetProperty(ref videoState, value);
        }

        /// <summary>
        /// Helper property.  Set by the root view model when this video is selected.
        /// </summary>
        private bool isSelected = false;
        [JsonIgnore]
        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        /// <summary>
        /// Selected post processing action.  This only exists for serialization
        /// </summary>
        private int postProcessingActionIndex = 0;
        public int PostProcessingActionIndex
        {
            get => postProcessingActionIndex;
            set => SetProperty(ref postProcessingActionIndex, value);
        }

        /// <summary>
        /// Action performed when the video has finished processing all of it's clips.
        /// </summary>
        private IVideoPostProcessingAction postProcessingAction;
        [JsonIgnore]
        public IVideoPostProcessingAction PostProcessingAction
        {
            get => postProcessingAction;
            set => SetProperty(ref postProcessingAction, value);
        }

        /// <summary>
        /// List of possible post processing actions
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<IVideoPostProcessingAction> PostProcessingActionList { get; } = new()
        {
            new NoVideoPostProcessingAction(),
            new RemoveVideoPostProcessingAction(),
            new DeleteVideoPostProcessingAction()
        };

        /// <summary>
        /// Helper property.  Returns video file size in a human readable format.
        /// </summary>
        [JsonIgnore]
        public string FileSize => Formatting.ByteCountToString(FileInfo.Length);

        /// <summary>
        /// Helper property. Returns the video's folder URI
        /// </summary>
        [JsonIgnore]
        public Uri? FolderURI => FileInfo?.DirectoryName == null ? null : new Uri(FileInfo.DirectoryName);

        /// <summary>
        /// File path, used for serialization.
        /// </summary>
        public string FilePath { get; set; }
        #endregion

        #region Commands
        [JsonIgnore]
        public ICommand RemoveClipsCommand { get; set; }

        [JsonIgnore]
        public ICommand RemoveVideoCommand { get; set; }

        [JsonIgnore]
        public ICommand ProcessVideoCommand { get; set; }
        #endregion
    }
}

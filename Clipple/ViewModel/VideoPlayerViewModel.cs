using MahApps.Metro.IconPacks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Clipple.ViewModel
{
    public enum VideoState
    {
        /// <summary>
        /// FFmpeg is loading the video, no frames available yet
        /// </summary>
        Loading,

        /// <summary>
        /// The video is ready to be played
        /// </summary>
        Ready,

        /// <summary>
        /// FFmpeg failed to load the video
        /// </summary>
        Failed
    }

    public class MediaTrack
    {
        public string Name { get; private set; }

        public int Index { get; private set; }

        public MediaTrack(string name, int index)
        {
            Name = name;
            Index = index;
        }

        public override string? ToString()
        {
            return Name;
        }
    }

    [Serializable]
    public class VideoPlayerViewModel : ObservableObject
    {
        public VideoPlayerViewModel()
        {
            AddClipCommand = new RelayCommand(CreateClip);
        }

        /// <summary>
        /// Updates all properties that are related to the time or duration of the video
        /// </summary>
        private void UpdateTimeProperties()
        {
            OnPropertyChanged(nameof(VideoProgressPercent));
            OnPropertyChanged(nameof(RemainingTime));
            OnPropertyChanged(nameof(IsFinished));
            OnPropertyChanged(nameof(ControlButtonIcon));
            OnPropertyChanged(nameof(HasContent));
            OnPropertyChanged(nameof(CurrentFrame));
            OnPropertyChanged(nameof(FrameCount));
        }

        /// <summary>
        /// Updates all properties that are related to the video state
        /// </summary>
        private void UpdateStateProperties()
        {
            OnPropertyChanged(nameof(IsReady));
            OnPropertyChanged(nameof(IsFailed));
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(HasContent));
        }

        #region Properties
        private bool isPlaying = false;
        public bool IsPlaying
        {
            get => isPlaying;
            set
            {
                SetProperty(ref isPlaying, value);
                OnPropertyChanged(nameof(ControlButtonIcon));
            }
        }

        private TimeSpan videoPosition = TimeSpan.Zero;
        public TimeSpan VideoPosition
        {
            get => videoPosition;
            set
            {
                SetProperty(ref videoPosition, value);
                UpdateTimeProperties();
            }
        }

        private TimeSpan videoDuration = TimeSpan.Zero;
        public TimeSpan VideoDuration
        {
            get => videoDuration;
            set
            {
                SetProperty(ref videoDuration, value);
                UpdateTimeProperties();
            }
        }

        public double VideoProgressPercent
        {
            get
            {
                if (VideoPosition == TimeSpan.Zero || VideoDuration == TimeSpan.Zero)
                    return 0.0;

                return (VideoPosition / VideoDuration) * 100;
            }
            set => VideoPosition = VideoDuration * Math.Clamp(value / 100, 0, 100);
        }

        private int videoFPS;
        public int VideoFPS
        {
            get => videoFPS;
            set
            {
                SetProperty(ref videoFPS, value);
            }
        }

        private int videoWidth;
        public int VideoWidth
        {
            get => videoWidth;
            set => SetProperty(ref videoWidth, value);
        }

        private int videoHeight;
        public int VideoHeight
        {
            get => videoHeight;
            set => SetProperty(ref videoHeight, value);
        }

        private VideoState videoState;
        public VideoState VideoState
        {
            get => videoState;
            set
            {
                SetProperty(ref videoState, value);
                UpdateStateProperties();
            }
        }

        private bool isMuted = false;
        public bool IsMuted
        {
            get => isMuted;
            set => SetProperty(ref isMuted, value);
        }

        private double volume = 1.0;
        public double Volume
        {
            get => volume;
            set => SetProperty(ref volume, value);
        }

        private MediaTrack[] audioTracks = Array.Empty<MediaTrack>();
        public MediaTrack[] AudioTracks
        {
            get => audioTracks;
            set => SetProperty(ref audioTracks, value);
        }

        private MediaTrack[] videoTracks = Array.Empty<MediaTrack>();
        public MediaTrack[] VideoTracks
        {
            get => videoTracks;
            set => SetProperty(ref videoTracks, value);
        }

        private int selectedAudioTrackIndex = -1;
        public int SelectedAudioTrackIndex
        {
            get => selectedAudioTrackIndex;
            set
            {
                SetProperty(ref selectedAudioTrackIndex, value);

                if (SelectedVideoTrack is MediaTrack videoTrack && SelectedAudioTrack is MediaTrack audioTrack)
                    AppCommands.ChangeMedia.Execute(new MediaChangeArgs(audioTrack.Index, videoTrack.Index));
            }
        }

        private int selectedVideoTrackIndex = -1;
        public int SelectedVideoTrackIndex
        {
            get => selectedVideoTrackIndex;
            set
            {
                SetProperty(ref selectedVideoTrackIndex, value);

                if (SelectedVideoTrack is MediaTrack videoTrack && SelectedAudioTrack is MediaTrack audioTrack)
                    AppCommands.ChangeMedia.Execute(new MediaChangeArgs(audioTrack.Index, videoTrack.Index));
            }
        }

        private VideoViewModel? video;
        public VideoViewModel? Video
        {
            get => video;
            set
            {
                SetProperty(ref video, value);

                OnPropertyChanged(nameof(IsReady));
                OnPropertyChanged(nameof(IsFailed));
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(IsWaitingVideo));
            }
        }

        public MediaTrack? SelectedVideoTrack => VideoTracks.ElementAtOrDefault(SelectedVideoTrackIndex);
        public MediaTrack? SelectedAudioTrack => AudioTracks.ElementAtOrDefault(SelectedAudioTrackIndex);

        /// <summary>
        /// Whether or not the video state is ready
        /// </summary>
        public bool IsReady => VideoState == VideoState.Ready && Video != null;

        /// <summary>
        /// Whether or not the video state is failed
        /// </summary>
        public bool IsFailed => VideoState == VideoState.Failed && Video != null;

        /// <summary>
        /// Whether or not the video state is loading
        /// </summary>
        public bool IsLoading => VideoState == VideoState.Loading && Video != null;

        /// <summary>
        /// True if the video player is waiting for a video to be selected
        /// </summary>
        public bool IsWaitingVideo => Video == null;

        /// <summary>
        /// True if the video state is ready and the video has not finished
        /// </summary>
        public bool HasContent => VideoState == VideoState.Ready && !IsFinished;

        /// <summary>
        /// The amount of time remaining in the video clip
        /// </summary>
        public TimeSpan RemainingTime => VideoDuration - VideoPosition;

        /// <summary>
        /// True when the remaining time is less than the length of one frame
        /// </summary>
        public bool IsFinished
        {
            get
            {
                if (VideoFPS == 0)
                    return false;

                return RemainingTime <= TimeSpan.FromSeconds(1.0 / VideoFPS);
            }
        }

        public long CurrentFrame => (long)Math.Round(VideoPosition.TotalSeconds * VideoFPS);
        public long FrameCount => (long)Math.Round(VideoDuration.TotalSeconds * VideoFPS);

        /// <summary>
        /// The icon for the control button, either a play pause or 
        /// </summary>
        public PackIconMaterialDesignKind ControlButtonIcon
        {
            get
            {
                if (IsFinished)
                    return PackIconMaterialDesignKind.RotateLeft;

                return App.MediaElement.IsPlaying ? PackIconMaterialDesignKind.Pause : PackIconMaterialDesignKind.PlayArrow;
            }
        }
        #endregion

        #region Commands
        public ICommand AddClipCommand { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Tries to seek a specific frame in the video
        /// </summary>
        /// <param name="frame">The frame to seek to</param>
        public void SeekFrame(long frame)
        {
            if (frame == 0 || frame > FrameCount)
                return;

            VideoPosition = TimeSpan.FromSeconds((double)frame / VideoFPS);
        }

        /// <summary>
        /// Creates a new clip at the playhead
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        internal void CreateClip()
        {
            if (Video == null)
                return;

            // Set a default clip length of 10 seconds
            var numFrames = (long)(10 * VideoFPS);
            var newClip = new ClipViewModel(VideoFPS, VideoWidth, VideoHeight, CurrentFrame, Math.Min(CurrentFrame + numFrames, FrameCount),
                $"Untitled {Video.Clips.Count + 1}", App.ViewModel.SettingsViewModel.DefaultOutputFolder);

            foreach (var track in AudioTracks)
                newClip.AudioSettings.Add(new AudioSettingsModel(track.Index, Video.TrackNames.ElementAtOrDefault(track.Index) ?? $"Audio track {track.Index}"));

            Video.Clips.Add(newClip);
        }

        /// <summary>
        /// NextClipEdge
        /// </summary>
        internal void NextClipEdge()
        {
            var edges = GetClipEdges();
            edges.Sort();

            foreach (var edge in edges)
            {
                if (edge > CurrentFrame)
                {
                    SeekFrame(edge);
                    return;
                }
            }
        }

        /// <summary>
        /// PreviosClipEdge
        /// </summary>
        internal void PreviosClipEdge()
        {
            var edges = GetClipEdges();
            edges.Sort();

            for (int i = edges.Count - 1; i >= 0; i--)
            {
                var edge = edges[i];
                if (edge < CurrentFrame)
                {
                    SeekFrame(edge);
                    return;
                }
            }
        }

        /// <summary>
        /// Generates a list of start and end frames of clips
        /// </summary>
        /// <returns>Said list</returns>
        private List<long> GetClipEdges()
        {
            var edges = new List<long>();
            if (Video == null)
                return edges;

            foreach (var clip in Video.Clips)
            {
                edges.Add(clip.StartFrame);
                edges.Add(clip.EndFrame);
            }

            return edges;
        }
        #endregion
    }
}

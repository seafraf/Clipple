using Clipple.DataModel;
using Clipple.Types;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using MahApps.Metro.IconPacks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.VisualBasic.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Clipple.ViewModel
{
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
            ZoomIn = new RelayCommand(() => Zoom = Math.Clamp(Zoom + 0.05, 0.0, 1.0));
            ZoomOut = new RelayCommand(() => Zoom = Math.Clamp(Zoom - 0.05, 0.0, 1.0));
            AddClipCommand = new RelayCommand(CreateClip);
            OpenAudioSettings = new RelayCommand(() =>
            {
                IsAudioSettingsOpen = !IsAudioSettingsOpen;
            });

            var Config = new Config();
            Config.Player.SeekAccurate = true;
            Config.Player.AutoPlay = false;
            Config.Player.MouseBindings.Enabled = false;
            Config.Player.KeyBindings.Enabled = false;
            Config.Audio.Enabled = false;

            MediaPlayer = new Player(Config);
            MediaPlayer.OpenCompleted   += OnMediaOpened;
            MediaPlayer.PropertyChanged += OnActivityChanged;
        }

        private void OnActivityChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Status")
                UpdateStatusProperties();

            if (e.PropertyName == "CurTime")
                UpdateTimeProperties();
        }

        private void OnMediaOpened(object? sender, OpenCompletedArgs e)
        {
            if (Video == null)
                return;

            OnPropertyChanged(nameof(VideoDuration));

            VideoFPS    = (int)Math.Round(MediaPlayer.Video.FPS);
            VideoWidth  = MediaPlayer.Video.Width;
            VideoHeight = MediaPlayer.Video.Height;

            var audioStreams = MediaPlayer.MainDemuxer.AudioStreams;
            var players      = new BackgroundAudioPlayer[audioStreams.Count];
            var videoState   = Video.VideoState;

            // Create a background audio player for each audio stream
            for (int i = 0; i < audioStreams.Count; i++)
            {
                players[i] = new BackgroundAudioPlayer(videoState, audioStreams[i].StreamIndex, Video.FileInfo.FullName,
                    GetAudioTrackName(audioStreams[i].StreamIndex))
                {
                    BaseMuted = isMuted,
                    BaseVolume = volume
                };
            }

            // Destroy old background audio players, only useful when swapping videos
            foreach (var audioPlayer in AudioPlayers)
                audioPlayer.Dispose();

            AudioPlayers = players;

            // Show the first frame of the video
            MediaPlayer.ShowFrame(0);

            // Load settings from previous video state
            Volume          = videoState.Volume;
            IsMuted         = videoState.Muted;
            PlaybackSpeed   = videoState.PlaybackSpeed;

            MediaPlayer.CurTime = (long)(videoState.CurTime.TotalMilliseconds * TimeSpan.TicksPerMillisecond);
        }

        /// <summary>
        /// Updates all properties that are related to the time or duration of the video
        /// </summary>
        private void UpdateTimeProperties()
        {
            OnPropertyChanged(nameof(VideoCurrentTime));
            OnPropertyChanged(nameof(RemainingTime));
            OnPropertyChanged(nameof(CurTime));

            if (Video != null)
                Video.VideoState.CurTime = VideoCurrentTime;
        }

        /// <summary>
        /// Updates all properties that are related to the media player's status
        /// </summary>
        private void UpdateStatusProperties()
        {
            OnPropertyChanged(nameof(IsReady));
            OnPropertyChanged(nameof(IsFailed));
            OnPropertyChanged(nameof(IsLoading));
            OnPropertyChanged(nameof(HasContent));
            OnPropertyChanged(nameof(ControlButtonIcon));
        }

        #region Properties
        /// <summary>
        /// Current video time in Flyleaf format
        /// </summary>
        public long CurTime
        {
            get => MediaPlayer.CurTime;
            set => MediaPlayer.CurTime = value;
        }

        /// <summary>
        /// Current video time in TimeSpan format
        /// </summary>
        public TimeSpan VideoCurrentTime
        {
            get => TimeSpan.FromTicks(MediaPlayer.CurTime);
            set => MediaPlayer.CurTime = (long)(value.TotalMilliseconds * TimeSpan.TicksPerMillisecond);
        }

        /// <summary>
        /// Duration of the currently loaded video
        /// </summary>
        public TimeSpan VideoDuration
        {
            get => TimeSpan.FromTicks(MediaPlayer.Duration);
        }

        /// <summary>
        /// Current video's FPS
        /// </summary>
        private int videoFPS;
        public int VideoFPS
        {
            get => videoFPS;
            set
            {
                SetProperty(ref videoFPS, value);
            }
        }

        /// <summary>
        /// Current video's width in pixels
        /// </summary>
        private int videoWidth;
        public int VideoWidth
        {
            get => videoWidth;
            set => SetProperty(ref videoWidth, value);
        }

        /// <summary>
        /// Current video's height in pixels
        /// </summary>
        private int videoHeight;
        public int VideoHeight
        {
            get => videoHeight;
            set => SetProperty(ref videoHeight, value);
        }

        /// <summary>
        /// Volume, between 0 and 100.  Setting this will change the volumes of the background audio players
        /// </summary>
        private double volume = 100.0;
        public double Volume
        {
            get => volume;
            set
            {
                SetProperty(ref volume, value);
                foreach (var audioPlayer in AudioPlayers)
                    audioPlayer.BaseVolume = value;

                if (Video != null)
                    Video.VideoState.Volume = value;
            }
        }

        /// <summary>
        /// Whether or not the video is muted.  Setting this will change the muted state of the background audio players
        /// </summary>
        private bool isMuted = false;
        public bool IsMuted
        {
            get => isMuted;
            set
            {
                SetProperty(ref isMuted, value);
                foreach (var audioPlayer in AudioPlayers)
                    audioPlayer.BaseMuted = value;

                if (Video != null)
                    Video.VideoState.Muted = value;
            }
        }

        /// <summary>
        /// Video and audio playback speed.
        /// </summary>
        private double playbackSpeed = 1.0;
        public double PlaybackSpeed
        {
            get => playbackSpeed;
            set
            {
                SetProperty(ref playbackSpeed, value);

                MediaPlayer.Speed = value;
                foreach (var audioPlayer in AudioPlayers)
                    audioPlayer.PlaybackSpeed = value;

                if (Video != null)
                    Video.VideoState.PlaybackSpeed = value;
            }
        }

        /// <summary>
        /// Audio players for each audio stream in the video.  These are played asynchronously 
        /// </summary>
        private BackgroundAudioPlayer[] audioPlayers = Array.Empty<BackgroundAudioPlayer>();
        public BackgroundAudioPlayer[] AudioPlayers
        {
            get => audioPlayers;
            set => SetProperty(ref audioPlayers, value);
        }

        /// <summary>
        /// The currently loaded video
        /// </summary>
        private VideoViewModel? video;
        public VideoViewModel? Video
        {
            get => video;
            set
            {
                // If the view model has been told to select no video, free any streams we may have had opened
                if (video != null && value == null)
                    Stop();

                SetProperty(ref video, value);
                OnPropertyChanged(nameof(IsReady));
                OnPropertyChanged(nameof(IsFailed));
                OnPropertyChanged(nameof(IsLoading));
                OnPropertyChanged(nameof(IsWaitingVideo));

                if (value != null && MediaPlayer.Control != null)
                    MediaPlayer.OpenAsync(value.FileInfo.FullName, true, true, false, false);
            }
        }

        /// <summary>
        /// Whether or not the controls for individual audio track settings is open
        /// </summary>
        private bool isAudioSettingsOpen = false;
        public bool IsAudioSettingsOpen
        {
            get => isAudioSettingsOpen;
            set => SetProperty(ref isAudioSettingsOpen, value);
        }

        /// <summary>
        /// Whether or not the video state is ready
        /// </summary>
        public bool IsReady => MediaPlayer.Status != Status.Failed &&
            MediaPlayer.Status != Status.Failed &&
            !IsWaitingVideo;

        /// <summary>
        /// Whether or not the video state is failed
        /// </summary>
        public bool IsFailed => MediaPlayer.Status == Status.Failed && !IsWaitingVideo;

        /// <summary>
        /// Whether or not the video state is loading
        /// </summary>
        public bool IsLoading => MediaPlayer.Status == Status.Opening && !IsWaitingVideo;

        /// <summary>
        /// True if the video player is waiting for a video to be selected
        /// </summary>
        public bool IsWaitingVideo => Video == null;

        /// <summary>
        /// True if the video state is ready and the video has not finished
        /// </summary>
        public bool HasContent => IsReady && MediaPlayer.Status != Status.Ended;

        /// <summary>
        /// The amount of time remaining in the video clip
        /// </summary>
        public TimeSpan RemainingTime => VideoDuration - VideoCurrentTime;

        /// <summary>
        /// True when the remaining time is less than the length of one frame
        /// </summary>
        public bool IsFinished
        {
            get
            {
                return MediaPlayer.Status == Status.Ended;
            }
        }

        /// <summary>
        /// Amount of ticks in one frame
        /// </summary>
        public long FrameTicks => TimeSpan.FromSeconds(1.0 / VideoFPS).Ticks;

        /// <summary>
        /// Icon for play/pause
        /// </summary>
        public PackIconMaterialDesignKind ControlButtonIcon
        {
            get => MediaPlayer.IsPlaying ? PackIconMaterialDesignKind.Pause : PackIconMaterialDesignKind.PlayArrow;
        }

        private int overlayContentCount = 0;
        public int OverlayContentCount
        {
            get => overlayContentCount;
            set
            {
                SetProperty(ref overlayContentCount, value);

                if (value == 1 && HasContent)
                {
                    if (MediaPlayer.IsPlaying)
                        Pause();

                    using (var bitmapStream = new MemoryStream())
                    {
                        // Generate bitmap from the last frame rendered in the video
                        var bitmap = MediaPlayer.renderer.GetBitmap();
                        bitmap.Save(bitmapStream, ImageFormat.Bmp);
                        bitmapStream.Position = 0;

                        // Source bitmap image with the formatted bitmap data
                        var image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = bitmapStream;
                        image.CacheOption = BitmapCacheOption.OnLoad;
                        image.EndInit();
                        image.Freeze();

                        // Use bitmap image as overlay
                        OverlayFrame = image;
                    }
                }
            }
        }

        /// <summary>
        /// Last frame recorded when OverlayContentCount was increased
        /// </summary>
        private ImageSource? overlayFrame;
        public ImageSource? OverlayFrame
        {
            get => overlayFrame;
            set => SetProperty(ref overlayFrame, value);
        }

        /// <summary>
        /// A reference to the media player
        /// </summary>
        public Player MediaPlayer { get; }

        /// <summary>
        /// Set by the Timeline control when the user is dragging any of the controls, whilst dragging the media players
        /// should be paused
        /// </summary>
        private bool isTimelineBusy = false;
        private bool isPlayQueued   = false;
        public bool IsTimelineBusy
        {
            get => isTimelineBusy;
            set
            {
                if (isTimelineBusy == value)
                    return;

                SetProperty(ref isTimelineBusy, value);
                if (value && MediaPlayer.IsPlaying)
                {
                    isPlayQueued = true;
                    Pause();
                }
                else if (!value && isPlayQueued) 
                {
                    isPlayQueued = false;
                    Play();
                }
            }
        }

        /// <summary>
        /// Zoom level for timeline.  0 - 1. 
        /// 0: fit waveform in timeline size
        /// 1: one to one pixel ratio with waveform resolution
        /// </summary>
        private double zoom = 0.0;
        public double Zoom
        {
            get => zoom;
            set => SetProperty(ref zoom, value);
        }
        #endregion

        #region Commands
        public ICommand AddClipCommand { get; }
        public ICommand OpenAudioSettings { get; }

        public ICommand ZoomIn { get; }

        public ICommand ZoomOut { get; }
        #endregion

        #region Methods
        /// <summary>
        /// Tries to seek a specific frame in the video
        /// </summary>
        /// <param name="frame">The time to seek to, in TimeSpan ticks</param>
        public void SeekTicks(long ticks)
        {
            if (ticks > MediaPlayer.Duration)
                return;

            MediaPlayer.CurTime = ticks;
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
            var numTicks = TimeSpan.FromSeconds(10).Ticks;
            var newClip  = new ClipViewModel(MediaPlayer.CurTime, Math.Min(MediaPlayer.CurTime + numTicks, MediaPlayer.Duration),
                $"Untitled {Video.Clips.Count + 1}", App.ViewModel.SettingsViewModel.DefaultOutputFolder)
            {
                Parent = Video
            };

            // Create default audio settings from current preview audio settings
            foreach (var player in AudioPlayers)
            {
                newClip.AudioSettings.Add(new AudioSettingsModel(player.StreamIndex, player.Name)
                {
                    IsEnabled   = !player.IsMuted,
                    Volume      = (int)player.Volume,
                    ConvertMono = App.ViewModel.SettingsViewModel.DefaultMicrophoneMono && Regex.IsMatch(player.Name, ".*(([Mm]ic)|([Dd]isc)|([Tt]eam[Sspeak])|([Tt][Ss])|([Vv]oice)).*")
                });
            }

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
                var edgeDifference = Math.Abs(edge - MediaPlayer.CurTime);

                if (edge >= MediaPlayer.CurTime && edgeDifference >= FrameTicks)
                {
                    SeekTicks(edge);
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
                var edge           = edges[i];
                var edgeDifference = Math.Abs(edge - MediaPlayer.CurTime);

                if (edge <= MediaPlayer.CurTime && edgeDifference >= FrameTicks)
                {
                    SeekTicks(edges[i]);
                    return;
                }
            }
        }

        /// <summary>
        /// Generates a list of start and end times of clips
        /// </summary>
        /// <returns>Said list</returns>
        private List<long> GetClipEdges()
        {
            var edges = new List<long>();
            if (Video == null)
                return edges;

            foreach (var clip in Video.Clips)
            {
                edges.Add(clip.StartTicks);
                edges.Add(clip.EndTicks);
            }

            return edges;
        }

        /// <summary>
        /// Tries to get a name for an audio stream by ID
        /// </summary>
        /// <param name="streamID">The audio stream ID</param>
        /// <returns>A name for the specified stream</returns>
        private string GetAudioTrackName(int streamID)
        {
            // Try to get the track names from the root viewmodel's videofile data
            string? trackName = Video?.TrackNames?.ElementAtOrDefault(streamID);
            if (trackName != null)
                return $"Track {streamID} - {trackName}";

            return $"Track {streamID}";
        }

        /// <summary>
        /// Closes all streams used by all players
        /// </summary>
        public void Stop()
        {
            foreach (var audio in AudioPlayers)
                audio.Player.Stop();

            MediaPlayer.Stop();
        }

        /// <summary>
        /// Toggles all of the players between a play and pause state
        /// </summary>
        public void TogglePlayPause()
        {
            // Sync audio before playing
            SyncAudio();

            foreach (var audio in AudioPlayers)
                audio.Player.TogglePlayPause();

            if (OverlayContentCount == 0)
                MediaPlayer.TogglePlayPause();
        }

        /// <summary>
        /// Runs play on all players
        /// </summary>
        public void Play()
        {
            // Sync audio before playing
            SyncAudio();

            foreach (var audio in AudioPlayers)
                audio.Player.Play();

            if (OverlayContentCount == 0)
                MediaPlayer.Play();
        }

        /// <summary>
        /// Runs pause on all players
        /// </summary>
        public void Pause()
        {
            foreach (var audio in AudioPlayers)
                audio.Player.Pause();

            MediaPlayer.Pause();
        }

        /// <summary>
        /// Runs ShowFrameNext on the main media player then syncs audio
        /// </summary>
        public void ShowFrameNext()
        {
            if (OverlayContentCount != 0)
                return;
            
            Pause();

            MediaPlayer.ShowFrameNext();
        }

        /// <summary>
        /// Runs ShowFramePrev on the main media player then syncs audio
        /// </summary>
        public void ShowFramePrev()
        {
            if (OverlayContentCount != 0)
                return;

            Pause();

            MediaPlayer.ShowFramePrev();

            // Reset reverse playback settings
            App.MediaPlayer.ReversePlayback = false;
            foreach (var audio in AudioPlayers)
                audio.Player.ReversePlayback = false;
        }

        /// <summary>
        /// Seeks all audio players to the current position of the main media player
        /// </summary>
        private void SyncAudio()
        {
            foreach (var audio in AudioPlayers)
                audio.Player.CurTime = MediaPlayer.CurTime;
        }
        #endregion
    }
}

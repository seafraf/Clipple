using Clipple.DataModel;
using Clipple.Types;
using Clipple.View;
using MahApps.Metro.IconPacks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Mpv.NET.Player;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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

            MediaPlayer = new MpvPlayer(Path.Combine(App.LibPath, "mpv-2.dll"))
            {
                KeepOpen = KeepOpen.Always
            };

            MediaPlayer.PositionChanged += OnVideoPositionChanged;

            MediaPlayer.MediaPaused   += (s, e) => OnPropertyChanged(nameof(IsPlaying));
            MediaPlayer.MediaResumed  += (s, e) => OnPropertyChanged(nameof(IsPlaying));
            MediaPlayer.MediaFinished += (s, e) => OnPropertyChanged(nameof(IsPlaying));
            MediaPlayer.MediaError    += (s, e) => State = MediaPlayerState.Error;
            MediaPlayer.MediaLoaded   += OnVideoLoaded;

            var timelineSeekTimer = new DispatcherTimer();
            timelineSeekTimer.Tick += (s, e) =>
            {
                if (State == MediaPlayerState.Ready && IsTimelineBusy)
                    Seek(VideoCurrentTime);
            };

            timelineSeekTimer.Interval = TimeSpan.FromMilliseconds(100);
            timelineSeekTimer.Start();
        }
    
        /// <summary>
        /// Called when a loading video finishes loading.
        /// </summary>
        private void OnVideoLoaded(object? sender, EventArgs e)
        {
            // Duration changes when a new video loads
            OnPropertyChanged(nameof(VideoDuration));

            State = MediaPlayerState.Ready;

            // Load Video settings into the MediaPlayer
            if (Video != null)
            {
                WaitingFirstSeek = true;

                MediaPlayer.Volume  = Volume;
                MediaPlayer.IsMuted = IsMuted;
                MediaPlayer.Speed   = PlaybackSpeed;

                VideoCurrentTime = Video.CurrentTime;
                Seek(Video.CurrentTime);

                // Apply updated audio stream filter
                UpdateAudioStreamFilter();
            }
        }

        /// <summary>
        /// Called every time the position chang
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnVideoPositionChanged(object? sender, MpvPlayerPositionChangedEventArgs e)
        {
            if (State != MediaPlayerState.Ready || WaitingFirstSeek)
                return;

            var clip = Video?.SelectedClip;
            if (clip != null)
            {
                if (e.NewPosition > clip.EndTime)
                {
                    Pause();
                    VideoCurrentTime = clip.EndTime;

                    Seek(VideoCurrentTime);
                }

                if (e.NewPosition < clip.StartTime)
                {
                    VideoCurrentTime = clip.StartTime;

                    Seek(VideoCurrentTime);
                }
            }

            OnPropertyChanged(nameof(VideoCurrentTime));

            if (Video != null)
                Video.CurrentTime = e.NewPosition;

            if (!IsTimelineBusy)
                VideoCurrentTime = e.NewPosition;
        }

        /// <summary>
        /// Called when a propety from the selected video changes
        /// </summary>
        private void OnAudioStreamPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AudioStreamViewModel.IsMuted) ||
                e.PropertyName == nameof(AudioStreamViewModel.IsEnabled) ||
                e.PropertyName == nameof(AudioStreamViewModel.Volume) ||
                e.PropertyName == nameof(AudioStreamViewModel.IsMono))
            {
                UpdateAudioStreamFilter();
            }
        }

        #region Properties
        /// <summary>
        /// A reference to the media player
        /// </summary>
        public MpvPlayer MediaPlayer { get; }

        /// <summary>
        /// Current video time in TimeSpan format
        /// </summary>
        private TimeSpan videoCurrentTime;
        public TimeSpan VideoCurrentTime
        {
            get => videoCurrentTime;
            set => SetProperty(ref videoCurrentTime, value);
        }

        /// <summary>
        /// Duration of the currently loaded video
        /// </summary>
        public TimeSpan VideoDuration => MediaPlayer.Duration;

        /// <summary>
        /// The currently loaded video
        /// </summary>
        private VideoViewModel? video;
        public VideoViewModel? Video
        {
            get => video;
            set
            {
                // Unload old video
                if (video != null)
                {
                    SetStreamEvents(false);
                    Unload();
                }
                    

                SetProperty(ref video, value);

                OnPropertyChanged(nameof(Volume));
                OnPropertyChanged(nameof(IsMuted));
                OnPropertyChanged(nameof(PlaybackSpeed));
                OnPropertyChanged(nameof(Zoom));
         
                if (value != null)
                {
                    SetStreamEvents(true);

                    if (MediaPlayer.Handle.ToInt64() != -1)
                        Load(value.FileInfo.FullName);
                }
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
        /// Video state
        /// </summary>
        private MediaPlayerState state = MediaPlayerState.Waiting; 
        public MediaPlayerState State
        {
            get => state;
            set => SetProperty(ref state, value);
        }

        /// <summary>
        /// Whether or not the video is playing
        /// </summary>
        public bool IsPlaying => MediaPlayer.IsPlaying && !MediaPlayer.EndReached;

        /// <summary>
        /// Number of UI elements covering the video player.  This is used to hide the video
        /// when UI elements are on top of it.  This is required because the video player is
        /// in a WindowsFormsHost and has airspacing issues.
        /// </summary>
        private int overlayContentCount = 0;
        public int OverlayContentCount
        {
            get => overlayContentCount;
            set
            {
                SetProperty(ref overlayContentCount, value);

                if (value == 1 && State == MediaPlayerState.Ready)
                {
                    if (MediaPlayer.IsPlaying)
                        Pause();

                    //using (var bitmapStream = new MemoryStream())
                    //{
                    //    // Generate bitmap from the last frame rendered in the video
                    //    var bitmap = MediaPlayer.renderer.GetBitmap();
                    //    bitmap.Save(bitmapStream, ImageFormat.Bmp);
                    //    bitmapStream.Position = 0;

                    //    // Source bitmap image with the formatted bitmap data
                    //    var image = new BitmapImage();
                    //    image.BeginInit();
                    //    image.StreamSource = bitmapStream;
                    //    image.CacheOption = BitmapCacheOption.OnLoad;
                    //    image.EndInit();
                    //    image.Freeze();

                    //    // Use bitmap image as overlay
                    //    OverlayFrame = image;
                    //}
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

                // When dragging, the actual video position is seeked to every 100 milliseconds (avoids unneccesary seeks),
                // but this also means if the total drag time is less than 100ms, it will never seek.  Seek here to avoid 
                // this happening
                if (!value)
                    Seek(VideoCurrentTime);
            }
        }

        /// <summary>
        /// This is true immediately after loading a video and is set back to false when the first
        /// seek used to recover the old position has finished.  This is required because at some 
        /// random (thread based) time, OnPositionChanged will be called with a zero point time, if
        /// this happens after the first seek then it reset the previously loaded position.  This 
        /// variable call be used to ignore that OnPositionChanged event
        /// </summary>
        private bool WaitingFirstSeek { get; set; } = false;
        #endregion

        #region Video view model passthrough properties
        /// <summary>
        /// Player volume, 0-100
        /// </summary>
        public int Volume
        {
            get => Video != null ? Video.Volume : 100;
            set
            {
                if (Video != null)
                    Video.Volume = value;

                MediaPlayer.Volume = value;
                OnPropertyChanged(nameof(Volume));
            }
        }

        /// <summary>
        /// Whether or not audio for this video is muted or not
        /// </summary>
        public bool IsMuted
        {
            get => Video != null ? Video.IsMuted : false;
            set
            {
                if (Video != null)
                    Video.IsMuted = value;

                MediaPlayer.IsMuted = value;
                OnPropertyChanged(nameof(IsMuted));
            }
        }

        /// <summary>
        /// Playback sppeed
        /// </summary>
        public double PlaybackSpeed
        {
            get => Video != null ? Video.PlaybackSpeed : 1.0;
            set
            {
                if (Video != null)
                    Video.PlaybackSpeed = value;

                MediaPlayer.Speed = value;
                OnPropertyChanged(nameof(PlaybackSpeed));
            }
        }

        /// <summary>
        /// Zoom level for timeline.  0 - 1. 
        /// 0: fit waveform in timeline size
        /// 1: one to one pixel ratio with waveform resolution
        /// </summary>
        public double Zoom
        {
            get => Video != null ? Video.TimelineZoom : 1.0;
            set
            {
                if (Video != null)
                    Video.TimelineZoom = value;

                OnPropertyChanged(nameof(Zoom));
            }
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
        /// Tries to seek to a specific position in the video.
        /// </summary>
        /// <param name="time">The time to seek to</param>
        public void Seek(TimeSpan time)
        {
            if (Video == null || State != MediaPlayerState.Ready)
                return;

            // Only perform a seek if the requested position is more 1 frame or greater away in time
            // NOTE: this optimisation is almost REQUIRED as otherwise when seeking to a position near the
            // end of a clip boundary, the PositionChanged callback will request a new position again, causing
            // an infinite loop of seeks
            var frameTime   = 1.0 / Video.VideoFPS;
            var diff        = Math.Abs(time.TotalSeconds - MediaPlayer.Position.TotalSeconds);

            if (diff >= frameTime)
            {
                Task.Run(async () =>
                {
                    await MediaPlayer.SeekAsync(time.TotalSeconds);
                    WaitingFirstSeek = false;
                });
            }
            else
                WaitingFirstSeek = false;
        }

        /// <summary>
        /// Creates a new clip at the playhead
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        internal void CreateClip()
        {
            if (Video == null || Video.AudioStreams == null)
                return;

            // Set a default clip length of 10 seconds
            var numTicks = TimeSpan.FromSeconds(10).Ticks;
            var newClip = new ClipViewModel(VideoCurrentTime,
                TimeSpan.FromTicks(Math.Min(numTicks, MediaPlayer.Duration.Ticks - VideoCurrentTime.Ticks)),
                $"Untitled {Video.Clips.Count + 1}", App.ViewModel.SettingsViewModel.DefaultOutputFolder)
            {
                Parent = Video
            };

            // Create default audio settings from current preview audio settings
            foreach (var audioStream in Video.AudioStreams)
            {
                newClip.AudioSettings.Add(new AudioSettingsModel(audioStream.StreamIndex, audioStream.Name)
                {
                    IsEnabled   = !audioStream.IsMuted && audioStream.IsEnabled,
                    Volume      = audioStream.Volume,
                    ConvertMono = App.ViewModel.SettingsViewModel.DefaultMicrophoneMono && Regex.IsMatch(audioStream.Name, ".*(([Mm]ic)|([Dd]isc)|([Tt]eam[Sspeak])|([Tt][Ss])|([Vv]oice)).*")
                });
            }

            Video.Clips.Add(newClip);
        }

        /// <summary>
        /// Seeks to the start of the video or clip
        /// </summary>
        public void SeekStart()
        {
            Seek(Video?.SelectedClip != null ? Video.SelectedClip.StartTime : TimeSpan.Zero);
        }


        /// <summary>
        /// Seeks to the end of the video or clip
        /// </summary>
        public void SeekEnd()
        {
            Seek(Video?.SelectedClip != null ? Video.SelectedClip.EndTime : VideoDuration);
        }

        /// <summary>
        /// Close current video
        /// </summary>
        public void Unload()
        {
            State = MediaPlayerState.Waiting;

            MediaPlayer.Stop();
        }

        /// <summary>
        /// Loads a video
        /// </summary>
        /// <param name="video">Full path to the video</param>
        public void Load(string video)
        {
            State = MediaPlayerState.Loading;
            MediaPlayer.Load(video);
        }

        /// <summary>
        /// Toggles all of the players between a play and pause state
        /// </summary>
        public void TogglePlayPause()
        {
            if (OverlayContentCount == 0 && State == MediaPlayerState.Ready && !IsTimelineBusy)
            {
                if (MediaPlayer.IsPlaying)
                {
                    MediaPlayer.Pause();
                }
                else
                    MediaPlayer.Resume();
            }
        }

        /// <summary>
        /// Runs play on all players
        /// </summary>
        public void Play()
        {
            if (OverlayContentCount == 0 && State == MediaPlayerState.Ready && !IsTimelineBusy)
                MediaPlayer.Resume();
        }

        /// <summary>
        /// Runs pause on all players
        /// </summary>
        public void Pause()
        {
            if (OverlayContentCount == 0 && State == MediaPlayerState.Ready)
                MediaPlayer.Pause();
        }

        /// <summary>
        /// Runs ShowFrameNext on the main media player then syncs audio
        /// </summary>
        public void ShowFrameNext()
        {
            if (OverlayContentCount == 0 && State == MediaPlayerState.Ready && !IsTimelineBusy)
                MediaPlayer.NextFrame();
        }

        /// <summary>
        /// Runs ShowFramePrev on the main media player then syncs audio
        /// </summary>
        public void ShowFramePrev()
        {

            if (OverlayContentCount == 0 && State == MediaPlayerState.Ready && !IsTimelineBusy)
                MediaPlayer.PreviousFrame();
        }

        /// <summary>
        /// Adds or removes event handlers for all of the current video's audio streams.
        /// </summary>
        /// <param name="install">True to install event handlers, false to remove them</param>
        private void SetStreamEvents(bool install = true)
        {
            if (Video != null && Video.AudioStreams != null)
            {
                foreach (var audioStream in Video.AudioStreams)
                {
                    if (install)
                    {
                        audioStream.PropertyChanged += OnAudioStreamPropertyChanged;
                    }
                    else
                        audioStream.PropertyChanged -= OnAudioStreamPropertyChanged;
                }
            }
        }

        /// <summary>
        /// Updates the filter used for audio on the media player to reflect the current videos AudioStream settings
        /// </summary>
        private void UpdateAudioStreamFilter()
        {
            if (Video == null || Video.AudioStreams == null)
                return;

            var volumeFilters = string.Join("; ", 
                Video.AudioStreams.Select(x => $"[aid{x.AudioStreamIndex + 1}]volume={x.VolumeString}[v{x.AudioStreamIndex}]" 
                + (x.IsMono ? $"; [v{x.AudioStreamIndex}]pan=mono|c0=.5*c0+.5*c1[p{x.AudioStreamIndex}]" : "")));

            var inputList = string.Join("", Video.AudioStreams.Select(x => $"[{(x.IsMono ? 'p' : 'v')}{x.AudioStreamIndex}]"));
            var filter    = $"{volumeFilters}; {inputList}amix=inputs={Video.AudioStreams.Length}[ao]";

            MediaPlayer.API.SetPropertyString("lavfi-complex", filter);
        }
        #endregion
    }
}

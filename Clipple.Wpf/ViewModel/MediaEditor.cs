using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Clipple.Types;
using Clipple.View;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Mpv.NET.Player;

namespace Clipple.ViewModel;

[Serializable]
public class MediaEditor : ObservableObject
{
    public MediaEditor()
    {
        ZoomIn  = new RelayCommand(() => Zoom = Math.Clamp(Zoom + 0.05, 0.0, 1.0));
        ZoomOut = new RelayCommand(() => Zoom = Math.Clamp(Zoom - 0.05, 0.0, 1.0));
        OpenExportDialogCommand = new RelayCommand(() =>
        {
            if (Media == null)
                return;

            DialogHost.Show(new ExportClip
            {
                DataContext = Media
            });
        });


        MediaPlayer = new(Path.Combine(App.LibPath, "mpv-2.dll"))
        {
            KeepOpen = KeepOpen.Always
        };

        MediaPlayer.PositionChanged += OnMediaPositionChanged;

        MediaPlayer.MediaPaused   += (s, e) => OnPropertyChanged(nameof(IsPlaying));
        MediaPlayer.MediaResumed  += (s, e) => OnPropertyChanged(nameof(IsPlaying));
        MediaPlayer.MediaFinished += (s, e) => OnPropertyChanged(nameof(IsPlaying));
        MediaPlayer.MediaError    += (s, e) => { State = MediaPlayerState.Error; };
        MediaPlayer.MediaLoaded   += OnMediaLoaded;

        var timelineDragTick = new DispatcherTimer();
        timelineDragTick.Tick += (s, e) =>
        {
            if (!IsTimelineBusy)
                CurrentTime = MediaPlayer.Position;
        };

        timelineDragTick.Interval = TimeSpan.FromMilliseconds(50);
        timelineDragTick.Start();
    }

    #region Members

    private TimeSpan         currentTime;
    private Media?           media;
    private MediaPlayerState state = MediaPlayerState.Waiting;
    private bool             isTimelineBusy;
    private bool             isPlayQueued;
    private bool             showAudioStreamNames;

    #endregion

    #region Properties

    /// <summary>
    /// A reference to the media player
    /// </summary>
    public MpvPlayer MediaPlayer { get; }

    /// <summary>
    /// Current media time.
    /// </summary>
    public TimeSpan CurrentTime
    {
        get => currentTime;
        set => SetProperty(ref currentTime, value);
    }

    /// <summary>
    /// The currently loaded media
    /// </summary>
    public Media? Media
    {
        get => media;
        set
        {
            if (value != null)
                App.ViewModel.AppState.EditorMediaId = value.Id;

            // Unload old video
            if (media != null)
            {
                SetStreamEvents(false);
                Unload();
            }

            SetProperty(ref media, value);

            OnPropertyChanged(nameof(Volume));
            OnPropertyChanged(nameof(IsMuted));
            OnPropertyChanged(nameof(PlaybackSpeed));
            OnPropertyChanged(nameof(Zoom));

            // Update audio filter before the stream loads
            UpdateAudioStreamFilter();

            if (value == null) return;

            SetStreamEvents();

            if (MediaPlayer.Handle.ToInt64() != -1)
                Load(value.FileInfo.FullName);
        }
    }

    /// <summary>
    /// Media state
    /// </summary>
    public MediaPlayerState State
    {
        get => state;
        set => SetProperty(ref state, value);
    }

    /// <summary>
    /// Whether or not the media is playing
    /// </summary>
    public bool IsPlaying => MediaPlayer.IsPlaying && !MediaPlayer.EndReached;

    /// <summary>
    /// Set by the Timeline control when the user is dragging any of the controls, whilst dragging the media players
    /// should be paused
    /// </summary>
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

            // When dragging, the actual media position is seeked to every 100 milliseconds (avoids unneccesary seeks),
            // but this also means if the total drag time is less than 100ms, it will never seek.  Seek here to avoid 
            // this happening
            if (!value)
                Seek(CurrentTime);
        }
    }

    /// <summary>
    /// This is true immediately after loading media and is set back to false when the first
    /// seek used to recover the old position has finished.  This is required because at some
    /// random (thread based) time, OnPositionChanged will be called with a zero point time, if
    /// this happens after the first seek then it reset the previously loaded position.  This
    /// variable call be used to ignore that OnPositionChanged event
    /// </summary>
    private bool WaitingFirstSeek { get; set; }

    /// <summary>
    /// Player volume, 0-100
    /// </summary>
    public int Volume
    {
        get => Media?.Volume ?? 100;
        set
        {
            if (Media != null)
                Media.Volume = value;

            MediaPlayer.Volume = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Whether or not audio is muted
    /// </summary>
    public bool IsMuted
    {
        get => Media?.IsMuted ?? false;
        set
        {
            if (Media != null)
                Media.IsMuted = value;

            MediaPlayer.IsMuted = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Playback speed
    /// </summary>
    public double PlaybackSpeed
    {
        get => Media?.PlaybackSpeed ?? 1.0;
        set
        {
            if (Media != null)
                Media.PlaybackSpeed = value;

            MediaPlayer.Speed = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Zoom level for timeline.  0 - 1.
    /// 0: fit waveform in timeline size
    /// 1: one to one pixel ratio with waveform resolution
    /// </summary>
    public double Zoom
    {
        get => Media?.TimelineZoom ?? 1.0;
        set
        {
            if (Media != null)
                Media.TimelineZoom = value;

            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Whether or not audio stream names should be drawn on the timeline
    /// </summary>

    public bool ShowAudioStreamNames
    {
        get => showAudioStreamNames;
        set => SetProperty(ref showAudioStreamNames, value);
    }

    /// <summary>
    /// Returns the time between frames for the currently loaded media.  Note that some videos and all audio-only files do not have a defined FPS, in those cases
    /// the frame time will be an assumed 60FPS.
    /// </summary>
    public TimeSpan FrameTime
    {
        get
        {
            if (Media is { HasVideo: true, VideoFps: { } fps } loadedMedia)
                return TimeSpan.FromSeconds(1.0 / (double)loadedMedia.VideoFps);

            return TimeSpan.FromSeconds(1.0 / 60.0);
        }
    }

    /// <summary>
    /// True if there is space for a full FrameTime between the current time and the end of the clip
    /// </summary>
    private bool HasNextFrame
    {
        get
        {
            if (media is not { Clip: { } clip })
                return false;

            return clip.EndTime - CurrentTime > FrameTime;
        }
    }
    
    /// <summary>
    /// True if there is space for a full FrameTime between the current time and the beginning of the clip
    /// </summary>
    private bool HasPreviousFrame
    {
        get
        {
            if (media is not { Clip: { } clip })
                return false;

            return CurrentTime - clip.StartTime > FrameTime;
        }
    }

    #endregion

    #region Commands

    public ICommand ZoomIn { get; }

    public ICommand ZoomOut { get; }

    public ICommand OpenExportDialogCommand { get; }
    
    public ICommand ControlCommand          => new RelayCommand(TogglePlayPause);
    
    public ICommand PreviousFrameCommand    => new RelayCommand(ShowFramePrev);
    
    public ICommand NextFrameCommand        => new RelayCommand(ShowFrameNext);

    #endregion

    #region Events

    /// <summary>
    /// Called when media is loaded by MPV
    /// </summary>
    private void OnMediaLoaded(object? sender, EventArgs e)
    {
        State = MediaPlayerState.Ready;

        if (Media == null)
            return;

        // Load media settings into the MediaPlayer
        WaitingFirstSeek = true;

        MediaPlayer.Volume  = Volume;
        MediaPlayer.IsMuted = IsMuted;
        MediaPlayer.Speed   = PlaybackSpeed;

        CurrentTime = Media.CurrentTime;
        Seek(Media.CurrentTime);

        // Apply updated audio stream filter
        UpdateAudioStreamFilter();
    }

    /// <summary>
    /// Called every time the position chang
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnMediaPositionChanged(object? sender, MpvPlayerPositionChangedEventArgs e)
    {
        if (State != MediaPlayerState.Ready || WaitingFirstSeek)
            return;

        if (Media is { Clip: { } clip })
        {
            if (e.NewPosition > clip.EndTime)
            {
                Pause();
                CurrentTime = clip.EndTime;

                Seek(CurrentTime);
            }

            if (e.NewPosition < clip.StartTime)
            {
                CurrentTime = clip.StartTime;

                Seek(CurrentTime);
            }
        }

        OnPropertyChanged(nameof(CurrentTime));

        if (Media != null)
            Media.CurrentTime = e.NewPosition;

        if (!IsTimelineBusy)
            CurrentTime = e.NewPosition;
    }

    /// <summary>
    /// Called when a property from the loaded media changes
    /// </summary>
    private void OnAudioStreamPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateAudioStreamFilter();
    }

    #endregion

    #region Methods

    /// <summary>
    /// Tries to seek to a specific position in the media.
    /// </summary>
    /// <param name="time">The time to seek to</param>
    /// <param name="resume">Set to true to resume after the seek has completed</param>
    public void Seek(TimeSpan time, bool resume = false)
    {
        if (Media == null || State != MediaPlayerState.Ready)
            return;

        // Only perform a seek if the requested position is more 1 frame or greater away in time
        // NOTE: this optimisation is almost REQUIRED as otherwise when seeking to a position near the
        // end of a clip boundary, the PositionChanged callback will request a new position again, causing
        // an infinite loop of seeks
        var diff      = Math.Abs(time.Ticks - MediaPlayer.Position.Ticks);

        if (diff >= FrameTime.Ticks)
            Task.Run(async () =>
            {
                await MediaPlayer.SeekAsync(time.TotalSeconds);
                WaitingFirstSeek = false;
                
                if (resume)
                    MediaPlayer.Resume();
            });
        else
            WaitingFirstSeek = false;
    }

    /// <summary>
    /// Seeks to the start of the media's clip
    /// </summary>
    public void SeekStart(bool resume = false)
    {
        if (Media?.Clip != null)
            Seek(Media.Clip.StartTime, resume);
    }


    /// <summary>
    /// Seeks to the end of the media's clip
    /// </summary>
    public void SeekEnd()
    {
        if (Media?.Clip != null)
            Seek(Media.Clip.EndTime);
    }

    /// <summary>
    /// Closes current media
    /// </summary>
    public void Unload()
    {
        State = MediaPlayerState.Waiting;

        MediaPlayer.Stop();
    }

    /// <summary>
    /// Loads a new media
    /// </summary>
    /// <param name="mediaFile">Full path to the media</param>
    public void Load(string mediaFile)
    {
        State = MediaPlayerState.Loading;

        MediaPlayer.Load(mediaFile);
    }

    /// <summary>
    /// Sends either a play or pause command to the media player
    /// </summary>
    public void TogglePlayPause()
    {
        if (MediaPlayer.IsPlaying)
            Pause();
        else
            Play();
    }

    /// <summary>
    /// Sends play command to the media player
    /// </summary>
    public void Play()
    {
        if (State != MediaPlayerState.Ready || IsTimelineBusy) 
            return;
        
        if (!HasNextFrame)
        {
            SeekStart(true);
        }
        else 
            MediaPlayer.Resume();
    }

    /// <summary>
    /// Sends a pause command to the media player
    /// </summary>
    public void Pause()
    {
        if (State == MediaPlayerState.Ready)
            MediaPlayer.Pause();
    }

    /// <summary>
    /// Runs ShowFrameNext on the main media player then syncs audio
    /// </summary>
    public void ShowFrameNext()
    {
        if (State == MediaPlayerState.Ready && !IsTimelineBusy && HasNextFrame)
            MediaPlayer.NextFrame();
    }

    /// <summary>
    /// Runs ShowFramePrev on the main media player then syncs audio
    /// </summary>
    public void ShowFramePrev()
    {
        if (State == MediaPlayerState.Ready && !IsTimelineBusy && HasPreviousFrame)
            MediaPlayer.PreviousFrame();
    }

    /// <summary>
    /// Adds or removes event handlers for all of the current media's audio streams.
    /// </summary>
    /// <param name="install">True to install event handlers, false to remove them</param>
    private void SetStreamEvents(bool install = true)
    {
        if (Media == null)
            return;

        foreach (var audioStream in Media.AudioStreams)
            if (install)
                audioStream.PropertyChanged += OnAudioStreamPropertyChanged;
            else
                audioStream.PropertyChanged -= OnAudioStreamPropertyChanged;
    }

    /// <summary>
    /// Updates the filter used for audio on the media player to reflect the current media's AudioStream settings
    /// </summary>
    private void UpdateAudioStreamFilter()
    {
        if (Media?.AudioStreams == null || Media.AudioStreams.Length == 0)
        {
            MediaPlayer.API.SetPropertyString("lavfi-complex", "");
            return;
        }

        var enabledStreams = Media.AudioStreams.Where(x => x.IsEnabled).ToList();

        var stringFilters = new List<string>();
        var inputs        = new List<string>();
        foreach (var stream in enabledStreams)
        {
            var filters = stream.AudioFilters.Where(x => x.IsEnabled).ToList();
            if (filters.Count > 0)
            {
                stringFilters.AddRange(filters.Select((t, i) =>
                    $"[{(i == 0 ? $"aid{stream.AudioStreamIndex + 1}" : $"f_{stream.StreamIndex}_{i - 1}")}]{t.FilterString}[f_{stream.StreamIndex}_{i}]"));
                inputs.Add($"[f_{stream.StreamIndex}_{filters.Count - 1}]");
            }
            else
            {
                inputs.Add($"[aid{stream.AudioStreamIndex + 1}]");
            }
        }


        var filterString = string.Join("; ", stringFilters);
        var inputString  = string.Join("", inputs);

        MediaPlayer.API.SetPropertyString("lavfi-complex",
            stringFilters.Count == 0 ? $"{inputString}amix=inputs={inputs.Count}[ao]" : $"{filterString}; {inputString}amix=inputs={inputs.Count}[ao]");
    }

    #endregion
}
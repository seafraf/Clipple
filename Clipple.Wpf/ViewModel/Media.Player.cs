using System;

namespace Clipple.ViewModel;

public partial class Media
{
    #region Members
    private TimeSpan currentTime = TimeSpan.Zero;
    private int volume = 100;
    private bool isMuted = false;
    private double playbackSpeed = 1.0;
    private double timelineZoom = 0.0;
    private AudioStreamSettings[] audioStreamSettings = Array.Empty<AudioStreamSettings>();
    #endregion

    #region Properties
    /// <summary>
    ///     Position of the media play head.
    /// </summary>
    public TimeSpan CurrentTime
    {
        get => currentTime;
        set => SetProperty(ref currentTime, value);
    }

    /// <summary>
    ///     Volume modifier for all tracks.
    /// </summary>
    public int Volume
    {
        get => volume;
        set => SetProperty(ref volume, value);
    }

    /// <summary>
    ///     Whether or not all audio tracks should be muted.
    /// </summary>
    public bool IsMuted
    {
        get => isMuted; 
        set => SetProperty(ref isMuted, value);
    }

    /// <summary>
    ///     Settings and information for the audio streams in this media
    /// </summary>
    public AudioStreamSettings[] AudioStreams
    {
        get => audioStreamSettings;
        set => SetProperty(ref audioStreamSettings, value);
    }

    /// <summary>
    ///     Playback speed
    /// </summary>
    public double PlaybackSpeed
    {
        get => playbackSpeed; 
        set => SetProperty(ref playbackSpeed, value);
    }

    /// <summary>
    ///     Timeline zoom
    /// </summary>
    public double TimelineZoom
    {
        get => timelineZoom; 
        set => SetProperty(ref timelineZoom, value);
    }
    #endregion
}
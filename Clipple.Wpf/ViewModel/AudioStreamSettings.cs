using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Windows.Media;
using Clipple.AudioFilters;
using FFmpeg.AutoGen;
using LiteDB;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Clipple.ViewModel;

/// <summary>
/// The audio stream class is built using information provided by FFMPEG.  It contains:
/// - Settings for playing the audio stream in the editor (filters, stream selection via IsEnabled, etc)
/// - Settings for exporting the audio stream as a clip
/// - Settings for displaying the audio clip in the UI (e.g the audio waveform)
/// 
/// When this class is used by a media view model, it:
/// - Has a populated Waveform property
/// - is used in the editor for:
///   - MPV's filter, allowing audio filters for per-stream volume control and anything else.
///   - Displaying a waveform for the stream inside the timeline control
///   
/// When this class is used by a clip view model, it:
/// - Does NOT have a populated Waveform property
/// - Is used for clip exporting exclusively
/// </summary>
public class AudioStreamSettings : ObservableObject
{
    [BsonCtor]
    public AudioStreamSettings(string name, int streamIndex, int audioStreamIndex, AVCodecID codecId)
    {
        CodecID = codecId;
        Name = name;
        StreamIndex = streamIndex;
        AudioStreamIndex = audioStreamIndex;

        InstallEvents();
    }

    #region Methods
    /// <summary>
    /// Add event listeners to all filters so that if any propety on any filter is changed, the OnPropertyChanged
    /// event is also called for this stream.
    /// </summary>
    private void InstallEvents()
    {
        foreach (var filter in audioFilters)
            filter.PropertyChanged += (s, e) => OnPropertyChanged(e.PropertyName);
    }
    #endregion

    #region Members
    private ImageSource? waveform;
    private readonly ObservableCollection<AudioFilter> audioFilters = new()
    {
        new Volume.ViewModel()
        {
            IsEnabled = true
        },
        new Pan.ViewModel()
        {
            IsEnabled = false
        },
    };
    #endregion

    #region Properties

    /// <summary>
    ///     The name of this stream, this is only provided in some containers.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    ///     Stream index.  This is the index of this audio stream in a list of all streams.
    /// </summary>
    public int StreamIndex { get; }

    /// <summary>
    ///     The index of this stream in the list of audio streams.  Note this is not the same as a stream index which would
    ///     be the index in the list of all streams.
    /// </summary>
    public int AudioStreamIndex { get; }

    /// <summary>
    /// The ID of the codec used for this stream
    /// </summary>
    public AVCodecID CodecID { get; }

    /// <summary>
    /// Helper function, returns the name of the codec for this stream
    /// </summary>
    [BsonIgnore]
    public string CodecName => ffmpeg.avcodec_get_name(CodecID);

    /// <summary>
    ///     Whether or not this audio stream is enabled.  When disabled, the audio stream
    ///     will be muted and will not show a waveform in the timeline.
    /// </summary>
    private bool enabled = true;

    public bool IsEnabled
    {
        get => enabled;
        set => SetProperty(ref enabled, value);
    }

    /// <summary>
    ///     An image representing the waveform for this stream
    /// </summary>
    [BsonIgnore]
    public ImageSource? Waveform
    {
        get => waveform;
        set => SetProperty(ref waveform, value);
    }

    /// <summary>
    /// A list of all audio filters that apply to this stream.  Note the individual audio filter may be disabled.
    /// </summary>
    public ObservableCollection<AudioFilter> AudioFilters { get => audioFilters; }
    #endregion
}
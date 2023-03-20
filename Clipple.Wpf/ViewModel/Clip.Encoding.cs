using System.Linq;
using Clipple.Types;
using LiteDB;

namespace Clipple.ViewModel;

public partial class Clip
{
    #region Constants

    private const double DefaultOutputTargetSize = 100;

    #endregion

    #region Members

    private AudioVideoCodec? audioCodec;
    private AudioVideoCodec? videoCodec;
    private int              audioCodecIndex;
    private int              videoCodecIndex;
    private bool             useTargetSize;
    private double           outputTargetSize = DefaultOutputTargetSize;
    private ContainerFormat  containerFormat;
    private int              containerFormatIndex;
    private string           extension;
    private int              extensionIndex;

    #endregion

    #region Methods

    private void InitialiseEncoder()
    {
        var audioIndex = AudioCodecIndex;
        var videoIndex = VideoCodecIndex;
        var extIndex   = ExtensionIndex;

        ContainerFormat = App.ViewModel.ContainerFormatCollection.SupportedFormats.ElementAtOrDefault(containerFormatIndex) ??
                          App.ViewModel.ContainerFormatCollection.SupportedFormats.First();

        AudioCodecIndex = audioIndex;
        VideoCodecIndex = videoIndex;
        ExtensionIndex  = extIndex;

        AudioCodec = ContainerFormat.AudioCodecs.ElementAtOrDefault(audioIndex);
        VideoCodec = ContainerFormat.VideoCodecs.ElementAtOrDefault(videoIndex);
        Extension  = ContainerFormat.Extensions.ElementAtOrDefault(extIndex) ?? ContainerFormat.Extension;
    }

    #endregion

    #region Properties

    /// <summary>
    ///     Index of , for serialization
    /// </summary>
    public int ContainerFormatIndex
    {
        get => containerFormatIndex;
        set => SetProperty(ref containerFormatIndex, value);
    }

    /// <summary>
    ///     Currently selected container format
    /// </summary>
    [BsonIgnore]
    public ContainerFormat ContainerFormat
    {
        get => containerFormat;
        set
        {
            SetProperty(ref containerFormat, value);

            // Merge audio is forced on for audio only formats
            if (!value.SupportsAudio)
                OnPropertyChanged(nameof(MergeAudio));

            // Reset codecs
            if (value.SupportsAudio)
            {
                AudioCodecIndex = 0;
                AudioCodec      = value.AudioCodecs.First();
            }
            else
            {
                AudioCodecIndex = -1;
            }

            if (value.SupportsVideo)
            {
                VideoCodecIndex = 0;
                VideoCodec      = value.VideoCodecs.First();
            }
            else
            {
                VideoCodecIndex = -1;
            }

            // Reset extension
            ExtensionIndex = 0;
            Extension      = value.Extensions.ElementAt(ExtensionIndex);
        }
    }

    /// <summary>
    ///     Audio codec index
    /// </summary>
    public int AudioCodecIndex
    {
        get => audioCodecIndex;
        set => SetProperty(ref audioCodecIndex, value);
    }

    /// <summary>
    ///     Selected audio codec
    /// </summary>
    [BsonIgnore]
    public AudioVideoCodec? AudioCodec
    {
        get => audioCodec;
        set => SetProperty(ref audioCodec, value);
    }

    /// <summary>
    ///     Video codec index
    /// </summary>
    public int VideoCodecIndex
    {
        get => videoCodecIndex;
        set => SetProperty(ref videoCodecIndex, value);
    }

    /// <summary>
    ///     Selected video codec
    /// </summary>
    [BsonIgnore]
    public AudioVideoCodec? VideoCodec
    {
        get => videoCodec;
        set => SetProperty(ref videoCodec, value);
    }

    /// <summary>
    ///     Selected extension
    /// </summary>
    [BsonIgnore]
    public string Extension
    {
        get => extension;
        set
        {
            SetProperty(ref extension, value);
            NotifyOutputChanged();
        }
    }

    /// <summary>
    ///     Selected extension
    /// </summary>
    public int ExtensionIndex
    {
        get => extensionIndex;
        set => SetProperty(ref extensionIndex, value);
    }


    /// <summary>
    ///     Whether or not the video bitrate should be set to try and achieve a specific output size
    /// </summary>
    public bool UseTargetSize
    {
        get => useTargetSize;
        set
        {
            SetProperty(ref useTargetSize, value);
            OnPropertyChanged(nameof(VideoBitrate));
            OnPropertyChanged(nameof(TwoPassEncoding));
        }
    }

    /// <summary>
    ///     Media output target size in megabytes. It is important that the full media including all the size of the video,
    ///     audio and container remains at or below this target size as users use it mostly to upload with upload size maximums
    ///     (e.g Discord).
    /// </summary>
    public double OutputTargetSize
    {
        get => outputTargetSize;
        set
        {
            SetProperty(ref outputTargetSize, value);
            OnPropertyChanged(nameof(VideoBitrate));
        }
    }

    /// <summary>
    ///     Whether or not to use two pass encoding, currently this is only required to reach specific target sizes and the
    ///     user has no other control over whether or not two pass encoding is used
    /// </summary>
    public bool TwoPassEncoding => UseTargetSize;

    #endregion
}
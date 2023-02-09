using LiteDB;

namespace Clipple.ViewModel;

public partial class Clip
{
    #region Members

    private ClipPresetViewModel? preset;
    private int                  presetIndex = -1;

    #endregion

    #region Properties

    /// <summary>
    ///     Current transcoding preset
    /// </summary>
    [BsonIgnore]
    public ClipPresetViewModel? Preset
    {
        get => preset;
        set
        {
            if (value != null)
            {
                VideoBitrate     = value.VideoBitrate ?? videoBitrate;
                TargetWidth      = value.TargetWidth ?? targetWidth;
                TargetHeight     = value.TargetHeight ?? targetHeight;
                TargetFps        = value.FPS ?? targetFps;
                //VideoCodec       = value.VideoCodec ?? videoCodec;
                //AudioCodec       = value.AudioCodec ?? audioCodec;
                AudioBitrate     = value.AudioBitrate ?? audioBitrate;
                UseTargetSize    = value.UseTargetSize;
                OutputTargetSize = value.TargetSize ?? outputTargetSize;
                ShouldCrop       = value.ShouldCrop;
                CropX            = value.CropX ?? cropX;
                CropY            = value.CropY ?? cropY;
                CropWidth        = value.CropWidth ?? cropWidth;
                CropHeight       = value.CropHeight ?? cropHeight;

                if (value.OutputFormat != null)
                {
                    ContainerFormat      = value.OutputFormat;
                    //OutputFormatIndex = MediaFormat.SupportedFormats.IndexOf(value.OutputFormat);
                }
            }

            SetProperty(ref preset, value);
            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     Index of transcoding preset, for serialization
    /// </summary>
    public int PresetIndex
    {
        get => presetIndex;
        set => SetProperty(ref presetIndex, value);
    }

    #endregion
}
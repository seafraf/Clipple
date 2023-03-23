namespace Clipple.ViewModel;

public partial class Clip
{
    #region Members

    private int presetIndex = -1;

    #endregion

    #region Properties

    /// <summary>
    ///     Index of transcoding preset, for serialization
    /// </summary>
    public int PresetIndex
    {
        get => presetIndex;
        set => SetProperty(ref presetIndex, value);
    }

    #endregion

    #region Methods

    public void ApplyPreset(Media media, ClipPreset preset, bool first = false)
    {
        VideoBitrate     = preset.VideoBitrate ?? DefaultVideoBitrate;
        AudioBitrate     = preset.AudioBitrate ?? DefaultAudioBitrate;
        OutputTargetSize = preset.TargetSize ?? DefaultOutputTargetSize;

        TargetWidth  = preset.TargetWidth ?? media.VideoWidth ?? -1;
        TargetHeight = preset.TargetHeight ?? media.VideoHeight ?? -1;
        if (preset.TargetWidth != null || preset.TargetHeight != null)
            UseSourceResolution = false;

        TargetFps = preset.Fps ?? media.VideoFps ?? -1;
        if (preset.Fps != null)
            UseSourceFps = false;

        CropWidth  = preset.CropWidth ?? media.VideoWidth ?? -1;
        CropHeight = preset.CropHeight ?? media.VideoHeight ?? -1;

        ContainerFormatIndex = preset.ContainerFormatIndex;
        VideoCodecIndex      = preset.VideoCodecIndex ?? -1;
        AudioCodecIndex      = preset.AudioCodecIndex ?? -1;

        UseTargetSize = preset.UseTargetSize ?? default;
        ShouldCrop    = preset.ShouldCrop ?? default;
        CropX         = preset.CropX ?? default;
        CropY         = preset.CropY ?? default;

        if (first)
        {
            PresetIndex = App.ViewModel.ClipPresetCollection.Presets.IndexOf(preset);
            InitialiseEncoder();
        }

        if (preset.Extension is not { } ext) 
            return;
        
        ExtensionIndex = ContainerFormat.Extensions.IndexOf(ext);
        Extension      = ExtensionIndex == -1 ? ContainerFormat.Extension : ext;
    }

    #endregion
}
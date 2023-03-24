using LiteDB;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Clipple.ViewModel;

public class ClipPreset : ObservableObject
{
    [BsonCtor]
    public ClipPreset(string name, string category, int containerFormatIndex, int? videoCodecIndex, int? audioCodecIndex)
    {
        this.name                 = name;
        this.category             = category;
        this.containerFormatIndex = containerFormatIndex;
        this.videoCodecIndex      = videoCodecIndex;
        this.audioCodecIndex      = audioCodecIndex;
    }

    #region Members

    private string name;
    private string category;
    private long   priority;
    private int    containerFormatIndex;
    private int?   videoCodecIndex;
    private int?   audioCodecIndex;

    private long?   videoBitrateMinOffset;
    private long?   videoBitrateMaxOffset;
    private long?   videoBitrate;
    private long?   audioBitrate;
    private int?    targetWidth;
    private int?    targetHeight;
    private int?    fps;
    private bool?   useTargetSize;
    private double? targetSize;
    private bool?   shouldCrop;
    private int?    cropX;
    private int?    cropY;
    private int?    cropWidth;
    private int?    cropHeight;
    private string? extension;
    private string? extraOptions;

    #endregion

    #region Properties

    public string Name
    {
        get => name;
        set => SetProperty(ref name, value);
    }

    public string Category
    {
        get => category;
        set => SetProperty(ref category, value);
    }

    public long Priority
    {
        get => priority;
        set => SetProperty(ref priority, value);
    }
    
    public long? VideoBitrateMinOffset
    {
        get => videoBitrateMinOffset;
        set => SetProperty(ref videoBitrateMinOffset, value);
    }
    
    public long? VideoBitrateMaxOffset
    {
        get => videoBitrateMaxOffset;
        set => SetProperty(ref videoBitrateMaxOffset, value);
    }
    
    public long? VideoBitrate
    {
        get => videoBitrate;
        set => SetProperty(ref videoBitrate, value);
    }

    public long? AudioBitrate
    {
        get => audioBitrate;
        set => SetProperty(ref audioBitrate, value);
    }

    public int? TargetWidth
    {
        get => targetWidth;
        set => SetProperty(ref targetWidth, value);
    }

    public int? TargetHeight
    {
        get => targetHeight;
        set => SetProperty(ref targetHeight, value);
    }

    public int? Fps
    {
        get => fps;
        set => SetProperty(ref fps, value);
    }

    public bool? UseTargetSize
    {
        get => useTargetSize;
        set => SetProperty(ref useTargetSize, value);
    }

    public double? TargetSize
    {
        get => targetSize;
        set => SetProperty(ref targetSize, value);
    }

    public bool? ShouldCrop
    {
        get => shouldCrop;
        set => SetProperty(ref shouldCrop, value);
    }

    public int? CropX
    {
        get => cropX;
        set => SetProperty(ref cropX, value);
    }

    public int? CropY
    {
        get => cropY;
        set => SetProperty(ref cropY, value);
    }

    public int? CropWidth
    {
        get => cropWidth;
        set => SetProperty(ref cropWidth, value);
    }

    public int? CropHeight
    {
        get => cropHeight;
        set => SetProperty(ref cropHeight, value);
    }

    public int? VideoCodecIndex
    {
        get => videoCodecIndex;
        set => SetProperty(ref videoCodecIndex, value);
    }

    public int? AudioCodecIndex
    {
        get => audioCodecIndex;
        set => SetProperty(ref audioCodecIndex, value);
    }

    public int ContainerFormatIndex
    {
        get => containerFormatIndex;
        set => SetProperty(ref containerFormatIndex, value);
    }

    public string? Extension
    {
        get => extension;
        set => SetProperty(ref extension, value);
    }

    public string? ExtraOptions
    {
        get => extraOptions;
        set => SetProperty(ref extraOptions, value);
    }
    #endregion
}
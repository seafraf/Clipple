using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using Clipple.Types;
using LiteDB;

namespace Clipple.ViewModel;

public partial class Clip
{
#region Methods

    private void InitialiseVideoViews()
    {
        ResolutionPresets.GroupDescriptions?.Clear();
        ResolutionPresets.GroupDescriptions?.Add(new PropertyGroupDescription("AspectRatioString"));
    }

    #endregion
    
#region Constants
    private const long DefaultVideoBitrate = 60000;
#endregion
    
    #region Members

    private long              videoBitrate = DefaultVideoBitrate;
    private int               targetFps;
    private int               targetWidth;
    private int               targetHeight;
    private bool              useSourceFps = true;
    private bool              shouldCrop;
    private int               cropX;
    private int               cropY;
    private int               cropWidth;
    private int               cropHeight;
    private ResolutionPreset? resolutionPreset;

    #endregion

    #region Properties

    /// <summary>
    ///     Video bitrate, kilobits/second.
    ///     If UseTargetSize is true, this will return the bitrate required to reach VideoTargetSize
    /// </summary>
    public long VideoBitrate
    {
        get
        {
            if (!UseTargetSize)
                return videoBitrate;

            // Total bitrate the whole media can take up
            var maxTotalBitrate = (long)(OutputTargetSize * 8000.0 / Duration.TotalSeconds);

            // Subtract audio from total bitrate, as that audio bitrate won't be changed when UseTargetSize is true
            // This has to be done for each enabled audio channel
            var enabledTracks = AudioSettings.Count(x => x.IsEnabled);

            // Merging audio tracks means there is only one.. unless no audio tracks are enabled, in which case there is zero
            maxTotalBitrate -= AudioBitrate * Math.Min(enabledTracks, MergeAudio ? 1 : enabledTracks);

            // subtract 1% for muxing overhead
            return (long)(maxTotalBitrate * 0.99);
        }
        set => SetProperty(ref videoBitrate, value);
    }

    /// <summary>
    ///     Video FPS
    /// </summary>
    public int TargetFps
    {
        get => targetFps;
        set => SetProperty(ref targetFps, value);
    }

    /// <summary>
    ///     Video width
    /// </summary>
    public int TargetWidth
    {
        get => targetWidth;
        set
        {
            ResolutionPreset = null;
            SetProperty(ref targetWidth, value);
        }
    }

    /// <summary>
    ///     Video height
    /// </summary>
    public int TargetHeight
    {
        get => targetHeight;
        set
        {
            ResolutionPreset = null;
            SetProperty(ref targetHeight, value);
        }
    }

    /// <summary>
    ///     Should the source video resolution be used in the the output clip?
    /// </summary>
    private bool useSourceResolution = true;

    public bool UseSourceResolution
    {
        get => useSourceResolution;
        set
        {
            SetProperty(ref useSourceResolution, value);
            OnPropertyChanged(nameof(TargetWidth));
            OnPropertyChanged(nameof(TargetHeight));
        }
    }

    /// <summary>
    ///     Should the source video FPS be used in the output clip?
    /// </summary>
    public bool UseSourceFps
    {
        get => useSourceFps;
        set
        {
            SetProperty(ref useSourceFps, value);
            OnPropertyChanged(nameof(TargetFps));
        }
    }

    /// <summary>
    ///     Should the video be cropped according to the crop(x/y/width/height) settings?
    /// </summary>

    public bool ShouldCrop
    {
        get => shouldCrop;
        set => SetProperty(ref shouldCrop, value);
    }

    /// <summary>
    ///     Crop x-position
    /// </summary>

    public int CropX
    {
        get => cropX;
        set => SetProperty(ref cropX, value);
    }

    /// <summary>
    ///     Crop y-position
    /// </summary>

    public int CropY
    {
        get => cropY;
        set => SetProperty(ref cropY, value);
    }

    /// <summary>
    ///     Crop width
    /// </summary>

    public int CropWidth
    {
        get => cropWidth;
        set => SetProperty(ref cropWidth, value);
    }


    /// <summary>
    ///     Crop y-position
    /// </summary>

    public int CropHeight
    {
        get => cropHeight;
        set => SetProperty(ref cropHeight, value);
    }

    /// <summary>
    ///     List of resolution presets for the resolution preset checkbox
    /// </summary>
    [BsonIgnore]
    public ListCollectionView ResolutionPresets { get; } = new(new ObservableCollection<ResolutionPreset>
                                                               {
                                                                   // 32:9
                                                                   new(32, 9, 5120, 1440),
                                                                   new(32, 9, 3840, 1080),

                                                                   // 21:9
                                                                   new(21, 9, 5120, 2160),
                                                                   new(21, 9, 3440, 1440),

                                                                   // 16:9
                                                                   new(16, 9, 7680, 4320),
                                                                   new(16, 9, 5120, 2880),
                                                                   new(16, 9, 3840, 2160),
                                                                   new(16, 9, 2560, 1440),
                                                                   new(16, 9, 1920, 1080),
                                                                   new(16, 9, 1600, 900),
                                                                   new(16, 9, 1366, 768),
                                                                   new(16, 9, 1280, 720),
                                                                   new(16, 9, 7680, 4320),
                                                                   new(16, 9, 7680, 4320),

                                                                   // 16:10
                                                                   new(16, 10, 2560, 1600),
                                                                   new(16, 10, 1920, 1200),
                                                                   new(16, 10, 1280, 800),

                                                                   // 4:3
                                                                   new(4, 3, 2048, 1536),
                                                                   new(4, 3, 1920, 1440),
                                                                   new(4, 3, 1600, 1200),
                                                                   new(4, 3, 1440, 1080),
                                                                   new(4, 3, 1400, 1050)
                                                               });

    /// <summary>
    ///     Resolution preset set by the resolution preset combo box.  Setting this will update TargetWidth and TargetHeight
    /// </summary>
    [BsonIgnore]
    public ResolutionPreset? ResolutionPreset
    {
        get => resolutionPreset;
        set
        {
            SetProperty(ref resolutionPreset, value);

            if (value == null) return;

            targetWidth  = value.ScreenW;
            targetHeight = value.ScreenH;

            OnPropertyChanged(nameof(TargetWidth));
            OnPropertyChanged(nameof(TargetHeight));
        }
    }
#endregion
}
using Clipple.Types;
using FFmpeg.AutoGen;
using LiteDB;
using System.Text.Json.Serialization;

namespace Clipple.ViewModel;

public partial class Media
{
    #region 
    private int? videoWidth = -1;
    private int? videoHeight = -1;
    private int? videoFps = -1;
    private AVCodecID? videoCodecID;
    #endregion

    #region Properties
    /// <summary>
    ///     Video width in pixels
    /// </summary>
    public int? VideoWidth
    {
        get => videoWidth;
        set => SetProperty(ref videoWidth, value);
    }

    /// <summary>
    ///     Video height in pixels
    /// </summary>
    public int? VideoHeight
    {
        get => videoHeight;
        set => SetProperty(ref videoHeight, value);
    }

    /// <summary>
    ///     Rounded video FPS
    /// </summary>
    public int? VideoFps
    {
        get => videoFps;
        set => SetProperty(ref videoFps, value);
    }

    /// <summary>
    ///     Video encoding ID.
    /// </summary>
    public AVCodecID? VideoCodecID
    {
        get => videoCodecID;
        set => SetProperty(ref videoCodecID, value);
    }

    /// <summary>
    ///     Video encoder name.  Only set if the media has video.
    /// </summary>
    [BsonIgnore]
    public string? VideoCodecName
    {
        get
        {
            var id = VideoCodecID;
            if (id == null)
                return null;

            return ffmpeg.avcodec_get_name((AVCodecID)id);
        }
    }

    /// <summary>
    ///     Helper function.  Returns the video's resolution by combining width and height.
    /// </summary>
    [BsonIgnore]
    public string VideoResolution => $"{VideoWidth}x{VideoHeight}";
    #endregion
}
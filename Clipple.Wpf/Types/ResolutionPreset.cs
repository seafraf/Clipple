namespace Clipple.Types;

public class ResolutionPreset
{
    public ResolutionPreset(int ratioW, int ratioH, int screenW, int screenH)
    {
        RatioW  = ratioW;
        RatioH  = ratioH;
        ScreenW = screenW;
        ScreenH = screenH;
    }

    #region Properties

    public int RatioW  { get; }
    public int RatioH  { get; }
    public int ScreenW { get; }
    public int ScreenH { get; }

    public string AspectRatioString => $"{RatioW}:{RatioH}";
    public string ResolutionString  => $"{ScreenW}x{ScreenH}";

    #endregion

    public override string? ToString()
    {
        return $"{ScreenW}x{ScreenH}";
    }
}
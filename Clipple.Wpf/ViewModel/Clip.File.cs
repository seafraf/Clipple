namespace Clipple.ViewModel;

public partial class Clip
{
    #region Member
    private bool deleteSourceVideo;
    private bool copySourceTimestamp;
    #endregion
 
    #region Properties
    public bool DeleteSourceVideo
    {
        get => deleteSourceVideo;
        set => SetProperty(ref deleteSourceVideo, value);
    }
    
    public bool CopySourceTimestamp
    {
        get => copySourceTimestamp;
        set => SetProperty(ref copySourceTimestamp, value);
    }
    #endregion
}
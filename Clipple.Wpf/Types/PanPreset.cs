namespace Clipple.Types;

public class PanPreset
{
    public PanPreset(string name, string category, string channelMapping, string channelLayout)
    {
        Name           = name;
        Category       = category;
        ChannelMapping = channelMapping;
        ChannelLayout  = channelLayout;
    }

    #region Properties

    public string Name           { get; }
    public string Category       { get; }
    public string ChannelMapping { get; }
    public string ChannelLayout  { get; }

    #endregion

    public override string? ToString()
    {
        return Name;
    }
}
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using Clipple.ViewModel;

namespace Clipple.Types;

public class ClipPresetCollection
{
    public ClipPresetCollection(ContainerFormatCollection containerFormatCollection)
    {
        LoadDefault(containerFormatCollection);
        
        var view = CollectionViewSource.GetDefaultView(Presets);
        view.GroupDescriptions.Clear();
        view.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new("Priority", ListSortDirection.Ascending));
    }

    /// <summary>
    /// Load built in default presets
    /// </summary>
    private void LoadDefault(ContainerFormatCollection containerFormatCollection)
    {
        var defaultContainerIndex = containerFormatCollection.SupportedFormats.FindIndex(x => x.Names.Contains("mp4"));
        if (defaultContainerIndex == -1)
            throw new NotSupportedException("mp4 not supported by ffmpeg");

        var defaultContainer = containerFormatCollection.SupportedFormats[defaultContainerIndex];

        var defaultVideoCodecIndex = defaultContainer.VideoCodecs.FindIndex(x => x.Name == "libx264");
        if (defaultVideoCodecIndex == -1)
            throw new NotSupportedException("h264 not supported by ffmpeg");
        
        var defaultAudioCodecIndex = defaultContainer.AudioCodecs.FindIndex(x => x.Name == "aac");
        if (defaultAudioCodecIndex == -1)
            throw new NotSupportedException("aac not supported by ffmpeg");
        
        Preset2160 = AddPreset(new("2160p, 60fps", "Recommended", defaultContainerIndex, defaultVideoCodecIndex, defaultAudioCodecIndex)
        {
            VideoBitrate = 45000,
            Extension = "mp4"
        });
        
        Preset1440 = AddPreset(new("1440p, 60fps", "Recommended", defaultContainerIndex, defaultVideoCodecIndex, defaultAudioCodecIndex)
        {
            VideoBitrate = 16000,
            Extension    = "mp4"
        });
        
        Preset1080 = AddPreset(new("1080p, 60fps", "Recommended", defaultContainerIndex, defaultVideoCodecIndex, defaultAudioCodecIndex)
        {
            VideoBitrate = 8000,
            Extension    = "mp4"
        }); 
        
        Preset720 = AddPreset(new("720p, 60fps", "Recommended", defaultContainerIndex, defaultVideoCodecIndex, defaultAudioCodecIndex)
        {
            VideoBitrate = 5000,
            Extension    = "mp4"
        });
        
        AddPreset(new("Video, 100MB", "Discord Nitro", defaultContainerIndex, defaultVideoCodecIndex, defaultAudioCodecIndex)
        {
            UseTargetSize = true,
            TargetSize = 100.0
        });
        
        AddPreset(new("Video, 50MB", "Discord Nitro", defaultContainerIndex, defaultVideoCodecIndex, defaultAudioCodecIndex)
        {
            UseTargetSize = true,
            TargetSize    = 50.0
        });
        
        AddPreset(new("Video, 8MB", "Discord", defaultContainerIndex, defaultVideoCodecIndex, defaultAudioCodecIndex)
        {
            UseTargetSize = true,
            TargetSize    = 8.0
        });
        
        LoadAudioPreset(containerFormatCollection);
        LoadGifPreset(containerFormatCollection);
    }

    private void LoadAudioPreset(ContainerFormatCollection containerFormatCollection)
    {
        var audioContainerIndex = containerFormatCollection.SupportedFormats.FindIndex(x => x.Names.Contains("mp3"));
        if (audioContainerIndex == -1)
            throw new NotSupportedException("mp4 not supported by ffmpeg");

        var defaultContainer = containerFormatCollection.SupportedFormats[audioContainerIndex];

        var lameCodecIndex = defaultContainer.AudioCodecs.FindIndex(x => x.Name == "libmp3lame");
        if (lameCodecIndex == -1)
            throw new NotSupportedException("libmp3lame not supported by ffmpeg");

        PresetAudio = AddPreset(new("Audio", "Discord", audioContainerIndex, null, lameCodecIndex)
        {
            AudioBitrate = 640,
        });
    }

    private void LoadGifPreset(ContainerFormatCollection containerFormatCollection)
    {
        var gifContainerIndex = containerFormatCollection.SupportedFormats.FindIndex(x => x.Names.Contains("gif"));
        if (gifContainerIndex == -1)
            throw new NotSupportedException("gif not supported by ffmpeg");

        var defaultContainer = containerFormatCollection.SupportedFormats[gifContainerIndex];

        var gifCodecIndex = defaultContainer.VideoCodecs.FindIndex(x => x.Name == "gif");
        if (gifCodecIndex == -1)
            throw new NotSupportedException("gif not supported by ffmpeg");

        AddPreset(new("GIF", "Discord", gifContainerIndex, gifCodecIndex, null)
        {
            Fps          = 24,
            TargetWidth  = 1280,
            TargetHeight = 720
        });
    }

    /// <summary>
    /// Load custom user presets
    /// </summary>
    private void LoadCustom()
    {
        
    }

    private ClipPreset AddPreset(ClipPreset preset)
    {
        preset.Priority = Presets.Count;
        Presets.Add(preset);

        return preset;
    }

    #region Properties
    public ObservableCollection<ClipPreset> Presets { get; } = new();

    public ClipPreset? Preset720 { get; private set; }
    public ClipPreset? Preset1080 { get; private set; }
    public ClipPreset? Preset1440 { get; private set; }
    public ClipPreset? Preset2160 { get; private set; }
    
    public ClipPreset? PresetAudio { get; private set; }
    #endregion
}
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Input;

namespace Clipple.ViewModel;

public partial class Clip
{
#region Constants
    private const long DefaultAudioBitrate = 320;
#endregion

#region Members
    private bool                                      mergeAudio    = true;
    private ObservableCollection<AudioStreamSettings> audioSettings = new();
    private long                                      audioBitrate  = DefaultAudioBitrate;
#endregion

#region Properties
    /// <summary>
    ///     Whether or not all audio streams should be merged into one audio stream, this is forced on
    ///     for output formats that only include audio
    /// </summary>
    public bool MergeAudio
    {
        get => mergeAudio || !ContainerFormat.SupportsVideo;
        set
        {
            SetProperty(ref mergeAudio, value);

            if (UseTargetSize)
                OnPropertyChanged(nameof(VideoBitrate));
        }
    }

    /// <summary>
    ///     Settings for each input audio stream
    /// </summary>
    public ObservableCollection<AudioStreamSettings> AudioSettings
    {
        get => audioSettings;
        set
        {
            foreach (var audioSetting in value)
            {
                audioSetting.PropertyChanged -= OnAudioSettingPropertyChanged;
                audioSetting.PropertyChanged += OnAudioSettingPropertyChanged;
            }

            SetProperty(ref audioSettings, value);
        }
    }

    /// <summary>
    ///     Audio bitrate, kilobits/second.
    /// </summary>
    public long AudioBitrate
    {
        get => audioBitrate;
        set
        {
            SetProperty(ref audioBitrate, value);

            if (UseTargetSize)
                OnPropertyChanged(nameof(VideoBitrate));
        }
    }
#endregion

#region Methods
    private void OnAudioSettingPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (!UseTargetSize)
            return;

        // If UseTargetSize is true, the number of enabled audio streams changes the maximum video bitrate
        if (e.PropertyName == nameof(AudioStreamSettings.IsEnabled))
            OnPropertyChanged(nameof(VideoBitrate));
    }
#endregion
}
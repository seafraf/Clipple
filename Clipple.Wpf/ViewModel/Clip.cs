using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using LiteDB;
using Microsoft.Toolkit.Mvvm.ComponentModel;
namespace Clipple.ViewModel;

public partial class Clip : ObservableObject, INotifyDataErrorInfo
{
#pragma warning disable CS8618
    /// <summary>
    ///     Called during deserialization only.
    /// </summary>
    [BsonCtor]
    public Clip()
#pragma warning restore CS8618
    {
        InitialiseVideoViews();

        // Events
        AudioSettings.CollectionChanged += OnAudioSettingsChanged;
    }

    public Clip(Media media) :
        this()
    {
        StartTime = TimeSpan.Zero;
        Duration  = media.Duration;

        TargetFps    = media.VideoFps ?? -1;
        TargetWidth  = media.VideoWidth ?? -1;
        TargetHeight = media.VideoHeight ?? -1;

        CropWidth  = media.VideoWidth ?? -1;
        CropHeight = media.VideoHeight ?? -1;

        FileName = media.Id.ToString();

        foreach (var audioStream in media.AudioStreams)
            AudioSettings.Add(new(audioStream.Name, audioStream.StreamIndex, audioStream.AudioStreamIndex, audioStream.CodecID));
    }

    #region Methods
    public void Initialise(Media media)
    {
        InitialiseEncoder();

        foreach (var audio in AudioSettings)
        {
            foreach (var filter in audio.AudioFilters)
                filter.Initialise();
        }

        if (PresetIndex != -1)
            return;
                
        // Find best default preset for video and audio clips
        var cpc = App.ViewModel.ClipPresetCollection;
        if (media.HasVideo)
        {
            // Find closest preset vertical resolution
            var videoPresets = new[]
            {
                (cpc.Preset2160, 2160),
                (cpc.Preset1440, 1440),
                (cpc.Preset1080, 1080),
                (cpc.Preset720, 720),
            };

            foreach (var (preset, resolution) in videoPresets)
            {
                if (preset == null || !(media.VideoHeight >= resolution)) 
                    continue;
                
                ApplyPreset(media, preset, true);
                break;
            }
        }
        else if (media.HasAudio && cpc.PresetAudio is { } preset)
            ApplyPreset(media, preset, true);
    }

    public void NotifyOutputChanged()
    {
        OnPropertyChanged(nameof(FullFileName));
        OnPropertyChanged(nameof(Uri));
        OnPropertyChanged(nameof(FileNameExists));
    }
    #endregion

    #region Members

    private TimeSpan              startTime;
    private TimeSpan              duration;
    private string                fileName;

    #endregion

    #region Properties

    /// <summary>
    ///     Clip start time
    /// </summary>
    public TimeSpan StartTime
    {
        get => startTime;
        set
        {
            SetProperty(ref startTime, value);
            OnPropertyChanged(nameof(EndTime));

            if (UseTargetSize)
                OnPropertyChanged(nameof(VideoBitrate));
        }
    }

    /// <summary>
    ///     Helper property.  The time that this clip ends
    /// </summary>
    [BsonIgnore]
    public TimeSpan EndTime
    {
        get => StartTime + Duration;
        set
        {
            Duration = value - StartTime;

            OnPropertyChanged();
        }
    }

    /// <summary>
    ///     Duration of the clip
    /// </summary>
    public TimeSpan Duration
    {
        get => duration;
        set
        {
            SetProperty(ref duration, value);
            OnPropertyChanged(nameof(EndTime));

            if (UseTargetSize)
                OnPropertyChanged(nameof(VideoBitrate));
        }
    }

    /// <summary>
    ///     Clip file name, not including the extension
    /// </summary>
    public string FileName
    {
        get => fileName;
        set
        {
            SetProperty(ref fileName, value);
            NotifyOutputChanged();
        }
    }

    /// <summary>
    ///     Returns a the full file name including extension for this clip
    /// </summary>
    [BsonIgnore]
    public string FullFileName => $"{Path.Combine(App.ViewModel.Settings.DefaultOutputFolder, FileName)}.{Extension}";

    /// <summary>
    ///     Full file name
    /// </summary>
    [BsonIgnore]
    public Uri Uri => new(FullFileName);

    /// <summary>
    /// Whether or not FullFileName points to a file that exists
    /// </summary>
    [BsonIgnore]
    public bool FileNameExists => File.Exists(FullFileName);
    #endregion

    #region INotifyDataErrorInfo implementation

    /// <summary>
    ///     Property-keyed dictionary containing list of errors for each propety
    /// </summary>
    private readonly Dictionary<string, List<string>> propertyErrors = new();

    /// <summary>
    ///     True if propertyErrors contains any entries
    /// </summary>
    [BsonIgnore]
    public bool HasErrors => propertyErrors.Any();

    /// <summary>
    ///     Implementation
    /// </summary>
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

    public IEnumerable GetErrors(string? propertyName)
    {
#pragma warning disable CS8603 // Possible null reference return.
        if (propertyName == null)
            return null;

        return propertyErrors.ContainsKey(propertyName) ? propertyErrors[propertyName] : null;
#pragma warning restore CS8603 // Possible null reference return.
    }

    /// <summary>
    ///     Add an error for a property
    /// </summary>
    /// <param name="propertyName">The property's name</param>
    /// <param name="error">The error to add</param>
    private void AddError(string propertyName, string error)
    {
        if (!propertyErrors.ContainsKey(propertyName))
            propertyErrors.Add(propertyName, new List<string>());

        if (!propertyErrors[propertyName].Contains(error))
        {
            propertyErrors[propertyName].Add(error);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

            OnPropertyChanged(nameof(HasErrors));
        }
    }

    private void ClearErrors(string propertyName)
    {
        if (propertyErrors.ContainsKey(propertyName) && propertyErrors[propertyName].Count > 0)
        {
            propertyErrors.Remove(propertyName);
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));

            OnPropertyChanged(nameof(HasErrors));
        }
    }

    #endregion

    #region Event handlers

    private void OnAudioSettingsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems == null)
            return;

        foreach (var audioSetting in e.NewItems)
        {
            ((AudioStreamSettings)audioSetting).PropertyChanged -= OnAudioSettingPropertyChanged;
            ((AudioStreamSettings)audioSetting).PropertyChanged += OnAudioSettingPropertyChanged;
        }
    }
#endregion
}
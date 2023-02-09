using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Automation.Peers;
using System.Windows.Forms;
using System.Windows.Input;
using Clipple.View;
using LiteDB;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

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
        PropertyChanged                 += OnPropertyChanged;
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

        // Select default output format
        ContainerFormatIndex = 6; // mp4

        if (media.AudioStreams == null)
            return;

        foreach (var audioStream in media.AudioStreams)
            AudioSettings.Add(new AudioStreamSettings(audioStream.Name, audioStream.StreamIndex, audioStream.AudioStreamIndex, audioStream.CodecID));
    }

    #region Methods
    public void Initialise()
    {
        InitialiseEncoder();

        foreach (var audioSettings in AudioSettings)
        {
            foreach (var filter in audioSettings.AudioFilters)
                filter.Initialise();
        }
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
        }
    }

    /// <summary>
    ///     Returns a the full file name including extension for this clip
    /// </summary>
    [BsonIgnore]
    public string FullFileName
    {
        get
        {
            return $"{Path.Combine(App.ViewModel.Settings.DefaultOutputFolder, FileName)}.{Extension}";
        }
    }

    /// <summary>
    ///     Full file name
    /// </summary>
    [BsonIgnore]
    public Uri Uri => new(FullFileName);
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

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // If any of the properties set by transcoding presets change, unselect the transcoding preset
        // to indicate the current settings don't reflect the previously selected preset any more
        if (e.PropertyName is "VideoBitrate" or "TargetWidth" or "TargetHeight" or "TargetFPS" or "VideoCodec" or "AudioBitrate" or "AudioBitrate" or "UseTargetSize" or "OutputTargetSize")
            Preset = null;
    }

    #endregion
}
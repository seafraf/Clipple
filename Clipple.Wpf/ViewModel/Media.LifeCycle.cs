using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Clipple.ViewModel;

/// <summary>
/// Properties and methods in this file manage the life cycle of a media instance.
/// 
/// The life cycle starts when media is imported into clipple.  Importing media:
/// - Calculates and caches:
///     1. Waveforms for each audio stream in the media, if applicable
///     2. Stream information
///     
/// After importing, various persistent data can change:
/// - Media player settings (play position, volume, filters, etc..)
/// - Clip settings (start, duration, encoding settings, etc..)
/// - Tags and other library management info
/// 
/// Media can then be removed from Clipple which:
/// - Will delete cached data, such as the waveforms
/// - Can optionally delete the source file from the disk
/// 
/// The library requires events from this life cycle to keep the most up to date media 
/// settings in a database.  The properties, methods and events in this class should 
/// give the library:
/// - Notifications when persistent data has changed 
///     - RATE LIMITED! MediaRequestUpdate should not be called more than once a second
/// - Notifications when media should be removed
/// </summary>
public partial class Media
{
    #region Events
    public event EventHandler? MediaRequestUpdate;
    public event EventHandler? MediaDirty;
    public event EventHandler<bool>? MediaRequestDelete;
    #endregion

    #region Members
    private DispatcherTimer? updateTimer;
    #endregion

    #region Methods
    private void InitialiseLifeCycle()
    {
        // Is this any better than actually making a null reference?
        if (Clip == null)
            throw new NullReferenceException();

        PropertyChanged += OnMediaPropertyChanged;
        Clip.PropertyChanged += OnClipPropertyChanged;
        Tags.CollectionChanged += OnTagsChanged;
        Clips.CollectionChanged += OnClipsChanged;
        
        foreach (var stream in AudioStreams)
            stream.PropertyChanged += OnAudioStreamPropertyChanged;

        foreach (var stream in Clip.AudioSettings)
            stream.PropertyChanged += OnClipAudioPropertyChanged;

        foreach (var tag in Tags)
            tag.PropertyChanged += TagPropertyChanged;

        updateTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        updateTimer.Tick += UpdateTimerTicker;
    }

    private void RequestUpdate()
    {
        if (updateTimer != null)
            updateTimer.IsEnabled = true;

        MediaDirty?.Invoke(this, new EventArgs());
    }

    public void RequestDelete(bool deleteFile)
    {
        MediaRequestDelete?.Invoke(this, deleteFile);
    }

    private void UpdateTimerTicker(object? sender, EventArgs e)
    {
        if (updateTimer != null)
            updateTimer.IsEnabled = false;

        MediaRequestUpdate?.Invoke(this, new EventArgs());
    }

    private void OnClipAudioPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) 
        => RequestUpdate();

    private void OnAudioStreamPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => RequestUpdate();

    private void TagPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => RequestUpdate();

    private void OnClipPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => RequestUpdate();

    private void OnMediaPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        => RequestUpdate();

    private void OnTagsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
        {
            foreach (Tag item in e.NewItems)
                item.PropertyChanged += TagPropertyChanged;
        }

        RequestUpdate();
    }

    private void OnClipsChanged(object? sender, NotifyCollectionChangedEventArgs e) => RequestUpdate();
#endregion
}


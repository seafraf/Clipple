using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Clipple.Types;
using Clipple.Util;
using Clipple.View;
using FFmpeg.AutoGen;
using LiteDB;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Clipple.ViewModel;

public partial class Media : ObservableObject
{
    [BsonCtor]
    public Media(string filePath)
    {
        fileInfo = new(filePath);
        FilePath = filePath;

        Clips = new();
    }

    /// <summary>
    /// Must be called after the media class has been constructed.
    /// </summary>
    public void Initialise()
    {
        if (!HasMediaInfo)
            GetMediaInfo();

        // Initialise/construct clip
        Clip ??= new(this);
        Clip.Initialise(this);

        // Initialise filters
        foreach (var audioStream in AudioStreams)
        {
            foreach (var filter in audioStream.AudioFilters)
                filter.Initialise();
        }

        // Post deserialization tasks
        Class = MediaClass.MediaClasses.ElementAtOrDefault(ClassIndex);

        // Set up events for life cycle management
        InitialiseLifeCycle();
    }

    /// <summary>
    /// Creates or loads from a cache; audio wave forms.
    /// </summary>
    /// <returns>Task</returns>
    public async Task BuildOrCacheResources()
    {
        // Generates waveforms for all audio streams, using cached audio waveforms if available
        await BuildAudioWaveforms();
    }

    /// <summary>
    /// Delete the cache directory and it's contents. 
    /// </summary>
    public void DeleteCache()
    {
        // Delete cached data (waveforms, folder)
        var dirInfo = new DirectoryInfo(CachePath);
        if (dirInfo.Exists)
            dirInfo.Delete(true);
    }

#region Methods

    private void CopyAudioStreamFilters(int audioStreamIndex, bool toMedia)
    {
        var a = AudioStreams?.Where(x => x.AudioStreamIndex == audioStreamIndex).FirstOrDefault();
        var b = Clip.AudioSettings?.Where(x => x.AudioStreamIndex == audioStreamIndex).FirstOrDefault();

        var source      = toMedia ? a : b;
        var destination = toMedia ? b : a;

        if (source == null || destination == null)
            return;

        for (int i = 0; i < source.AudioFilters.Count; i++)
        {
            var sourceFilter      = source.AudioFilters[i];
            var destinationFilter = destination.AudioFilters[i];

            sourceFilter?.CopyFrom(destinationFilter);
        }
    }

    /// <summary>
    /// Imports audio stream filters from the media to the media's clip.
    /// </summary>
    /// <param name="audioStreamIndex">The index of the stream whose filters to import from</param>
    private void ImportAudioStreamFilters(int audioStreamIndex)
    {
        CopyAudioStreamFilters(audioStreamIndex, false);
    }

    /// <summary>
    /// Exports audio stream filters from the media's clip to the media.
    /// </summary>
    /// <param name="audioStreamIndex">The index of the stream whose filters to export from</param>
    private void ExportAudioStreamFilters(int audioStreamIndex)
    {
        CopyAudioStreamFilters(audioStreamIndex, true);
    }
#endregion

#region Members

    private FileInfo                       fileInfo;
    private Clip?                          clip;
    private string?                        description;
    private int                            classIndex = 1; // raw by default
    private MediaClass?                    @class;
    private ObjectId?                      parentId;

#endregion

#region Properties

    /// <summary>
    ///     A reference to the media's file info. This can be used to determine the path, file size, etc
    /// </summary>
    [BsonIgnore]
    public FileInfo FileInfo
    {
        get => fileInfo;
        set
        {
            SetProperty(ref fileInfo, value);
            OnPropertyChanged(nameof(FileSize));
        }
    }

    /// <summary>
    ///     Clip settings
    /// </summary>
    public Clip? Clip
    {
        get => clip;
        set => SetProperty(ref clip, value);
    }

    /// <summary>
    /// User provided description of the media
    /// </summary>
    public string? Description
    {
        get => description;
        set => SetProperty(ref description, value);
    }

    /// <summary>
    /// User provided media class.  This is used for quickly organising media in the library.
    /// </summary>
    [BsonIgnore]
    public MediaClass? Class
    {
        get => @class;
        set => SetProperty(ref @class, value);
    }

    /// <summary>
    /// Class index in MediaClass.MediaClasses
    /// </summary>
    public int ClassIndex
    {
        get => classIndex;
        set => SetProperty(ref classIndex, value);
    }

    /// <summary>
    ///     Helper property.  Returns media file size in a human readable format.
    /// </summary>
    [BsonIgnore]
    public string FileSize => FileInfo.Exists ? Formatting.ByteCountToString(FileInfo.Length) : "0b";

    /// <summary>
    ///     Helper property. Returns the media's folder URI
    /// </summary>
    [BsonIgnore]
    public Uri? Uri => FileInfo?.FullName == null ? null : new Uri(FileInfo.FullName);

    /// <summary>
    ///     File path, used for serialization.
    /// </summary>
    public string FilePath { get; set; }

    /// <summary>
    ///     ID for this media, used in the database.  This is a unique ID.
    /// </summary>
    [BsonId]
    public ObjectId Id { get; set; } = ObjectId.NewObjectId();

    /// <summary>
    /// The ID of the media that produced this media.  This is not set for media that is imported.
    /// </summary>
    public ObjectId? ParentId
    {
        get => parentId;
        set => SetProperty(ref parentId, value);
    }

    /// <summary>
    /// Clips that this media has produced.
    /// </summary>
    public ObservableCollection<ObjectId> Clips { get; private set; }

    /// <summary>
    ///     Path used for caching data related to this media
    /// </summary>
    [BsonIgnore]
    private string CachePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Clipple", "cache", Id.ToString());

    #endregion


    #region Commands
    [BsonIgnore]
    public ICommand StartExportCommand => new RelayCommand(() =>
    {
        DialogHost.Show(new View.ExportingClip()
        {
            DataContext = new ExportingClip(this)
        }, "ExportClip");
    });

    [BsonIgnore]
    public ICommand ImportAudioStreamFiltersCommand => new RelayCommand<int>(ImportAudioStreamFilters);

    [BsonIgnore]
    public ICommand ExportAudioStreamFiltersCommand => new RelayCommand<int>(ExportAudioStreamFilters);

    [BsonIgnore]
    public ICommand AddTagCommand => new RelayCommand(AddNewTag);
    #endregion
}
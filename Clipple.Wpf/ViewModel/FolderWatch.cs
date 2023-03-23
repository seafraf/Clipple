using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using System.Windows;
using System.Windows.Input;
using Clipple.Types;
using LiteDB;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.Input;

namespace Clipple.ViewModel;

public class FolderWatch : AbstractTagContainer, IDisposable
{
    /// <summary>
    /// Copy another folder watch
    /// </summary>
    /// <param name="folderWatch">The other folder watch to copy from</param>
    public FolderWatch(FolderWatch folderWatch)
    {
        @class     = folderWatch.@class;
        ClassIndex = folderWatch.ClassIndex;
        Directory  = folderWatch.Directory;
        Filter     = folderWatch.Filter;
        
        foreach (var tag in folderWatch.Tags)
            Tags.Add(new(tag.Name, tag.Value));
    }
    
    /// <summary>
    /// Default folder watch
    /// </summary>
    public FolderWatch()
    {
        @class     = MediaClass.RawMedia;
        ClassIndex = MediaClass.MediaClasses.IndexOf(MediaClass.Automatic);
    }

    /// <summary>
    /// Constructor for deserialization, populates the class field
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="classIndex"></param>
    [BsonCtor]
    public FolderWatch(string directory, int classIndex)
    {
        Directory = directory;
        @class    = MediaClass.MediaClasses[classIndex];

        // Create file system watchers from deserialized folder watchers as they have been saved at some point
        if (DirectoryExists)
            CreateFileSystemWatcher();
    }

    #region Members
    private string             directory = string.Empty;
    private string             filter    = "*";
    private MediaClass         @class;
    private int                classIndex;
    private FileSystemWatcher? fileSystemWatcher;
    #endregion

    #region Properties
    public string Directory
    {
        get => directory;
        set
        {
            SetProperty(ref directory, value);
            OnPropertyChanged(nameof(DirectoryExists));
        }
    }

    public string Filter
    {
        get => filter;
        set => SetProperty(ref filter, value);
    }

    [BsonIgnore]
    public MediaClass Class
    {
        get => @class;
        set => SetProperty(ref @class, value);
    }

    public int ClassIndex
    {
        get => classIndex;
        set => SetProperty(ref classIndex, value);
    }

    [BsonIgnore]
    public bool DirectoryExists => System.IO.Directory.Exists(Directory);
    #endregion
    
    #region Events
    public event EventHandler<FolderWatch>? OnSave;
    #endregion

    #region Commands
    [BsonIgnore]
    public ICommand AddTagCommand => new RelayCommand(AddNewTag);
    
    [BsonIgnore]
    public ICommand ClearTagsCommand => new RelayCommand(ClearTags);

    [BsonIgnore]
    public ICommand? SaveCommand => new RelayCommand(() =>
    {
        // Create file system watcher for any saved file system watcher
        CreateFileSystemWatcher();
        
        DialogHost.Close(null);
        OnSave?.Invoke(this, this);
    });
    #endregion
    
    #region Methods
    public async Task<List<string>> FindMedia()
    {
        return await Task.Run(() =>
        {
            var files = System.IO.Directory.GetFiles(Directory, filter, SearchOption.AllDirectories);
            return files.Where(fileName =>
            {
                // Already imported
                if (App.ViewModel.Library.GetMediaByFilePath(fileName) is { })
                    return false;
                
                // Check if the file has a supported extension
                var extension = Path.GetExtension(fileName).TrimStart('.');
                return App.ContainerFormatCollection.SupportedExtensions.Contains(extension);
            }).ToList();
        });
    }

    private void CreateFileSystemWatcher()
    {
        if (fileSystemWatcher is { } old)
        {
            old.Created -= OnFileCreated;
            old.Dispose();
        }

        if (!DirectoryExists)
            return;
        
        fileSystemWatcher = new(Directory)
        {
            NotifyFilter          = NotifyFilters.FileName,
            EnableRaisingEvents   = true,
            IncludeSubdirectories = true
        };
        
        fileSystemWatcher.Created += OnFileCreated;
    }

    private async void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        
        if (App.ViewModel.Library.GetMediaByFilePath(e.FullPath) is { })
            return;
        
        var extension = Path.GetExtension(e.FullPath).TrimStart('.');
        if (!App.ContainerFormatCollection.SupportedExtensions.Contains(extension))
            return;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var opened = false;
        while (stopwatch.ElapsedMilliseconds < 10000 && !opened)
        {
            try
            {
                await using (File.Open(e.FullPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    opened = true;
                }
            }
            catch (IOException)
            {
                await Task.Delay(500);
            }
        }

        if (!opened)
        {
            App.ViewModel.Notifications.NotifyWarning("Timed out trying to import new file");
            return;
        }

        await Application.Current.Dispatcher.Invoke(async () =>
        {
            await App.ViewModel.Library.AddMedia(e.FullPath);
        });
    }
    
    public void Dispose()
    {
        ClearTags();
        
        if (fileSystemWatcher is not { } fsw)
            return;
        
        fsw.Created -= OnFileCreated;
        fsw.Dispose();
    }
    #endregion
}
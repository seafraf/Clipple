using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using Clipple.Types;
using Clipple.View;
using LiteDB;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Clipple.ViewModel;

public class Library : ObservableObject
{
    public Library()
    {
        Database = new(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Application.ProductName,
            "library.db"));

        var collection = Database.GetCollection<Media>();
        collection.EnsureIndex(x => x.FilePath, true);

        MediaSource = new();
        Media = new(MediaSource)
        {
            Filter = MediaFilter
        };

        MediaSource.CollectionChanged += (s, e) => { OnPropertyChanged(nameof(SearchHelp)); };
    }

    #region Members

    private          Media?          selectedMedia;
    private readonly HashSet<Media>  dirtyMedia        = new();
    private readonly HashSet<string> importingMedia    = new();
    private          string?         searchText        = string.Empty;
    private          string?         activeSearchText  = string.Empty;
    private          MediaFilterMode filterTagsMode    = MediaFilterMode.All;
    private          MediaFilterMode filterClassesMode = MediaFilterMode.Any;
    private          MediaFilterMode filterFormatsMode = MediaFilterMode.All;
    private          int             activeFilterCount;

    #endregion

    #region Properties

    private LiteDatabase Database { get; }

    /// <summary>
    ///     The source collection for the Media collection view
    /// </summary>
    private ObservableCollection<Media> MediaSource { get; }

    /// <summary>
    ///     All of the media in the library
    /// </summary>
    public ListCollectionView Media { get; }

    /// <summary>
    ///     The selected media entry.  Can be null if the Media list is empty
    /// </summary>
    public Media? SelectedMedia
    {
        get => selectedMedia;
        set
        {
            if (value != null)
                App.ViewModel.AppState.LibraryMediaId = value.Id;

            SetProperty(ref selectedMedia, value);
            OnPropertyChanged(nameof(SelectedMediaParentPath));
            OnPropertyChanged(nameof(SelectedMediaClipPaths));
        }
    }

    /// <summary>
    ///     The selected media's parent media's path.  This a helper property mapping the selected media's
    ///     parent ID to path, which is displayed under the "Parent path" field for the selected media.
    /// </summary>
    public string? SelectedMediaParentPath =>
        SelectedMedia is not { ParentId: { } id }
            ? null
            : Database.GetCollection<Media>().Query()
                .Where(x => x.Id == id)
                .Select(x => x.FilePath)
                .FirstOrDefault();

    /// <summary>
    ///     The selected media's clips as a list of their paths.  This is a helper property mapping the selected media's
    ///     list of produced clips to the path of those clips
    /// </summary>
    public List<string> SelectedMediaClipPaths
    {
        get
        {
            if (SelectedMedia is not { } media)
                return new();

            return Database.GetCollection<Media>().Query()
                .Where(x => media.Clips.Contains(x.Id))
                .Select(x => x.FilePath)
                .ToList();
        }
    }

    /// <summary>
    ///     Search phrase
    /// </summary>
    public string? SearchText
    {
        get => searchText;
        set
        {
            SetProperty(ref searchText, value);
            OnPropertyChanged(nameof(SearchHelp));

            // Only set to null by the clear button, so we can use this to apply filters instead of the requirement of pressing enter
            if (value == null)
                ApplyFilters();
        }
    }

    /// <summary>
    ///     The text that was used to generate the current filtered list
    /// </summary>
    public string? ActiveSearchText
    {
        get => activeSearchText;
        set
        {
            SetProperty(ref activeSearchText, value);
            OnPropertyChanged(nameof(SearchHelp));
        }
    }

    /// <summary>
    ///     Helper text for the search bar.  Due to a bug with XAML this must be a property
    /// </summary>
    public string SearchHelp
    {
        get
        {
            if (ActiveSearchText != SearchText)
                return "Press enter to search";

            return $"Displaying {Media.Count} of {MediaSource.Count}";
        }
    }

    /// <summary>
    ///     Tags used to filter displayed media
    /// </summary>
    public ObservableCollection<Tag> FilterTags { get; } = new();

    /// <summary>
    ///     Classes used to filter displayed media
    /// </summary>
    public ObservableCollection<MediaClassFilter> FilterClasses { get; } = new();

    /// <summary>
    ///     Types used to filter displayed media
    /// </summary>
    public ObservableCollection<ContainerFormat> FilterFormats { get; } = new();

    /// <summary>
    ///     Mode used to filter tags
    /// </summary>
    public MediaFilterMode FilterTagsMode
    {
        get => filterTagsMode;
        set => SetProperty(ref filterTagsMode, value);
    }

    /// <summary>
    ///     Mode used to filter classes
    /// </summary>
    public MediaFilterMode FilterClassesMode
    {
        get => filterClassesMode;
        set => SetProperty(ref filterClassesMode, value);
    }

    /// <summary>
    ///     Mode used to filter formats
    /// </summary>
    public MediaFilterMode FilterFormatsMode
    {
        get => filterFormatsMode;
        set => SetProperty(ref filterFormatsMode, value);
    }

    /// <summary>
    ///     The number of active filters.  Only set when filter are applied (when ApplyFilters method is called)
    /// </summary>
    public int ActiveFilterCount
    {
        get => activeFilterCount;
        set => SetProperty(ref activeFilterCount, value);
    }

    #endregion

    #region Methods

    /// <summary>
    ///     Creates a list of Media from the Media collection in the database.
    /// </summary>
    /// <returns>Task</returns>
    public async Task<List<Media>> GetMediaFromDatabase()
    {
        return await Task.Run(() => Database.GetCollection<Media>().FindAll().ToList());
    }

    /// <summary>
    ///     Takes a list of media and initialises it for use in the library.  This will set the Media property.
    /// </summary>
    /// <returns>Task</returns>
    public async Task LoadMedia(List<Media> mediaList)
    {
        var errors = new List<Exception>();
        foreach (var media in mediaList)
            try
            {
                media.Initialise();

                media.MediaRequestUpdate += OnMediaRequestUpdate;
                media.MediaDirty         += OnMediaDirty;
                media.MediaRequestDelete += OnMediaRequestDelete;
                await media.BuildOrCacheResources();

                Media.AddNewItem(media);
                Media.CommitNew();
            }
            catch (Exception e)
            {
                // Media that fails to load should be removed from the library
                Database.GetCollection<Media>().Delete(media.Id);

                errors.Add(e);
            }

        if (errors.Count > 0)
            App.ViewModel.Notifications.NotifyException("Errors loading library", errors.ToArray());
    }

    /// <summary>
    ///     Called when media changes and requires saving.
    /// </summary>
    private void OnMediaDirty(object? mediaObj, EventArgs _)
    {
        if (mediaObj is Media media)
            dirtyMedia.Add(media);
    }

    /// <summary>
    ///     Called when media changes and requires saving.  This event is rate limited and only called a max of one time
    ///     per second per media.
    /// </summary>
    private void OnMediaRequestUpdate(object? mediaObj, EventArgs _)
    {
        if (mediaObj is not Media media)
            return;

        // Possible change to the list of clips that the selected media has 
        if (media == SelectedMedia)
            OnPropertyChanged(nameof(SelectedMediaClipPaths));

        dirtyMedia.Remove(media);
        Database.GetCollection<Media>().Update(media);
    }

    /// <summary>
    ///     Called when a media a user has requested to delete a media file.  This operation needs will:
    ///     - Unload the media from the editor if it is loaded
    ///     - Delete the file from the disk if deleteFile is true
    ///     - Delete the media's cache directory
    ///     - Remove the media from the database
    ///     - Remove the media from any collection in the library that might have it
    /// </summary>
    private void OnMediaRequestDelete(object? mediaObj, bool deleteFile)
    {
        if (mediaObj is not Media media)
            return;

        // Make sure nothing is using files from this media
        if (App.ViewModel.MediaEditor.Media == media)
            App.ViewModel.MediaEditor.Media = null;

        if (media == SelectedMedia)
            App.Window.LibraryControl.MediaPreview.MediaPlayer.Stop();

        media.DeleteCache();

        // Delete the media file if the user has requested that
        if (deleteFile)
            File.Delete(media.FilePath);

        // Remove from the database
        Database.GetCollection<Media>().Delete(media.Id);

        // Remove from every collection that might have it so that it gets GC'd
        Media.Remove(media);
        Media.CommitEdit();
        dirtyMedia.Remove(media);

        if (SelectedMedia is not { } selected)
            return;

        // If the selected media's parent was deleted, the property needs to update 
        if (selected.ParentId == media.Id)
            OnPropertyChanged(nameof(SelectedMediaParentPath));

        // And if the media was one of the selected media's produced clips, that list needs to update too
        if (selected.Clips.Contains(media.Id))
            OnPropertyChanged(nameof(SelectedMediaClipPaths));
    }

    /// <summary>
    ///     Add a media file to the library.
    /// </summary>
    /// <param name="file">The file path</param>
    /// <param name="parentId">
    ///     The ID of the media that this media was created from. Pass null if this was not
    ///     created by media in the library
    /// </param>
    private async Task<Media> AddMediaInternal(string file, ObjectId? parentId)
    {
        // Mark this file as being imported
        importingMedia.Add(file);

        try
        {
            // Create and initialise
            var media = new Media(file)
            {
                ParentId = parentId
            };
            media.Initialise();

            // Add listeners
            media.MediaRequestUpdate += OnMediaRequestUpdate;
            media.MediaDirty         += OnMediaDirty;
            media.MediaRequestDelete += OnMediaRequestDelete;
            await media.BuildOrCacheResources();

            Media.AddNewItem(media);
            Media.CommitNew();

            // Add to database
            var collection = Database.GetCollection<Media>();
            collection.Insert(media);

            // Is the parent the selected media?  Update their clips if so
            if (SelectedMedia != null && SelectedMedia.Id == parentId)
                OnPropertyChanged(nameof(SelectedMediaClipPaths));

            return media;
        }
        finally
        {
            // The file is no longer being imported
            importingMedia.Remove(file);
        }
    }


    /// <summary>
    ///     Adds a single media file to the library
    /// </summary>
    /// <param name="file">The file to add</param>
    /// <param name="parentId">
    ///     The ID of the media that this media was created from. Pass null if this was not
    ///     created by media in the library
    /// </param>
    /// <param name="replace">
    ///     Whether or not this media should replace media with the same path.  If this is false
    ///     this function will fail and warn the user
    /// </param>
    /// <returns>Task</returns>
    public async Task<Media?> AddMedia(string file, ObjectId? parentId = null, bool replace = false)
    {
        if (importingMedia.Contains(file))
        {
            App.ViewModel.Notifications.NotifyWarning("File already queued for import");
            return null;
        }

        if (MediaSource.FirstOrDefault(x => string.Equals(x.FilePath, file, StringComparison.CurrentCultureIgnoreCase)) is { } existingMedia)
        {
            if (replace)
            {
                // Delete the original video from the library
                OnMediaRequestDelete(existingMedia, false);
            }
            else
            {
                App.ViewModel.Notifications.NotifyWarning("File already imported");
                return null;
            }
        }

        try
        {
            var media = await AddMediaInternal(file, parentId);
            App.ViewModel.Notifications.NotifyInfo("Added media to the library!");

            return media;
        }
        catch (Exception e)
        {
            App.ViewModel.Notifications.NotifyException("Failed to add media", e);
            return null;
        }
    }

    /// <summary>
    ///     Adds a group of media, e.g from a folder
    /// </summary>
    /// <param name="files">The files to add</param>
    /// <param name="source">A name describing the source of the files</param>
    /// <param name="open">Selects one the first successfully opened imported media and opens in the media editor</param>
    /// <param name="completionCallback">A function called after each media is imported/fails to import.  The number passed is the number of processed items</param>
    /// <returns>Where the files came from</returns>
    public async Task<List<Media>> AddMedias(string[] files, string source, bool open = false, Action<int>? completionCallback = default)
    {
        var importedMedia = new List<Media>();
        
        // This function should only be used on collections of media > 1
        if (files.Length == 1)
        {
            if (await AddMedia(files[0]) is not { } media || !open)
                return importedMedia;

            SelectedMedia                   = media;
            App.ViewModel.MediaEditor.Media = media;
            return importedMedia;
        }

        var    errors             = new List<Exception>();
        foreach (var file in files)
            try
            {
                if (Database.GetCollection<Media>().Exists(x => x.FilePath.Equals(file, StringComparison.OrdinalIgnoreCase)))
                    throw new DuplicateNameException($"{file} already exists");

                var media = await AddMediaInternal(file, null);
                importedMedia.Add(media);

                completionCallback?.Invoke(errors.Count + importedMedia.Count);
            }
            catch (Exception e)
            {
                errors.Add(e);
            }

        if (importedMedia.FirstOrDefault() is { } first && open)
        {
            SelectedMedia                   = first;
            App.ViewModel.MediaEditor.Media = first;
        }

        if (errors.Count == 0)
            App.ViewModel.Notifications.NotifyInfo($"Added {files.Length} media files from {source}");
        else
            App.ViewModel.Notifications.NotifyException($"Failed to import {errors.Count}/{files.Length} media files from {source}", errors.ToArray());

        return importedMedia;
    }

    /// <summary>
    ///     Saves the media from the dirty media list.  This function should be called before
    /// </summary>
    public void SaveDirtyMedia()
    {
        foreach (var media in dirtyMedia)
            Database.GetCollection<Media>().Update(media);
    }

    /// <summary>
    ///     Checks whether or not a specific media matches the currently configured filters.
    /// </summary>
    /// <param name="media">The media to check</param>
    /// <returns>Whether or not filters are matched</returns>
    private bool MediaFilter(object obj)
    {
        if (obj is not Media media)
            return false;

        if (FilterTags.Count > 0)
        {
            if (FilterTagsMode == MediaFilterMode.All && !FilterTags.All(x => media.Tags.Any(y => y.Equals(x))))
                return false;

            if (FilterTagsMode == MediaFilterMode.Any && !FilterTags.Any(x => media.Tags.Any(y => y.Equals(x))))
                return false;

            if (FilterTagsMode == MediaFilterMode.None && FilterTags.Any(x => media.Tags.Any(y => y.Equals(x))))
                return false;
        }

        if (FilterClasses.Count > 0)
        {
            if (FilterClassesMode == MediaFilterMode.Any && !FilterClasses.Any(x => x.Class == media.Class))
                return false;

            if (FilterClassesMode == MediaFilterMode.None && FilterClasses.Any(x => x.Class == media.Class))
                return false;
        }

        // Filter via text after filters have been applied
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            if (media.FilePath.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                return true;

            if (media.Description != null && media.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        return true;
    }

    /// <summary>
    ///     Applies all filters
    /// </summary>
    public void ApplyFilters()
    {
        ActiveSearchText  = SearchText;
        ActiveFilterCount = FilterTags.Count + FilterClasses.Count + FilterFormats.Count;

        Media.Refresh();
        OnPropertyChanged(nameof(SearchHelp));
    }

    /// <summary>
    ///     Finds media by ID.  This doesn't use the Database for querying to avoid deserializing it again.
    /// </summary>
    /// <param name="id">The media's ID</param>
    /// <returns>The media or null if no media is found with the specified ID</returns>
    public Media? GetMediaById(ObjectId id)
    {
        return MediaSource.FirstOrDefault(x => x.Id == id);
    }

    /// <summary>
    ///     Finds media by file path.  TThis doesn't use the Database for querying to avoid deserializing it again.
    /// </summary>
    /// <param name="path">The media's full file path</param>
    /// <returns>The media or null if no media is found with the specified file path</returns>
    public Media? GetMediaByFilePath(string path)
    {
        return MediaSource.FirstOrDefault(x => string.Equals(x.FilePath, path, StringComparison.CurrentCultureIgnoreCase));
    }

    #endregion

    #region Media commands

    public ICommand OpenInEditorCommand => new RelayCommand(() =>
    {
        if (SelectedMedia == null)
            return;

        App.ViewModel.MediaEditor.Media = SelectedMedia;
        App.ViewModel.IsEditorSelected  = true;
    });

    public static ICommand OpenDeleteDialogCommand { get; } = new RelayCommand<IList>(list =>
    {
        if (list == null)
            return;

        DialogHost.Show(new LibraryDeleteMedia
        {
            DataContext = new LibraryDeleteTask(list.OfType<Media>())
        });
    });

    public static ICommand OpenEditTagsCommand { get; } = new RelayCommand<IList>(list =>
    {
        if (list == null)
            return;

        DialogHost.Show(new LibraryEditTags
        {
            DataContext = new LibraryEditTagsTask(list.OfType<Media>())
        });
    });

    public static ICommand EditClassesCommand { get; } = new RelayCommand<EditClassesTask>(task =>
    {
        if (task == null)
            return;

        task.SelectedMedia.ForEach(x => { x.ClassIndex = MediaClass.MediaClasses.IndexOf(task.Class); });
    });

    public ICommand SelectByPathCommand => new RelayCommand<string>(path =>
    {
        foreach (var media in MediaSource)
            if (media.FilePath == path)
            {
                SelectedMedia = media;
                return;
            }
    });

    public ICommand OpenInExplorerCommand => new RelayCommand<string>(path =>
    {
        Process.Start(new ProcessStartInfo("explorer.exe")
        {
            UseShellExecute = true,
            Arguments       = $"/select,\"{path}\""
        });
    });

    #endregion

    #region Filter tag commands

    public ICommand AddFilterTagCommand => new RelayCommand(() =>
    {
        var name = App.TagSuggestionRegistry.ActiveTagNames.LastOrDefault();
        if (name == null)
            return;

        var value = App.TagSuggestionRegistry.Tags.GetValueOrDefault(name)?.ActiveValues.LastOrDefault();
        if (value == null)
            return;

        FilterTags.Add(new(name, value, true));
    });

    public ICommand RemoveFilterTagCommand => new RelayCommand<Tag>(tag =>
    {
        if (tag != null)
            FilterTags.Remove(tag);
    });

    public ICommand ClearFilterTagsCommand => new RelayCommand(FilterTags.Clear);

    #endregion

    #region Filter class commands

    public ICommand AddFilterClassCommand => new RelayCommand(() =>
    {
        foreach (var @class in MediaClass.MediaClasses)
            if (FilterClasses.All(x => x.Class != @class))
            {
                FilterClasses.Add(new(@class));
                break;
            }
    });

    public ICommand RemoveFilterClassCommand => new RelayCommand<MediaClassFilter>(@class =>
    {
        if (@class != null)
            FilterClasses.Remove(@class);
    });

    public ICommand ClearFilterClassesCommand => new RelayCommand(FilterClasses.Clear);

    #endregion

    #region Filter type commands

    public ICommand AddFilterFormatCommand => new RelayCommand(() =>
    {
        // foreach (var format in MediaFormat.SupportedFormats)
        // {
        //     if (!FilterFormats.Contains(format))
        //     {
        //         FilterFormats.Add(format);
        //         break;
        //     }
        // }
    });

    public ICommand RemoveFilterFormatCommand => new RelayCommand<ContainerFormat>(output =>
    {
        if (output != null)
            FilterFormats.Remove(output);
    });

    public ICommand ClearFilterFormatsCommand => new RelayCommand(FilterFormats.Clear);

    #endregion

    #region Filter commands

    public ICommand ApplyFiltersCommand => new RelayCommand(ApplyFilters);

    public ICommand ResetFiltersCommand => new RelayCommand(() =>
    {
        FilterTags.Clear();
        FilterClasses.Clear();
        FilterFormats.Clear();
    });

    #endregion
}
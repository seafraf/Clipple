using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Input;
using Clipple.Types;
using Clipple.ViewModel.PersistentData;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Clipple.ViewModel;

public class Root : ObservableObject
{
    public Root()
    {
        Updater               = new();
        MediaEditor           = new();
 
        ClipPresetCollection = new(App.ContainerFormatCollection);

        // Persistent data
        AppState = PersistentDataHelper.Load<AppState>() ?? new();
        Settings = PersistentDataHelper.Load<Settings>() ?? new();

        AppState.PropertyChanged                  += (_, _) => PersistentDataHelper.Save(AppState);
        Settings.PropertyChanged                  += (_, _) => PersistentDataHelper.Save(Settings);
        Settings.FolderWatchers.CollectionChanged += (_, _) => PersistentDataHelper.Save(Settings);
    }

    #region Members

    private bool isLoading = true;

    private string loadingText = string.Empty;

    private bool isEditorSelected = true;
    private bool isLibrarySelected;
    private bool isSettingSelected;

    #endregion

    #region Methods
    private async Task LoadWatchers()
    {
        foreach (var folderWatch in Settings.FolderWatchers)
        {
            LoadingText = "Searching " + folderWatch.Directory;
            var files = await folderWatch.FindMedia();
            if (files.Count <= 0) 
                continue;
                
            LoadingText = $"Importing {files.Count} files from {folderWatch.Directory}";
            var mediaList = await Library.AddMedias(files.ToArray(), folderWatch.Directory, false, (count) =>
            {
                LoadingText = $"Imported {count}/{files.Count} files from {folderWatch.Directory}";
            });

            // Apply folder watch settings to imported media
            foreach (var media in mediaList)
            {
                media.Class      = folderWatch.Class;
                media.ClassIndex = folderWatch.ClassIndex;
                
                foreach (var tag in folderWatch.Tags)
                    media.Tags.Add(new(tag.Name, tag.Value));
            }
        }
    }

    private async void ReloadWatchers()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        
        IsLoading = true;
        await LoadWatchers();

        // Display an ending splash to make it more clear that the reload did anything
        LoadingText = "Watchers reloaded!";
        await Task.Delay(2000);

        IsLoading = false;
    }
    
    public async Task Load()
    {
        LoadingText = "Checking for updates";
        await Updater.CheckForUpdate();
        
        LoadingText = "Loading library";
        var media = await Library.GetMediaFromDatabase();

        LoadingText = "Initialising media";
        await Library.LoadMedia(media);

        if (Settings.FolderWatchers.Count > 0)
            await LoadWatchers();

            // Restore AppState
        if (AppState.LibraryMediaId is { } libraryId)
            Library.SelectedMedia = Library.GetMediaById(libraryId);

        if (AppState.EditorMediaId is { } editorId)
            MediaEditor.Media = Library.GetMediaById(editorId);

        IsLoading = false;
    }

    #endregion

    #region Properties

    /// <summary>
    ///     The index of the root transitioner.  0 is the index of the loading panel and 1 is the index of the grid panel.,
    /// </summary>
    public int LoadingTransitionIndex => IsLoading ? 0 : 1;

    /// <summary>
    ///     True immediately after launching Clipple.  Set to false by Load(), called by the code behind for the main window.
    /// </summary>
    public bool IsLoading
    {
        get => isLoading;
        set
        {
            SetProperty(ref isLoading, value);
            OnPropertyChanged(nameof(LoadingTransitionIndex));
        }
    }

    /// <summary>
    ///     Text describing the loading process.
    /// </summary>
    public string LoadingText
    {
        get => loadingText;
        set => SetProperty(ref loadingText, value);
    }

    /// <summary>
    ///     Whether or nor the editor tab is selected
    /// </summary>
    public bool IsEditorSelected
    {
        get => isEditorSelected;
        set
        {
            SetProperty(ref isEditorSelected, value);

            if (!value)
                return;

            IsLibrarySelected  = false;
            IsSettingsSelected = false;
        }
    }

    /// <summary>
    ///     Whether or not the library tab is selected
    /// </summary>
    public bool IsLibrarySelected
    {
        get => isLibrarySelected;
        set
        {
            // If the library was selected and is no longer going to be selected, pause the media preview
            // just in case it is left playing in the background
            if (isLibrarySelected && !value)
                App.Window.LibraryControl.MediaPreview.MediaPlayer.Pause();

            SetProperty(ref isLibrarySelected, value);

            if (!value)
                return;

            IsEditorSelected   = false;
            IsSettingsSelected = false;
        }
    }

    /// <summary>
    ///     Whether or not the settings tab is selected
    /// </summary>
    public bool IsSettingsSelected
    {
        get => isSettingSelected;
        set
        {
            SetProperty(ref isSettingSelected, value);

            if (!value)
                return;

            IsEditorSelected  = false;
            IsLibrarySelected = false;
        }
    }

    /// <summary>
    ///     Reference to the video editor view model
    /// </summary>
    public MediaEditor MediaEditor { get; }

    /// <summary>
    ///     Library view model
    /// </summary>
    public Library Library { get; } = new();

    /// <summary>
    ///     Reference to the settings
    /// </summary>
    public Settings Settings { get; }

    /// <summary>
    ///     Reference to the updater
    /// </summary>
    private Updater Updater { get; }

    /// <summary>
    ///     Collection of clip presets, user and default
    /// </summary>
    public ClipPresetCollection ClipPresetCollection { get; }

    /// <summary>
    ///     App state persistent data
    /// </summary>
    public AppState AppState { get; }
    
    /// <summary>
    ///     Reference to the notifications VM
    /// </summary>
    public Notifications Notifications { get; } = new();

    /// <summary>
    ///     Title for the main window
    /// </summary>
    public string Title => $"Clipple ({App.Version})";

    #endregion

    #region Commands
    public ICommand ReloadFolderWatchersCommand => new RelayCommand(ReloadWatchers);
    #endregion
}
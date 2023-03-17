using System.Threading.Tasks;
using Clipple.Types;
using Clipple.Wpf.ViewModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Clipple.ViewModel;

public class Root : ObservableObject
{
    public Root()
    {
        UpdateViewModel       = new();
        MediaEditor           = new();
        TagSuggestionRegistry = new();

        ClipPresetCollection = new(ContainerFormatCollection);
        
        // Persistent data
        AppState = PersistentDataHelper.Load<PersistentData.AppState>() ?? new();

        AppState.PropertyChanged += (_, _) => PersistentDataHelper.Save(AppState); 
    }

#region Members
    private bool isLoading = true;

    private string loadingText = string.Empty;

    private bool isEditorSelected  = true;
    private bool isLibrarySelected = false;
    private bool isSettingSelected = false;
#endregion

#region Methods
    public async Task Load()
    {
        // TODO:
        LoadingText = "Checking for updates";
        await UpdateViewModel.CheckForUpdate();

        LoadingText = "Loading library";
        var media = await Library.GetMediaFromDatabase();

        LoadingText = "Initialising media";
        await Library.LoadMedia(media);
        
        // Restore AppState
        if (AppState.LibraryMediaId is {} libraryId)
            Library.SelectedMedia = Library.GetMediaById(libraryId);
        
        if (AppState.EditorMediaId is {} editorId)
            MediaEditor.Media = Library.GetMediaById(editorId);

        IsLoading = false;
    }
#endregion

#region Properties
    /// <summary>
    /// The index of the root transitioner.  0 is the index of the loading panel and 1 is the index of the grid panel.,
    /// </summary>
    public int LoadingTransitionIndex
    {
        get => IsLoading ? 0 : 1;
    }

    /// <summary>
    /// True immediately after launching Clipple.  Set to false by Load(), called by the code behind for the main window.
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
    /// Text describing the loading process.
    /// </summary>
    public string LoadingText
    {
        get => loadingText;
        set => SetProperty(ref loadingText, value);
    }

    /// <summary>
    /// Whether or nor the editor tab is selected
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
    /// Whether or not the library tab is selected
    /// </summary>
    public bool IsLibrarySelected
    {
        get => isLibrarySelected;
        set
        {
            SetProperty(ref isLibrarySelected, value);

            if (!value)
                return;

            IsEditorSelected   = false;
            IsSettingsSelected = false;
        }
    }

    /// <summary>
    /// Whether or not the settings tab is selected
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
    /// Reference to the video editor view model
    /// </summary>
    public MediaEditor MediaEditor { get; }

    /// <summary>
    /// Library view model
    /// </summary>
    public Library Library { get; } = new();

    /// <summary>
    /// Reference to the settings
    /// </summary>
    public Settings Settings { get; } = new();

    /// <summary>
    /// Reference to the settings
    /// </summary>
    public UpdateViewModel UpdateViewModel { get; }

    /// <summary>
    /// Refernece to the tag suggestion registry
    /// </summary>
    public TagSuggestionRegistry TagSuggestionRegistry { get; }

    /// <summary>
    /// Reference to the collection of valid media formats for encoding and decoding.
    /// </summary>
    public ContainerFormatCollection ContainerFormatCollection { get; } = new();

    /// <summary>
    /// Collection of clip presets, user and default
    /// </summary>
    public ClipPresetCollection ClipPresetCollection { get; }
    
    /// <summary>
    /// App state persistent data
    /// </summary>
    public PersistentData.AppState AppState { get; }

    /// <summary>
    /// Title for the main window
    /// </summary>
    public string Title => $"Clipple ({UpdateViewModel.CurrentVersion})";
#endregion

#region Commands
#endregion
}
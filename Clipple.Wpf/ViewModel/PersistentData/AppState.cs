using LiteDB;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Clipple.ViewModel.PersistentData;

public class AppState : ObservableObject
{
#region Members
    private ObjectId? libraryMediaId;
    private ObjectId? editorMediaId;
#endregion
    
#region Properties
    /// <summary>
    /// The selected media in the library
    /// </summary>
    public ObjectId? LibraryMediaId
    {
        get => libraryMediaId;
        set => SetProperty(ref libraryMediaId, value);
    }
    
    /// <summary>
    /// The opened media in the editor
    /// </summary>
    public ObjectId? EditorMediaId
    {
        get => editorMediaId;
        set => SetProperty(ref editorMediaId, value);
    }
#endregion
}
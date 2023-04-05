using System.Windows.Input;
using LiteDB;
using Microsoft.Toolkit.Mvvm.Input;

namespace Clipple.ViewModel;

public partial class Clip
{
    #region Members
    private string               description;
    private bool                 addToLibrary = true;
    #endregion
 
    #region Properties
    public string Description
    {
        get => description;
        set => SetProperty(ref description, value);
    }

    public bool AddToLibrary
    {
        get => addToLibrary;
        set => SetProperty(ref addToLibrary, value);
    }
    #endregion
    
    #region Commands
    [BsonIgnore]
    public new ICommand AddTagCommand => new RelayCommand(() =>
    {
        AddNewTag(true);
    });
    #endregion
}
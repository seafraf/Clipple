using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Clipple.ViewModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Clipple.ViewModel;

public abstract class AbstractTagContainer : ObservableObject
{
    #region Properties
    // Setter used by deserialization
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public ObservableCollection<Tag> Tags { get; set; } = new();
    #endregion
    
    #region Methods
    protected void AddNewTag(bool hidden = false)
    {
        var tag = Tags.LastOrDefault();
        AddTag(tag?.Name ?? "New tag", tag?.Value ?? "", hidden);
    }

    private void AddTag(string name, string value, bool hidden = false)
    {
        Tags.Add(new(name, value, hidden));
    }

    public void DeleteTag(Tag tag)
    {
        Tags.Remove(tag);
        tag.Dispose();
    }

    public void ClearTags()
    {
        foreach (var tag in Tags)
            tag.Dispose();
        
        Tags.Clear();
    }
    #endregion
    
    #region Commands
    public ICommand AddTagCommand => new RelayCommand(() =>
    {
        AddNewTag();
    });

    public ICommand ClearTagsCommand => new RelayCommand(ClearTags);
    #endregion
}
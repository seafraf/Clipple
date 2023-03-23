using System.Collections.ObjectModel;
using System.Linq;
using Clipple.ViewModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Clipple.Types;

public abstract class AbstractTagContainer : ObservableObject
{
    #region Properties
    // Setter used by deserialization
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public ObservableCollection<Tag> Tags { get; set; } = new();
    #endregion
    
    #region Methods
    protected void AddNewTag()
    {
        var tag = Tags.LastOrDefault();
        AddTag(tag?.Name ?? "New tag", tag?.Value ?? "");
    }

    private void AddTag(string name, string value)
    {
        Tags.Add(new(name, value));
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
}
using System.Collections.ObjectModel;
using System.Linq;
using LiteDB;

namespace Clipple.ViewModel;

public partial class Media
{
    #region Members

    private ObservableCollection<Tag> tags = new();

    #endregion

    #region Properties

    /// <summary>
    ///     A list of this media's tags
    /// </summary>
    public ObservableCollection<Tag> Tags
    {
        get => tags;
        set => SetProperty(ref tags, value);
    }

    /// <summary>
    ///     Formatting helper. Describes the amount of tags that this media has
    /// </summary>
    [BsonIgnore]
    public string TagCountString
    {
        get
        {
            if (Tags.Count == 0)
                return "No tags";

            if (Tags.Count == 1)
                return "1 tag";

            return $"{Tags.Count} tags";
        }
    }

    #endregion

    #region Methods

    private void AddNewTag()
    {
        var tag = Tags.LastOrDefault();
        AddTag(tag?.Name ?? "New tag", tag?.Value ?? "");
    }

    public void AddTag(string name, string value)
    {
        Tags.Add(new(name, value));

        OnPropertyChanged(nameof(TagCountString));
    }

    public void DeleteTag(Tag tag)
    {
        Tags.Remove(tag);
        tag.Dispose();

        OnPropertyChanged(nameof(TagCountString));
    }

    #endregion
}
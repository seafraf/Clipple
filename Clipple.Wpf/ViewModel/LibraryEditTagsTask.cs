using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Clipple.ViewModel;

public class LibraryEditTagsTask : ObservableObject
{
    public class TagGroup : ObservableObject
    {
        public TagGroup(string name, string value)
        {
            this.name  = name;
            this.value = value;
        }

        #region Members

        private string name;
        private string value;

        #endregion

        #region Properties

        public ObservableCollection<Tag> Tags { get; } = new();

        public string Name
        {
            get => name;
            set
            {
                SetProperty(ref name, value);
                OnPropertyChanged(nameof(ValueSuggestions));

                foreach (var tag in Tags)
                    tag.Name = value;
            }
        }

        public string Value
        {
            get => value;
            set
            {
                SetProperty(ref this.value, value);

                foreach (var tag in Tags)
                    tag.Value = value;
            }
        }

        public ObservableCollection<string> NameSuggestions => App.TagSuggestionRegistry.ActiveTagNames;

        public ObservableCollection<string> ValueSuggestions
        {
            get
            {
                var nameInfo = App.TagSuggestionRegistry.Tags.GetValueOrDefault(Name);
                if (nameInfo != null)
                    return nameInfo.ActiveValues;

                return new();
            }
        }

        #endregion
    }

    public LibraryEditTagsTask(IEnumerable<Media> media)
    {
        foreach (var m in media)
        {
            foreach (var tag in m.Tags)
            {
                var group = TagGroups.FirstOrDefault(x => x.Name == tag.Name && x.Value == tag.Value);
                if (group == null)
                {
                    group = new(tag.Name, tag.Value);
                    TagGroups.Add(group);
                }

                group.Tags.Add(tag);
            }

            Media.Add(m);
        }

        ;
    }

    public ObservableCollection<Media> Media { get; } = new();

    public ObservableCollection<TagGroup> TagGroups { get; } = new();

    #region Commands

    public ICommand DeleteCommand => new RelayCommand<TagGroup>(group =>
    {
        if (group == null)
            return;

        // Remove the tag from all media
        foreach (var media in Media)
            for (var i = 0; i < media.Tags.Count; i++)
            {
                var tag = media.Tags[i];
                if (tag.Name == group.Name && tag.Value == group.Value)
                {
                    media.Tags.RemoveAt(i);
                    tag.Dispose();

                    i--;
                }
            }

        // Remove the group from this display
        TagGroups.Remove(group);
    });

    public ICommand ExpandCommand => new RelayCommand<TagGroup>(group =>
    {
        if (group == null)
            return;

        // Add tags to each media that is missing the tag described by this group and
        // add any created tag to the group also
        foreach (var media in Media)
            if (!media.Tags.Any(x => x.Name == group.Name && x.Value == group.Value))
            {
                var tag = new Tag(group.Name, group.Value);
                media.Tags.Add(tag);
                group.Tags.Add(tag);
            }
    });

    public ICommand NewTagCommand => new RelayCommand(() =>
    {
        var name  = App.TagSuggestionRegistry.ActiveTagNames.LastOrDefault("New tag");
        var value = App.TagSuggestionRegistry.Tags.GetValueOrDefault(name)?.ActiveValues.LastOrDefault() ?? "";
        var group = new TagGroup(name, value);

        foreach (var media in Media)
        {
            var tag = new Tag(name, value);
            media.Tags.Add(tag);
            group.Tags.Add(tag);
        }

        TagGroups.Add(group);
    });

    #endregion
}
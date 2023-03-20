using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Clipple.ViewModel;

public class TagSuggestionRegistry : ObservableObject
{
    public class NameStats
    {
        public NameStats(Tag tag)
        {
            ReferenceCount = 1;
            RegisterValue(tag.Value);
        }

        /// <summary>
        ///     Key:    string representing the tag value
        ///     Value:  a count of how many tags are using this value
        /// </summary>
        public Dictionary<string, int> ValueReferenceCount { get; } = new();

        /// <summary>
        ///     A count of how many tags have this name
        /// </summary>
        public int ReferenceCount { get; set; }

        /// <summary>
        ///     A list of values for this tag name that have more than one active usage.
        ///     This value is intended as a user suggestion for values when adding tags to media.
        /// </summary>
        public ObservableCollection<string> ActiveValues { get; } = new();

        /// <summary>
        ///     Registers a usage of a tag value.
        /// </summary>
        /// <param name="value">The value to register the usage of</param>
        public void RegisterValue(string value)
        {
            // Empty values are not useful suggestions
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (ValueReferenceCount.ContainsKey(value))
            {
                ValueReferenceCount[value]++;
            }
            else
            {
                ValueReferenceCount[value] = 1;
                ActiveValues.Add(value);
            }
        }

        /// <summary>
        ///     Releases a usage of a tag value.
        /// </summary>
        /// <param name="value"></param>
        public void ReleaseValue(string value)
        {
            if (ValueReferenceCount.ContainsKey(value))
            {
                if (ValueReferenceCount[value] == 1)
                {
                    ValueReferenceCount.Remove(value);
                    ActiveValues.Remove(value);
                }
                else
                {
                    ValueReferenceCount[value]--;
                }
            }
        }
    }

    #region Properties

    /// <summary>
    ///     A list of tag stats by tag name.  The NameStats class contains a reference count for
    ///     the tag name and reference counts for individual values for that tag name.
    /// </summary>
    public Dictionary<string, NameStats> Tags { get; } = new();

    /// <summary>
    ///     A list of registered tag names.  This list will be updated when all usages of a tag are
    ///     released.  The intended usage of this property is to get a list of currentyl
    /// </summary>
    public ObservableCollection<string> ActiveTagNames { get; } = new();

    #endregion

    #region Methods

    /// <summary>
    ///     Registers the usage of a tag.  It is important that after this tag is registered as used, both:
    ///     a) It is released when the tag is no longer used (e.g removed from media, or the media itself was removed)
    ///     b) The tag does not change in name or value after being registered.  If a tag needs to change it should be removed
    ///     and registered again with a new name and value.
    /// </summary>
    /// <param name="tag">The tag to register</param>
    public void RegisterTag(Tag tag)
    {
        // Empty names are not useful suggestions
        if (string.IsNullOrWhiteSpace(tag.Name))
            return;

        if (Tags.ContainsKey(tag.Name))
        {
            Tags[tag.Name].ReferenceCount++;
            Tags[tag.Name].RegisterValue(tag.Value);
        }
        else
        {
            Tags.Add(tag.Name, new(tag));
            ActiveTagNames.Add(tag.Name);
        }
    }

    /// <summary>
    ///     Releases the usage of a tag.  This will also release the usage of the tag's value.
    /// </summary>
    /// <param name="tag">The tag to release</param>
    public void ReleaseTag(Tag tag)
    {
        if (Tags.ContainsKey(tag.Name))
        {
            var nameInfo = Tags[tag.Name];
            if (nameInfo.ReferenceCount == 1)
            {
                Tags.Remove(tag.Name);
                ActiveTagNames.Remove(tag.Name);
            }
            else
            {
                nameInfo.ReferenceCount--;
                nameInfo.ReleaseValue(tag.Value);
            }
        }
    }

    #endregion
}
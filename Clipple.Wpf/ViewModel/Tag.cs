using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using LiteDB;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Clipple.ViewModel;

public class Tag : ObservableObject, IDisposable
{
    public Tag(string name, string value, bool hidden = false)
    {
        this.name   = name;
        this.value  = value;
        this.hidden = hidden;

        DeleteCommand = new RelayCommand<Media>(media =>
        {
            if (!hidden)
                media?.DeleteTag(this);
        });

        // Register the new tag
        if (!hidden)
            App.ViewModel.TagSuggestionRegistry.RegisterTag(this);
    }

    #region Methods

    /// <summary>
    ///     Removes this tag from the tag registry
    /// </summary>
    public void Dispose()
    {
        if (!hidden)
            App.ViewModel.TagSuggestionRegistry.ReleaseTag(this);
    }

    public override bool Equals(object? obj)
    {
        return obj is Tag tag &&
               Name == tag.Name &&
               Value == tag.Value;
    }

    #endregion

    #region Members

    private readonly bool hidden;

    private string name;

    private string value;

    #endregion

    #region Properties

    /// <summary>
    ///     The name of this tag.
    /// </summary>
    public string Name
    {
        get => name;
        set
        {
            if (!hidden)
                App.ViewModel.TagSuggestionRegistry.ReleaseTag(this);

            SetProperty(ref name, value);

            if (!hidden)
                App.ViewModel.TagSuggestionRegistry.RegisterTag(this);

            OnPropertyChanged(nameof(ValueSuggestions));
        }
    }

    /// <summary>
    ///     The value of this tag
    /// </summary>
    public string Value
    {
        get => value;
        set
        {
            var nameInfo = App.ViewModel.TagSuggestionRegistry.Tags.GetValueOrDefault(Name);

            if (this.value != null && !hidden)
                nameInfo?.ReleaseValue(this.value);

            SetProperty(ref this.value, value);

            if (!hidden)
                nameInfo?.RegisterValue(value);
        }
    }

    /// <summary>
    ///     Helper property.  Returns the list of existing tag names provided by the tag registry.
    /// </summary>
    [BsonIgnore]
    public ObservableCollection<string> NameSuggestions => App.ViewModel.TagSuggestionRegistry.ActiveTagNames;

    /// <summary>
    ///     Helper property.  Returns value suggestions based off of the name of this tag.  Value suggestions are provided by
    ///     the tag registry.
    /// </summary>
    [BsonIgnore]
    public ObservableCollection<string> ValueSuggestions
    {
        get
        {
            var nameInfo = App.ViewModel.TagSuggestionRegistry.Tags.GetValueOrDefault(Name);
            if (nameInfo != null)
                return nameInfo.ActiveValues;

            return new();
        }
    }

    #endregion

    #region Commands

    [BsonIgnore] public ICommand DeleteCommand { get; }

    #endregion
}
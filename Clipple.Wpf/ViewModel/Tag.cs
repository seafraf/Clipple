using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Clipple.Types;
using LiteDB;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Clipple.ViewModel;

public class Tag : ObservableObject, IDisposable
{
    [BsonCtor]
    // ReSharper disable once IntroduceOptionalParameters.Global
    public Tag(string name, string value) : this(name, value, false)
    {
        // Note that this function requires to exist for deserialization.  A default parameter on another constructor 
        // won't work
    }
        
    public Tag(string name, string value, bool hidden)
    {
        this.name   = name;
        this.value  = value;
        this.hidden = hidden;
        
        // Register the new tag
        if (!hidden)
            App.TagSuggestionRegistry.RegisterTag(this);
    }

    #region Methods

    /// <summary>
    ///     Removes this tag from the tag registry
    /// </summary>
    public void Dispose()
    {
        if (!hidden)
            App.TagSuggestionRegistry.ReleaseTag(this);
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
                App.TagSuggestionRegistry.ReleaseTag(this);

            SetProperty(ref name, value);

            if (!hidden)
                App.TagSuggestionRegistry.RegisterTag(this);

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
            var nameInfo = App.TagSuggestionRegistry.Tags.GetValueOrDefault(Name);

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
    public ObservableCollection<string> NameSuggestions => App.TagSuggestionRegistry.ActiveTagNames;

    /// <summary>
    ///     Helper property.  Returns value suggestions based off of the name of this tag.  Value suggestions are provided by
    ///     the tag registry.
    /// </summary>
    [BsonIgnore]
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

    #region Commands
    [BsonIgnore] 
    public ICommand DeleteCommand => new RelayCommand<AbstractTagContainer>(media =>
    {
        if (!hidden)
            media?.DeleteTag(this);
    });
    #endregion
}
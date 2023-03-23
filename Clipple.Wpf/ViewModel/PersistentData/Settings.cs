using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using Clipple.View;
using LiteDB;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Clipple.ViewModel.PersistentData;

public class Settings : ObservableObject
{
    #region Members
    private string clipOutputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
    #endregion
    
    #region Properties
    public string ClipOutputFolder
    {
        get => clipOutputFolder;
        set => SetProperty(ref clipOutputFolder, value);
    }

    // Setter used by deserialization
    // ReSharper disable once PropertyCanBeMadeInitOnly.Global
    public ObservableCollection<FolderWatch> FolderWatchers { get; set; } = new();
    #endregion
    
    #region Commands
    [BsonIgnore]
    public ICommand RemoveFolderWatchCommand => new RelayCommand<FolderWatch>((fw) =>
    {
        if (fw == null)
            return;
        
        fw.Dispose();
        FolderWatchers.Remove(fw);
    });
    
    [BsonIgnore]
    public ICommand EditFolderWatchCommand => new RelayCommand<FolderWatch>((fw) =>
    {
        if (fw == null)
            return;
        
        var fwIdx = FolderWatchers.IndexOf(fw);
        if (fwIdx == -1)
            return;

        var aid = new FolderWatch(fw);
        aid.OnSave += (_, _) =>
        {
            fw.Dispose();
            FolderWatchers[fwIdx] = aid;
        };

        DialogHost.Show(new FolderWatchEditor
        {
            DataContext = aid
        });
    });
    
    [BsonIgnore]
    public ICommand AddFolderWatchCommand => new RelayCommand(() =>
    {
        var aid = new FolderWatch();
        aid.OnSave += (_, _) => FolderWatchers.Add(aid);
        
        DialogHost.Show(new FolderWatchEditor
        {
            DataContext = aid
        });
    });
    #endregion
}
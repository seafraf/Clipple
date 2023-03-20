using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Clipple.ViewModel;

namespace Clipple.View;

public partial class ExportClip
{
    public ExportClip()
    {
        InitializeComponent();

        App.ViewModel.Settings.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName != nameof(ViewModel.Settings.DefaultOutputFolder))
                return;

            if (outputFolderWatcher is { } watcher)
            {
                watcher.Created -= OnFileSystemChanged;
                watcher.Deleted -= OnFileSystemChanged;
                watcher.Renamed -= OnFileSystemChanged;
            }

            CreateWatcher();
        };


        CreateWatcher();
    }

    private FileSystemWatcher? outputFolderWatcher;

    private void CreateWatcher()
    {
        if (!Directory.Exists(App.ViewModel.Settings.DefaultOutputFolder))
            return;

        outputFolderWatcher = new(App.ViewModel.Settings.DefaultOutputFolder)
        {
            NotifyFilter        = NotifyFilters.FileName,
            EnableRaisingEvents = true
        };

        outputFolderWatcher.Created += OnFileSystemChanged;
        outputFolderWatcher.Deleted += OnFileSystemChanged;
        outputFolderWatcher.Renamed += OnFileSystemChanged;
    }

    private void OnFileSystemChanged(object sender, FileSystemEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (DataContext is not Media { Clip: { } clip })
                return;

            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Renamed when e is RenamedEventArgs rename:
                {
                    if (string.Equals(clip.FullFileName, rename.OldFullPath, StringComparison.CurrentCultureIgnoreCase))
                        clip.NotifyOutputChanged();

                    if (string.Equals(clip.FullFileName, rename.FullPath, StringComparison.CurrentCultureIgnoreCase))
                        clip.NotifyOutputChanged();
                    break;
                }
                case WatcherChangeTypes.Deleted or WatcherChangeTypes.Created:
                {
                    if (string.Equals(clip.FullFileName, e.FullPath, StringComparison.CurrentCultureIgnoreCase))
                        clip.NotifyOutputChanged();
                    break;
                }
            }
        });
    }

    private void OnPresetSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // RemovedItems is zero when the combobox loads (it goes from no selected item to the default/deserialised item)
        // It is important that this is skipped because this was NOT a user selection of a preset.  If this was not skipped
        // then every time the export dialog would open, the preset would apply.
        if (e.RemovedItems.Count == 0)
            return;

        if (e.AddedItems[0] is ClipPreset preset && DataContext is Media media)
            media.Clip?.ApplyPreset(media, preset);
    }
}
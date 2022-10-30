using Clipple.ViewModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Clipple.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public VideoPlayer VideoPlayer => videoPlayer;

        public MainWindow()
        {
            InitializeComponent();

            var vm = (RootViewModel)DataContext;

            // Load theme
            //ThemeManager.Current.ChangeTheme(this, vm.SettingsViewModel.ThemeKey);

            // Create key bindings now
            UpdateKeyBindings();

            // Update key bindings when the settings change
            vm.SettingsViewModel.PropertyChanged += (s, e) => UpdateKeyBindings();

            // Load ingest resources
            var ingestResource = vm.SettingsViewModel.IngestAutomatically ? vm.SettingsViewModel.IngestFolder : null;

            // Handle CLI input path/video
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
                ingestResource = args[1];

            if (ingestResource != null)
            {
                if (Directory.Exists(ingestResource))
                {
                    vm.AddVideosFromFolder(ingestResource);
                }
                else if (File.Exists(ingestResource))
                    vm.AddVideo(ingestResource);
            }
        }

        private void UpdateKeyBindings()
        {
            var vm = (RootViewModel)DataContext;

            //hotKeys = new()
            //{
            //    (vm.SettingsViewModel.ControlHotKey, AppCommands.ControlCommand),
            //    (vm.SettingsViewModel.PreviousFrameHotKey, AppCommands.PreviousFrameCommand),
            //    (vm.SettingsViewModel.NextFrameHotKey, AppCommands.NextFrameCommand),
            //    (vm.SettingsViewModel.ToggleMuteHotKey, AppCommands.ToggleMuteCommand),
            //    (vm.SettingsViewModel.VolumeUpHotKey, AppCommands.VolumeUpCommand),
            //    (vm.SettingsViewModel.VolumeDownHotKey, AppCommands.VolumeDownCommand),
            //    (vm.SettingsViewModel.NextVideoHotKey, AppCommands.NextVideoCommand),
            //    (vm.SettingsViewModel.PreviousVideoHotKey, AppCommands.PreviousVideoCommand),
            //    (vm.SettingsViewModel.CreateClipHotKey, AppCommands.CreateClipCommand),
            //    (vm.SettingsViewModel.SeekStartHotKey, AppCommands.SeekStartCommand),
            //    (vm.SettingsViewModel.SeekEndHotKey, AppCommands.SeekEndCommand),
            //    (vm.SettingsViewModel.SaveHotKey, AppCommands.SaveCommand),
            //};
        }

        #region Members
        //private List<(HotKey, RelayCommand)> hotKeys;
        //private HashSet<Flyout> openFlyouts = new();
        #endregion

        private void OnDragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effects = DragDropEffects.Copy;
                e.Handled = true;
            }
        }

        private void OnDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (DataContext is not RootViewModel vm)
                    return;

                var addedAny = false;
                foreach (string file in files)
                {
                    if (vm.AddVideo(file))
                        addedAny = true;
                }    

                // Select the new(est) video
                if (addedAny)
                    vm.SelectedVideo = vm.Videos[vm.Videos.Count - 1];
            }
        }

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.OriginalSource is TextBox)
                return;

            FocusManager.SetIsFocusScope(this, true);
            FocusManager.SetFocusedElement(this, this);

            //foreach (var (key, command) in hotKeys)
            //{
            //    if (key.Key == e.Key && e.KeyboardDevice.Modifiers == key.ModifierKeys)
            //    {
            //        command.Execute(null);
            //        return;
            //    }
            //}
        }

        private async void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var vm = (RootViewModel)DataContext;

            if (vm.SettingsViewModel.SaveOnExit)
                await vm.Save();

            vm.VideoPlayerViewModel.MediaPlayer.Dispose();
        }

        private void OnStatusBarClicked(object sender, MouseButtonEventArgs e)
        {
            var dialog = new LogsView();
            dialog.ShowDialog();
        }

        private void FlyoutIsOpenChanged(object sender, RoutedEventArgs e)
        {
            //var flyout = ((Flyout)sender);

            //if (flyout.IsOpen && !openFlyouts.Contains(flyout))
            //{
            //    openFlyouts.Add(flyout);
            //    App.ViewModel.VideoPlayerViewModel.OverlayContentCount++;
            //}
        }

        private void FlyoutClosingFinished(object sender, RoutedEventArgs e)
        {
            //var flyout = ((Flyout)sender);

            //openFlyouts.Remove(flyout);
            //App.ViewModel.VideoPlayerViewModel.OverlayContentCount--;
        }
    }
}

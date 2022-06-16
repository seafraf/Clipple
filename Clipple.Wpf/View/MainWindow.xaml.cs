using Clipple.ViewModel;
using ControlzEx.Theming;
using MahApps.Metro.Controls;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Clipple.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public VideoPlayer VideoPlayer => videoPlayer;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public MainWindow()
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        {
            InitializeComponent();

            var vm = (RootViewModel)DataContext;

            // Load theme
            ThemeManager.Current.ChangeTheme(this, vm.SettingsViewModel.ThemeKey);

            // Create key bindings now
            UpdateKeyBindings();

            // Update key bindings when the settings change
            vm.SettingsViewModel.PropertyChanged += (s, e) => UpdateKeyBindings();
        }

        private void UpdateKeyBindings()
        {
            var vm = (RootViewModel)DataContext;

            hotKeys = new()
            {
                (vm.SettingsViewModel.ControlHotKey, AppCommands.ControlCommand),
                (vm.SettingsViewModel.PreviousFrameHotKey, AppCommands.PreviousFrameCommand),
                (vm.SettingsViewModel.NextFrameHotKey, AppCommands.NextFrameCommand),
                (vm.SettingsViewModel.ToggleMuteHotKey, AppCommands.ToggleMuteCommand),
                (vm.SettingsViewModel.VolumeUpHotKey, AppCommands.VolumeUpCommand),
                (vm.SettingsViewModel.VolumeDownHotKey, AppCommands.VolumeDownCommand),
                (vm.SettingsViewModel.NextVideoHotKey, AppCommands.NextVideoCommand),
                (vm.SettingsViewModel.PreviousVideoHotKey, AppCommands.PreviousVideoCommand),
                (vm.SettingsViewModel.CreateClipHotKey, AppCommands.CreateClipCommand),
                (vm.SettingsViewModel.NextEdgeHotKey, AppCommands.NextClipEdgeCommand),
                (vm.SettingsViewModel.PreviousEdgeHotKey, AppCommands.PreviousClipEdgeCommand),
                (vm.SettingsViewModel.SaveHotKey, AppCommands.SaveCommand),
            };
        }

        #region Members
        private List<(HotKey, RelayCommand)> hotKeys;
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

            foreach (var (key, command) in hotKeys)
            {
                if (key.Key == e.Key && e.KeyboardDevice.Modifiers == key.ModifierKeys)
                {
                    command.Execute(null);
                    return;
                }
            }
        }

        private void OnFlyoutClosed(object sender, RoutedEventArgs e)
        {
            var vm = (RootViewModel)DataContext;

            if (!vm.IsSettingsFlyoutOpen && !vm.IsVideosFlyoutOpen)
                App.VideoPlayerVisible = true;
        }

        private async void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var vm = (RootViewModel)DataContext;

            if (vm.SettingsViewModel.SaveOnExit)
                await vm.Save();
        }
    }
}

using Clipple.Command;
using Clipple.ViewModel;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
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

        public MainWindow()
        {
            InitializeComponent();

            var vm = (RootViewModel)DataContext;

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

        private void OnSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            throw new NotImplementedException();
        }

        #region Members
        private List<(HotKey, DelegateCommand)> hotKeys;
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

                foreach (string file in files)
                    vm.AddVideo(file);
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
                    command.Execute();
                    return;
                }
            }
        }
    }
}

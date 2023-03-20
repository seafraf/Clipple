using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Clipple.ViewModel;

namespace Clipple.View;

/// <summary>
///     Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MediaEditor MediaEditor => mediaEditor;

    public MainWindow()
    {
        InitializeComponent();

        var vm = (Root)DataContext;

        // Load theme
        //ThemeManager.Current.ChangeTheme(this, vm.SettingsViewModel.ThemeKey);

        // Create key bindings now
        UpdateKeyBindings();

        // Update key bindings when the settings change
        //vm.SettingsViewModel.PropertyChanged += (s, e) => UpdateKeyBindings();
    }

    private void UpdateKeyBindings()
    {
        var vm = (Root)DataContext;

        //hotKeys = new()
        //{
        //    (vm.SettingsViewModel.ControlHotKey, AppCommands.ControlCommand),
        //    (vm.SettingsViewModel.PreviousFrameHotKey, AppCommands.PreviousFrameCommand),
        //    (vm.SettingsViewModel.NextFrameHotKey, AppCommands.NextFrameCommand),
        //    (vm.SettingsViewModel.ToggleMuteHotKey, AppCommands.ToggleMuteCommand),
        //    (vm.SettingsViewModel.VolumeUpHotKey, AppCommands.VolumeUpCommand),
        //    (vm.SettingsViewModel.VolumeDownHotKey, AppCommands.VolumeDownCommand),
        //    (vm.SettingsViewModel.CreateClipHotKey, AppCommands.CreateClipCommand),
        //    (vm.SettingsViewModel.SeekStartHotKey, AppCommands.SeekStartCommand),
        //    (vm.SettingsViewModel.SeekEndHotKey, AppCommands.SeekEndCommand),
        //    (vm.SettingsViewModel.SaveHotKey, AppCommands.SaveCommand),
        //};
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

    private void OnClosing(object sender, CancelEventArgs e)
    {
        var vm = (Root)DataContext;

        vm.MediaEditor.MediaPlayer.Dispose();
        vm.Library.SaveDirtyMedia();
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        var vm = (Root)DataContext;

        // place this somewhere better
        await vm.Load();
    }
}
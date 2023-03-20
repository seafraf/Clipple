using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Clipple.View;

/// <summary>
///     Interaction logic for VideoEditor.xaml
/// </summary>
public partial class MediaEditor
{
    public MediaEditor()
    {
        InitializeComponent();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var vm = (ViewModel.MediaEditor)DataContext;
        vm.MediaPlayer.Handle = PlayerHost.Handle;

        if (vm.Media != null)
            vm.Load(vm.Media.FileInfo.FullName);
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var vm = (ViewModel.MediaEditor)DataContext;

        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Delta > 0)
                vm.ZoomIn.Execute(null);

            if (e.Delta < 0)
                vm.ZoomOut.Execute(null);
        }
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            return;

        // Try to open existing media first
        foreach (var file in files)
        {
            if (App.ViewModel.Library.GetMediaByFilePath(file) is not { } media)
                continue;

            App.ViewModel.Library.SelectedMedia = media;
            App.ViewModel.MediaEditor.Media     = media;
            return;
        }

        // If no existing media found, add the media and open
        await App.ViewModel.Library.AddMedias(files, "drag and drop", true);
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }
}
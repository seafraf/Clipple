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
    
    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var vm = (ViewModel.MediaEditor)DataContext;

        if (Keyboard.Modifiers != ModifierKeys.Control) 
            return;
        
        if (e.Delta > 0)
            vm.ZoomIn.Execute(null);

        if (e.Delta < 0)
            vm.ZoomOut.Execute(null);
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

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        var vm = (ViewModel.MediaEditor)DataContext;

        if (e.Key == App.ViewModel.Settings.ControlKey)
        {
            vm.TogglePlayPause();
        }
        else if (e.Key == App.ViewModel.Settings.PreviousFrameKey)
        {
            vm.ShowFramePrev();
        }
        else if (e.Key == App.ViewModel.Settings.NextFrameKey)
        {
            vm.ShowFrameNext();
        }
        else
            return;
        
        e.Handled = true;
        FocusManager.SetIsFocusScope(this, true);
        FocusManager.SetFocusedElement(this, this);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var vm = (ViewModel.MediaEditor)DataContext;
        MediaPlayerHost.Child = vm.MediaPlayer;
    }
}
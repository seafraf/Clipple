using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Clipple.View;

/// <summary>
///     Interaction logic for Library.xaml
/// </summary>
public partial class Library
{
    public Library()
    {
        InitializeComponent();
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files)
            return;

        if (DataContext is not ViewModel.Library vm)
            return;

        await vm.AddMedias(files, "drag and drop");
    }

    private void OnDragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            return;

        e.Effects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ViewModel.Library vm || e.Key != Key.Enter)
            return;

        vm.ApplyFilters();
    }

    private void OnFiltersClosed(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ViewModel.Library vm)
            return;

        vm.ApplyFilters();
    }

    private void OnDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ViewModel.Library vm)
            return;

        if (vm.SelectedMedia?.FileInfo.Exists == true)
            vm.OpenInEditorCommand.Execute(null);
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (DataContext is not ViewModel.Library vm || e.Key != Key.Delete)
            return;

        ViewModel.Library.OpenDeleteDialogCommand.Execute(((DataGrid)sender).SelectedItems);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (!e.WidthChanged) 
            return;
        
        InfoColumn.MinWidth = e.NewSize.Width * 0.3;
        InfoColumn.MaxWidth = e.NewSize.Width * 0.7;
    }
}
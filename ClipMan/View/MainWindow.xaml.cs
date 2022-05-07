using ClipMan.ViewModel;
using MahApps.Metro.Controls;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ClipMan.View
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
        }

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
    }
}

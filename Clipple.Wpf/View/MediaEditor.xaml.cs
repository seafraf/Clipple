using Clipple.ViewModel;
using FFmpeg.AutoGen;
using Mpv.NET.Player;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clipple.View
{
    /// <summary>
    /// Interaction logic for VideoEditor.xaml
    /// </summary>
    public partial class MediaEditor : UserControl
    {
        public MediaEditor()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var vm = (ViewModel.MediaEditor)DataContext;
            vm.MediaPlayer.Handle = playerHost.Handle;

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
    }
}

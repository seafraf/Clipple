using Clipple.ViewModel;
using FFmpeg.AutoGen;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clipple.View
{
    /// <summary>
    /// Interaction logic for Video.xaml
    /// </summary>
    public partial class VideoPlayer : UserControl
    {
        public VideoPlayer()
        {
            InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var vm = (VideoPlayerViewModel)DataContext;
            if (vm.Video != null)
                vm.MediaPlayer.OpenAsync(vm.Video.FileInfo.FullName);
        }
    }
}

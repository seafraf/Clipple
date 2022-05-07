using ClipMan.View;
using ClipMan.ViewModel;
using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Unosquare.FFME;

namespace ClipMan
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static RootViewModel ViewModel => (RootViewModel)Current.MainWindow.DataContext;
        public static MainWindow Window => (MainWindow)Current.MainWindow;
        public static MediaElement MediaElement => ((MainWindow)Current.MainWindow).VideoPlayer.MediaElement;

       public App()
        {
            Library.FFmpegDirectory = $"{AppDomain.CurrentDomain.BaseDirectory}/lib";
            Library.FFmpegLoadModeFlags = FFmpegLoadMode.FullFeatures;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Task.Run(async () =>
            {
                try
                {
                    await Library.LoadFFmpegAsync();
                }
                catch (Exception)
                {
                    var dispatcher = Current?.Dispatcher;
                    if (dispatcher != null)
                    {
                        await dispatcher.BeginInvoke(new Action(() =>
                        {
                            MessageBox.Show(MainWindow,
                                $"Couldn't load FFmpeg binaries from {Library.FFmpegDirectory}",
                                "FFmpeg Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);

                            Current?.Shutdown();
                        }));
                    }
                }
            });
        }
    }
}
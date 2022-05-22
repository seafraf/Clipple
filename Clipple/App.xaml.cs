using Clipple.View;
using Clipple.ViewModel;
using FFmpeg.AutoGen;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;

namespace Clipple
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static RootViewModel ViewModel => (RootViewModel)Current.Resources[nameof(RootViewModel)];

        public static MainWindow Window => (MainWindow)Current.MainWindow;

        public static Player MediaPlayer => ViewModel.VideoPlayerViewModel.MediaPlayer;

        public static bool VideoPlayerVisible
        {
            get => ViewModel.VideoPlayerViewModel.VideoVisibility == Visibility.Visible;
            set => ViewModel.VideoPlayerViewModel.VideoVisibility = value ? Visibility.Visible : Visibility.Hidden;
        }

        public static Timer AutoSaveTimer { get; } = new Timer();

        public App()
        {
            // This timer is started when the settings load in the RootViewModel
            AutoSaveTimer.Elapsed += (s, e) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    if (ViewModel.SettingsViewModel.AutoSave)
                        await ViewModel.Save();
                });
            };

            try
            {
                Engine.Start(new EngineConfig()
                {
                    FFmpegLogLevel = FFmpegLogLevel.Debug,
                    FFmpegPath = $"Binaries/{(Environment.Is64BitProcess ? "x64" : "x86")}",
                    UIRefresh = true,
                    UIRefreshInterval = 100,
                    UICurTimePerSecond = false,
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load dependencies", ex.Message);
                Shutdown(1);
            }
        }
    }
}
using Clipple.View;
using Clipple.ViewModel;
using FFmpeg.AutoGen;
using FlyleafLib;
using FlyleafLib.MediaPlayer;
using MahApps.Metro.Controls.Dialogs;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
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
        /// <summary>
        /// A reference to the root view model.
        /// </summary>
        public static RootViewModel ViewModel => (RootViewModel)Current.Resources[nameof(RootViewModel)];

        /// <summary>
        /// Reference to the logger
        /// </summary>
        public static LogsViewModel Logger { get; } = new();

        /// <summary>
        /// A reference to the main window instance.
        /// </summary>
        public static MainWindow Window => (MainWindow)Current.MainWindow;

        /// <summary>
        /// A reference to the FlyLeaf video player.
        /// </summary>
        public static Player MediaPlayer => ViewModel.VideoPlayerViewModel.MediaPlayer;

        /// <summary>
        /// The path to the FFmpeg libraries and executables.
        /// </summary>
        public static string LibPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Binaries", Environment.Is64BitProcess ? "64" : "32");

        public static bool VideoPlayerVisible
        {
            get => ViewModel.VideoPlayerViewModel.VideoVisibility == Visibility.Visible;
            set => ViewModel.VideoPlayerViewModel.VideoVisibility = value ? Visibility.Visible : Visibility.Hidden;
        }

        public static Timer AutoSaveTimer { get; } = new Timer();

        public App()
        {
            DispatcherUnhandledException += OnDispatcherUnhandledException;

            // This timer is started when the settings load in the RootViewModel
            AutoSaveTimer.Elapsed += (s, e) =>
            {
                Dispatcher.Invoke(async () =>
                {
                    if (ViewModel.SettingsViewModel.AutoSave)
                        await ViewModel.Save();
                });
            };

            // Load FFmpeg libraries now so that we can display an error and shut down if they fail to load.  
            // FFmpeg libraries are required to preview video and process clips.
            try
            {
                var stopwatch = Stopwatch.StartNew();
                Engine.Start(new EngineConfig()
                {
                    FFmpegLogLevel = FFmpegLogLevel.Debug,
                    FFmpegPath = LibPath,
                    UIRefresh = true,
                    UIRefreshInterval = 100,
                    UICurTimePerSecond = false,
                });

                stopwatch.Stop();
                Logger.Log($"Loaded FFmpeg binaries in {stopwatch.ElapsedMilliseconds}ms");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load dependencies", ex.Message);
                Shutdown(1);
            }
        }

        /// <summary>
        /// Attempt to handle all uncaught exceptions.  This is mostly here for debugging purposes so that users can send error messages 
        /// in, instead of the application just closing.
        /// </summary>
        private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            App.Logger.LogError("Unhandled exception", e.Exception);
            e.Handled = true;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            SquirrelAwareApp.HandleEvents(onInitialInstall: OnAppInstall, onAppUninstall: OnAppUninstall, onEveryRun: OnAppRun);
        }

        private static void OnAppInstall(SemanticVersion version, IAppTools tools)
        {
            tools.CreateShortcutForThisExe(ShortcutLocation.StartMenu);
        }

        private static void OnAppUninstall(SemanticVersion version, IAppTools tools)
        {
            tools.RemoveShortcutForThisExe(ShortcutLocation.StartMenu);
        }

        private static void OnAppRun(SemanticVersion version, IAppTools tools, bool firstRun)
        {
            ViewModel.UpdateViewModel.CurrentVersion = version ?? new SemanticVersion("1.0.0-VS-debug");

            tools.SetProcessAppUserModelId();

            if (firstRun) 
                MessageBox.Show("Installed!");
        }
    }
}
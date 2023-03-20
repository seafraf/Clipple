using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Clipple.View;
using Clipple.ViewModel;
using FFmpeg.AutoGen;
using LiteDB;
using Squirrel;

namespace Clipple;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App
{
    /// <summary>
    ///     A reference to the root VM
    /// </summary>
    public static Root ViewModel => (Root)Current.Resources[nameof(Root)];

    /// <summary>
    ///     Reference to the notifications VM
    /// </summary>
    public static Notifications Notifications { get; } = new();

    /// <summary>
    ///     A reference to the main window instance.
    /// </summary>
    public static MainWindow Window => (MainWindow)Current.MainWindow;

    /// <summary>
    ///     The path to the FFmpeg libraries and executables.
    /// </summary>
    public static string LibPath { get; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Binaries", Environment.Is64BitProcess ? "64" : "32");

    /// <summary>
    ///     Version provided by Clowd.Squirrel
    /// </summary>
    public static SemanticVersion? Version { get; private set; }

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        ffmpeg.RootPath                     = LibPath;
        BsonMapper.Global.EmptyStringToNull = false;
    }

    /// <summary>
    ///     Attempt to handle all uncaught exceptions.  This is mostly here for debugging purposes so that users can send error
    ///     messages
    ///     in, instead of the application just closing.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Notifications.NotifyException("Unexpected error", e.Exception);
        e.Handled = true;
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        SquirrelAwareApp.HandleEvents(OnAppInstall, onAppUninstall: OnAppUninstall, onEveryRun: OnAppRun);
    }

    private static void OnAppInstall(SemanticVersion version, IAppTools tools)
    {
        tools.CreateShortcutForThisExe(ShortcutLocation.StartMenu);
    }

    private static void OnAppUninstall(SemanticVersion version, IAppTools tools)
    {
        tools.RemoveShortcutForThisExe(ShortcutLocation.StartMenu);
    }

    private static void OnAppRun(SemanticVersion? version, IAppTools tools, bool firstRun)
    {
        Version = version ?? new SemanticVersion("0.0.0-DEBUG");

        tools.SetProcessAppUserModelId();

        if (firstRun)
            MessageBox.Show("Installed!");
    }
}
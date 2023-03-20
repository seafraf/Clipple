using Microsoft.Toolkit.Mvvm.Input;

namespace Clipple;

public static class AppCommands
{
    /// <summary>
    ///     Next frame
    /// </summary>
    public static RelayCommand NextFrameCommand => new(() => { App.ViewModel.MediaEditor.ShowFrameNext(); });

    /// <summary>
    ///     Previous frame
    /// </summary>
    public static RelayCommand PreviousFrameCommand => new(() => { App.ViewModel.MediaEditor.ShowFramePrev(); });

    /// <summary>
    ///     Play/pause media
    /// </summary>
    public static RelayCommand ControlCommand => new(() => { App.ViewModel.MediaEditor.TogglePlayPause(); });

    /// <summary>
    ///     Mutes or unmutes the media player
    /// </summary>
    public static RelayCommand ToggleMuteCommand => new(() =>
    {
        //App.MediaPlayer.Audio.ToggleMute();
    });

    /// <summary>
    ///     Incrases the media player's volume by 5%, if not maxed out
    /// </summary>
    public static RelayCommand VolumeUpCommand => new(() =>
    {
        //App.MediaPlayer.Audio.VolumeUp();
    });

    /// <summary>
    ///     Decreases the media player's volume by 5%, if not at 0%
    /// </summary>
    public static RelayCommand VolumeDownCommand => new(() =>
    {
        //App.MediaPlayer.Audio.VolumeDown();
    });

    /// <summary>
    ///     Goes to the end of the media or clip
    /// </summary>
    public static RelayCommand SeekStartCommand => new(() => { App.ViewModel.MediaEditor.SeekStart(); });

    /// <summary>
    ///     Goes to the start of the media or clip
    /// </summary>
    public static RelayCommand SeekEndCommand => new(() => { App.ViewModel.MediaEditor.SeekEnd(); });
}
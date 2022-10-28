using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Clipple
{
    public static class AppCommands
    {
        /// <summary>
        /// Next frame
        /// </summary>
        public static RelayCommand NextFrameCommand => new(() =>
        {
            App.ViewModel.VideoPlayerViewModel.ShowFrameNext();
        });

        /// <summary>
        /// Previous frame
        /// </summary>
        public static RelayCommand PreviousFrameCommand => new(() =>
        {
            App.ViewModel.VideoPlayerViewModel.ShowFramePrev();
        });

        /// <summary>
        /// Play/pause media
        /// </summary>
        public static RelayCommand ControlCommand => new(() =>
        {
            App.ViewModel.VideoPlayerViewModel.TogglePlayPause();
        });

        /// <summary>
        /// Mutes or unmutes the video player
        /// </summary>
        public static RelayCommand ToggleMuteCommand => new(() =>
        {
            //App.MediaPlayer.Audio.ToggleMute();
        });

        /// <summary>
        /// Incrases the video player's volume by 5%, if not maxed out
        /// </summary>
        public static RelayCommand VolumeUpCommand => new(() =>
        {
            //App.MediaPlayer.Audio.VolumeUp();
        });

        /// <summary>
        /// Decreases the video player's volume by 5%, if not at 0%
        /// </summary>
        public static RelayCommand VolumeDownCommand => new(() =>
        {
            //App.MediaPlayer.Audio.VolumeDown();
        });

        /// <summary>
        /// Loads the next video in the video list
        /// </summary>
        public static RelayCommand NextVideoCommand => new(() =>
        {
            App.ViewModel.NextVideo();
        });

        /// <summary>
        /// Loads the previous video in the video list
        /// </summary>
        public static RelayCommand PreviousVideoCommand => new(() =>
        {
            App.ViewModel.PreviousVideo();
        });

        /// <summary>
        /// Creates a clip at the current position in the play head, if media is loaded
        /// </summary>
        public static RelayCommand CreateClipCommand => new(() =>
        {
            App.ViewModel.VideoPlayerViewModel.CreateClip();
        });

        /// <summary>
        /// Goes to the end of the video or clip
        /// </summary>
        public static RelayCommand SeekStartCommand => new(() =>
        {
            App.ViewModel.VideoPlayerViewModel.SeekStart();
        });

        /// <summary>
        /// Goes to the start of the video or clip
        /// </summary>
        public static RelayCommand SeekEndCommand => new(() =>
        {
            App.ViewModel.VideoPlayerViewModel.SeekEnd();
        });

        /// <summary>
        /// Saves the current video list and clip settings
        /// </summary>
        public static RelayCommand SaveCommand => new(async () =>
        {
            await App.ViewModel.Save();
        });
    }
}

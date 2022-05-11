using Clipple.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Clipple
{
    public class MediaChangeArgs
    {
        public int AudioIndex { get; private set; }
        public int VideoIndex { get; private set; }

        public MediaChangeArgs(int audioIndex, int videoIndex)
        {
            AudioIndex = audioIndex;
            VideoIndex = videoIndex;
        }
    }

    public static class AppCommands
    {
        private static MediaChangeArgs? pendingMediaChange;

        public static MediaChangeArgs? ConsumeMediaChange()
        {
            var args = pendingMediaChange;
            pendingMediaChange = null;

            return args;
        }

        /// <summary>
        /// Opens a video
        /// </summary>
        public static DelegateCommand OpenCommand => new(async arg =>
       {
           try
           {
               var uriString = arg as string;
               if (string.IsNullOrWhiteSpace(uriString))
                   return;

               await App.MediaElement.Open(new Uri(uriString));
           }
           catch (Exception ex)
           {
               MessageBox.Show(Application.Current.MainWindow, $"Media Failed: {ex.GetType()}\r\n{ex.Message}",
                   $"{nameof(App.MediaElement)} Error",
                   MessageBoxButton.OK,
                   MessageBoxImage.Error,
                   MessageBoxResult.OK);
           }
       });

        /// <summary>
        /// Trigger a ChangeMedia call, this is currently on using when swapping between audio/video streams
        /// </summary>
        public static DelegateCommand ChangeMedia => new(async arg =>
        {
            // Change media if either the video or audio stream indexes have changed
            if (arg is MediaChangeArgs changeArgs && (changeArgs.AudioIndex != App.MediaElement.AudioStreamIndex || changeArgs.VideoIndex != App.MediaElement.VideoStreamIndex))
            {
                pendingMediaChange = changeArgs;
                await App.MediaElement.ChangeMedia();
            }
        });

        /// <summary>
        /// Pause/play
        /// </summary>
        public static DelegateCommand PlayCommand => new(async arg => await App.MediaElement.Play());

        /// <summary>
        ///  Pause/play
        /// </summary>
        public static DelegateCommand PauseCommand => new(async arg => await App.MediaElement.Pause());

        /// <summary>
        /// Next frame
        /// </summary>
        public static DelegateCommand NextFrameCommand => new(async arg =>
        {
            if (!App.MediaElement.IsAtEndOfStream)
                await App.MediaElement.StepForward();
        });

        /// <summary>
        /// Previous frame
        /// </summary>
        public static DelegateCommand PreviousFrameCommand => new(async arg =>
        {
            if (App.MediaElement.Position.Milliseconds > 0)
                await App.MediaElement.StepBackward();
        });

        /// <summary>
        /// Plays if the current media is paused, pauses if the current media is playing and resets to frame one then
        /// plays if at the end of the current media
        /// </summary>
        public static DelegateCommand ControlCommand => new(async arg =>
        {
            if (App.ViewModel.VideoPlayerViewModel.IsFinished)
            {
                await App.MediaElement.Seek(TimeSpan.Zero);
                await App.MediaElement.Play();
                return;
            }

            if (App.MediaElement.IsPlaying)
            {
                await App.MediaElement.Pause();
            }
            else
                await App.MediaElement.Play();
        });

        /// <summary>
        /// Mutes or unmutes the video player
        /// </summary>
        public static DelegateCommand ToggleMuteCommand => new(arg =>
        {
            App.MediaElement.IsMuted = !App.MediaElement.IsMuted;
        });

        /// <summary>
        /// Incrases the video player's volume by 5%, if not maxed out
        /// </summary>
        public static DelegateCommand VolumeUpCommand => new(arg =>
        {
            App.MediaElement.Volume = Math.Min(1.0, App.MediaElement.Volume + 0.05);
        });

        /// <summary>
        /// Decreases the video player's volume by 5%, if not at 0%
        /// </summary>
        public static DelegateCommand VolumeDownCommand => new(arg =>
        {
            App.MediaElement.Volume = Math.Max(0.0, App.MediaElement.Volume - 0.05);
        });

        /// <summary>
        /// Loads the next video in the video list
        /// </summary>
        public static DelegateCommand NextVideoCommand => new(async arg =>
        {
            App.ViewModel.NextVideo();
        });

        /// <summary>
        /// Loads the previous video in the video list
        /// </summary>
        public static DelegateCommand PreviousVideoCommand => new(async arg =>
        {
            App.ViewModel.PreviousVideo();
        });

        /// <summary>
        /// Creates a clip at the current position in the play head, if media is loaded
        /// </summary>
        public static DelegateCommand CreateClipCommand => new(arg =>
        {
            App.ViewModel.VideoPlayerViewModel.CreateClip();
        });

        /// <summary>
        /// Goes to the next clip edge
        /// </summary>
        public static DelegateCommand NextClipEdgeCommand => new(arg =>
        {
            App.ViewModel.VideoPlayerViewModel.NextClipEdge();
        });

        /// <summary>
        /// Goes to the previous clip edge
        /// </summary>
        public static DelegateCommand PreviousClipEdgeCommand => new(arg =>
        {
            App.ViewModel.VideoPlayerViewModel.PreviosClipEdge();
        });

        /// <summary>
        /// Saves the current video list and clip settings
        /// </summary>
        public static DelegateCommand SaveCommand => new(arg =>
        {
            App.ViewModel.Save();
        });
    }
}

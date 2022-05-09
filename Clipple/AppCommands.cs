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

        public static DelegateCommand OpenCommand => new (async arg =>
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

        public static DelegateCommand PlayCommand => new(async arg => await App.MediaElement.Play());

        public static DelegateCommand PauseCommand => new(async arg => await App.MediaElement.Pause());

        public static DelegateCommand NextFrameCommand => new(async arg => await App.MediaElement.StepForward());

        public static DelegateCommand PreviousFrameCommand => new(async arg => await App.MediaElement.StepBackward());

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

        public static DelegateCommand ChangeMedia => new(async arg =>
        {
            // Change media if either the video or audio stream indexes have changed
            if (arg is MediaChangeArgs changeArgs && (changeArgs.AudioIndex != App.MediaElement.AudioStreamIndex || changeArgs.VideoIndex != App.MediaElement.VideoStreamIndex))
            {
                pendingMediaChange = changeArgs;
                await App.MediaElement.ChangeMedia();
            }
        });
    }
}

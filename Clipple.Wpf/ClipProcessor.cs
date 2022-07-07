using Clipple.FFMPEG;
using Clipple.Types;
using Clipple.View;
using Clipple.ViewModel;
using FFmpeg.AutoGen;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple
{
    internal class ClipProcessor
    {
        private static async Task OpenJobsDialog(params JobViewModel[] processingJobs)
        {
            App.ViewModel.VideoPlayerViewModel.OverlayContentCount++;

            ClipProcessingDialog? dialog = null;
            ClipProcessingDialogViewModel? vm = null;
            vm = new ClipProcessingDialogViewModel(processingJobs.ToList(),
                new RelayCommand(async () =>
                {
                    // On close
                    await App.Window.HideMetroDialogAsync(dialog);
                }));

            dialog = new ClipProcessingDialog(vm);

            await App.Window.ShowMetroDialogAsync(dialog);

            // If Clipple is set to process clips automatically, start processing as soon as the dialog has opened
            if (App.ViewModel.SettingsViewModel.StartProcessingAutomatically)
                vm.StartProcesses();

            await dialog.WaitUntilUnloadedAsync();
            await dialog.WaitForCloseAsync();

            App.ViewModel.VideoPlayerViewModel.OverlayContentCount--;
        }

        /// <summary>
        /// Process one clip from one video
        /// </summary>
        /// <param name="video">The video to process a clip from</param>
        /// <param name="clip">The clip to process</param>
        public static async Task Process(VideoViewModel video, ClipViewModel clip)
        {
            await OpenJobsDialog(new JobViewModel(video, clip));
        }

        /// <summary>
        /// Processes all clips from one video
        /// </summary>
        /// <param name="video">The video to process all clips from</param>
        public static async Task Process(VideoViewModel video)
        {
            await OpenJobsDialog(video.Clips.Select(x => new JobViewModel(video, x)).ToArray());
        }

        /// <summary>
        /// Processes all clips from all videos
        /// </summary>
        public static async Task Process()
        {
            var clips = new List<JobViewModel>();
            foreach (var video in App.ViewModel.Videos)
                clips.AddRange(video.Clips.Select(x => new JobViewModel(video, x)));

            await OpenJobsDialog(clips.ToArray());
        }
    }
}

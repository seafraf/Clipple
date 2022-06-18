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
            var oldVisiblity = App.VideoPlayerVisible;
            App.VideoPlayerVisible = false;

            ClipProcessingDialog? dialog = null;
            ClipProcessingDialogViewModel? vm = null;
            vm = new ClipProcessingDialogViewModel(processingJobs.ToList(),
                new RelayCommand(async () =>
                {
                    // On close
                    await App.Window.HideMetroDialogAsync(dialog);

                    if (vm != null && vm.PostProcessingActionsEnabled)
                    {
                        foreach (var job in vm.SuccesfulJobs)
                        {
                            // Succesful jobs must have had all clips run succesfully, so this is safe to do without any further checks
                            var clips = job.VideoViewModel.Clips.Where(x => x.RemoveAfterProcessing).ToList();
                            foreach (var clip in clips)
                                job.VideoViewModel.Clips.Remove(clip);

                            // TODO: async? this could delete large files and take time
                            if (job.EnablePostProcessingActions)
                                job.VideoViewModel.PostProcessingAction.Run(job.VideoViewModel);
                        }
                    }
                }));

            dialog = new ClipProcessingDialog(vm);

            await App.Window.ShowMetroDialogAsync(dialog);

            // If Clipple is set to process clips automatically, start processing as soon as the dialog has opened
            if (App.ViewModel.SettingsViewModel.StartProcessingAutomatically)
                vm.StartProcesses();

            await dialog.WaitUntilUnloadedAsync();
            await dialog.WaitForCloseAsync();

            App.VideoPlayerVisible = oldVisiblity;
        }

        /// <summary>
        /// Process one clip from one video
        /// </summary>
        /// <param name="video">The video to process a clip from</param>
        /// <param name="clip">The clip to process</param>
        public static async Task Process(VideoViewModel video, ClipViewModel clip)
        {
            await OpenJobsDialog(new JobViewModel(video, new List<ClipViewModel> { clip }, false));
        }

        /// <summary>
        /// Processes all clips from one video
        /// </summary>
        /// <param name="video">The video to process all clips from</param>
        public static async Task Process(VideoViewModel video)
        {
            await OpenJobsDialog(new JobViewModel(video, video.Clips.ToList(), true));
        }

        /// <summary>
        /// Processes all clips from all videos
        /// </summary>
        public static async Task Process()
        {
            await OpenJobsDialog(App.ViewModel.Videos.Select(x => new JobViewModel(x, x.Clips.ToList(), true)).ToArray());
        }
    }
}

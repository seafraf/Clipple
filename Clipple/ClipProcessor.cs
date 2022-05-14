using Clipple.MediaProcessing;
using Clipple.ViewModel;
using FFmpeg.AutoGen;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple
{
    internal class ClipProcessor
    {
        private static async Task CreateMediaOutputTask(string file, string titlePrefix, ProgressDialogController controller, params ClipViewModel[] clips)
        {
            await Task.Run(() =>
            {
                var outputTasks = clips.Select((x) => new MediaOutputTask(x)).ToArray();
                using var inputTask = new MediaInputTask(file, outputTasks);

                // Update progress when the input task gives us back estimations
                inputTask.OnProgressUpdate += (s, progresses) =>
                {
                    int complete = 0;
                    double totalProgress = 0.0;
                    foreach (var progress in progresses)
                    {
                        // hehe..
                        if (progress >= 0.998)
                            complete++;

                        totalProgress += progress;
                    }

                    controller.SetTitle($"{titlePrefix} - Clip {complete + 1}/{clips.Length}");
                    controller.SetProgress(totalProgress / clips.Length);
                };
                
                inputTask.ProcessContexts();

                // Cleanup
                foreach (var t in outputTasks)
                    t.Dispose();
            });
        }

        /// <summary>
        /// Process one clip from one video
        /// </summary>
        /// <param name="video">The video to process a clip from</param>
        /// <param name="clip">The clip to process</param>
        public static async Task Process(VideoViewModel video, ClipViewModel clip)
        {
            var oldVisiblity        = App.VideoPlayerVisible;
            App.VideoPlayerVisible  = false;

            var controller = await App.Window.ShowProgressAsync("Please wait", $"Processing \"{clip.Title}\" for file:\n \"{video.FileInfo.FullName}\"");

            try
            {
                await CreateMediaOutputTask(video.FileInfo.FullName, "Video 1/1", controller, clip);
                await controller.CloseAsync();
            }
            catch (MediaProcessingException e)
            {
                await controller.CloseAsync();
                await App.Window.ShowMessageAsync($"Encoding error", $"An encoding error occurred: {e.Message}");
            }

            App.VideoPlayerVisible = oldVisiblity;
        }

        /// <summary>
        /// Processes all clips from one video
        /// </summary>
        /// <param name="video">The video to process all clips from</param>
        public static async Task Process(VideoViewModel video)
        {
            var oldVisiblity        = App.VideoPlayerVisible;
            App.VideoPlayerVisible  = false;

            var controller = await App.Window.ShowProgressAsync("Please wait", $"Processing file \"{video.FileInfo.FullName}\"");

            try
            {
                await CreateMediaOutputTask(video.FileInfo.FullName, "Video 1/1", controller, video.Clips.ToArray());
                await controller.CloseAsync();
            }
            catch (MediaProcessingException e)
            {
                await controller.CloseAsync();
                await App.Window.ShowMessageAsync($"Encoding error", $"An encoding error occurred: {e.Message}");
            }

            App.VideoPlayerVisible = oldVisiblity;
        }

        /// <summary>
        /// Processes all clips from all videos
        /// </summary>
        public static async Task Process()
        {
            var oldVisiblity        = App.VideoPlayerVisible;
            App.VideoPlayerVisible  = false;

            var controller = await App.Window.ShowProgressAsync("Please wait", "Please wait");

            try
            {
                var videos = App.ViewModel.Videos.Where(x => x.Clips.Count > 0).ToArray();
                for (int i = 0; i < videos.Length; i++)
                {
                    var video = videos[i];
                    controller.SetMessage($"Processing file \"{video.FileInfo.FullName}\"");

                    await CreateMediaOutputTask(video.FileInfo.FullName, $"Video {i + 1}/{videos.Length}", controller, video.Clips.ToArray());
                }

                await controller.CloseAsync();
            }
            catch (MediaProcessingException e)
            {
                await controller.CloseAsync();
                await App.Window.ShowMessageAsync($"Encoding error", $"An encoding error occurred: {e.Message}");
            }

            App.VideoPlayerVisible = oldVisiblity;
        }
    }
}

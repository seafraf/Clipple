using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.PPA
{
    /// <summary>
    /// Post processing action.  Deletes the video from Clipple and from the computer.
    /// </summary>
    public class DeleteVideo : PostProcessingAction
    {
        public DeleteVideo(object parameter) : base(parameter)
        {
        }

        public override string LongDescription => $"Delete {Video.FileInfo.FullName}";

        public override string ShortDescription => $"Permanently delete the video";

        public override void Run()
        {
            // Handles to this file used by the media player need to be freed before the file is deleted
            if (App.ViewModel.SelectedVideo == Video)
                App.ViewModel.VideoPlayerViewModel.Stop();

            try
            {
                File.Delete(Video.FileInfo.FullName);
                App.ViewModel.Videos.Remove(Video);
            }
            catch (Exception e)
            {
                App.Logger.LogError($"Failed to delete {Video.FileInfo.FullName}", e);
            }
        }
    }
}

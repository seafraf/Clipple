using Clipple.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Types
{
    public interface IVideoPostProcessingAction
    {
        public void Run(VideoViewModel video);
    }

    public class DeleteVideoPostProcessingAction : IVideoPostProcessingAction
    {
        public void Run(VideoViewModel video)
        {
            // Remove from Clipple
            App.ViewModel.Videos.Remove(video);

            // Remove from disk
            video.FileInfo.Delete();
        }

        public override string? ToString()
        {
            return "Permanently delete the video";
        }
    }

    public class RemoveVideoPostProcessingAction : IVideoPostProcessingAction
    {
        public void Run(VideoViewModel video)
        {

            // Remove from clipple
            App.ViewModel.Videos.Remove(video);
        }

        public override string? ToString()
        {
            return "Remove the video from Clipple";
        }
    }

    public class NoVideoPostProcessingAction : IVideoPostProcessingAction
    {
        public void Run(VideoViewModel model)
        {
            // Do nothing
        }

        public override string? ToString()
        {
            return "None";
        }
    }
}

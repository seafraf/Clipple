using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.PPA
{
    public class RemoveVideo : PostProcessingAction
    {
        public RemoveVideo(object parameter) : base(parameter)
        {
        }

        public override string LongDescription => $"Remove {Video.FileInfo.FullName} from Clipple";

        public override string ShortDescription => $"Remove the video from Clipple";

        public override void Run()
        {
            App.ViewModel.Videos.Remove(Video);
        }
    }
}

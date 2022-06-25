using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.PPA
{
    public class RemoveClip : PostProcessingAction
    {
        public RemoveClip(object parameter) : base(parameter)
        {
        }

        public override string LongDescription => $"Remove clip {Clip.Title}";

        public override string ShortDescription => $"Remove clip after processing";

        public override void Run()
        {
            Clip.Parent?.Clips.Remove(Clip);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.PPA
{
    public class NoAction : PostProcessingAction
    {
        public NoAction(object parameter) : base(parameter)
        {
        }

        public override string LongDescription => $"No action";

        public override string ShortDescription => $"None";

        public override void Run()
        {
            // Nothing
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Types
{
    public enum ClipProcessingDialogState
    {
        // Waiting for the user to press the start button
        Waiting,

        // ffmpeg process(es) running
        Running,
        
        // All processes done, waiting for the user to hit the done button
        Done
    }
}

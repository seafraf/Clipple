using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Types
{
    public enum ClipProcessingDialogState
    {
        // Waiting for user input
        Idle,

        // FFmpeg process(es) running
        Running,
        
        // The user is reviewing post processing actions
        Review,
    }
}

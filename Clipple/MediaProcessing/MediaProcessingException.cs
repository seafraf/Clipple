using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.MediaProcessing
{
    internal class MediaProcessingException : Exception
    {
        public MediaProcessingException(string msg) : base(msg)
        {
        }
    }
}

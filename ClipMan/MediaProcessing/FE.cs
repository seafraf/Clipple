using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClipMan.MediaProcessing
{
    /// <summary>
    /// Stands for "FFMPEG Errors".  This class is used a lot to do error checking on ffmpeg.* calls, so it was called
    /// something short.  Not a very C# way to deal with this issue, but the alternative is to wrap all ffmpeg calls 
    /// myself, no thanks.
    /// </summary>
    internal static class FE
    {
        /// <summary>
        /// Throws a MediaProcessingException <param name="code" /> is less than zero.
        /// </summary>
        /// <param name="code">The ffmpeg code</param>
        /// <param name="error">An error string</param>
        /// <returns>True, always</returns>
        /// <exception cref="MediaProcessingException"></exception>
        public unsafe static bool Code(int code, string error = "ffmpeg function failed")
        {
            if (code < 0)
            {
                var bufferSize = 1024;
                var buffer = stackalloc byte[bufferSize];
                ffmpeg.av_strerror(code, buffer, (ulong)bufferSize);

                throw new MediaProcessingException($"{error}: {Marshal.PtrToStringAnsi((IntPtr)buffer)}");
            }

            return true;
        }

        public static unsafe T* Null<T>(T* nullable, string error = "no memory for allocation") where T: unmanaged
        {
            if (nullable == null)
                throw new MediaProcessingException(error);

            return nullable;
        }
    }
}

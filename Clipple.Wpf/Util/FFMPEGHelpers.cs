using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;

namespace Clipple.Util;

public class FFMPEGHelpers
{
    public static unsafe void CheckCode(int code, string error)
    {
        if (code >= 0)
            return;
        
        const int bufferSize = 1024;
        var       buffer     = stackalloc byte[bufferSize];
        ffmpeg.av_strerror(code, buffer, (ulong)bufferSize);

        throw new InvalidOperationException($"{error}: {Marshal.PtrToStringAnsi((nint)buffer)}");
    }

    public static unsafe T* CheckNull<T>(T* nullable, string error) where T : unmanaged
    {
        if (nullable == null)
            throw new InvalidOperationException(error);

        return nullable;
    }
}
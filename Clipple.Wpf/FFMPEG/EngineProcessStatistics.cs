using System;

namespace Clipple.FFMPEG;

public class EngineProcessStatistics
{
    public EngineProcessStatistics(TimeSpan time)
    {
        Time    = time;
    }
    
    public  TimeSpan Time  { get; }
}
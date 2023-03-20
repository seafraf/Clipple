using System;
using Clipple.ViewModel;

namespace Clipple.FFMPEG;

public class MediaInput
{
    public MediaInput(string inputFile, Media media)
    {
        InputFile = inputFile;
        if (media.Clip is not { } clip)
            throw new NullReferenceException("Media input cannot have a null clip");

        Clip = clip;
    }

    #region Properties

    private string InputFile { get; }
    private Clip   Clip      { get; }

    private TimeSpan StartTime => Clip.StartTime;
    private TimeSpan Duration  => Clip.Duration;

    #endregion

    public override string? ToString()
    {
        return $"-ss {StartTime} -t {Duration} -i \"{InputFile}\"";
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using Clipple.ViewModel;

namespace Clipple.FFMPEG;

public class MediaOutput
{
    public MediaOutput(Media media, bool firstPass)
    {
        this.media  = media;
        IsFirstPass = firstPass;

        if (media.Clip is not { } clip)
            throw new NullReferenceException("Media output cannot have a null clip");

        Clip = clip;
    }

    #region Members

    private readonly Media media;

    #endregion

    #region Properties

    private Clip   Clip        { get; }
    public  bool   IsFirstPass { get; }
    public  string OutputFile  => Clip.FullFileName;

    private string VideoFilter
    {
        get
        {
            List<string> filterOpts = new();
            if (Clip.TargetFps != media.VideoFps && !Clip.UseSourceFps)
                filterOpts.Add($"fps={Clip.TargetFps}");

            if ((Clip.TargetWidth != media.VideoWidth || Clip.TargetHeight != media.VideoHeight) && !Clip.UseSourceResolution)
                filterOpts.Add($"scale={Clip.TargetWidth}:{Clip.TargetHeight}");

            if (Clip.ShouldCrop)
                filterOpts.Add($"crop={Clip.CropWidth}:{Clip.CropHeight}:{Clip.CropX}:{Clip.CropY}");

            return filterOpts.Count > 0 ? $"-filter:v \"{string.Join(", ", filterOpts)}\" -map 0:v" : "-map 0:v";
        }
    }

    private string AudioFilter
    {
        get
        {
            var enabledStreams = Clip.AudioSettings.Where(x => x.IsEnabled).ToList();

            if (enabledStreams.Count == 0)
                return "";

            var stringFilters = new List<string>();
            var inputs        = new List<string>();
            foreach (var stream in enabledStreams)
            {
                var filters = stream.AudioFilters.Where(x => x.IsEnabled).ToList();
                if (filters.Count > 0)
                {
                    stringFilters.AddRange(filters.Select((t, i) =>
                        $"[{(i == 0 ? $"0:{stream.StreamIndex}" : $"f_{stream.StreamIndex}_{i - 1}")}]{t.FilterString}[f_{stream.StreamIndex}_{i}]"));

                    inputs.Add($"[f_{stream.StreamIndex}_{filters.Count - 1}]");
                }
                else
                {
                    inputs.Add($"[0:{stream.StreamIndex}]");
                }
            }

            var filterString = string.Join("; ", stringFilters);
            if (!Clip.MergeAudio)
                return $"-filter_complex \"{filterString}\" " + string.Join(" ", inputs.Select(x => $"-map {x}"));
            var inputString = string.Join("", inputs);

            return $"-filter_complex \"{filterString}; {inputString}amix=inputs={inputs.Count}[mixed]\" -map \"[mixed]\"";
        }
    }

    private string VideoCodec => Clip.VideoCodec != null ? $"-c:v {Clip.VideoCodec.Name}" : "";
    private string AudioCodec => Clip.AudioCodec != null ? $"-c:a {Clip.AudioCodec.Name}" : "";

    private string VideoBitrate    => $"-b:v {Clip.VideoBitrate}K";
    private string AudioBitrate    => $"-b:a {Clip.AudioBitrate}K";
    private string Format          => $"-f {Clip.ContainerFormat.Name}";
    public  bool   TwoPassEncoding => Clip.TwoPassEncoding;

    #endregion

    public override string? ToString()
    {
        var pass = TwoPassEncoding ? $"-pass {(IsFirstPass ? '1' : '2')} -passlogfile \"{OutputFile}\" " : "";

        var videoArgs = "-vn";
        if (Clip.ContainerFormat.SupportsVideo)
            videoArgs = $"{VideoFilter} {VideoCodec} {VideoBitrate}";

        var audioArgs = "-an";
        if (Clip.ContainerFormat.SupportsAudio)
            audioArgs = $"{AudioFilter} {AudioCodec} {AudioBitrate}";

        if (IsFirstPass && TwoPassEncoding)
            return $"{pass} -stats {audioArgs} {videoArgs} -f null NUL";

        return $"{pass} -stats {audioArgs} {videoArgs} {Format} \"{OutputFile}\"";
    }
}
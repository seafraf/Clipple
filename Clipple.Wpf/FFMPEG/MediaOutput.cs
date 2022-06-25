using Clipple.ViewModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.FFMPEG
{
    public class MediaOutput
    {
        public MediaOutput(ClipViewModel clip, bool firstPass)
        {
            this.clip = clip;

            IsFirstPass = firstPass;
        }

        #region Members
        private readonly ClipViewModel clip;
        #endregion

        #region Properties
        public bool IsFirstPass { get; }
        public string OutputFile => clip.FullFileName;

        public string VideoFilter
        {
            get
            {
                List<string> filterOpts = new();
                if (clip.TargetFPS != clip.SourceFPS)
                    filterOpts.Add($"fps={clip.TargetFPS}");

                if (clip.TargetWidth != clip.SourceWidth || clip.TargetHeight != clip.SourceHeight)
                    filterOpts.Add($"scale={clip.TargetWidth}:{clip.TargetHeight}");

                if (filterOpts.Count > 0)
                {
                    return $"-filter:v \"{string.Join(", ", filterOpts)}\" -map 0:v";
                }
                else 
                    return $"-map 0:v";
            }
        }

        public string AudioFilter
        {
            get
            {
                var enabledTracks = clip.AudioSettings.Where(x => x.IsEnabled).ToList();

                if (enabledTracks.Count == 0)
                    return "";

                if (clip.MergeAudio)
                {
                    
                    var volumeFilters = string.Join("; ", enabledTracks.Select(x => x.ConvertMono ? 
                        $"[0:{x.TrackID}]volume={x.VolumeDecimal.ToString("0.00", CultureInfo.InvariantCulture)}[v{x.TrackID}]; [v{x.TrackID}]pan=mono|c0=.5*c0+.5*c1[o{x.TrackID}]" :
                        $"[0:{x.TrackID}]volume={x.VolumeDecimal.ToString("0.00", CultureInfo.InvariantCulture)}[o{x.TrackID}]"));

                    var inputList     = string.Join("", enabledTracks.Select(x => $"[o{x.TrackID}]"));

                    var amix = $"{volumeFilters}; {inputList}amix=inputs={enabledTracks.Count}[a]";

                    return $"-filter_complex \"{amix}\" -map \"[a]\"";
                }
                else
                {
                    var volumeFilters = string.Join("; ", enabledTracks.Select(x => x.ConvertMono ?
                        $"[0:{x.TrackID}]volume={x.VolumeDecimal.ToString("0.00", CultureInfo.InvariantCulture)}[v{x.TrackID}]; [v{x.TrackID}]pan=mono|c0=.5*c0+.5*c1[o{x.TrackID}]" :
                        $"[0:{x.TrackID}]volume={x.VolumeDecimal.ToString("0.00", CultureInfo.InvariantCulture)}[o{x.TrackID}]"));

                    var mappings      = string.Join(" ", enabledTracks.Select(x => $"-map [o{x.TrackID}]"));

                    return $"-filter_complex \"{volumeFilters}\" {mappings}";
                }
            }
        }

        public string VideoCodec => clip.VideoCodec != null ? $"-c:v {clip.VideoCodec}" : "";
        public string AudioCodec => clip.AudioCodec != null ? $"-c:a {clip.AudioCodec}" : "";

        public string VideoBitrate => $"-b:v {clip.VideoBitrate}K";
        public string AudioBitrate => $"-b:a {clip.AudioBitrate}K";
        public string Format => $"-f {clip.OutputFormat.Name}";
        public bool TwoPassEncoding => clip.TwoPassEncoding;
        #endregion

        public override string? ToString()
        {
            var pass = TwoPassEncoding ? $"-pass {(IsFirstPass ? '1' : '2')} -passlogfile \"{OutputFile}\" " : "";
            if (IsFirstPass && TwoPassEncoding) 
                return $"{VideoFilter} {VideoCodec} {VideoBitrate} {pass} -stats -an -f null NUL";

            string videoArgs = "-vn";
            if (clip.OutputFormat.SupportsVideo)
                videoArgs = $"{VideoFilter} {VideoCodec} {VideoBitrate}";

            string audioArgs = "-an";
            if (clip.OutputFormat.SupportsAudio)
                audioArgs = $"{AudioFilter} {AudioCodec} {AudioBitrate}";

            return $"{pass} -stats {audioArgs} {videoArgs} {Format} \"{OutputFile}\"";
        }
    }
}

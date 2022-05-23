using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Types
{
    public class TranscodingPreset
    {
        public TranscodingPreset(string name, string category, long? videoBitrate = null, int? targetWidth = null, int? targetHeight = null, 
            int? fps = null, string? videoCodec = null, long? audioBitrate = null, bool? useTargetSize = null, double? targetSize = null)
        {
            Name            = name;
            Category        = category;
            VideoBitrate    = videoBitrate;
            TargetWidth     = targetWidth;
            TargetHeight    = targetHeight;
            FPS             = fps;
            VideoCodec      = videoCodec;
            AudioBitrate    = audioBitrate;
            UseTargetSize   = useTargetSize;
            TargetSize      = targetSize;
        }

        #region Properties
        public string Name { get; }
        public string Category { get; }
        public long? VideoBitrate { get; }
        public int? TargetWidth { get; }
        public int? TargetHeight { get; }
        public int? FPS { get; }
        public string? VideoCodec { get; }
        public long? AudioBitrate { get; }
        public bool? UseTargetSize { get; }
        public double? TargetSize { get; }
        #endregion
    }
}

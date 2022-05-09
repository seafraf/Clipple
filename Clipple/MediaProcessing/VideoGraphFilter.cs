using Clipple.ViewModel;
using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.MediaProcessing
{
    internal class VideoGraphFilter : GraphFilter
    {
        public unsafe VideoGraphFilter(AVFormatContext* context, MediaInputStream inputStream, AVStream* outputStream, ClipViewModel clipSettings)
        {
            var decoder = inputStream.DecoderContext;

            // Input buffer
            BufferContext = AddFilter("buffer",
                $"width={decoder->width}:" +
                $"height={decoder->height}:" +
                $"pix_fmt={ffmpeg.av_get_pix_fmt_name(decoder->pix_fmt)}:" +
                $"time_base={outputStream->time_base.num}/{outputStream->time_base.den}");

            // Resize filter
            if (clipSettings.SourceWidth != clipSettings.TargetWidth || clipSettings.SourceHeight != clipSettings.TargetHeight)
            {
                AddFilter("scale",
                    $"w={clipSettings.TargetWidth}:" +
                    $"h={clipSettings.TargetHeight}");
            }

            if (clipSettings.SourceFPS != clipSettings.TargetFPS)
                AddFilter("fps", $"fps={clipSettings.TargetFPS}:");

            // Output buffer
            BufferSinkContext = AddFilter("buffersink");

            // Validate graph
            FE.Code(ffmpeg.avfilter_graph_config(FilterGraph, null), "invalid graph");
        }

        #region Properties
        public unsafe AVFilterContext* BufferContext { get; }
        public override unsafe AVFilterContext* BufferSinkContext { get; }
        #endregion

        /// <summary>
        /// Returns <propertry name="BufferContext" />
        /// </summary>
        /// <param name="inputStreamIndex">Ignored</param>
        /// <returns>Returns <propertry name="BufferContext" /></returns>
        public override unsafe AVFilterContext* GetBufferContext(int inputStreamIndex) 
            => BufferContext;
    }
}

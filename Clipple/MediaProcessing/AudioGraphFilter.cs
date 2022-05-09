using Clipple.ViewModel;
using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.MediaProcessing
{
    internal class AudioGraphFilter : GraphFilter
    {
        private class ContextWrapper
        {
            public unsafe AVFilterContext* Context { get; }

            public unsafe ContextWrapper(AVFilterContext* context)
            {
                Context = context;
            }
        }

        public unsafe AudioGraphFilter(AVFormatContext* context, AVStream* outputStream, ClipViewModel clipSettings, 
            MediaInputStream bestInputStream, params MediaInputStream[] inputStreams)
        {
            var bestDecoder = bestInputStream.DecoderContext;
            
            // Mixer filter, each buffer will feed into this
            var mixer = AddFilter("amix", 
                $"inputs={inputStreams.Length}:" +
                $"duration=longest");

            // Audio format, last step before going into the sink
            AddFilter("aformat",
                $"sample_fmts={ffmpeg.av_get_sample_fmt_name(bestDecoder->sample_fmt)}:" +
                $"sample_rates={bestDecoder->sample_rate}:" +
                $"channel_layouts=0x{bestDecoder->channel_layout:X}");

            BufferSinkContext = AddFilter("abuffersink");

            // Create and link each input to the mixer
            for (int i = 0; i < inputStreams.Length; i++)
            {
                ResetLastContext();

                var inputStream = inputStreams[i];
                var decoder     = inputStream.DecoderContext;

                var buffer = AddFilter("abuffer",
                    $"sample_rate={decoder->sample_rate}:" +
                    $"sample_fmt={ffmpeg.av_get_sample_fmt_name(decoder->sample_fmt)}:" +
                    $"channel_layout=0x{decoder->channel_layout:X}:" +
                    $"time_base={outputStream->time_base.num}/{outputStream->time_base.den}");

                var audioSettings = clipSettings.AudioSettings.Where(x => x.TrackID == inputStream.StreamIndex).FirstOrDefault();
                if (audioSettings != null)
                    AddFiltersFromSettings(inputStream, audioSettings);

                bufferContextMap[inputStream.StreamIndex] = new ContextWrapper(buffer);
                ffmpeg.avfilter_link(lastContext, 0, mixer, (uint)i);
            }

            // Validate graph
            FE.Code(ffmpeg.avfilter_graph_config(FilterGraph, null), "invalid graph");
        }

        private unsafe void AddFiltersFromSettings(MediaInputStream inputStream, AudioSettingsModel settings)
        {
            // No need to filter volume if it's already at max
            if (settings.Volume != 1.0)
                AddFilter("volume", $"volume={(double)settings.Volume / 100}");
        }

        #region Members
        private readonly Dictionary<int, ContextWrapper> bufferContextMap = new();
        #endregion

        #region Properties
        public override unsafe AVFilterContext* BufferSinkContext { get; }
        #endregion

        public override unsafe AVFilterContext* GetBufferContext(int inputStreamIndex)
        {
            return bufferContextMap[inputStreamIndex].Context;
        }
    }
}

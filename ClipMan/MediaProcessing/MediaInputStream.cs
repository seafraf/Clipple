using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipMan.MediaProcessing
{
    internal class MediaInputStream
    {
        public unsafe MediaInputStream(AVStream* stream, int streamIndex, AVCodecContext* decoderContext, AVRational frameRate)
        {
            Stream          = stream;
            StreamIndex     = streamIndex;
            DecoderContext  = decoderContext;
            FrameRate       = frameRate;

            IsAudio = stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO;
            IsVideo = stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO;
        }

        public unsafe AVStream* Stream { get; }
        public int StreamIndex { get; }
        public unsafe AVCodecContext* DecoderContext { get; }
        public AVRational FrameRate { get; }

        public bool IsAudio { get; }
        public bool IsVideo { get; }
    }
}

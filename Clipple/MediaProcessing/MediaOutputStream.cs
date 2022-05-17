using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FFmpeg.AutoGen;

namespace Clipple.MediaProcessing
{
    internal class MediaOutputStream : IDisposable
    {
        public unsafe MediaOutputStream(AVStream* stream, int streamIndex, AVCodecContext* encoderContext, GraphFilter? graphFilter, 
            TimeSpan clipStartTime, TimeSpan clipEndTime, params MediaInputStream[] inputStreams)
        {
            Stream          = stream; 
            StreamIndex     = streamIndex;
            EncoderContext  = encoderContext;
            GraphFilter     = graphFilter;

            InputStreams = inputStreams;

            IsAudio = stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO;
            IsVideo = stream->codecpar->codec_type == AVMediaType.AVMEDIA_TYPE_VIDEO;

            StartPTS = new long[inputStreams.Length];
            EndPTS   = new long[inputStreams.Length];

            // 1. Cache stream index -> inputStreams/StartPTS/EndPTS index 
            // 2. Cache StartPTS/EndPTS for each input stream
            for (int i = 0; i < inputStreams.Length; i++)
            {
                var inputStream = inputStreams[i];
                var timeBase    = inputStream.Stream->time_base;
                inputStreamIndexMap.Add(inputStream.StreamIndex, i);

                StartPTS[i] = (long)(timeBase.den == 0 ?
                    clipStartTime.TotalSeconds * ffmpeg.AV_TIME_BASE :
                    clipStartTime.TotalSeconds / ((double)timeBase.num / timeBase.den));

                EndPTS[i] = (long)(timeBase.den == 0 ?
                    clipEndTime.TotalSeconds * ffmpeg.AV_TIME_BASE :
                    clipEndTime.TotalSeconds / ((double)timeBase.num / timeBase.den));
            }
        }

        /// <summary>
        /// Checks if this output stream accepts input from the specified input stream (specified via index).  This 
        /// function has to be fast as it will likely be called for every frame that is read
        /// </summary>
        /// <param name="index">Input stream index</param>
        /// <returns>True if this output stream accepts input from the specified input stream index</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasStreamIndex(int index) => inputStreamIndexMap.ContainsKey(index);

        /// <summary>
        /// Check if a presentation time stamp is within the StartPTS/EndPTS for this stream
        /// </summary>
        /// <param name="streamIndex">The stream index</param>
        /// <param name="pts">The presentation time stamp</param>
        /// <returns>
        /// 1. MediaPacketAction.BadStream if the PTS came from an unknown stream
        /// 2. MediaPacketAction.Early if the PTS came before the start PTS for this stream
        /// 3. MediaPacketAction.Late if the PTS came after the end PTS for this stream
        /// 4. MediaPacketAction.Decode if this stream is interested in frames from the specified PTS
        /// </returns>
        public bool CheckPTS(int streamIndex, long pts)
        {
            var index = inputStreamIndexMap.GetValueOrDefault(streamIndex, -1);
            if (index == -1)
                return false;

            return pts >= StartPTS[index]; 
        }

        #region Members
        /// <summary>
        /// Maps stream index to InputStreams, StartPTS and EndPTS index
        /// </summary>
        private readonly Dictionary<int, int> inputStreamIndexMap = new();

        /// <summary>
        /// Last written presentation time stamp for a given stream
        /// </summary>
        private readonly Dictionary<int, long> lastWrittenPTS = new();

        /// <summary>
        /// List of input streams that have written enough content to satisfy the clip's end time
        /// </summary>
        private readonly HashSet<int> streamFinished = new();
        #endregion

        #region Properties
        /// <summary>
        /// A pointer to the output stream
        /// </summary>
        public unsafe AVStream* Stream { get; }

        /// <summary>
        /// The index of this stream in the output media
        /// </summary>
        public int StreamIndex { get; }

        /// <summary>
        /// The encoder's context for this stream
        /// </summary>
        public unsafe AVCodecContext* EncoderContext { get; }

        /// <summary>
        /// Output filter for this stream, used for every transformation.  null when in copy mode
        /// </summary>
        public GraphFilter? GraphFilter { get; }

        /// <summary>
        /// A list of input streams that this output stream sources frames from.  For videos, this will only contain
        /// one input stream, for audio clips this can contain 1 or more depending on whether or not the clip was set
        /// to merge audio.
        /// </summary>
        public MediaInputStream[] InputStreams { get; }

        /// <summary>
        /// 
        /// </summary>
        public long[] StartPTS { get; }

        /// <summary>
        /// 
        /// </summary>
        public long[] EndPTS { get; }

        /// <summary>
        /// True if the output stream is a video stream
        /// </summary>
        public bool IsVideo { get; }

        /// <summary>
        /// True if the output stream is an audio stream
        /// </summary>
        public bool IsAudio { get; }

        /// <summary>
        /// Estimates completion of this stream by 
        /// </summary>
        public double CompletionEstimate
        {
            get
            {
                if (lastWrittenPTS.Count == 0)
                    return 0;

                double estimate = 0.0;
                foreach (var (index, pts) in lastWrittenPTS)
                {
                    var cur  = pts;
                    var size = EndPTS[index] - StartPTS[index];
                    if (cur == 0 || size == 0)
                        continue;

                    estimate += (double)cur / size;
                }

                return Math.Max(0.0, Math.Min(1.0, estimate / lastWrittenPTS.Count));
            }
        }

        /// <summary>
        /// True if every input stream has given enough content to this stream
        /// </summary>
        public bool IsFinished
        {
            get
            {
                for (var i = 0; i < InputStreams.Length; i++)
                {
                    if (!streamFinished.Contains(i))
                        return false;
                }
                return true;
            }
        }

        public int PacketCount { get; set; } = 0;

        internal unsafe void WritePacket(int inputStreamIndex, AVPacket* packet, AVFormatContext* context, bool fakeWrite = false)
        {
            var internalIndex = inputStreamIndexMap.GetValueOrDefault(inputStreamIndex, -1);
            if (internalIndex == -1)
                return;

            PacketCount++;

            if (packet->pts >= EndPTS[internalIndex])
            {
                streamFinished.Add(internalIndex);
                return;
            }

            // Fix packet
            packet->stream_index = StreamIndex;
            //packet->pts -= Math.Min(packet->pts, StartPTS[internalIndex]);
            //packet->dts -= Math.Min(packet->dts, StartPTS[internalIndex]);

            // Record pts before scaling
            lastWrittenPTS[internalIndex] = packet->pts;

            if (fakeWrite)
                return;

            ffmpeg.av_packet_rescale_ts(packet, InputStreams[internalIndex].Stream->time_base, Stream->time_base);
            packet->pos = -1;

            FE.Code(ffmpeg.av_interleaved_write_frame(context, packet));
        }

        public unsafe void Dispose()
        {
            FE.Code(ffmpeg.avcodec_close(EncoderContext));
        }
        #endregion
    }
}

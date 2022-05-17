using Clipple.ViewModel;
using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.MediaProcessing
{
    internal class MediaOutputTask
    {
        public unsafe MediaOutputTask(ClipViewModel clipSettings)
        {
            ClipSettings = clipSettings;

            AVFormatContext* formatContext;
            FE.Code(ffmpeg.avformat_alloc_output_context2(&formatContext, null, null, clipSettings.FullFileName));
            FE.Null(formatContext);

            outputContext = formatContext;
        }

        #region Members
        private unsafe AVFormatContext* outputContext;
        #endregion

        #region Properties
        /// <summary>
        /// Start of the clip
        /// </summary>
        private ClipViewModel ClipSettings { get; }

        /// <summary>
        /// A list of stream stream mappings
        /// </summary>
        private List<MediaOutputStream> OutputStreams { get; } = new();

        /// <summary>
        /// Estimates completion of this stream by 
        /// </summary>
        public double CompletionEstimate
        {
            get
            {
                double estimate = 0.0;
                double streams = 0;
                foreach (var stream in OutputStreams)
                {
                    if (stream.IsVideo)
                    {
                        estimate += stream.CompletionEstimate;
                        streams++;
                    }
                }

                if (estimate == 0.0)
                    return 0;

                return estimate / streams;
            }
        }

        /// <summary>
        /// The video stream's start presentation timestamp. 
        /// </summary>
        public long VideoStreamStartTimePTS
        {
            get
            {
                // Assume one video stream
                var videoStream = OutputStreams.Where(x => x.IsVideo).First();
                if (videoStream == null)
                    return 0;

                return videoStream.StartPTS.First();
            }
        }

        /// <summary>
        /// The video stream's end presentation timestamp
        /// </summary>
        public long VideoStreamEndPTS
        {
            get
            {
                // Assume one video stream
                var videoStream = OutputStreams.Where(x => x.IsVideo).First();
                if (videoStream == null)
                    return 0;

                return videoStream.EndPTS.First();
            }
        }

        /// <summary>
        /// Proxy for the two pass encoding setting for this clip
        /// </summary>
        public bool TwoPassEnabled => ClipSettings.TwoPassEncoding;

        /// <summary>
        /// True when this is the first pass in a TwoPassEnabled clip
        /// </summary>
        public bool IsFirstPass { get; private set; } = true;

        public bool IsFinished
        {
            get => OutputStreams.All(x => x.IsFinished);
        }
        #endregion

        /// <summary>
        /// Creates an audio codec as close as possible to the input codec provided with changes to accomodate clip settings.
        /// </summary>
        /// <param name="inputContext">Input codec</param>
        /// <returns>Output codec</returns>
        private unsafe AVCodecContext* CreateAudioContext(AVCodecContext* inputContext)
        {
            // Support output formats in the future.  For now, use the source format in videos and extract to mp3 otherwise.
            var codecId = ClipSettings.ExportMode == ClipExportMode.AudioOnly ? AVCodecID.AV_CODEC_ID_MP3 : inputContext->codec_id;

            var codec = FE.Null(ffmpeg.avcodec_find_encoder(codecId),
                $"couldn't find codec {ffmpeg.avcodec_get_name(codecId)}");

            var codecContext = FE.Null(ffmpeg.avcodec_alloc_context3(codec));

            // Copy everything, in the future we can look to change audio quality settings but for right now
            // I don't think anyone wants it
            codecContext->channels              = inputContext->channels;
            codecContext->channel_layout        = (ulong)ffmpeg.av_get_default_channel_layout(inputContext->channels);
            codecContext->sample_rate           = inputContext->sample_rate;
            codecContext->sample_fmt            = inputContext->sample_fmt;
            codecContext->bit_rate              = inputContext->bit_rate;
            codecContext->strict_std_compliance = ffmpeg.FF_COMPLIANCE_EXPERIMENTAL;
            codecContext->time_base             = ffmpeg.av_make_q(1, inputContext->sample_rate);

            // Initialise codec
            FE.Code(ffmpeg.avcodec_open2(codecContext, codec, null));

            return codecContext;
        }

        /// <summary>
        /// Creates a video codec as close as possible to the input codec provided with changes to accomodate clip settings.
        /// </summary>
        /// <param name="inputContext">Input codec</param>
        /// <returns>Output codec</returns>
        private unsafe AVCodecContext* CreateVideoContext(AVCodecContext* inputContext)
        {
            var codec = FE.Null(ffmpeg.avcodec_find_encoder_by_name("libvpx-vp9"),
                $"couldn't find libx264, make sure libav* libraries were compiled with it");

            var codecContext = FE.Null(ffmpeg.avcodec_alloc_context3(codec));

            int fps = ClipSettings.TargetFPS;

            // TODO: add settings for these
            //FE.Code(ffmpeg.av_opt_set(codecContext->priv_data, "preset", "slow", 0));
            //FE.Code(ffmpeg.av_opt_set(codecContext->priv_data, "tune", "zerolatency", 0));
            //FE.Code(ffmpeg.av_opt_set(codecContext->priv_data, "passlogfile", ClipSettings.FullFileName + ".stats", 0));
            //FE.Code(ffmpeg.av_opt_set(codecContext->priv_data, "fps", fps.ToString(), 0));

            long bitrate = (long)(ClipSettings.VideoBitrate * 1000000);
            
            codecContext->width         = ClipSettings.TargetWidth;
            codecContext->height        = ClipSettings.TargetHeight;
            codecContext->pix_fmt       = inputContext->pix_fmt;
            //codecContext->sample_aspect_ratio   = inputContext->sample_aspect_ratio;
            //codecContext->bit_rate      = bitrate;
            codecContext->time_base     = ffmpeg.av_make_q(1, fps);
            codecContext->thread_count = 10;
          //  codecContext->stats_out = (byte*)ffmpeg.av_fopen_utf8("test", "wb");
            //codecContext->framerate     = ffmpeg.av_make_q(fps, 1);
            //codecContext->max_b_frames  = 1;
            //codecContext->gop_size      = 10;
            ////codecContext->keyint_min    = 1;

            if (TwoPassEnabled)
                codecContext->flags |= IsFirstPass ? ffmpeg.AV_CODEC_FLAG_PASS1 : ffmpeg.AV_CODEC_FLAG_PASS2;

            // Initialise codec
            FE.Code(ffmpeg.avcodec_open2(codecContext, codec, null));

            return codecContext;
        }

        /// <summary>
        /// Creates audio streams from a list of audio video streams.  Note that if the clip settings specify to merge audio or disable specific
        /// audio tracks, this function will not create the same amount of output audio tracks as input audio tracks.
        /// </summary>
        /// <param name="streams">The input source audio streams</param>
        private unsafe void CreateAudioStreams(MediaInputStream[] streams)
        {
            // No audio in first pass
            if (TwoPassEnabled && IsFirstPass)
                return;

            var enabledStreams = streams.Where((stream) =>
            {
                var settings = ClipSettings.AudioSettings.Where((settings) => settings.TrackID == stream.StreamIndex).FirstOrDefault();
                return settings?.IsEnabled ?? false;
            }).ToList().ToArray();

            /*
             * If audio is merged, instead of creating one stream per track:
             * 1. Create one audio stream 
             * 2. Decide which of the available audio streams is the "best" and the output stream's encoding settings
             *    to copy the "best" audio stream
             * 3. Pass every audio stream to the AudioGraphFilter letting it handle mixing with a "amix" filter
             */
            if (ClipSettings.MergeAudio && enabledStreams.Length > 0)
            {
                var bestStream   = enabledStreams.Last();
                var newStream    = FE.Null(ffmpeg.avformat_new_stream(outputContext, null));
                var codecContext = CreateAudioContext(bestStream.DecoderContext);

                // Stream's time_base needs to be set, codecContext has it calculated already
                newStream->time_base = codecContext->time_base;

                // Copy codec params from the context into the new stream too
                FE.Code(ffmpeg.avcodec_parameters_from_context(newStream->codecpar, codecContext));

                OutputStreams.Add(new MediaOutputStream(newStream, OutputStreams.Count, codecContext,
                     new AudioGraphFilter(outputContext, newStream, ClipSettings, bestStream, enabledStreams),
                     ClipSettings.StartTime, ClipSettings.EndTime, enabledStreams));

                // We've processed every stream here
                return;
            }

            foreach (var stream in enabledStreams)
            {
                var newStream = FE.Null(ffmpeg.avformat_new_stream(outputContext, null));
                var codecContext = CreateAudioContext(stream.DecoderContext);

                // Stream's time_base needs to be set, codecContext has it calculated already
                newStream->time_base = codecContext->time_base;

                // Copy codec params from the context into the new stream too
                FE.Code(ffmpeg.avcodec_parameters_from_context(newStream->codecpar, codecContext));

                OutputStreams.Add(new MediaOutputStream(newStream, OutputStreams.Count, codecContext,
                    new AudioGraphFilter(outputContext, newStream, ClipSettings, stream, stream),
                    ClipSettings.StartTime, ClipSettings.EndTime, stream));
            }
        }

        /// <summary>
        /// Creates video streams from a list of source video streams
        /// </summary>
        /// <param name="streams">The input source video streams</param>
        private unsafe void CreateVideoStreams(MediaInputStream[] streams)
        {
            foreach (var stream in streams)
            {
                var newStream = FE.Null(ffmpeg.avformat_new_stream(outputContext, null));
                var codecContext = CreateVideoContext(stream.DecoderContext);

                // Stream's time_base needs to be set, codecContext has it calculated already
                newStream->time_base = codecContext->time_base;
                newStream->avg_frame_rate = codecContext->framerate;

                // Copy codec params from the context into the new stream too
                FE.Code(ffmpeg.avcodec_parameters_from_context(newStream->codecpar, codecContext));

                OutputStreams.Add(new MediaOutputStream(newStream, OutputStreams.Count, codecContext,
                    new VideoGraphFilter(outputContext, stream, newStream, ClipSettings),
                    ClipSettings.StartTime, ClipSettings.EndTime, stream));
            }
        }

        /// <summary>
        /// Creates output streams from a list of streams that the source video has
        /// </summary>
        /// <param name="streams">List of streams from the source video</param>
        public unsafe void CreateStreams(List<MediaInputStream> streams)
        {
            // If copy packets is enabled, copy streams too 
            if (ClipSettings.CopyPackets)
            {
                foreach (var stream in streams)
                {
                    var newStream = FE.Null(ffmpeg.avformat_new_stream(outputContext, null));
                    FE.Code(ffmpeg.avcodec_parameters_copy(newStream->codecpar, stream.Stream->codecpar));

                    OutputStreams.Add(new MediaOutputStream(newStream, OutputStreams.Count, null, null, ClipSettings.StartTime, ClipSettings.EndTime, stream));
                }
                return;
            }

            //if (ClipSettings.ExportMode == ClipExportMode.Both || ClipSettings.ExportMode == ClipExportMode.AudioOnly)
            //    CreateAudioStreams(streams.Where((stream) => stream.IsAudio).ToList().ToArray());

            if (ClipSettings.ExportMode == ClipExportMode.Both || ClipSettings.ExportMode == ClipExportMode.VideoOnly)
                CreateVideoStreams(streams.Where((stream) => stream.IsVideo).ToList().ToArray());

            // TODO: handle other stream types, subtitles etc
        }

        public unsafe void HandleRawPacket(AVPacket* packet)
        {
            var inputStreamIndex = packet->stream_index;
            var outputStream = OutputStreams.Where(x => x.HasStreamIndex(inputStreamIndex)).FirstOrDefault();
            var filter = outputStream?.GraphFilter;

            if (outputStream == null || filter == null || !outputStream.CheckPTS(inputStreamIndex, packet->pts))
                return;

            var filteredFrame = FE.Null(ffmpeg.av_frame_alloc());
            var encodedPacket = FE.Null(ffmpeg.av_packet_alloc());

            FE.Code(ffmpeg.avcodec_send_packet(outputStream.EncoderContext, packet));

            while (true)
            {
                int res = ffmpeg.avcodec_receive_packet(outputStream.EncoderContext, encodedPacket);
                if (res == ffmpeg.AVERROR(ffmpeg.EAGAIN) || res == ffmpeg.AVERROR_EOF || !FE.Code(res))
                    break;

                outputStream.WritePacket(inputStreamIndex, encodedPacket, outputContext, TwoPassEnabled && IsFirstPass);
            }
        }

        /// <summary>
        /// Attempts to write a packet from the input stream to this video's corresponding output stream.  This 
        /// VideoOutputTask may choose to ignore the incoming packet.
        /// </summary>
        /// <param name="packet">The input packet</param>
        /// <exception cref="MediaProcessingException"></exception>
        public unsafe void HandleFrame(AVPacket* packet, AVFrame* frame)
        {
            var inputStreamIndex = packet->stream_index;
            var outputStream     = OutputStreams.Where(x => x.HasStreamIndex(inputStreamIndex)).FirstOrDefault();
            var filter           = outputStream?.GraphFilter;

            if (outputStream == null || filter == null || !outputStream.CheckPTS(inputStreamIndex, packet->pts))
                return;

            var filteredFrame = FE.Null(ffmpeg.av_frame_alloc());
            var encodedPacket = FE.Null(ffmpeg.av_packet_alloc());

            Trace.WriteLine($"writing packet {outputStream.PacketCount} in {inputStreamIndex}/{outputStream.StreamIndex} (IsVideo={outputStream.IsVideo}, type={frame->pict_type})");

            // Write decoded frame into the filter
            FE.Code(ffmpeg.av_buffersrc_write_frame(filter.GetBufferContext(inputStreamIndex), frame));

            // Read all frames produced by the filter and pass them into the encoder
            while (true)
            {
                int res = ffmpeg.av_buffersink_get_frame(filter.BufferSinkContext, filteredFrame);
                if (res == ffmpeg.AVERROR(ffmpeg.EAGAIN) || res == ffmpeg.AVERROR_EOF || !FE.Code(res))
                    break;

                int a = ffmpeg.avcodec_send_frame(outputStream.EncoderContext, filteredFrame);
            }

            // Read all packets produced by the encoder and write them into the file
            while (true)
            {
                int res = ffmpeg.avcodec_receive_packet(outputStream.EncoderContext, encodedPacket);
                if (res == ffmpeg.AVERROR(ffmpeg.EAGAIN) || res == ffmpeg.AVERROR_EOF || !FE.Code(res))
                    break;

                outputStream.WritePacket(inputStreamIndex, encodedPacket, outputContext, TwoPassEnabled && IsFirstPass);
            }

            ffmpeg.av_packet_free(&encodedPacket);
            ffmpeg.av_frame_free(&filteredFrame);
        }

        /// <summary>
        /// Prepares the output context to write frames to disk
        /// <exception cref="MediaProcessingException"></exception>
        /// </summary>
        public unsafe void Initialise()
        {
            if (ffmpeg.avio_open(&outputContext->pb, ClipSettings.FullFileName, ffmpeg.AVIO_FLAG_WRITE) < 0)
                throw new MediaProcessingException($"couldn't open {ClipSettings.FullFileName} for writing");

            if (ffmpeg.avformat_write_header(outputContext, null) < 0)
                throw new MediaProcessingException($"couldn't write header to {ClipSettings.FullFileName}");
        }

        /// <summary>
        /// Performs all cleanup.  The task cannot be used after this function is called
        /// </summary>
        public unsafe void Finalise()
        {
            FE.Code(ffmpeg.av_write_trailer(outputContext));
            FE.Code(ffmpeg.avio_close(outputContext->pb));

            foreach (var stream in OutputStreams)
                stream.Dispose();

            ffmpeg.avformat_free_context(outputContext);
        }

        /// <summary>
        /// Finishes the first pass for this output task.
        /// </summary>
        public unsafe void FinishFirstPass(List<MediaInputStream> inputStreams)
        {
            IsFirstPass = false;

            // Finish first pass & clear old streams
            Finalise();
            OutputStreams.Clear();

            // Create new format context for pass 2
            AVFormatContext* formatContext;
            FE.Code(ffmpeg.avformat_alloc_output_context2(&formatContext, null, null, ClipSettings.FullFileName));
            FE.Null(formatContext);

            // Replace old format context
            outputContext = formatContext;

            // Initialise new format context & streams for pass 2
            CreateStreams(inputStreams);
            Initialise();
        }
    }
}
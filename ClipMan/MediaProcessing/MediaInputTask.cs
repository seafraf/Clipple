using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClipMan.MediaProcessing
{
    /// <summary>
    /// Reads a single input video and creates 1 or more output videos using the passed in VideoOutputTask class instances
    /// </summary>
    internal class MediaInputTask : IDisposable
    {
        public unsafe MediaInputTask(string inputFile, MediaOutputTask[] outputTasks)
        {
            this.inputFile      = inputFile;
            this.outputTasks    = outputTasks;
        }

        #region Members
        private readonly string inputFile;
        private readonly MediaOutputTask[] outputTasks;
        #endregion

        #region Properties
        /// <summary>
        /// Called with the current position in the file
        /// </summary>
        public event EventHandler<double[]> OnProgressUpdate;

        /// <summary>
        /// TODO
        /// </summary>
        public event EventHandler<string> OnStatusUpdate;
        #endregion

        public unsafe void ProcessContexts()
        {
            var formatContext = ffmpeg.avformat_alloc_context();
            if (formatContext == null)
                throw new MediaProcessingException("couldn't allocate memory for AVFormatContext");

            var input = ffmpeg.avformat_open_input(&formatContext, inputFile, null, null);
            if (input != 0)
                throw new MediaProcessingException($"couldn't open {inputFile}");

            if (ffmpeg.avformat_find_stream_info(formatContext, null) < 0)
            {
                ffmpeg.avformat_close_input(&formatContext);
                throw new MediaProcessingException($"couldn't open {inputFile}");
            }

            var codecContextArray = new AVCodecContext*[formatContext->nb_streams];
            var streams           = new List<MediaInputStream>();

            // Create streams for the output videos
            for (var i = 0; i < formatContext->nb_streams; i++)
            {
                var stream          = formatContext->streams[i];

                var codec           = FE.Null(ffmpeg.avcodec_find_decoder(stream->codecpar->codec_id));
                var codecContext    = FE.Null(ffmpeg.avcodec_alloc_context3(codec));

                FE.Code(ffmpeg.avcodec_parameters_to_context(codecContext, stream->codecpar),
                    $"couldn't copy codec params for stream {i}");

                FE.Code(ffmpeg.avcodec_open2(codecContext, codec, null),
                    $"can't open codec for stream {i}");

                streams.Add(new MediaInputStream(stream, i, codecContext, ffmpeg.av_guess_frame_rate(formatContext, stream, null)));

                // Store context in an array, slightly faster for decoder loop
                codecContextArray[i] = codecContext;
            }

            foreach (var outputTask in outputTasks)
                outputTask.CreateStreams(streams);

            // Get handles ready to write to disk
            foreach (var outputTask in outputTasks)
                outputTask.Initialise();

            var frame  = FE.Null(ffmpeg.av_frame_alloc());
            var packet = FE.Null(ffmpeg.av_packet_alloc());

            // Write packets
            while (ffmpeg.av_read_frame(formatContext, packet) >= 0)
            {
                var interestedTasks = new List<MediaOutputTask>();
                foreach (var outputTask in outputTasks)
                {
                    if (outputTask.HandlePacket(packet))
                        interestedTasks.Add(outputTask);
                }

                if (interestedTasks.Count == 0)
                {
                    ffmpeg.av_packet_unref(packet);
                    continue;
                }

                var codecContext = codecContextArray[packet->stream_index];

                // Send packet to codec
                int res = ffmpeg.avcodec_send_packet(codecContext, packet);

                // Read frames
                while (res >= 0)
                {
                    res = ffmpeg.avcodec_receive_frame(codecContext, frame);
                    if (res == ffmpeg.AVERROR(ffmpeg.EAGAIN) || res == ffmpeg.AVERROR_EOF)
                        break;

                    if (res < 0)
                        throw new MediaProcessingException("couldn't receive frame?");

                    foreach (var outputTask in interestedTasks)
                        outputTask.HandleFrame(packet, frame);
                }

                // Send an array of progress updates to any event listener
                SetProgress(outputTasks.Select((t) => t.CompletionEstimate).ToArray());

                ffmpeg.av_packet_unref(packet);
            }

            // Write trailer (finish) all clips
            foreach (var clipContext in outputTasks)
                clipContext.Finalise();

            ffmpeg.av_packet_free(&packet);
            ffmpeg.av_frame_free(&frame);

            ffmpeg.avformat_close_input(&formatContext);
        }

        private void SetStatus(string status)
        {
            OnStatusUpdate?.Invoke(this, status);
        }

        private void SetProgress(double[] progress)
        {
            OnProgressUpdate?.Invoke(this, progress);
        }

        public unsafe void Dispose()
        {
            //foreach (var outputContext in outputContexts)
            //{
            //    if (outputContext != null)
            //    {
            //        ffmpeg.avformat_close_
            //    }
            //}
        }
    }
}

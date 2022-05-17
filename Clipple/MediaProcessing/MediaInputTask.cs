using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.MediaProcessing
{
    /// <summary>
    /// Reads a single input video and creates 1 or more output videos using the passed in VideoOutputTask class instances
    /// </summary>
    internal class MediaInputTask
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
        public event EventHandler<double[]>? OnProgressUpdate;

        /// <summary>
        /// TODO
        /// </summary>
        public event EventHandler<string>? OnStatusUpdate;
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

            // Initialise tasks
            foreach (var outputTask in outputTasks)
            {
                outputTask.CreateStreams(streams);
                outputTask.Initialise();
            }   

            // Batch together tasks that overlap and 
            var taskBatches = BatchOutputTasks();

            foreach (var (pts, batch) in taskBatches)
            {
                // Seek to the beginning of this batch of clips
                FE.Code(ffmpeg.av_seek_frame(formatContext, 0, pts, ffmpeg.AVSEEK_FLAG_BACKWARD));

                ProcessBatch(formatContext, codecContextArray, batch);

                var twoPassTasks = batch.Where(x => x.TwoPassEnabled).ToList();
                if (twoPassTasks.Count > 0)
                {
                    // Go back to the start for this batch of clips
                    FE.Code(ffmpeg.av_seek_frame(formatContext, 0, pts, ffmpeg.AVSEEK_FLAG_BACKWARD));

                    // Finish pass 1 and prepare for pass 2
                    foreach (var task in twoPassTasks)
                        task.FinishFirstPass(streams);

                    // Perform second pass for each two-pass enabled task
                    ProcessBatch(formatContext, codecContextArray, twoPassTasks);
                }
            }

            // Write trailer and close file handles
            foreach (var clipContext in outputTasks)
                clipContext.Finalise();

            ffmpeg.avformat_close_input(&formatContext);
        }

        private unsafe void ProcessBatch(AVFormatContext* formatContext, AVCodecContext*[] codecContextArray, List<MediaOutputTask> batch)
        {
            var frame  = FE.Null(ffmpeg.av_frame_alloc());
            var packet = FE.Null(ffmpeg.av_packet_alloc());

            while (ffmpeg.av_read_frame(formatContext, packet) >= 0)
            {
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

                    // Run each frame through each task that was interested in the packet
                    foreach (var task in batch)
                        task.HandleFrame(packet, frame);
                }

                if (batch.All(x => x.IsFinished))
                    break;

                // Send an array of progress updates to any event listener
                SetProgress(outputTasks.Select((t) => t.CompletionEstimate).ToArray());

                ffmpeg.av_packet_unref(packet);
            }

            ffmpeg.av_packet_free(&packet);
            ffmpeg.av_frame_free(&frame);
        }

        /// <summary>
        /// Create a list of batched output tasks, where each entry contains the start of 1 or more tasks.
        /// </summary>
        /// <returns></returns>
        private List<(long, List<MediaOutputTask>)> BatchOutputTasks()
        {
            var batches = new List<(long, List<MediaOutputTask>)>();
            if (outputTasks.Length == 0)
                return batches;

            var orderedTasks    = outputTasks.OrderBy((x) => x.VideoStreamStartTimePTS).ToList();
            var firstTask       = orderedTasks.First();

            var currentBatch = (firstTask.VideoStreamStartTimePTS, new List<MediaOutputTask>()
            {
                firstTask
            });
            long currentBatchEnd = firstTask.VideoStreamEndPTS;

            for (int i = 1; i < orderedTasks.Count; i++)
            {
                var task = orderedTasks[i];
                if (task.VideoStreamStartTimePTS <= currentBatchEnd)
                {
                    // Include this clip as part of the current batch
                    currentBatch.Item2.Add(task);

                    // Allow this clip to extend the current intersection area
                    currentBatchEnd = Math.Max(currentBatchEnd, task.VideoStreamEndPTS);
                }
                else
                {
                    // Create new batch
                    batches.Add(currentBatch);
                    currentBatch = (task.VideoStreamStartTimePTS, new List<MediaOutputTask>()
                    {
                        task
                    });

                    currentBatchEnd = task.VideoStreamEndPTS;
                }
            }

            // Add last/first batch to the list
            batches.Add(currentBatch);

            return batches;
        }

        private void SetStatus(string status)
        {
            OnStatusUpdate?.Invoke(this, status);
        }

        private void SetProgress(double[] progress)
        {
            OnProgressUpdate?.Invoke(this, progress);
        }
    }
}

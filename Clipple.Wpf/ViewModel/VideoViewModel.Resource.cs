using Clipple.FFMPEG;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Clipple.ViewModel
{
    public partial class VideoViewModel
    {
        private static readonly int MaximumJobs = Math.Max(1, Environment.ProcessorCount / 2);
        private static readonly Semaphore RateLimitSemaphore = new(MaximumJobs, MaximumJobs);

        private async Task<ImageSource> GetThumbnail()
        {
            if (!Directory.Exists(CachePath))
                Directory.CreateDirectory(CachePath);

            var path = Path.Combine(CachePath, "thumb01.png");
            if (!File.Exists(path))
            {
                // Wait for a semaphore slot to become available
                await Task.Run(() => RateLimitSemaphore.WaitOne());

                var stopwatch = Stopwatch.StartNew();
                var engine = new ThumbnailEngine(FilePath, path, TimeSpan.Zero);
                if (await engine.Run() != 0)
                {
                    App.Logger.Log($"Failed to generate thumbnail for {FilePath}", engine.Output.ToString());
                }
                else
                {
                    stopwatch.Stop();
                    App.Logger.Log($"Generated thumbnail for {FilePath} in {stopwatch.ElapsedMilliseconds}");
                }

                RateLimitSemaphore.Release();
            }

            return new BitmapImage(new Uri(path));
        }

        private async Task BuildAudioWaveforms()
        {
            if (AudioStreams == null)
                return;

            if (!Directory.Exists(CachePath))
                Directory.CreateDirectory(CachePath);

            foreach (var audioStream in AudioStreams)
            {
                var path = Path.Combine(CachePath, $"waveform{audioStream.AudioStreamIndex:00}.png");

                if (!File.Exists(path))
                {
                    // Wait for a semaphore slot to become available
                    await Task.Run(() => RateLimitSemaphore.WaitOne());

                    var stopwatch = Stopwatch.StartNew();
                    var engine = new WaveformEngine(FilePath, path, audioStream.AudioStreamIndex);
                    if (await engine.Run() != 0)
                    {
                        App.Logger.Log($"Failed to generate waveform {audioStream.AudioStreamIndex} for {FilePath}", engine.Output.ToString());
                    }
                    else
                    {
                        stopwatch.Stop();
                        App.Logger.Log($"Generated waveform {audioStream.AudioStreamIndex} for {FilePath} in {stopwatch.ElapsedMilliseconds}");
                    }

                    RateLimitSemaphore.Release();
                }

                var img = new BitmapImage(new Uri(path));
                img.Freeze();
                audioStream.Waveform = img;
            }
        }

        #region Properties
        /// <summary>
        /// A thumbnail for this videofrien
        /// </summary>
        private ImageSource? thumbnail;
        [JsonIgnore]
        public ImageSource? Thumbnail
        {
            get => thumbnail;
            set => SetProperty(ref thumbnail, value);
        }
        #endregion
    }
}

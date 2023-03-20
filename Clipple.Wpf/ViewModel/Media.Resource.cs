using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using Clipple.FFMPEG;

namespace Clipple.ViewModel;

public partial class Media
{
    private static readonly int       MaximumJobs        = 200; // Math.Max(1, Environment.ProcessorCount / 2);
    private static readonly Semaphore RateLimitSemaphore = new(MaximumJobs, MaximumJobs);

    #region Methods

    private async Task BuildAudioWaveforms()
    {
        if (!Directory.Exists(CachePath))
            Directory.CreateDirectory(CachePath);

        foreach (var audioStream in AudioStreams)
        {
            var path = Path.Combine(CachePath, $"waveform{audioStream.AudioStreamIndex:00}.png");

            if (!File.Exists(path))
            {
                // Wait for a semaphore slot to become available
                await Task.Run(() => RateLimitSemaphore.WaitOne());
                var engine = new WaveformEngine(FilePath, path, audioStream.AudioStreamIndex);
                if (await engine.Run() != 0)
                    App.Notifications.NotifyWarning($"Failed to generate waveform ({audioStream.AudioStreamIndex}) for {FilePath}");

                RateLimitSemaphore.Release();
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.UriSource   = new(path);
            image.EndInit();

            audioStream.Waveform = image;
        }
    }

    #endregion
}
using Clipple.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Clipple.FFMPEG
{
    /// <summary>
    /// This class was previouosly designed to support multiple inputs and multiple outputs but has been changed to support
    /// only a singular input and output, this is because:
    /// - Seeking to specific point using the input is considerably faster 
    /// - ffmpeg will only output stats for the last output processed, so there was no way of doing a progress bar using multiple outputs
    /// </summary>
    public class Engine
    {
        public Engine(string executablePath, string inputPath, ClipViewModel clip, bool firstPass)
        {
            ExecutablePath = executablePath;

            Input  = new MediaInput(inputPath, clip);
            Output = new MediaOutput(clip, firstPass);
        }

        #region Properties
        /// <summary>
        /// Full path to ffmpeg.exe
        /// </summary>
        public string ExecutablePath { get; }

        /// <summary>
        /// Input media class.  This should ToString into ffmpeg arguments.
        /// </summary>
        public MediaInput Input { get; }

        /// <summary>
        /// Output media class.  This should ToString into ffmpeg arguments.
        /// </summary>
        public MediaOutput Output { get; }
        #endregion

        #region Events
        /// <summary>
        /// Called when ffmpeg prints a "report", this can be used to estimate the progress of the ffmpeg job
        /// </summary>
        public event EventHandler<EngineProcessStatistics>? OnProcessStats;

        /// <summary>
        /// Called whenever ffmpeg prints anything to standard output or standard error
        /// </summary>
        public event EventHandler<string>? OnOutput;
        #endregion

        #region Methods
        /// <summary>
        /// Runs the ffmpeg executable with arguments from the input and output classes.
        /// </summary>
        /// <param name="token">A token that can be used to exit the ffmpeg process. Note, cancelling the process
        /// will result in a TaskCanceledException</param>
        /// <returns>The exit code of the process.</returns>
        public async Task<int> Run(CancellationToken token)
        {
            var argumentStr = $"-y -vsync cfr {Input} {Output}";
            OnOutput?.Invoke(this, $"ffmpeg.exe {argumentStr}");

            var process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName                = ExecutablePath,
                    Arguments               = argumentStr,
                    CreateNoWindow          = true,
                    UseShellExecute         = false,
                    RedirectStandardError   = true,
                    RedirectStandardOutput  = true
                }
            };

            process.ErrorDataReceived  += OnProcessOutput;
            process.OutputDataReceived += OnProcessOutput;
            process.Start();
            process.BeginErrorReadLine();
            process.BeginOutputReadLine();

            try
            {
                await process.WaitForExitAsync(token);
            }
            catch (TaskCanceledException)
            {
                if (!process.HasExited)
                    process.Kill();
            }

            if (Output.TwoPassEncoding && !Output.IsFirstPass)
            {
                // Delete temp files
                File.Delete($"{Output.OutputFile}-0.log");
                File.Delete($"{Output.OutputFile}-0.log.mbtree");
            }

            return process.ExitCode;
        }

        private void OnProcessOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            OnOutput?.Invoke(this, e.Data);
            
            try
            {
                var bitrateRegex = new Regex(@"bitrate=\s*([0-9]*\.?[0-9]*?)kbits/s");
                var frameRegex   = new Regex(@"frame=\s*([0-9]*)");
                var timeRegex    = new Regex(@"time=\s*([^ ]*)");

                if (bitrateRegex.Match(e.Data) is var bitrateMatch 
                    && frameRegex.Match(e.Data) is var frameMatch
                    && timeRegex.Match(e.Data) is var timeMatch)
                {
                    // At least frame needs to match
                    if (!frameMatch.Success)
                        return;

                    var bitrate = bitrateMatch.Success ? double.Parse(bitrateMatch.Groups[1].Value) : 0.0;
                    var frame   = int.Parse(frameMatch.Groups[1].Value);
                    var time    = timeMatch.Success ? TimeSpan.Parse(timeMatch.Groups[1].Value) : TimeSpan.Zero;
  
                    OnProcessStats?.Invoke(this, new EngineProcessStatistics(frame, time, bitrate));              
                }
            }
            catch (Exception)
            { }
        }
        #endregion
    }
}

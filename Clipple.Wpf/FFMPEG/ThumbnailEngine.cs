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

namespace Clipple.FFMPEG
{
    public class ThumbnailEngine
    {
        public ThumbnailEngine(string inputPath, string outputPath, TimeSpan frameTime)
        {
            ExecutablePath  = Path.Combine(App.LibPath, "ffmpeg.exe"); ;
            InputPath       = inputPath;
            OutputPath      = outputPath;
            FrameTime       = frameTime;
        }

        #region Properties
        /// <summary>
        /// Full path to ffmpeg.exe
        /// </summary>
        private string ExecutablePath { get; }

        /// <summary>
        /// Input media path
        /// </summary>
        private string InputPath { get; }

        /// <summary>
        /// Output media path
        /// </summary>
        private string OutputPath { get; }

        /// <summary>
        /// The video time to take the thumbnail at
        /// </summary>
        private TimeSpan FrameTime { get; }

        /// <summary>
        /// Output from ffmpeg
        /// </summary>
        public StringBuilder Output { get; } = new();
        #endregion

        #region Methods
        /// <summary>
        /// Runs the thumbnail engine process.
        /// </summary>
        /// <returns>The exit code of the process.</returns>
        public async Task<int> Run()
        {
            var argumentStr = $"-y -ss {FrameTime} -i \"{InputPath}\" -vframes 1 {OutputPath}";
            Output.AppendLine($"ffmpeg.exe {argumentStr}");

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
                await process.WaitForExitAsync();
            }
            catch (TaskCanceledException e)
            {
                throw e;
            }

            return process.ExitCode;
        }


        private void OnProcessOutput(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
                return;

            Output.AppendLine(e.Data);
        }
        #endregion
    }
}

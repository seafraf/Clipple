using Clipple.FFMPEG;
using Clipple.Types;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using static System.Windows.Forms.AxHost;

namespace Clipple.ViewModel
{
    public class ExportingClip : ObservableObject
    {
        public ExportingClip(Media media)
        {
            this.media = media;
            CancelCommand = new RelayCommand(() => cancellationTokenSource.Cancel());

            // Run the export task as soon as the ClipExporter view model is created
            Task.Run(async () =>
            {
                try
                {
                    await Export();
                }
                catch (OperationCanceledException)
                {
                    Status = ClipExportingStatus.Cancelled;
                }
            });
        }

        #region Members
        private Media media;
        private bool isIndeterminate = true;
        private double completionFactor = 0.0;
        private CancellationTokenSource cancellationTokenSource = new();
        private ClipExportingStatus status = ClipExportingStatus.Waiting;
        #endregion

        #region Properties

        /// <summary>
        /// The media whose clip is being processed by this export.
        /// </summary>
        public Media Media
        {
            get => media;
            set => SetProperty(ref media, value);
        }

        /// <summary>
        /// All output given from the clip engine
        /// </summary>
        public StringBuilder Output { get; } = new();

        /// <summary>
        /// Whether or not the progress of the export can be determined
        /// </summary>
        public bool IsIndeterminate
        {
            get => isIndeterminate;
            set => SetProperty(ref isIndeterminate, value);
        }

        /// <summary>
        /// Factor representing how complete the export process is.  This will be zero when IsIndeterminate = true
        /// </summary>
        public double CompletionFactor
        {
            get => completionFactor;
            set => SetProperty(ref completionFactor, value);
        }

        /// <summary>
        /// The status 
        /// </summary>
        public ClipExportingStatus Status
        {
            get => status;
            set => SetProperty(ref status, value);
        }
        #endregion


        #region Methods
        private async Task Export()
        {
            var clip = media.Clip;
            var inputFile = media.FileInfo.FullName;

            // Run first pass if required
            if (clip.TwoPassEncoding)
            {
                var firstJob = new ClipEngine(inputFile, Media, true);
                firstJob.OnProcessStats += OnEngineStats;
                firstJob.OnOutput       += OnEngineOutput;

                Status = ClipExportingStatus.ProcessingFirstPass;
                var exitCode = await firstJob.Run(cancellationTokenSource.Token);

                // Don't continue with the second pass if the first pass failed
                if (exitCode != 0)
                {
                    Status = ClipExportingStatus.Failed;
                    return;
                }
            }

            // Run real job (first job ran if two pass is not enabled)
            var mainJob = new ClipEngine(inputFile, Media, false);
            mainJob.OnProcessStats += OnEngineStats;
            mainJob.OnOutput += OnEngineOutput;

            Status = ClipExportingStatus.Processing;
            Status = (await mainJob.Run(cancellationTokenSource.Token)) == 0 ? ClipExportingStatus.Finished : ClipExportingStatus.Failed;
        }

        private void OnEngineOutput(object? sender, string e)
        {
            Output.AppendLine(e);
            OnPropertyChanged(nameof(Output));
        }

        private void OnEngineStats(object? sender, EngineProcessStatistics e)
        {
            var clipDuration = media.Clip.Duration;

            if (clipDuration.TotalSeconds > 0.0 && e.Time.TotalSeconds > 0)
            {
                CompletionFactor = Math.Min(Math.Max(0.0, e.Time.TotalSeconds / clipDuration.TotalSeconds), 1.0);
                IsIndeterminate = false;
            }
        }
        #endregion

        #region Commands
        public ICommand CancelCommand { get; }
        #endregion
    }
}

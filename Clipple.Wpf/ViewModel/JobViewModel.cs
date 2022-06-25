using Clipple.FFMPEG;
using Clipple.Types;
using Clipple.View;
using Clipple.ViewModel;
using MahApps.Metro.IconPacks;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Clipple.ViewModel
{
    /// <summary>
    /// Represents a clip proessing job
    /// </summary>
    public class JobViewModel : ObservableObject
    {
        public JobViewModel(VideoViewModel videoViewModel, ClipViewModel clip)
        {
            LogsCommand = new RelayCommand(() =>
            {
                LogsViewOpen = true;

                var view = new JobLogsView(this);
                view.Closed += (s, e) => LogsViewOpen = false;
                view.Show();
            });

            VideoViewModel              = videoViewModel;
            Clip                        = clip;
            Status                      = ClipProcessingStatus.InQueue;
        }

        /// <summary>
        /// The video that owns this clip
        /// </summary>
        public VideoViewModel VideoViewModel { get; }

        /// <summary>
        /// The clip to process
        /// </summary>
        public ClipViewModel Clip { get; }

        /// <summary>
        /// Current output from this job as provided by the FFmpeg engine.
        /// </summary>
        public StringBuilder JobOutput { get; } = new StringBuilder();

        /// <summary>
        /// The status of this job, this will set the StatusIcon and StatusString properties.
        /// </summary>
        private ClipProcessingStatus status;
        public ClipProcessingStatus Status
        {
            get => status;
            set
            {
                SetProperty(ref status, value);

                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusString));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(ProgressIndeterminate));
                OnPropertyChanged(nameof(CompletionFactor));
            }
        }

        /// <summary>
        /// An icon denoting the status of this job
        /// </summary>
        public PackIconMaterialDesignKind StatusIcon
        {
            get
            {
                switch (status)
                {
                    case ClipProcessingStatus.InQueue:
                        return PackIconMaterialDesignKind.Queue;
                    case ClipProcessingStatus.Processing:
                    case ClipProcessingStatus.ProcessingFirstPass:
                        return PackIconMaterialDesignKind.RotateRight;
                    case ClipProcessingStatus.Finished:
                        return PackIconMaterialDesignKind.Done;
                    case ClipProcessingStatus.Cancelled:
                        return PackIconMaterialDesignKind.Cancel;
                    case ClipProcessingStatus.Failed:
                    default:
                        return PackIconMaterialDesignKind.Warning;
                }
            }
        }

        /// <summary>
        /// A string describing the current status of this job
        /// </summary>
        public string StatusString
        {
            get
            {
                switch (status)
                {
                    case ClipProcessingStatus.InQueue:
                        return "In queue";
                    case ClipProcessingStatus.Processing:
                        return "Processing";
                    case ClipProcessingStatus.ProcessingFirstPass:
                        return "First pass";
                    case ClipProcessingStatus.Finished:
                        return "Finished";
                    case ClipProcessingStatus.Cancelled:
                        return "Cancelled";
                    case ClipProcessingStatus.Failed:
                    default:
                        return "Failed";
                }
            }
        }

        /// <summary>
        /// Color used for status string and icon
        /// </summary>
        public Brush StatusColor
        {
            get
            {
                switch (status)
                {
                    case ClipProcessingStatus.InQueue:
                        // This is actually handled by a data trigger in XAML
                        return Brushes.White;
                    case ClipProcessingStatus.Processing:
                    case ClipProcessingStatus.ProcessingFirstPass:
                        return Brushes.Turquoise;
                    case ClipProcessingStatus.Finished:
                        return Brushes.ForestGreen;
                    case ClipProcessingStatus.Cancelled:
                    case ClipProcessingStatus.Failed:
                    default:
                        return Brushes.Red;
                }
            }
        }

        /// <summary>
        /// True if the progress cannot currently be determined 
        /// </summary>
        public bool ProgressIndeterminate
        {
            get
            {
                // Can't determine the progress of the first pass
                return status == ClipProcessingStatus.ProcessingFirstPass;
            }
        }

        /// <summary>
        /// Statisitics string, provided by ffmpeg
        /// </summary>
        private string statistics = "";
        public string Statistics
        {
            get => statistics;
            set => SetProperty(ref statistics, value);
        }

        /// <summary>
        /// A factor (0-1) describing how close this job is to being finished
        /// </summary>
        private double completionFactor;
        public double CompletionFactor
        {
            get
            {
                switch (status)
                {
                    
                    case ClipProcessingStatus.Processing:
                    case ClipProcessingStatus.ProcessingFirstPass:
                        return completionFactor;
                    case ClipProcessingStatus.Finished:
                        return 1.0;
                    case ClipProcessingStatus.InQueue:
                    case ClipProcessingStatus.Cancelled:
                    case ClipProcessingStatus.Failed:
                    default:
                        return 0.0;
                }
            }
            set => SetProperty(ref completionFactor, value);
        }

        /// <summary>
        /// True if the logs view window for this job is open
        /// </summary>
        private bool logsViewOpen = false;
        public bool LogsViewOpen
        {
            get => logsViewOpen;
            set => SetProperty(ref logsViewOpen, value);
        }

        #region Methods

        /// <summary>
        /// Called by the FFmpeg when processing the second pass (or the first and only pass when two pass encoding is not used).
        /// </summary>
        /// <param name="stat">Information regarding the current progress</param>
        /// <param name="firstPass">Whether or not this output came from the first or second pass (always false if the clip does not use two pass encoding)</param>
        public void NotifyStats(EngineProcessStatistics stat, bool firstPass)
        {
            Statistics = $"{stat.Bitrate:N2}kbit/s";

            if (Clip.Duration.TotalSeconds != 0.0)
                CompletionFactor = Math.Min(Math.Max(0.0, stat.Time.TotalSeconds / Clip.Duration.TotalSeconds), 1.0);
        }

        /// <summary>
        /// Called by the FFmpeg engine when it receives std(out|err) output from the ffmpeg.exe process.
        /// </summary>
        /// <param name="output">The line of output</param>
        public void RecordOutput(string output)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                JobOutput.AppendLine(output);
                OnPropertyChanged(nameof(JobOutput));
            });
        }

        /// <summary>
        /// Reset the job.  This should be called before each new run of this job.
        /// </summary>
        public void Reset()
        {
            Status           = ClipProcessingStatus.InQueue;
            CompletionFactor = 0.0;
            Statistics       = "";
            JobOutput.Clear();
        }
        #endregion

        #region Commands
        public ICommand LogsCommand { get; }
        #endregion
    }
}

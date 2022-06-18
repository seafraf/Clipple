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
    public class JobViewModel : ObservableObject
    {
        public JobViewModel(VideoViewModel videoViewModel, List<ClipViewModel> clips, bool enablePostProcessingActions)
        {
            LogsCommand = new RelayCommand(() =>
            {
                LogsViewOpen = true;

                var view = new JobLogsView(this);
                view.Closed += (s, e) => LogsViewOpen = false;
                view.Show();
            });

            VideoViewModel              = videoViewModel;
            Clips                       = clips;
            EnablePostProcessingActions = enablePostProcessingActions;

            var remainingClipNames = new List<string>();
            for (int i = 1; i < clips.Count; i++)
                remainingClipNames.Add(clips[i].FullFileName);

            RemainingClipNames = string.Join("\n", remainingClipNames);
            Status = ClipProcessingStatus.InQueue;

            foreach (var clip in clips)
                clipCompletionFactors[clip] = 0.0;
        }

        #region Members
        private readonly Dictionary<ClipViewModel, double> clipCompletionFactors = new();
        private readonly Dictionary<string, LogOutputViewModel> logOutputLookup = new();
        #endregion

        public VideoViewModel VideoViewModel { get; }
        public List<ClipViewModel> Clips { get; }
        public bool EnablePostProcessingActions { get; }
        public int SuccesfulJobCount { get; set; }

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

                switch (value)
                {
                    case ClipProcessingStatus.InQueue:
                        StatusIcon = PackIconMaterialDesignKind.Queue;
                        StatusString = "In queue";
                        ProgressIndeterminate = false;
                        break;
                    case ClipProcessingStatus.Failed:
                        StatusIcon = PackIconMaterialDesignKind.Warning;
                        StatusString = "Failed";
                        ProgressIndeterminate = false;
                        break;
                    case ClipProcessingStatus.Processing:
                        StatusIcon = PackIconMaterialDesignKind.RotateRight;
                        StatusString = "Running";
                        ProgressIndeterminate = true;
                        break;
                    case ClipProcessingStatus.ProcessingFirstPass:
                        StatusIcon = PackIconMaterialDesignKind.RotateRight;
                        StatusString = "First pass";
                        ProgressIndeterminate = true;
                        break;
                    case ClipProcessingStatus.Finished:
                        StatusIcon = PackIconMaterialDesignKind.Done;
                        StatusString = "Finished";
                        ProgressIndeterminate = false;
                        break;
                }
            }
        }

        /// <summary>
        /// An icon denoting the status of this job
        /// </summary>
        private PackIconMaterialDesignKind statusIcon;
        public PackIconMaterialDesignKind StatusIcon
        {
            get => statusIcon;
            private set => SetProperty(ref statusIcon, value);
        }

        /// <summary>
        /// A string describing the current status of this job
        /// </summary>
        private string statusString = "In queue";
        public string StatusString
        {
            get => statusString;
            private set => SetProperty(ref statusString, value);
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
        /// The first clip in this job
        /// </summary>
        public ClipViewModel FirstClip => Clips.First();

        /// <summary>
        /// True if this job contains more than one clip
        /// </summary>
        public bool IsMultiClip => Clips.Count > 1;

        /// <summary>
        /// Number of clips other than the first clip
        /// </summary>
        public int RemainingClips => Clips.Count - 1;

        /// <summary>
        /// A newline separated string of the remaining clip's full paths.
        /// </summary>
        public string RemainingClipNames { get; }

        /// <summary>
        /// A factor (0-1) describing how close this job is to being finished
        /// </summary>
        public double CompletionFactor
        {
            get
            {
                if (Status == ClipProcessingStatus.Finished)
                    return 1.0;

                double total = clipCompletionFactors.Sum(x => x.Value);
                return total == 0.0 ? 0.0 : total / clipCompletionFactors.Count;
            }
        }

        /// <summary>
        /// True if the progress cannot currently be determined 
        /// </summary>
        private bool progressIndeterminate = false;
        public bool ProgressIndeterminate
        {
            get => progressIndeterminate;
            set => SetProperty(ref progressIndeterminate, value);
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

        /// <summary>
        /// Outputs by name, used by the logs view
        /// </summary>
        private ObservableCollection<LogOutputViewModel> logOutputs = new();
        public ObservableCollection<LogOutputViewModel> LogOutputs
        {
            get => logOutputs;
            set => SetProperty(ref logOutputs, value);
        }

        #region Methods

        /// <summary>
        /// Called by the ffmpeg engine when it gives stats, this only occurs during the encoding phase of ffmpeg.
        /// </summary>
        /// <param name="clip">The clip the output is for</param>
        /// <param name="stat">Information regarding the current encoding progress</param>
        /// <param name="firstPass">Whether or not this output came from the first or second pass (always false if the clip does not use two pass encoding)</param>
        public void NotifyStats(ClipViewModel clip, EngineProcessStatistics stat, bool firstPass)
        {
            Statistics = $"{stat.Bitrate:N2}kbit/s";

            if (clip.Duration.TotalSeconds != 0.0)
            {
                ProgressIndeterminate = false;

                double prog = stat.Time.TotalSeconds / clip.Duration.TotalSeconds;

                // If two pass encoding is on, the progress is only half what the stats say it is, but if it is the second
                // pass then it is half what the stats say it is + 50% completion from the first pass
                if (clip.TwoPassEncoding)
                {
                    prog *= 0.5;
                    if (!firstPass)
                        prog += 0.5;
                }

                clipCompletionFactors[clip] = Math.Min(Math.Max(0.0, prog), 1.0);
                OnPropertyChanged(nameof(CompletionFactor));
            }
        }

        /// <summary>
        /// Called by the ffmpeg engine when it gives output.
        /// </summary>
        /// <param name="clip">The clip the output is for</param>
        /// <param name="output">The line of output</param>
        /// <param name="firstPass">Whether or not this output came from the first or second pass (always false if the clip does not use two pass encoding)</param>
        public void RecordOutput(ClipViewModel clip, string output, bool firstPass)
        {
            App.Current.Dispatcher.Invoke(new Action(() =>
            {
                string title = clip.Title;
                if (clip.TwoPassEncoding)
                    title += $" - Pass {(firstPass ? '1' : '2')}";

                LogOutputViewModel logOut;
                if (!logOutputLookup.ContainsKey(title))
                {
                    logOut = new LogOutputViewModel(title, new StringBuilder());

                    logOutputLookup[title] = logOut;
                    LogOutputs.Add(logOut);
                }
                else
                {
                    logOut = logOutputLookup[title];
                }

                logOut.AddOutput(output);
            }));
        }

        /// <summary>
        /// Reset the job.  This should be used if the user canceled the job and runs it again
        /// </summary>
        public void Reset()
        {
            SuccesfulJobCount = 0;

            logOutputs.Clear();
            logOutputLookup.Clear();

            foreach (var clip in Clips)
                clipCompletionFactors[clip] = 0.0;
        }
        #endregion

        #region Commands
        public ICommand LogsCommand { get; }
        #endregion
    }
}

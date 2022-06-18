using Clipple.FFMPEG;
using Clipple.Types;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Clipple.ViewModel
{
    public class ClipProcessingDialogViewModel : ObservableObject
    {
        public ClipProcessingDialogViewModel(List<JobViewModel> jobs, ICommand closeCommand)
        {
            CancelCommand = new RelayCommand(() =>
            {
                cancellationTokenSource.Cancel();
            });
            StartCommand = new RelayCommand(StartProcesses);
            CloseCommand = closeCommand;

            foreach (var job in jobs)
                Jobs.Add(job);
        }

        #region Members
        private CancellationTokenSource cancellationTokenSource = new();
        #endregion

        #region Properties

        private ObservableCollection<JobViewModel> jobs = new();
        public ObservableCollection<JobViewModel> Jobs
        {
            get => jobs;
            set => SetProperty(ref jobs, value);
        }

        /// <summary>
        /// Current state of this dialog
        /// </summary>
        private ClipProcessingDialogState state = ClipProcessingDialogState.Waiting;
        public ClipProcessingDialogState State
        {
            get => state;
            set => SetProperty(ref state, value);
        }

        /// <summary>
        /// Whether or not the user has enabled post processing actions
        /// </summary>
        private bool postProcessingActionsEnabled = false;
        public bool PostProcessingActionsEnabled
        {
            get => postProcessingActionsEnabled;
            set => SetProperty(ref postProcessingActionsEnabled, value);
        }

        /// <summary>
        /// A list of jobs that were completed succesfully last time the job list was ran
        /// </summary>
        public List<JobViewModel> SuccesfulJobs { get; } = new();
        #endregion

        #region Commands
        public ICommand CancelCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand CloseCommand { get; }
        #endregion

        #region Method

        /// <summary>
        /// Generates a list of actions from a job's clips. See GetActionForClip.
        /// </summary>
        /// <param name="job">The job to get actions for</param>
        /// <returns></returns>
        private List<Func<Task>> GetTasksForClip(JobViewModel job)
        {
            var actions = new List<Func<Task>>();
            foreach (var clip in job.Clips)
                actions.Add(GetTaskForClip(job, clip));

            return actions;
        }

        /// <summary>
        /// Returns an action that can be ran to process the specified clip.
        /// </summary>
        /// <param name="job">The job that owns the clip</param>
        /// <param name="clip">The clip to generate an action for</param>
        /// <returns></returns>
        private Func<Task> GetTaskForClip(JobViewModel job, ClipViewModel clip)
        {
            return async () =>
            {
                var path      = Path.Combine(App.LibPath, "ffmpeg.exe");
                var inputFile = job.VideoViewModel.FileInfo.FullName;

                // Run first pass if required
                if (clip.TwoPassEncoding)
                {
                    var firstJob = new Engine(path, inputFile, clip, true);
                    firstJob.OnProcessStats += (s, e) => job.NotifyStats(clip, e, true);
                    firstJob.OnOutput += (s, e) => job.RecordOutput(clip, e, true);

                    job.Status   = ClipProcessingStatus.ProcessingFirstPass;

                    // Run
                    var exitCode = await firstJob.Run(cancellationTokenSource.Token);
    
                    // Don't continue with the second pass if the first pass failed
                    if (exitCode != 0)
                    {
                        job.Status = ClipProcessingStatus.Failed;
                        return;
                    }
                }

                // Run real job (first job ran if two pass is not enabled)
                var mainJob = new Engine(path, inputFile, clip, false);
                mainJob.OnProcessStats += (s, e) => job.NotifyStats(clip, e, false);
                mainJob.OnOutput += (s, e) => job.RecordOutput(clip, e, false);

                job.Status = ClipProcessingStatus.Processing;
                job.Status = await mainJob.Run(cancellationTokenSource.Token) == 0 ? ClipProcessingStatus.Finished : ClipProcessingStatus.Failed;

                if (job.Status == ClipProcessingStatus.Finished)
                    job.SuccesfulJobCount++;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public async void StartProcesses()
        {
            State = ClipProcessingDialogState.Running;

            // Clear succesful job list, this will be rebuilt after processing has finished
            SuccesfulJobs.Clear();

            // Create new cancel token
            cancellationTokenSource = new CancellationTokenSource();

            var actionQueue = new Queue<Func<Task>>();
            foreach (var job in jobs)
            {
                job.Reset();
                GetTasksForClip(job).ForEach(action => actionQueue.Enqueue(action));
            }

            while (actionQueue.Count > 0)
            {
                // Batch as many tasks as we can run concurrently into a list
                var tasks = new List<Task>();
                for (int i = 0; i < App.ViewModel.SettingsViewModel.MaxConcurrentJobs; i++)
                {
                    if (!actionQueue.TryDequeue(out var action) || action == null)
                        break;

                    tasks.Add(Task.Run(action));
                }

                // Then run all tasks until complete
                try
                {
                    await Task.WhenAll(tasks);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            // Tally up every job that completed each clip succesfully, these jobs qualify for PPAs
            foreach (var job in jobs)
            {
                if (job.SuccesfulJobCount == job.Clips.Count)
                    SuccesfulJobs.Add(job);
            }

            State = ClipProcessingDialogState.Waiting;
        }
        #endregion
    }
}

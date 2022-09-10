using Clipple.FFMPEG;
using Clipple.PPA;
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
            StartCommand        = new RelayCommand(StartProcesses);
            CloseCommand        = closeCommand;
            CancelReviewCommand = new RelayCommand(() => State = ClipProcessingDialogState.Idle);
            ReviewCommand       = new RelayCommand(() => State = ClipProcessingDialogState.Review);

            RunSelectedPostProcessingActionsCommand = new RelayCommand(() =>
            {
                // Run every selected PPA
                foreach (var ppa in PostProcessingActions)
                {
                    if (ppa.IsSelected)
                        ppa.PostProcessingAction.Run();
                }

                // Close dialog
                CloseCommand.Execute(null);
            });

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
        private ClipProcessingDialogState state = ClipProcessingDialogState.Idle;
        public ClipProcessingDialogState State
        {
            get => state;
            set => SetProperty(ref state, value);
        }

        /// <summary>
        /// Powers the check/uncheck all checkbox in the post processing actions view
        /// </summary>
        private bool postProcessingActionsCheckAll = true;
        public bool PostProcessingActionsCheckAll
        {
            get => postProcessingActionsCheckAll;
            set
            {
                SetProperty(ref postProcessingActionsCheckAll, value);

                foreach (var ppa in PostProcessingActions)
                    ppa.IsSelected = value;
            }
        }

        /// <summary>
        /// A list of tuples containing a post processing action that is applicable for the last run of StartProcesses
        /// and a boolean denoting whether or not the user has agreed to use it
        /// </summary>
        public ObservableCollection<PostProcessingActionViewModel> PostProcessingActions { get; } = new();
        #endregion

        #region Commands
        public ICommand CancelCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand CloseCommand { get; }
        public ICommand ReviewCommand { get; }
        public ICommand CancelReviewCommand { get; }

        public ICommand RunSelectedPostProcessingActionsCommand { get; }
        #endregion

        #region Method
        /// <summary>
        /// Returns a task that can be ran to process the specified job.
        /// </summary>
        /// <param name="job">The job to create a task for</param>
        /// <returns></returns>
        private Func<Task> GetTaskForJob(JobViewModel job)
        {
            return async () =>
            {
                var inputFile = job.VideoViewModel.FileInfo.FullName;

                // Run first pass if required
                if (job.Clip.TwoPassEncoding)
                {
                    var firstJob = new ClipEngine(inputFile, job.Clip, true);
                    firstJob.OnProcessStats += (s, e) => job.NotifyStats(e, true);
                    firstJob.OnOutput += (s, e) => job.RecordOutput(e);

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
                var mainJob = new ClipEngine(inputFile, job.Clip, false);
                mainJob.OnProcessStats += (s, e) => job.NotifyStats(e, false);
                mainJob.OnOutput += (s, e) => job.RecordOutput(e);

                job.Status = ClipProcessingStatus.Processing;
                job.Status = await mainJob.Run(cancellationTokenSource.Token) == 0 ? ClipProcessingStatus.Finished : ClipProcessingStatus.Failed;
            };
        }

        /// <summary>
        /// 
        /// </summary>
        public async void StartProcesses()
        {
            State = ClipProcessingDialogState.Running;

            // Clear PPAs
            PostProcessingActions.Clear();

            // Create new cancel token
            cancellationTokenSource = new CancellationTokenSource();

            var actionQueue = new Queue<Func<Task>>();
            foreach (var job in jobs)
            {
                job.Reset();
                actionQueue.Enqueue(GetTaskForJob(job));
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
                catch (OperationCanceledException)
                {
                    // Mark all non-completed jobs as cancelled
                    foreach (var job in jobs)
                    {
                        if (job.Status == ClipProcessingStatus.Processing || job.Status == ClipProcessingStatus.InQueue)
                            job.Status = ClipProcessingStatus.Cancelled;
                    }

                    break;
                }
            }

            // List of videos that had at least one clip that processed succesfully
            var videos = new HashSet<VideoViewModel>();

            // List of clips that processed succesfully
            var clips  = new HashSet<ClipViewModel>();
            foreach (var job in jobs)
            {
                if (job.Status == ClipProcessingStatus.Finished && job.Clip.Parent != null)
                {
                    videos.Add(job.Clip.Parent);
                    clips.Add(job.Clip);
                }
            }

            // Create a list of PPAs in the order of video PPA -> video's clip's PPAs
            foreach (var video in videos)
            {
                // Clips' PPAs need to be added/ran first because the video's PPA might remove the video,
                // thus removing the video's clips
                video.Clips.Where(x => clips.Contains(x) && x.PostProcessingAction is not NoAction)
                    .Select(x => new PostProcessingActionViewModel(x.PostProcessingAction)).ToList().ForEach(x => PostProcessingActions.Add(x));

                var allClipsRan = !video.Clips.Any(x => !clips.Contains(x));
                if (allClipsRan && video.PostProcessingAction is not NoAction)
                    PostProcessingActions.Add(new PostProcessingActionViewModel(video.PostProcessingAction));
            }

            State = ClipProcessingDialogState.Idle;
        }
        #endregion
    }
}

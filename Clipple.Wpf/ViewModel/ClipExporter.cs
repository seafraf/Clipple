﻿using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Clipple.FFMPEG;
using Clipple.Types;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;

namespace Clipple.ViewModel;

public class ExportingClip : ObservableObject
{
    public ExportingClip(Media media)
    {
        this.media = media;

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

    private          Media                   media;
    private          Media?                  outputMedia;
    private          double                  completionFactor;
    private          bool                    isIndeterminate         = true;
    private readonly CancellationTokenSource cancellationTokenSource = new();
    private          ClipExportingStatus     status                  = ClipExportingStatus.Waiting;

    #endregion

    #region Properties

    /// <summary>
    ///     The media whose clip is being processed by this export.
    /// </summary>
    public Media Media
    {
        get => media;
        set => SetProperty(ref media, value);
    }

    /// <summary>
    ///     All output given from the clip engine
    /// </summary>
    public StringBuilder Output { get; } = new();

    /// <summary>
    ///     Whether or not the progress of the export can be determined
    /// </summary>
    public bool IsIndeterminate
    {
        get => isIndeterminate;
        set => SetProperty(ref isIndeterminate, value);
    }

    /// <summary>
    ///     Factor representing how complete the export process is.  This will be zero when IsIndeterminate = true
    /// </summary>
    public double CompletionFactor
    {
        get => completionFactor;
        set => SetProperty(ref completionFactor, value);
    }

    /// <summary>
    ///     The status
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
        if (media.Clip is not { } clip)
            return;

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
        mainJob.OnOutput       += OnEngineOutput;

        Status = ClipExportingStatus.Processing;
        Status = await mainJob.Run(cancellationTokenSource.Token) == 0
            ? ClipExportingStatus.Finished
            : ClipExportingStatus.Failed;

        if (Status == ClipExportingStatus.Finished)
            await Application.Current.Dispatcher.Invoke(ImportResult);
    }

    private void OnEngineOutput(object? sender, string e)
    {
        Output.AppendLine(e);
        OnPropertyChanged(nameof(Output));
    }

    private void OnEngineStats(object? sender, EngineProcessStatistics e)
    {
        if (media.Clip is not { } clip)
            return;

        var clipDuration = clip.Duration;
        if (!(clipDuration.TotalSeconds > 0.0) || !(e.Time.TotalSeconds > 0))
            return;

        CompletionFactor = Math.Min(Math.Max(0.0, e.Time.TotalSeconds / clipDuration.TotalSeconds), 1.0);
        IsIndeterminate  = false;
    }

    /// <summary>
    ///     Imports the output clip to the library, setting various properties on the media to link the produced
    ///     clip to the media.
    /// </summary>
    private async Task ImportResult()
    {
        if (media.Clip is not { } clip)
            return;

        outputMedia = await App.ViewModel.Library.AddMedia(clip.FullFileName, media.Id, true);
        if (outputMedia is { } libraryMedia)
        {
            libraryMedia.Class      = MediaClass.Clip;
            libraryMedia.ClassIndex = MediaClass.MediaClasses.IndexOf(MediaClass.Clip);
            media.Clips.Add(libraryMedia.Id);
        }
    }

    #endregion

    #region Commands

    public ICommand CloseCommand => new RelayCommand(() =>
    {
        // Cancel token just in case the ffmpeg engine is still running
        cancellationTokenSource.Cancel();

        // Close exporting dialog only
        DialogHost.Close("ExportClip");
    });

    public ICommand OpenInExplorer => new RelayCommand(() =>
    {
        if (outputMedia?.Uri is not { } uri)
            return;

        Process.Start(new ProcessStartInfo("explorer.exe")
        {
            UseShellExecute = true,
            Arguments       = $"/select,\"{uri.AbsoluteUri}\""
        });
    });

    public ICommand OpenInLibrary => new RelayCommand(() =>
    {
        if (outputMedia is not { } output)
            return;

        // Close both dialogs
        DialogHost.Close("ExportClip");
        DialogHost.Close(null);

        // Select the output clip
        App.ViewModel.Library.SelectedMedia = output;
        App.ViewModel.IsLibrarySelected     = true;
    });

    #endregion
}
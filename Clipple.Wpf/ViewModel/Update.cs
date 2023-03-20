using System;
using System.Linq;
using System.Windows.Input;
using Clipple.Util;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Squirrel;

namespace Clipple.ViewModel;

public class Update : ObservableObject
{
    public Update(GithubUpdateManager manager, UpdateInfo updateInfo)
    {
        Manager       = manager;
        UpdateInfo    = updateInfo;
        UpdateSize    = updateInfo.ReleasesToApply.Sum(x => x.Filesize);
        UpdateVersion = updateInfo.FutureReleaseEntry.Version;
    }

    #region Methods

    /// <summary>
    ///     Performs the update described by this model
    /// </summary>
    private async void PerformUpdate()
    {
        DownloadProgress = 0;
        await Manager.DownloadReleases(UpdateInfo.ReleasesToApply, progress => DownloadProgress = progress);

        InstallProgress = 0;
        await Manager.ApplyReleases(UpdateInfo, progress => InstallProgress = progress);
        UpdateManager.RestartApp();
    }

    #endregion

    #region Members

    private int downloadProgress = -1;

    private int installProgress = -1;

    #endregion

    #region Properties

    public  SemanticVersion     UpdateVersion { get; }
    private UpdateInfo          UpdateInfo    { get; }
    private long                UpdateSize    { get; }
    private GithubUpdateManager Manager       { get; }

    public Uri    InstalledUri     => new($"https://github.com/seafraf/Clipple/releases/tag/{App.Version}");
    public Uri    UpdateUri        => new($"https://github.com/seafraf/Clipple/releases/tag/{UpdateVersion}");
    public string UpdateSizeString => Formatting.ByteCountToString(UpdateSize);

    public int DownloadProgress
    {
        get => downloadProgress;
        set => SetProperty(ref downloadProgress, value);
    }


    public int InstallProgress
    {
        get => installProgress;
        set => SetProperty(ref installProgress, value);
    }

    #endregion

    #region Commands

    public ICommand PerformUpdateCommand => new RelayCommand(PerformUpdate);

    #endregion
}
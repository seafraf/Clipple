using Clipple.Util;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Squirrel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Wpf.ViewModel
{
    public class UpdateViewModel : ObservableObject
    {
        public UpdateViewModel()
        {
            Manager = new GithubUpdateManager($"https://github.com/seafraf/Clipple");
        }

        #region Methods
        public async Task CheckForUpdate()
        {
            if (!Manager.IsInstalledApp)
                return;

            updateInfo = await Manager.CheckForUpdate();
            if (updateInfo != null && updateInfo.ReleasesToApply.Count > 0)
            {
                UpdateSize      = updateInfo.ReleasesToApply.Sum(x => x.Filesize);
                LatestVersion   = updateInfo.FutureReleaseEntry.Version;
                UpdateAvailable = true;
            }
        }
        #endregion

        #region Properties
        private bool updateAvailable;
        public bool UpdateAvailable
        {
            get => updateAvailable;
            set => SetProperty(ref updateAvailable, value);
        }

        private SemanticVersion? currentVersion;
        public SemanticVersion? CurrentVersion
        {
            get => currentVersion;
            set => SetProperty(ref currentVersion, value);
        }

        private SemanticVersion? latestVersion;
        public SemanticVersion? LatestVersion
        {
            get => latestVersion;
            set => SetProperty(ref latestVersion, value);
        }

        public Uri CurrentURI => new($"https://github.com/seafraf/Clipple/releases/tag/{CurrentVersion}");
        public Uri LatestURI => new($"https://github.com/seafraf/Clipple/releases/tag/{LatestVersion}");

        public GithubUpdateManager Manager { get; }

        private UpdateInfo? updateInfo;
        public UpdateInfo? UpdateInfo
        {
            get => updateInfo;
            set => SetProperty(ref updateInfo, value);
        }

        private long updateSize;
        public long UpdateSize
        {
            get => updateSize;
            set
            {
                SetProperty(ref updateSize, value);
                OnPropertyChanged(nameof(UpdateSizeString));
            }
        }

        public string UpdateSizeString => Formatting.ByteCountToString(UpdateSize);
        #endregion
    }
}

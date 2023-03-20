using System.Threading.Tasks;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Squirrel;

namespace Clipple.ViewModel;

public class Updater : ObservableObject
{
    public Updater()
    {
        manager = new("https://github.com/seafraf/Clipple");
    }

    #region Members

    private readonly GithubUpdateManager manager;

    #endregion

    #region Methods

    /// <summary>
    ///     Checks for updates and opens an update prompt if updates were found
    /// </summary>
    public async Task CheckForUpdate()
    {
        if (!manager.IsInstalledApp)
            return;

        var updateInfo = await manager.CheckForUpdate();
        if (updateInfo != null && updateInfo.ReleasesToApply.Count > 0)
            await DialogHost.Show(new View.Updater
            {
                DataContext = new Update(manager, updateInfo)
            });
    }

    #endregion
}
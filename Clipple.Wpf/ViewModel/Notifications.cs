using System;
using MaterialDesignThemes.Wpf;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Clipple.ViewModel;

public class Notifications : ObservableObject
{
    #region Methods

    public void NotifyException(string message, params Exception[] exceptions)
    {
        MessageQueue.Enqueue(message, "Show Errors", e =>
        {
            if (e == null)
                return;

            DialogHost.Show(new View.NotificationErrorList
            {
                DataContext = new NotificationErrorList(e)
            });
        }, exceptions, true, true, TimeSpan.FromSeconds(15));
    }

    public void NotifyWarning(string message)
    {
        MessageQueue.Enqueue(message, "OK", p => { }, null, true, true, TimeSpan.FromSeconds(15));
    }

    public void NotifyInfo(string message)
    {
        MessageQueue.Enqueue(message, "OK", p => { }, null, true, false);
    }

    #endregion

    #region Properties

    /// <summary>
    ///     The snackbar message queue
    /// </summary>
    public SnackbarMessageQueue MessageQueue { get; } = new();

    #endregion
}
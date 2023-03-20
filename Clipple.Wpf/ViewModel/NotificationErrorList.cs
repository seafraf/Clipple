using System;
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Clipple.ViewModel;

public class NotificationErrorList : ObservableObject
{
    public NotificationErrorList(Exception[] exceptions)
    {
        Exceptions = exceptions;
    }

    public Exception[] Exceptions { get; }
}
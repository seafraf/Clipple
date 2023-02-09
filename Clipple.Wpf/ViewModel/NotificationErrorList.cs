using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.ViewModel;

public class NotificationErrorList : ObservableObject
{
    public NotificationErrorList(Exception[] exceptions)
    {
        Exceptions = exceptions;
    }

    public Exception[] Exceptions { get; private set; }
}

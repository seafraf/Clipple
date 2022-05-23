using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.ViewModel
{
    public class LogOutputViewModel : ObservableObject
    {
        public LogOutputViewModel(string title, StringBuilder output)
        {
            Title = title;
            Output = output;
        }

        public string Title { get; }
        public StringBuilder Output { get; }

        public void AddOutput(string output)
        {
            Output.AppendLine(output);
            OnPropertyChanged(nameof(Output));
        }
    }
}

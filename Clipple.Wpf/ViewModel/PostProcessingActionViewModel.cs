using Clipple.PPA;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.ViewModel
{
    public class PostProcessingActionViewModel : ObservableObject
    {
        public PostProcessingActionViewModel(PostProcessingAction postProcessingAction)
        {
            this.postProcessingAction = postProcessingAction;
        }

        private bool isSelected = true;
        public bool IsSelected
        {
            get => isSelected;
            set => SetProperty(ref isSelected, value);
        }

        private PostProcessingAction postProcessingAction;

        public PostProcessingAction PostProcessingAction
        {
            get => postProcessingAction;
            set => SetProperty(ref postProcessingAction, value);
        }
    }
}

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Clipple.ViewModel
{
    public class ClipPresetManagerViewModel : ObservableObject
    {
        public ClipPresetManagerViewModel(Clip clipViewModel)
        {
            ClipViewModel = clipViewModel;
        }

        #region Properties

        public Clip ClipViewModel { get; }

        /// <summary>
        /// The selected preset
        /// </summary>
        private ClipPresetViewModel? selectedPreset;
        public ClipPresetViewModel? SelectedPreset
        {
            get => selectedPreset;
            set
            {
                SetProperty(ref selectedPreset, value);
                OnPropertyChanged(nameof(HasSelected));
            }
        }

        /// <summary>
        /// Helper function.  True if selectedPreset is not null
        /// </summary>
        public bool HasSelected => selectedPreset != null;
        #endregion

        #region Commands
        public ICommand DeleteSelectedCommand => new RelayCommand(DeleteSelected);
        public ICommand RestoreDefaultsCommand => new RelayCommand(RestoreDefaults);
        public ICommand CreateNewCommand => new RelayCommand(CreateNew);
        #endregion

        #region Methods
        private void DeleteSelected()
        {
            //if (SelectedPreset != null)
            //    App.ViewModel.ClipPresetsViewModel.Presets.Remove(SelectedPreset);
        }

        private void RestoreDefaults()
        {
            //App.ViewModel.ClipPresetsViewModel.Restore();
        }

        private void CreateNew()
        {
            //App.ViewModel.ClipPresetsViewModel.Presets.Add(new ClipPresetViewModel("New preset", "User presets",
            //    ClipViewModel.UseTargetSize ? null : ClipViewModel.VideoBitrate, ClipViewModel.AudioBitrate,
            //    ClipViewModel.UseSourceResolution ? null : ClipViewModel.TargetWidth, ClipViewModel.UseSourceResolution ? null : ClipViewModel.TargetHeight,
            //    ClipViewModel.UseSourceFps ? null : ClipViewModel.TargetFps,
            //    ClipViewModel.UseTargetSize, ClipViewModel.UseTargetSize ? ClipViewModel.OutputTargetSize : null,
            //    ClipViewModel.VideoCodec, ClipViewModel.AudioCodec,
            //    ClipViewModel.ShouldCrop, ClipViewModel.CropX, ClipViewModel.CropY, ClipViewModel.CropWidth, ClipViewModel.CropHeight,
            //    ClipViewModel.OutputFormat, DateTime.Now.Ticks));
        }
        #endregion
    }
}

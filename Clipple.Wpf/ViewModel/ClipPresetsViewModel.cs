using Clipple.Types;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Clipple.ViewModel
{
    public class ClipPresetsViewModel : ObservableObject, IJsonOnDeserialized
    {
        public ClipPresetsViewModel(bool initialiseDefaults)
        {
            long priority = 0;

            /**
             * Recommended formats for sharing media on Discord
             */
            defaults.Add(new ClipPresetViewModel("8MB", "Discord", priority: priority++, 
                useTargetSize: true, targetSize: 8, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("50MB", "Discord", priority: priority++, 
                useTargetSize: true, targetSize: 50, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("100MB", "Discord", priority: priority++, 
                useTargetSize: true, targetSize: 100, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("Audio", "Discord", priority: priority++, 
                audioBitrate: 320, 
                audioCodec: "libmp3lame",
                outputFormat: OutputFormatViewModel.GetByName("mp3")));

            /**
             * Recommendations by YouTube for SDR videos at various resolutions
             */
            defaults.Add(new ClipPresetViewModel("2160p@60", "Recommended", priority: priority++,
                videoBitrate: 680000, targetWidth: 3840, targetHeight: 2160, fps: 60, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("2160p@30", "Recommended", priority: priority++,
                videoBitrate: 450000, targetWidth: 3840, targetHeight: 2160, fps: 30,
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("1440p@60", "Recommended", priority: priority++,
                videoBitrate: 240000, targetWidth: 2560, targetHeight: 1440, fps: 60, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("1440p@30", "Recommended", priority: priority++,
                videoBitrate: 160000, targetWidth: 2560, targetHeight: 1440, fps: 30, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("1080p@60", "Recommended", priority: priority++,
                videoBitrate: 120000, targetWidth: 1920, targetHeight: 1080, fps: 60, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("1080p@30", "Recommended", priority: priority++,
                videoBitrate: 80000, targetWidth: 1920, targetHeight: 1080, fps: 30, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("720p@60", "Recommended", priority: priority++,
                videoBitrate: 75000, targetWidth: 1280, targetHeight: 720, fps: 60, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("720p@30", "Recommended", priority: priority++,
                videoBitrate: 50000, targetWidth: 1280, targetHeight: 720, fps: 30, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("480p@60", "Recommended", priority: priority++,
                videoBitrate: 40000, targetWidth: 852, targetHeight: 480, fps: 60, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("480p@30", "Recommended", priority: priority++,
                videoBitrate: 25000, targetWidth: 852, targetHeight: 480, fps: 30, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("360p@60", "Recommended", priority: priority++,
                videoBitrate: 15000, targetWidth: 480, targetHeight: 360, fps: 60, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            defaults.Add(new ClipPresetViewModel("360p@30", "Recommended", priority: priority++,
                videoBitrate: 10000, targetWidth: 480, targetHeight: 360, fps: 30, 
                videoCodec: "libx264",
                outputFormat: OutputFormatViewModel.GetByName("mp4")));

            if (!initialiseDefaults)
                return;

            foreach (var preset in defaults)
                presets.Add(preset);

            AddGroupDescriptions();
        }

        public ClipPresetsViewModel() : this(false)
        {

        }

        #region Members
        private List<ClipPresetViewModel> defaults = new();
        #endregion

        #region Properties
        private ObservableCollection<ClipPresetViewModel> presets = new();
        public ObservableCollection<ClipPresetViewModel> Presets
        {
            get => presets;
            set => SetProperty(ref presets, value);
        }
        #endregion

        #region Methods
        public void AddGroupDescriptions()
        {
            var cvs = CollectionViewSource.GetDefaultView(presets);
            cvs.GroupDescriptions.Clear();
            cvs.GroupDescriptions.Add(new PropertyGroupDescription("Category"));
            cvs.SortDescriptions.Clear();
            cvs.SortDescriptions.Add(new SortDescription("Priority", ListSortDirection.Ascending));
        }

        public void OnDeserialized()
        {
            AddGroupDescriptions();
        }

        /// <summary>
        /// Ensures that every default preset exists in the list of presets
        /// </summary>
        public void Restore()
        {
            foreach (var defaultPreset in defaults)
            {
                if (!presets.Contains(defaultPreset))
                    presets.Add(defaultPreset);
            }
        }
        #endregion
    }
}

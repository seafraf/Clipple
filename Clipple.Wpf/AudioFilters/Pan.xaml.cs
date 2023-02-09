using Clipple.Types;
using Clipple.ViewModel;
using FFmpeg.AutoGen;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clipple.AudioFilters
{
    public partial class Pan : UserControl
    {
        public class ViewModel : AudioFilter
        {
            #region Members
            private string channelMapping = "";
            private string channelLayout = "stereo";
            private PanPreset? preset;
            private int presetIndex = -1;
            #endregion

            #region Properties
            [BsonIgnore]
            public override string FilterString => $"pan={ChannelLayout}|{ChannelMapping}";

            [BsonIgnore]
            public override string FilterName => "Pan";

            [BsonIgnore]
            public ObservableCollection<string> ChannelLayoutOptions { get; } = new()
            {
                "mono",
                "stereo",
                "2.1",
                "3.0",
                "3.0(back)",
                "4.0",
                "quad",
                "quad(side)",
                "3.1",
                "5.0",
                "5.0(side)",
                "4.1",
                "5.1",
                "5.1(side)",
                "6.0",
                "6.0(front)",
                "hexagonal",
                "6.1",
                "6.1(front)",
                "7.0",
                "7.0(front)",
                "7.1",
                "7.1(wide)",
                "7.1(wide-side)",
                "octagonal",
                "downmix"
            };

            [BsonIgnore]
            public ObservableCollection<PanPreset> PresetOptions { get; } = new()
            {
                new PanPreset("Duplicate left", "Stereo", "c0=c0|c1=c0", "stereo"),
                new PanPreset("Duplicate right", "Stereo", "c0=c1|c1=c1", "stereo")
            };

            [BsonIgnore]
            public PanPreset? Preset
            {
                get => preset;
                set
                {
                    SetProperty(ref preset, value);

                    if (value == null)
                        return;

                    SetProperty(ref channelMapping, value.ChannelMapping, nameof(ChannelMapping));
                    SetProperty(ref channelLayout, value.ChannelLayout, nameof(ChannelLayout));
                }
            }

            public int PresetIndex
            {
                get => presetIndex;
                set => SetProperty(ref presetIndex, value);
            }

            public string ChannelMapping
            {
                get => channelMapping;
                set
                {
                    Preset = null;
                    SetProperty(ref channelMapping, value);
                }
            }

            public string ChannelLayout
            {
                get => channelLayout;
                set
                {
                    Preset = null;
                    SetProperty(ref channelLayout, value);
                }
            }
            #endregion

            public override UserControl GenerateControl()
            {
                return new Pan()
                {
                    DataContext = this
                };
            }

            public override void CopyFrom<T>(T other)
            {
                base.CopyFrom(other);

                if (other is ViewModel vm)
                {
                    ChannelMapping = vm.ChannelMapping;
                    ChannelLayout = vm.ChannelLayout;

                    if (vm.PresetIndex != -1)
                        PresetIndex = vm.PresetIndex;
                }
            }

            public override void Initialise()
            {
                Preset = PresetOptions.ElementAtOrDefault(PresetIndex);
            }
        }

        public Pan()
        {
            InitializeComponent();
        }
    }
}

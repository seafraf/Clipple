using FFmpeg.AutoGen;
using LiteDB;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public partial class Volume : UserControl
    {
        public class ViewModel : AudioFilter
        {
            #region Members
            private int volume = 100;
            #endregion

            #region Properties
            [BsonIgnore]
            public override string FilterString => $"volume={VolumeDecimal.ToString("0.00", CultureInfo.InvariantCulture)}";

            [BsonIgnore]
            public override string FilterName => "Volume / Gain";

            public int Volume
            {
                get => volume;
                set => SetProperty(ref volume, value);
            }

            [BsonIgnore]
            public double VolumeDecimal => volume / 100.0;
            #endregion

            public override void CopyFrom<T>(T other)
            {
                base.CopyFrom(other);

                if (other is ViewModel vm)
                    Volume = vm.Volume;
            }

            public override UserControl GenerateControl()
            {
                return new Volume()
                {
                    DataContext = this
                };
            }
            public override void Initialise()
            {
                
            }
        }

        public Volume()
        {
            InitializeComponent();
        }
    }
}

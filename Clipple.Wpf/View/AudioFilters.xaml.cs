using Clipple.AudioFilters;
using System;
using System.Collections.Generic;
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

namespace Clipple.View
{
    /// <summary>
    /// Interaction logic for AudioFilters.xaml
    /// </summary>
    public partial class AudioFilters : UserControl
    {
        public AudioFilters()
        {
            InitializeComponent();
        }

        private void OnExpanderLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Expander expander)
                expander.Content = ((AudioFilter)expander.DataContext).GenerateControl();
        }

        private void OnExpanderEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Expander expander)
                expander.IsExpanded = (bool)e.NewValue;
        }
    }
}

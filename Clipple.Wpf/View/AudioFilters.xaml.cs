using System.Windows;
using System.Windows.Controls;
using Clipple.AudioFilters;

namespace Clipple.View;

/// <summary>
///     Interaction logic for AudioFilters.xaml
/// </summary>
public partial class AudioFilters
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
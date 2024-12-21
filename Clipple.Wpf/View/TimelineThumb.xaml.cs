using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Clipple.View;

/// <summary>
///     Interaction logic for TimelineThumb.xaml
/// </summary>
public partial class TimelineThumb
{
    public TimelineThumb()
    {
        InitializeComponent();
    }

    #region Dependency properties

    public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
        nameof(Size),
        typeof(double),
        typeof(TimelineThumb),
        new FrameworkPropertyMetadata(24.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty TimelineMarkerSizeProperty = DependencyProperty.Register(
        nameof(TimelineMarkerSize),
        typeof(double),
        typeof(TimelineThumb),
        new FrameworkPropertyMetadata(24.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty ButtonAlignmentProperty = DependencyProperty.Register(
        nameof(ButtonAlignment),
        typeof(VerticalAlignment),
        typeof(TimelineThumb),
        new FrameworkPropertyMetadata(VerticalAlignment.Center, FrameworkPropertyMetadataOptions.AffectsRender));

    #endregion

    #region Properties

    public double Size
    {
        get => (double)GetValue(SizeProperty);
        set => SetValue(SizeProperty, value);
    }

    public double TimelineMarkerSize
    {
        get => (double)GetValue(TimelineMarkerSizeProperty);
        set => SetValue(TimelineMarkerSizeProperty, value);
    }

    public VerticalAlignment ButtonAlignment
    {
        get => (VerticalAlignment)GetValue(ButtonAlignmentProperty);
        set => SetValue(ButtonAlignmentProperty, value);
    }

    public Thickness GridMargin => new(0, -TimelineMarkerSize, 0, 0);

    public Thickness ButtonMargin => new(-TimelineMarkerSize / 2.0);

    public double ButtonOpacity { get; set; } = 1.0;

    #endregion

    public void NotifyDragStart(bool draggingThis)
    {
        ThumbButton.Opacity         = draggingThis ? 0.7 : 0.0;
        TimelineMarkerCover.Opacity = draggingThis ? 1 : 0.0;

        Cursor = Cursors.ScrollWE;
    }

    public void NotifyDragEnd(bool draggingThis)
    {
        ThumbButton.Opacity         = 1.0;
        TimelineMarkerCover.Opacity = 0;
    }
}
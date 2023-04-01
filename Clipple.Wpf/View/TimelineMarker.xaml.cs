using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Clipple.View;

/// <summary>
///     Interaction logic for TimelineMarker.xaml
/// </summary>
public partial class TimelineMarker
{
    public TimelineMarker()
    {
        InitializeComponent();
    }

    #region Dependency Properties

    public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
        nameof(Duration),
        typeof(TimeSpan),
        typeof(TimelineMarker),
        new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.AffectsRender));
    
    public static readonly DependencyProperty GoToMarkerGridProperty = DependencyProperty.Register(
        nameof(GoToMarkerGrid),
        typeof(Grid),
        typeof(TimelineMarker),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

    #endregion

    #region Properties

    public TimeSpan Duration
    {
        get => (TimeSpan)GetValue(DurationProperty);
        set => SetValue(DurationProperty, value);
    }
    
    public Grid? GoToMarkerGrid
    {
        get => (Grid?)GetValue(GoToMarkerGridProperty);
        set => SetValue(GoToMarkerGridProperty, value);
    }
    #endregion

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var paddedWidth = ActualWidth - 1;
        
        // Aim to have one marker every 18 pixels, but the total lines should be a multiple of frequency
        const int frequency = 10;
        var       maxLines  = frequency * (int)Math.Round(Math.Floor(paddedWidth / 12) / frequency);
        var       dist      = paddedWidth / maxLines;
        if (maxLines == 0)
            return;
        for (var i = 0; i <= maxLines; i++)
        {
            var lineTime = Duration * (i / (double)maxLines);
            var x        = Math.Round(i * dist);

            var isFirst = i == 0;
            var isLast  = i == maxLines;

            if (isFirst || isLast || i % frequency == 0)
            {
                var text = new FormattedText($"{(int)lineTime.TotalMinutes:00}:{lineTime.Seconds:00}",
                    CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight,
                    new("Segoe UI"),
                    12,
                    Brushes.White,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                var textPos = x;
                if (isLast)
                    textPos -= text.WidthIncludingTrailingWhitespace + 4;
                else if (!isFirst)
                    textPos -= text.WidthIncludingTrailingWhitespace / 2;
                else
                    textPos += 2;

                var height = isFirst || isLast ? ActualHeight - 4 : ActualHeight - 16;
                drawingContext.DrawRectangle(Brushes.White, null, new(x, ActualHeight - height, 1, height));
                drawingContext.DrawText(text, new((int)textPos, 0));
            }
            else
            {
                drawingContext.DrawRectangle(Brushes.White, null, new(x, ActualHeight - 4, 1, 4));
            }
        }
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (GoToMarkerGrid == null)
            return;

        GoToMarkerGrid.Visibility = Visibility.Visible;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (GoToMarkerGrid == null)
            return;
        
        GoToMarkerGrid.Visibility = Visibility.Collapsed;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (GoToMarkerGrid == null)
            return;

        GoToMarkerGrid.Margin = new(e.GetPosition(this).X, 0, 0, 0);
    }
}
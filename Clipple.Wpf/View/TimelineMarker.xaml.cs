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
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clipple.View
{
    /// <summary>
    /// Interaction logic for TimelineMarker.xaml
    /// </summary>
    public partial class TimelineMarker : UserControl
    {
        public TimelineMarker()
        {
            InitializeComponent();
        }

        #region Dependency Properties
        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration",
            typeof(TimeSpan),
            typeof(TimelineMarker),
            new FrameworkPropertyMetadata(defaultValue: TimeSpan.Zero, flags: FrameworkPropertyMetadataOptions.AffectsRender));
        #endregion

        #region Properties
        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }
        #endregion
        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            var paddedWidth = ActualWidth - 1;

            // Aim to have one marker every 18 pixels, but the total lines should be a multiple of frequency
            var frequency = 10;
            var maxLines  = frequency * Math.Round(Math.Floor(paddedWidth / 12) / frequency);
            var dist = paddedWidth / maxLines;
            for (var i = 0; i <= maxLines; i++)
            {
                var lineTime = Duration * (i / maxLines);
                var x = Math.Round(i * dist);

                var isFirst = i == 0;
                var isLast  = i == maxLines;

                if (isFirst || isLast || i % frequency == 0)
                {
                    var text = new FormattedText($"{(int)lineTime.TotalMinutes:00}:{(int)lineTime.Seconds:00}", 
                        CultureInfo.GetCultureInfo("en-us"), FlowDirection.LeftToRight, 
                        new Typeface("Segoe UI"), 
                        12,
                        Brushes.White, 
                        VisualTreeHelper.GetDpi(this).PixelsPerDip);

                    var textPos = x;
                    if (isLast)
                    {
                        textPos -= (text.WidthIncludingTrailingWhitespace + 4);
                    }
                    else if (!isFirst)
                    {
                        textPos -= (text.WidthIncludingTrailingWhitespace / 2);
                    }
                    else
                        textPos += 2;

                    var height = isFirst || isLast ? ActualHeight - 4 : ActualHeight - 16;
                    drawingContext.DrawRectangle(Brushes.White, null, new Rect(x, ActualHeight - height, 1, height));
                    drawingContext.DrawText(text, new Point((int)textPos, 0));
                }
                else
                    drawingContext.DrawRectangle(Brushes.White, null, new Rect(x, ActualHeight - 4, 1, 4));
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for TimelineThumb.xaml
    /// </summary>
    public partial class TimelineThumb : UserControl
    {
        public TimelineThumb()
        {
            InitializeComponent();
        }

        #region Dependency properties
        public static readonly DependencyProperty SizeProperty = DependencyProperty.Register(
            "Size",
            typeof(double),
            typeof(TimelineThumb),
            new FrameworkPropertyMetadata(defaultValue: 32.0, flags: FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TimelineMarkerSizeProperty = DependencyProperty.Register(
            "TimelineMarkerSize",
            typeof(double),
            typeof(TimelineThumb),
            new FrameworkPropertyMetadata(defaultValue: 32.0, flags: FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ButtonAlignmentProperty = DependencyProperty.Register(
            "ButtonAlignment",
            typeof(VerticalAlignment),
            typeof(TimelineThumb),
            new FrameworkPropertyMetadata(defaultValue: VerticalAlignment.Center, flags: FrameworkPropertyMetadataOptions.AffectsRender));
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

        private double buttonOpacity = 1.0;

        public double ButtonOpacity
        {
            get { return buttonOpacity; }
            set { buttonOpacity = value; }
        }

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
}

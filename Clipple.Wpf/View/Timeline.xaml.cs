using Clipple.FFMPEG;
using Clipple.ViewModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Clipple.View
{
    /// <summary>
    /// Interaction logic for Timeline.xaml
    /// </summary>
    public partial class Timeline : UserControl
    {
        private enum DragTarget
        {
            Clip,
            ClipStart,
            ClipEnd,
            Playhead
        }

        public Timeline()
        {
            InitializeComponent();
        }

        #region Members
        private Point lastDragPoint;
        #endregion

        #region Dependency Properties
        public static readonly DependencyProperty TimeProperty = DependencyProperty.Register(
            "Time",
            typeof(TimeSpan),
            typeof(Timeline),
            new FrameworkPropertyMetadata(defaultValue: TimeSpan.Zero, flags: FrameworkPropertyMetadataOptions.AffectsRender, OnPositionChanged));

        public static readonly DependencyProperty DurationProperty = DependencyProperty.Register(
            "Duration",
            typeof(TimeSpan),
            typeof(Timeline),
            new FrameworkPropertyMetadata(defaultValue: TimeSpan.Zero, flags: FrameworkPropertyMetadataOptions.AffectsRender, OnPositionChanged));

        public static readonly DependencyProperty ClipStartProperty = DependencyProperty.Register(
            "ClipStart",
            typeof(TimeSpan),
            typeof(Timeline),
            new FrameworkPropertyMetadata(defaultValue: TimeSpan.Zero, flags: FrameworkPropertyMetadataOptions.AffectsRender, OnPositionChanged));

        public static readonly DependencyProperty ClipDurationProperty = DependencyProperty.Register(
            "ClipDuration",
            typeof(TimeSpan),
            typeof(Timeline),
            new FrameworkPropertyMetadata(defaultValue: TimeSpan.Zero, flags: FrameworkPropertyMetadataOptions.AffectsRender, OnPositionChanged));

        public static readonly DependencyProperty IsDraggingProperty = DependencyProperty.Register(
            "IsDragging",
            typeof(bool),
            typeof(Timeline),
            new FrameworkPropertyMetadata(defaultValue: false));

        public static readonly DependencyProperty ZoomProperty = DependencyProperty.Register(
            "Zoom",
            typeof(double),
            typeof(Timeline),
            new FrameworkPropertyMetadata(defaultValue: 1.0, flags: FrameworkPropertyMetadataOptions.AffectsRender, OnZoomChanged));
        #endregion

        #region Properties
        public TimeSpan Time
        {
            get => (TimeSpan)GetValue(TimeProperty);
            set => SetValue(TimeProperty, 
                TimeSpan.FromTicks(Math.Clamp(value.Ticks, ClipStart.Ticks, ClipStart.Ticks + ClipDuration.Ticks)));
        }

        public TimeSpan ClipStart
        {
            get => (TimeSpan)GetValue(ClipStartProperty);
            set
            {
                // Set property
                SetValue(ClipStartProperty, 
                    TimeSpan.FromTicks(Math.Clamp(value.Ticks, TimeSpan.Zero.Ticks, Duration.Ticks)));

                // Ensure playhead is within clip boundaries
                Time = TimeSpan.FromTicks(Math.Clamp(Time.Ticks, value.Ticks, value.Ticks + Duration.Ticks));
            }
        }

        public TimeSpan ClipDuration
        {
            get => (TimeSpan)GetValue(ClipDurationProperty);
            set
            {
                // Set property
                SetValue(ClipDurationProperty,
                    TimeSpan.FromTicks(Math.Clamp(value.Ticks, 0, Duration.Ticks - ClipStart.Ticks)));

                // Ensure playhead is within clip boundaries
                Time = TimeSpan.FromTicks(Math.Clamp(Time.Ticks, ClipStart.Ticks, ClipStart.Ticks + value.Ticks));
            }
        }

        public TimeSpan Duration
        {
            get => (TimeSpan)GetValue(DurationProperty);
            set => SetValue(DurationProperty, value);
        }

        public bool IsDragging
        {
            get => (bool)GetValue(IsDraggingProperty);
            set => SetValue(IsDraggingProperty, value);
        }

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        /// <summary>
        /// 12 points subtracted for margin on the waveform images
        /// </summary>
        public double TimelineWidth => ActualWidth - RootScrollable.Margin.Left - RootScrollable.Margin.Right;

        public ScaleTransform Transform { get; } = new ScaleTransform();
        #endregion

        #region Property callbacks
        private static void OnZoomChanged(DependencyObject timelineObj, DependencyPropertyChangedEventArgs args)
        {
            if (timelineObj is not Timeline timeline)
                return;

            timeline.UpdateZoom();
        }

        private static void OnPositionChanged(DependencyObject timelineObj, DependencyPropertyChangedEventArgs args)
        {
            if (timelineObj is not Timeline timeline)
                return;

            timeline.UpdateMarkers();
        }
        #endregion

        public VideoPlayerViewModel Context => (VideoPlayerViewModel)DataContext;

        #region Methods
        private void UpdateMarkers()
        {
            if (Time == TimeSpan.Zero || Duration == TimeSpan.Zero)
                return;

            // Playhead
            double playheadProgress = Math.Min(1.0, Math.Max(0.0, Time / Duration));

            PlayheadColumnSpacerStart.Width = new GridLength(playheadProgress, GridUnitType.Star);
            PlayheadButtonColumnSpacerStart.Width = PlayheadColumnSpacerStart.Width;

            PlayheadColumnSpacerEnd.Width = new GridLength(1.0 - playheadProgress, GridUnitType.Star);
            PlayheadButtonColumnSpacerEnd.Width = PlayheadColumnSpacerEnd.Width;

            // Clip start
            double clipStartProgress = Math.Min(1.0, Math.Max(0.0, ClipStart / Duration));
            double clipDuration = Math.Min(1.0, Math.Max(0.0, ClipDuration / Duration));

            ClipColumnSpacerStart.Width = new GridLength(clipStartProgress, GridUnitType.Star);
            ClipColumnSpacerEnd.Width = new GridLength(Math.Clamp(1.0 - clipStartProgress - clipDuration, 0.0, 1.0), GridUnitType.Star);
            ClipColumnSize.Width = new GridLength(clipDuration, GridUnitType.Star);
        }
        private void UpdateZoom()
        {
            var prevWidth = ScrollViewer.ScrollableWidth;
            var prevRatio = ScrollViewer.HorizontalOffset / prevWidth;
            if (ScrollViewer.HorizontalOffset == 0)
                prevRatio = 0.5;

            // The scale require to fit the waveform in the available width
            var fitScale = TimelineWidth / WaveformEngine.ResolutionX;

            // Interpolate between the scale required to fit the waveform and 1 (full waveform size)
            Transform.ScaleX = fitScale * (1 - Zoom) + 1 * Zoom;
            UpdateLayout();

            var diffWidth = ScrollViewer.ScrollableWidth - prevWidth;
            ScrollViewer.ScrollToHorizontalOffset(ScrollViewer.HorizontalOffset + diffWidth * prevRatio);
        }

        private void DragStart(UIElement element, MouseButtonEventArgs e)
        {
            e.Handled     = true;
            IsDragging    = true;
            lastDragPoint = e.GetPosition(this);

            element.CaptureMouse();
        }

        private void DragStop(UIElement element, MouseButtonEventArgs e)
        {
            e.Handled  = true;
            IsDragging = false;
            element.ReleaseMouseCapture();

            UpdateMarkers();
        }

        private void DragScroll(double offset)
        {
            if (offset < ScrollViewer.HorizontalOffset)
                ScrollViewer.ScrollToHorizontalOffset(offset);

            if (offset > (TimelineWidth + ScrollViewer.HorizontalOffset))
                ScrollViewer.ScrollToHorizontalOffset(offset - TimelineWidth);
        }

        private void DragTick(MouseEventArgs e, DragTarget dragTarget)
        {
            if (IsDragging)
            {
                e.Handled = true;
                var pos   = e.GetPosition(this);
                var diffX = pos.X - lastDragPoint.X;
                var diffY = pos.Y - lastDragPoint.Y;

                lastDragPoint = new Point(pos.X, lastDragPoint.Y);


                var wavePxOnTimeline = WaveformEngine.ResolutionX * Transform.ScaleX;
                var sens             = 1.0 - (-diffY / (App.Window.VideoPlayer.ActualHeight / 3.0));
                var ticksPerPixel    = Duration.Ticks / wavePxOnTimeline;
                var timePerPixel     = TimeSpan.FromTicks((long)ticksPerPixel);
                var timeDiff         = (diffX * timePerPixel) * Math.Clamp(sens, 0.01, 1);

                switch (dragTarget)
                {
                    case DragTarget.Playhead:
                        {
                            Time += timeDiff;

                            DragScroll(wavePxOnTimeline * (Time / Duration));
                            break;
                        }
                    case DragTarget.ClipStart:
                        {
                            ClipStart += timeDiff;

                            DragScroll(wavePxOnTimeline * (ClipStart / Duration));
                            break;
                        }
                    case DragTarget.ClipEnd:
                        {
                            ClipDuration += timeDiff;

                            DragScroll(wavePxOnTimeline * ((ClipStart + ClipDuration) / Duration));
                            break;
                        }
                    case DragTarget.Clip:
                        {
                            ClipStart += timeDiff;
                            ClipDuration += timeDiff;

                            if (timeDiff < TimeSpan.Zero)
                            {
                                DragScroll(wavePxOnTimeline * (ClipStart / Duration));
                            }
                            else 
                                DragScroll(wavePxOnTimeline * ((ClipStart + ClipDuration) / Duration));

                            break;
                        }
                }
            }
        }
        #endregion

        #region Events
        private void ClipStart_MouseDown(object sender, MouseButtonEventArgs e) => DragStart((UIElement)sender, e);
        private void ClipStart_MouseUp(object sender, MouseButtonEventArgs e) => DragStop((UIElement)sender, e);
        private void ClipStart_MouseMove(object sender, MouseEventArgs e) => DragTick(e, DragTarget.ClipStart);

        private void ClipEnd_MouseDown(object sender, MouseButtonEventArgs e) => DragStart((UIElement)sender, e);
        private void ClipEnd_MouseUp(object sender, MouseButtonEventArgs e) => DragStop((UIElement)sender, e);
        private void ClipEnd_MouseMove(object sender, MouseEventArgs e) => DragTick(e, DragTarget.ClipEnd);

        private void Clip_MouseDown(object sender, MouseButtonEventArgs e) => DragStart((UIElement)sender, e);
        private void Clip_MouseUp(object sender, MouseButtonEventArgs e) => DragStop((UIElement)sender, e);
        private void Clip_MouseMove(object sender, MouseEventArgs e) => DragTick(e, DragTarget.Clip);

        private void Playhead_MouseDown(object sender, MouseButtonEventArgs e) => DragStart((UIElement)sender, e);
        private void Playhead_MouseUp(object sender, MouseButtonEventArgs e) => DragStop((UIElement)sender, e);
        private void Playhead_MouseMove(object sender, MouseEventArgs e) => DragTick(e, DragTarget.Playhead);

        private void OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateZoom();
        #endregion
    }
}

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Clipple.FFMPEG;
using Clipple.ViewModel;

namespace Clipple.View;

/// <summary>
///     Interaction logic for Timeline.xaml
/// </summary>
public partial class Timeline
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

    public static readonly DependencyProperty TimeProperty =
        DependencyProperty.Register(
            nameof(Time),
            typeof(TimeSpan),
            typeof(Timeline),
            new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.AffectsRender, OnPositionChanged));

    public static readonly DependencyProperty DurationProperty =
        DependencyProperty.Register(
            nameof(Duration),
            typeof(TimeSpan),
            typeof(Timeline),
            new FrameworkPropertyMetadata(TimeSpan.Zero, FrameworkPropertyMetadataOptions.AffectsRender, OnPositionChanged));

    public static readonly DependencyProperty ClipStartProperty =
        DependencyProperty.Register(
            "ClipStart",
            typeof(TimeSpan?),
            typeof(Timeline),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPositionChanged));

    public static readonly DependencyProperty ClipDurationProperty =
        DependencyProperty.Register(
            "ClipDuration",
            typeof(TimeSpan?),
            typeof(Timeline),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPositionChanged));

    public static readonly DependencyProperty IsDraggingProperty =
        DependencyProperty.Register(
            nameof(IsDragging),
            typeof(bool),
            typeof(Timeline),
            new FrameworkPropertyMetadata(false));

    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register(
            nameof(Zoom),
            typeof(double),
            typeof(Timeline),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender, OnZoomChanged));

    public static readonly DependencyProperty ShowAudioStreamNamesProperty =
        DependencyProperty.Register(
            nameof(ShowAudioStreamNames),
            typeof(bool),
            typeof(Timeline),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

    #endregion

    #region Properties

    public TimeSpan Time
    {
        get => (TimeSpan)GetValue(TimeProperty);
        set
        {
            if (value.Ticks < 0)
                return;

            SetValue(TimeProperty, value);
        }
    }

    public TimeSpan ClipStart
    {
        get => (TimeSpan?)GetValue(ClipStartProperty) ?? TimeSpan.Zero;
        set
        {
            if (value.Ticks < 0)
                return;

            SetValue(ClipStartProperty, value);
        }
    }

    public TimeSpan ClipDuration
    {
        get => (TimeSpan?)GetValue(ClipDurationProperty) ?? Duration;
        set
        {
            if (value.Ticks < 0)
                return;

            SetValue(ClipDurationProperty, value);
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

    public bool ShowAudioStreamNames
    {
        get => (bool)GetValue(ShowAudioStreamNamesProperty);
        set => SetValue(ShowAudioStreamNamesProperty, value);
    }

    /// <summary>
    ///     12 points subtracted for margin on the waveform images
    ///// </summary> 
    public double TimelineWidth => ActualWidth - RootScrollable.Margin.Left - RootScrollable.Margin.Right;

    public ScaleTransform Transform { get; } = new();

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
    
    #region Methods

    private void SetClipStartClamped(TimeSpan time)
    {
        ClipStart = TimeSpan.FromTicks(Math.Clamp(time.Ticks, 0, Duration.Ticks - ClipDuration.Ticks));
        SetTimeClamped(Time);
    }

    private void SetClipDurationClamped(TimeSpan time)
    {
        if (time.Ticks < 0)
            return;
        
        ClipDuration = TimeSpan.FromTicks(Math.Clamp(time.Ticks, 0, Duration.Ticks - ClipStart.Ticks));
        SetTimeClamped(Time);
    }

    private void SetTimeClamped(TimeSpan time)
    {
        if (time.Ticks < 0)
            return;
        
        Time = TimeSpan.FromTicks(Math.Clamp(time.Ticks, ClipStart.Ticks, ClipStart.Ticks + ClipDuration.Ticks));
    }

    private void UpdateMarkers()
    {
        if (Duration == TimeSpan.Zero)
            return;

        // Playhead
        var playheadProgress = Math.Min(1.0, Math.Max(0.0, Time / Duration));

        // Clip start
        var clipStartProgress = Math.Min(1.0, Math.Max(0.0, ClipStart / Duration));
        var clipDuration      = Math.Min(1.0, Math.Max(0.0, ClipDuration / Duration));

        // Extra clamping… why? because the play position given to us by MPV is only millisecond accurate and can
        // lead to the playhead appearing outside the clip bounds sometimes, after being automatically paused
        playheadProgress = Math.Clamp(playheadProgress, clipStartProgress, clipStartProgress + clipDuration);

        PlayheadColumnSpacerStart.Width       = new(playheadProgress, GridUnitType.Star);
        PlayheadButtonColumnSpacerStart.Width = PlayheadColumnSpacerStart.Width;

        PlayheadColumnSpacerEnd.Width       = new(1.0 - playheadProgress, GridUnitType.Star);
        PlayheadButtonColumnSpacerEnd.Width = PlayheadColumnSpacerEnd.Width;


        ClipColumnSpacerStart.Width = new(clipStartProgress, GridUnitType.Star);
        ClipColumnSpacerEnd.Width   = new(Math.Clamp(1.0 - clipStartProgress - clipDuration, 0.0, 1.0), GridUnitType.Star);
        ClipColumnSize.Width        = new(clipDuration, GridUnitType.Star);
    }

    private void UpdateZoom()
    {
        // Record the mouse position and cursor time before applying the zoom
        var mousePos   = Mouse.GetPosition(ScrollViewer).X - RootScrollable.Margin.Left;
        var cursorTime = PixelsToTime(Mouse.GetPosition(RootScrollable).X);

        // Actually perform the zoom
        {
            // The scale require to fit the waveform in the available width
            var fitScale = TimelineWidth / Waveform.DoubleResolutionX;
        
            // Interpolate between the scale required to fit the waveform and 1 (full waveform size)
            Transform.ScaleX = fitScale * (1 - Zoom) + 1 * Zoom;
            UpdateLayout();
        }

        // Try to scroll the timeline so that the time that our cursor was hovering will still hover the same time
        // after zooming.  E.g: the cursor was on 00:30:00, after the zoom, the zoom should scroll so that the cursor
        // still points at 00:30:00
        // 
        // Note if the cursor is not available, try to perform the same action using the playhead instead (centered)
        {
            if (mousePos == 0)
                mousePos = TimelineWidth / 2;

            if (cursorTime == TimeSpan.Zero)
                cursorTime = Time;
        
            var pos = Math.Clamp(TimeToPixels(cursorTime) - mousePos, 0, ScrollViewer.ScrollableWidth);
            ScrollViewer.ScrollToHorizontalOffset(pos);   
        }
    }

    private void DragStart(UIElement element, MouseButtonEventArgs e, DragTarget dragTarget)
    {
        e.Handled     = true;
        IsDragging    = true;
        lastDragPoint = e.GetPosition(this);

        element.CaptureMouse();

        PlayheadThumb.NotifyDragStart(dragTarget == DragTarget.Playhead);
        ClipThumbLeft.NotifyDragStart(dragTarget == DragTarget.ClipStart);
        ClipThumbRight.NotifyDragStart(dragTarget == DragTarget.ClipEnd);
    }

    private void DragStop(UIElement element, MouseButtonEventArgs e, DragTarget dragTarget)
    {
        e.Handled  = true;
        IsDragging = false;
        element.ReleaseMouseCapture();

        UpdateMarkers();

        PlayheadThumb.NotifyDragEnd(dragTarget == DragTarget.Playhead);
        ClipThumbLeft.NotifyDragEnd(dragTarget == DragTarget.ClipStart);
        ClipThumbRight.NotifyDragEnd(dragTarget == DragTarget.ClipEnd);
    }

    private void DragScroll(double offset)
    {
        if (offset < ScrollViewer.HorizontalOffset)
        {
            ScrollViewer.ScrollToHorizontalOffset(offset);
        }
        else if (offset > TimelineWidth + ScrollViewer.HorizontalOffset)
            ScrollViewer.ScrollToHorizontalOffset(offset - TimelineWidth);
    }

    private TimeSpan PixelsToTime(double pixels)
    {
        var wavePxOnTimeline = Waveform.DoubleResolutionX * Transform.ScaleX;
        var ticksPerPixel    = Duration.Ticks / wavePxOnTimeline;
        var timePerPixel     = TimeSpan.FromTicks((long)ticksPerPixel);
        return pixels * timePerPixel;
    }

    private double TimeToPixels(TimeSpan time)
    {
        if (time.Ticks == 0)
            return 0;
        
        var wavePxOnTimeline = Waveform.DoubleResolutionX * Transform.ScaleX;
        var ticksPerPixel    = Duration.Ticks / wavePxOnTimeline;
        return time.Ticks / ticksPerPixel;
    }

    private void DragTick(MouseEventArgs e, DragTarget dragTarget)
    {
        if (!IsDragging) return;
        
        e.Handled = true;
        var pos   = e.GetPosition(this);
        var diffX = pos.X - lastDragPoint.X;
        var diffY = pos.Y - lastDragPoint.Y;

        lastDragPoint = new(pos.X, lastDragPoint.Y);
            
        var sens             = 1.0 - -diffY / (App.Window.MediaEditor.ActualHeight / 3.0);
        var wavePxOnTimeline = Waveform.DoubleResolutionX * Transform.ScaleX;
        var timeDiff         = PixelsToTime(diffX) * Math.Clamp(sens, 0.01, 1);

        switch (dragTarget)
        {
            case DragTarget.Playhead:
            {
                SetTimeClamped(Time + timeDiff);
                DragScroll(wavePxOnTimeline * (Time / Duration));
                break;
            }
            case DragTarget.ClipStart:
            {
                var effectiveDiff = TimeSpan.FromTicks(Math.Max(timeDiff.Ticks, -ClipStart.Ticks));

                SetClipDurationClamped(ClipDuration - effectiveDiff);
                SetClipStartClamped(ClipStart + effectiveDiff);
                DragScroll(wavePxOnTimeline * (ClipStart / Duration));
                break;
            }
            case DragTarget.ClipEnd:
            {
                SetClipDurationClamped(ClipDuration + timeDiff);
                DragScroll(wavePxOnTimeline * ((ClipStart + ClipDuration) / Duration));
                break;
            }
            case DragTarget.Clip:
            {
                SetClipStartClamped(ClipStart + timeDiff);
                
                // Move playhead with the clip so that it maintains relative position
                if (ClipStart > TimeSpan.Zero && ClipStart + ClipDuration < Duration)
                    SetTimeClamped(Time + timeDiff);

                if (timeDiff < TimeSpan.Zero)
                    DragScroll(wavePxOnTimeline * (ClipStart / Duration));
                    
                if (timeDiff > TimeSpan.Zero)
                    DragScroll(wavePxOnTimeline * ((ClipStart + ClipDuration) / Duration));

                break;
            }
        }
    }

    #endregion

    #region Events

    private void ClipStart_MouseDown(object sender, MouseButtonEventArgs e)
    {
        DragStart((UIElement)sender, e, DragTarget.ClipStart);
    }

    private void ClipStart_MouseUp(object sender, MouseButtonEventArgs e)
    {
        DragStop((UIElement)sender, e, DragTarget.ClipStart);
    }

    private void ClipStart_MouseMove(object sender, MouseEventArgs e)
    {
        DragTick(e, DragTarget.ClipStart);
    }

    private void ClipEnd_MouseDown(object sender, MouseButtonEventArgs e)
    {
        DragStart((UIElement)sender, e, DragTarget.ClipEnd);
    }

    private void ClipEnd_MouseUp(object sender, MouseButtonEventArgs e)
    {
        DragStop((UIElement)sender, e, DragTarget.ClipEnd);
    }

    private void ClipEnd_MouseMove(object sender, MouseEventArgs e)
    {
        DragTick(e, DragTarget.ClipEnd);
    }

    private void Clip_MouseDown(object sender, MouseButtonEventArgs e)
    {
        DragStart((UIElement)sender, e, DragTarget.Clip);
    }

    private void Clip_MouseUp(object sender, MouseButtonEventArgs e)
    {
        DragStop((UIElement)sender, e, DragTarget.Clip);
    }

    private void Clip_MouseMove(object sender, MouseEventArgs e)
    {
        DragTick(e, DragTarget.Clip);
    }

    private void Playhead_MouseDown(object sender, MouseButtonEventArgs e)
    {
        DragStart((UIElement)sender, e, DragTarget.Playhead);
    }

    private void Playhead_MouseUp(object sender, MouseButtonEventArgs e)
    {
        DragStop((UIElement)sender, e, DragTarget.Playhead);
    }

    private void Playhead_MouseMove(object sender, MouseEventArgs e)
    {
        DragTick(e, DragTarget.Playhead);
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateZoom();
    }

    private void OnGoToMarkerClicked(object sender, MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(sender as UIElement);
        e.Handled = true;

        IsDragging = true;
        SetTimeClamped(PixelsToTime(pos.X));
        IsDragging = false;
    }
    #endregion
}
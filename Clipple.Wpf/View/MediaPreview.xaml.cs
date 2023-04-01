using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Clipple.Types;
using Clipple.ViewModel;
using MaterialDesignThemes.Wpf;
using Mpv.NET.Player;

namespace Clipple.View;

public partial class MediaPreview
{
    public MediaPreview()
    {
        InitializeComponent();
        
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(50)
        };
        timer.Tick += OnSliderTick;
        timer.Start();

        PreviewHostControl.Child = MediaPlayer;
    }

    #region Methods

    private void UpdateMediaPosition()
    {
        MediaPlayer.Position = TimeSpan.FromTicks(Math.Clamp((long)(MediaPlayer.Duration.Ticks * PositionSlider.Value), 0, MediaPlayer.Duration.Ticks));
    }

    private void UpdatePlayButton()
    {
        TogglePlayButton.Content = new PackIcon
        {
            Kind = MediaPlayer!.EndReached ? PackIconKind.Rewind : MediaPlayer.IsPlaying ? PackIconKind.Pause : PackIconKind.Play
        };
    }

    #endregion

    #region Event handlers

    private void OnSliderTick(object? sender, EventArgs e)
    {
        if (isDragging)
        {
            UpdateMediaPosition();
        }
        else
        {
            updatingSlider = true;
            if (MediaPlayer.Duration.Ticks <= 0 || MediaPlayer.Position.Ticks <= 0)
            {
                PositionSlider.Value = 0;
            }
            else
            {
                if (MediaPlayer.EndReached)
                {
                    PositionSlider.Value = 1;
                    UpdatePlayButton();
                }
                else
                {
                    PositionSlider.Value = MediaPlayer.Position / MediaPlayer.Duration;
                    UpdatePlayButton();
                }
            }

            updatingSlider = false;
        }
    }

    private void OnMediaChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (DataContext is not Media media)
            return;

        MediaPlayer.Stop();
        MediaPlayer.Load(media.FilePath);

        TogglePlayButton.Content = new PackIcon
        {
            Kind = PackIconKind.Play
        };

        ToggleMuteButton.Content = new PackIcon
        {
            Kind = PackIconKind.VolumeOff
        };

        if (media?.AudioStreams == null || media.AudioStreams.Length == 0)
        {
            MediaPlayer.SetFilter("");
            return;
        }

        var inputString = string.Join("", media.AudioStreams.Select(stream => $"[aid{stream.AudioStreamIndex + 1}]"));
        MediaPlayer.SetFilter($"{inputString}amix=inputs={media.AudioStreams.Length}[ao]");
    }

    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (DataContext is not Media media)
            return;

        if (!media.HasVideo || media.VideoWidth is not { } width || media.VideoHeight is not { } height || !e.WidthChanged)
            return;
        
        PreviewHostControl.Height = e.NewSize.Width * ((double)height / width);
    }

    private void TogglePlayButtonClick(object sender, RoutedEventArgs e)
    {
        if (MediaPlayer!.EndReached)
        {
            MediaPlayer!.Position = TimeSpan.Zero;
            MediaPlayer!.Resume();
        }
        else
        {
            if (MediaPlayer!.IsPlaying)
                MediaPlayer!.Pause();
            else
                MediaPlayer!.Resume();
        }

        UpdatePlayButton();
    }

    private void ToggleMuteButtonClick(object sender, RoutedEventArgs e)
    {
        MediaPlayer!.IsMuted = !MediaPlayer!.IsMuted;

        ToggleMuteButton.Content = new PackIcon
        {
            Kind = MediaPlayer!.IsMuted ? PackIconKind.VolumeHigh : PackIconKind.VolumeOff
        };
    }

    private void PositionDragStarted(object sender, DragStartedEventArgs e)
    {
        isDragging = true;

        if (MediaPlayer!.IsPlaying)
        {
            MediaPlayer!.Pause();
            resumeAfterDrag = true;
        }
        else
        {
            resumeAfterDrag = false;
        }
    }

    private void PositionDragCompleted(object sender, DragCompletedEventArgs e)
    {
        isDragging = false;

        if (resumeAfterDrag)
            MediaPlayer!.Resume();

        UpdateMediaPosition();
    }

    private void OnPositionSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (!updatingSlider)
            UpdateMediaPosition();
    }

    #endregion

    #region Members

    private bool isDragging;
    private bool resumeAfterDrag;
    private bool updatingSlider;

    #endregion

    #region Properties
    public HostedMediaPlayer MediaPlayer { get; } = new();

    #endregion
}
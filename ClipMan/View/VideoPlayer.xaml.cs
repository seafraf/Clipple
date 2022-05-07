using ClipMan.ViewModel;
using FFmpeg.AutoGen;
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
using Unosquare.FFME.Common;

namespace ClipMan.View
{
    /// <summary>
    /// Interaction logic for Video.xaml
    /// </summary>
    public partial class VideoPlayer : UserControl
    {
        public Unosquare.FFME.MediaElement MediaElement => videoPlayer;

        private bool startedDuringPlay;
        private bool isScrubbing;

        public VideoPlayer()
        {
            InitializeComponent();
        }

        private async void Slider_DragStarted(object sender, System.Windows.Controls.Primitives.DragStartedEventArgs e)
        {
            isScrubbing       = true;
            startedDuringPlay = videoPlayer.IsPlaying;

            await videoPlayer.Pause();
        }

        private async void Slider_DragCompleted(object sender, System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            isScrubbing = false;

            if (startedDuringPlay)
                await videoPlayer.Play();
        }

        private void OnMediaOpened(object sender, MediaOpenedEventArgs e)
        {
            if (DataContext is not VideoPlayerViewModel vm)
                return;

            if (e.Info.BitRate == 0 || !e.Info.BestStreams.ContainsKey(AVMediaType.AVMEDIA_TYPE_VIDEO))
            {
                vm.VideoState = VideoState.Failed;
                return;
            }
            
            vm.VideoState       = VideoState.Ready;
            vm.VideoPosition    = TimeSpan.Zero;
            vm.IsPlaying        = false;
            vm.VideoDuration    = e.Info.Duration;

            var bestAudioStream = e.Info.BestStreams[AVMediaType.AVMEDIA_TYPE_AUDIO];
            var bestVideoStream = e.Info.BestStreams[AVMediaType.AVMEDIA_TYPE_VIDEO];
            
            // Generate available audio tracks
            vm.AudioTracks = e.Info.Streams.Values.Where((stream) => stream.CodecType == AVMediaType.AVMEDIA_TYPE_AUDIO)
                .Select(stream => new MediaTrack(GetAudioTrackName(stream.StreamIndex), stream.StreamIndex))
                .ToArray();

            // Generate available video tracks
            vm.VideoTracks = e.Info.Streams.Values.Where((stream) => stream.CodecType == AVMediaType.AVMEDIA_TYPE_VIDEO)
                .Select(stream => new MediaTrack($"Track {stream.StreamId} - {stream.CodecName} {stream.PixelWidth}x{stream.PixelHeight}", stream.StreamIndex))
                .ToArray();

            // Choose best starting audio and video track
            var bestAudioIndex = bestAudioStream?.StreamIndex ?? -1;
            var bestVideoIndex = bestVideoStream.StreamIndex;

            if (bestAudioIndex != -1)
            {
                vm.SelectedAudioTrackIndex = vm.AudioTracks.Where((track) => track.Index == bestAudioIndex)
                    .Select((track, index) => index).First();
            }

            vm.SelectedVideoTrackIndex = vm.VideoTracks.Where((track) => track.Index == bestVideoIndex)
                .Select((track, index) => index).First();

            vm.VideoFPS     = bestVideoStream.FPS;
            vm.VideoWidth   = bestVideoStream.PixelWidth;
            vm.VideoHeight  = bestVideoStream.PixelHeight;
        }

        private void OnMediaChanging(object sender, MediaOpeningEventArgs e)
        {
            var mediaChange = AppCommands.ConsumeMediaChange();
            if (mediaChange == null)
                return;

            if (e.Info.Streams.ContainsKey(mediaChange.AudioIndex))
                e.Options.AudioStream = e.Info.Streams[mediaChange.AudioIndex];

            if (e.Info.Streams.ContainsKey(mediaChange.VideoIndex))
                e.Options.VideoStream = e.Info.Streams[mediaChange.VideoIndex];
        }

        private void OnMediaChanged(object sender, MediaOpenedEventArgs e)
        {
            if (DataContext is not VideoPlayerViewModel vm)
                return;

            var stream = e.Info.Streams[videoPlayer.VideoStreamIndex];

            vm.VideoFPS     = stream.FPS;
            vm.VideoWidth   = stream.PixelWidth;
            vm.VideoHeight  = stream.PixelHeight;
        }

        private void OnMediaClosed(object sender, EventArgs e)
        {
            if (DataContext is not VideoPlayerViewModel vm)
                return;

            vm.VideoState    = VideoState.Loading;
            vm.VideoPosition = TimeSpan.Zero;
            vm.IsPlaying     = false;
            vm.VideoDuration = TimeSpan.Zero;
            vm.VideoFPS      = 0;
        }

        private void OnMediaEnded(object sender, EventArgs e)
        {
            if (DataContext is not VideoPlayerViewModel vm)
                return;

            vm.IsPlaying = false;
        }

        private void OnMediaFailed(object sender, MediaFailedEventArgs e)
        {
            if (DataContext is not VideoPlayerViewModel vm)
                return;

            vm.VideoState = VideoState.Failed;
        }

        private async void OnRenderingVideo(object sender, RenderingVideoEventArgs e)
        {
            if (DataContext is not VideoPlayerViewModel vm || vm.Video == null)
                return;

            if (isScrubbing)
                return;

            var currentFrame = (long)Math.Round(e.StartTime.TotalSeconds * vm.VideoFPS);

            foreach (var clip in vm.Video.Clips)
            {
                if (clip.ShowInPlayer)
                {
                    if ((clip.PauseAtClipStart && currentFrame == clip.StartFrame) ||
                        clip.PauseAtClipEnd && currentFrame == clip.EndFrame)
                    {
                        await videoPlayer.Pause();

                        // Move to the frame we were meant to pause at to avoid the pause delay changing the frame
                        vm.SeekFrame(currentFrame);
                    }
                }
            }
        }

        /// <summary>
        /// Helper function to try and get the best name for an audio track.
        /// </summary>
        /// <param name="trackId">The ID of the track to get audio for</param>
        /// <returns>The best name generated from the track ID</returns>
        private static string GetAudioTrackName(int trackId)
        {
            // Try to get the track names from the root viewmodel's videofile data
            string? trackName = App.ViewModel.SelectedVideo?.TrackNames.ElementAtOrDefault(trackId);
            if (trackName != null)
                return $"Track {trackId} - {trackName}";

            return $"Track {trackId}";
        }
    }
}

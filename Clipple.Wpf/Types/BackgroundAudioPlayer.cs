using Clipple.DataModel;
using FlyleafLib;
using FlyleafLib.MediaFramework.MediaStream;
using FlyleafLib.MediaPlayer;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Clipple.Types
{
    public class BackgroundAudioPlayer : ObservableObject, IDisposable
    {
        public BackgroundAudioPlayer(VideoState videoState, int streamIndex, string sourceFile, string name)
        {
            VideoState       = videoState;
            this.name        = name;
            this.streamIndex = streamIndex;

            var config  = new Config();

            config.Player.Usage                     = Usage.Audio;
            config.Player.SeekAccurate              = true;
            config.Player.AutoPlay                  = false;
            config.Player.VolumeMax                 = 100;
            config.Player.MouseBindings.Enabled     = false;
            config.Player.KeyBindings.Enabled       = false;

            player = new Player(config);
            player.OpenAsync(sourceFile);
            player.OpenCompleted += (s, e) =>
            {
                player.Open(player.MainDemuxer.AudioStreams.Where(x => x.StreamIndex == streamIndex).First());

                IsMuted = VideoState.MutedTracks.GetValueOrDefault(streamIndex, false);
                Volume = VideoState.TrackVolume.GetValueOrDefault(streamIndex, 100.0);
            };
        }

        #region Properties
        private double volume = 100;
        public double Volume
        {
            get => volume;
            set
            {
                SetProperty(ref volume, value);

                player.Audio.Volume = (int)(value * (BaseVolume / 100));
                player.Audio.Mute   = BaseMuted || IsMuted;

                VideoState.TrackVolume[StreamIndex] = value;
            }
        }

        private double baseVolume;
        public double BaseVolume
        {
            get => baseVolume;
            set
            {
                SetProperty(ref baseVolume, value);

                player.Audio.Volume = (int)(volume * (value / 100));
                player.Audio.Mute   = BaseMuted || IsMuted;
            }
        }

        private bool isMuted = false;
        public bool IsMuted
        {
            get => isMuted;
            set
            {
                SetProperty(ref isMuted, value);

                if (!BaseMuted)
                    player.Audio.Mute = value;

                VideoState.MutedTracks[StreamIndex] = value;
            }
        }

        private bool baseMuted = false;
        public bool BaseMuted
        {
            get => baseMuted;
            set
            {
                SetProperty(ref baseMuted, value);

                player.Audio.Mute = value || isMuted;
            }
        }

        private Player player;
        public Player Player
        {
            get => player;
            set => SetProperty(ref player, value);
        }

        private int streamIndex;
        public int StreamIndex
        {
            get => streamIndex;
            set => SetProperty(ref streamIndex, value);
        }

        private string name;
        public string Name
        {
            get => name;
            set => SetProperty(ref name, value);
        }

        public VideoState VideoState { get; }
        #endregion

        public void Dispose()
        {
            player.Dispose();
        }
    }
}

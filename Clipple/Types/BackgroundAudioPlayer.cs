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
        public BackgroundAudioPlayer(int streamIndex, string sourceFile, string name)
        {
            this.name        = name;
            this.streamIndex = streamIndex;

            var config  = new Config();

            config.Player.Usage                     = Usage.Audio;
            config.Player.SeekAccurate              = true;
            config.Player.AutoPlay                  = false;
            config.Player.MouseBindings.Enabled     = false;
            config.Player.KeyBindings.Enabled       = false;

            player = new Player(config);
            player.OpenAsync(sourceFile);
            player.OpenCompleted += (s, e) =>
            {
                player.OpenAsync(player.MainDemuxer.AudioStreams.Where(x => x.StreamIndex == streamIndex).First());
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

                player.Audio.Volume = (int)(value * (BaseVolume / 150));
                player.Audio.Mute   = BaseMuted || IsMuted;
            }
        }

        private double baseVolume;
        public double BaseVolume
        {
            get => baseVolume;
            set
            {
                SetProperty(ref baseVolume, value);

                player.Audio.Volume = (int)(volume * (value / 150));
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
        #endregion

        public void Dispose()
        {
            player.Dispose();
        }
    }
}

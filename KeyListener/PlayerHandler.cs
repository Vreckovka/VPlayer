using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Listener;
using Vlc.DotNet.Core;
using VPlayer.AudioStorage.Models;

namespace VPlayer.Models
{
    public static class PlayerHandler
    {
        public static List<Song> PlayList;
        private static int actualSongIndex = 0;
        public static VlcMediaPlayer MediaPlayer{ get; private set; }

        public static Song ActualSong { get; private set; }

        public static bool IsPlaying { get; set; }
        static PlayerHandler()
        {
            KeyListener.OnKeyPressed += KeyListener_OnKeyPressed;

            var libDirectory = new DirectoryInfo(Path.Combine("C:\\Users\\Roman Pecho\\source\\repos\\VPlayer\\KeyListener", "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));
            MediaPlayer = new VlcMediaPlayer(libDirectory);

          

            bool playFinished = false;
            MediaPlayer.EncounteredError += (sender, e) =>
            {
                Console.Error.Write("An error occurred");
                playFinished = true;
            };

            MediaPlayer.EndReached += (sender, e) => { PlayNext(); };
        }

        /// <summary>
        /// Plays playlist or play actual song
        /// </summary>
        public static void Play()
        {
            Task.Run(() =>
            {
                if (PlayList.Count > 0)
                {
                    if (PlayList[actualSongIndex] == ActualSong)
                    {
                        //mediaPlayer.SetMedia(new Uri(PlayList.Peek().DiskLocation));
                        MediaPlayer.Play();

                    }
                    else
                    {
                        MediaPlayer.SetMedia(new Uri(PlayList[actualSongIndex].DiskLocation));
                        MediaPlayer.Play();
                        ActualSong = PlayList[actualSongIndex];
                        ActualSong.IsPlaying = true;

                    }
                }
            });
        }

        public static void Pause()
        {
            MediaPlayer.Pause();
        }

        public static void PlayNext()
        {
            ActualSong.IsPlaying = false;
            actualSongIndex++;
            Play();
        }

        private static void KeyListener_OnKeyPressed(object sender, KeyPressedArgs e)
        {
            if (e.KeyPressed == Key.MediaPlayPause)
            {
                if (IsPlaying)
                {
                    IsPlaying = false;
                    Play();
                }
                else
                {
                    IsPlaying = true;
                    Pause();
                }
            }
            else if (e.KeyPressed == Key.MediaNextTrack)
            {
                PlayNext();
            }
        }

    }
}
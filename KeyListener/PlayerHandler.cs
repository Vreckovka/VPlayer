using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Listener;

namespace VPlayer.Models
{
    public static class PlayerHandler
    {
        public static bool IsPlaying { get; set; }
        public static event EventHandler Play;
        public static event EventHandler Pause;
        public static event EventHandler Stop;
        public static event EventHandler<string> Next;
        public static event EventHandler<string> Previous;
        public static event EventHandler<double> ChangeTime;

        static PlayerHandler()
        {
            KeyListener.OnKeyPressed += KeyListener_OnKeyPressed;
        }
        private static void KeyListener_OnKeyPressed(object sender, KeyPressedArgs e)
        {
            if (e.KeyPressed == Key.MediaPlayPause)
            {
                if (IsPlaying)
                {
                    IsPlaying = false;
                    OnPause(null);
                }
                else
                {
                    IsPlaying = true;
                    OnPlay(null);
                }
            }
        }

        public static void OnPlay(object sender)
        {
            Play?.Invoke(sender, EventArgs.Empty);
        }

        public static void OnPause(object sender)
        {
            Pause?.Invoke(sender, EventArgs.Empty);
        }

        public static void OnChangeTime(double e)
        {
            ChangeTime?.Invoke(null, e);
        }
    }
}
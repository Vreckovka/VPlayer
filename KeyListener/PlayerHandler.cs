using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Events;
using VCore.ViewModels;
using Vlc.DotNet.Core;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Events;

namespace KeyListener
{
  public class PlayerHandler : ViewModel
  {
    public static List<Song> PlayList;
    private static int actualSongIndex = 0;
    public static VlcMediaPlayer MediaPlayer { get; private set; }

    public static Song ActualSong { get; private set; }

    public static bool IsPlaying { get; set; }
    public PlayerHandler(IEventAggregator eventAggregator)
    {
      KeyListener.OnKeyPressed += KeyListener_OnKeyPressed;

      var path = Path.Combine("C:\\Users\\Roman Pecho\\source\\repos\\VPlayer\\KeyListener", "libvlc",
        IntPtr.Size == 4 ? "win-x86" : "win-x64");

      if (Directory.Exists(path))
      {
        var libDirectory = new DirectoryInfo(path);
        MediaPlayer = new VlcMediaPlayer(libDirectory);

        bool playFinished = false;

        MediaPlayer.EncounteredError += (sender, e) =>
        {
          Console.Error.Write("An error occurred");
          playFinished = true;
        };

        MediaPlayer.EndReached += (sender, e) => { PlayNext(); };

        eventAggregator.GetEvent<PlaySongsEvent>().Subscribe(PlayArtist);
        eventAggregator.GetEvent<PauseEvent>().Subscribe(Pause);
      }
    }

    private void PlayArtist(IEnumerable<Song> songs)
    {
      PlayList = songs.ToList();
      Play();
    }

    /// <summary>
    /// Plays playlist or play actual song
    /// </summary>
    public void Play()
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
          }
        }
      });
    }



    public void Pause()
    {
      MediaPlayer.Pause();
    }

    public void PlayNext()
    {
      actualSongIndex++;
      //Play();
    }

    private void KeyListener_OnKeyPressed(object sender, KeyPressedArgs e)
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
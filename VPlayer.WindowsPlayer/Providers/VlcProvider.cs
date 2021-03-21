using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LibVLCSharp.Shared;
using LibVLCSharp.WPF;

namespace VPlayer.WindowsPlayer.Providers
{
  public class VlcProvider : IVlcProvider
  {
    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);
    private bool initilize;

    #region LoadVlc

    public async Task<KeyValuePair<MediaPlayer,LibVLC>> InitlizeVlc()
    {
      await semaphoreSlim.WaitAsync();

      var player = await Task.Run(() =>
      {
        if(!initilize)
        {
          LibVLCSharp.Shared.Core.Initialize();
          initilize = true;
        }

        var libVLC = new LibVLC("--freetype-background-opacity=120", "--freetype-background-color=0");

        return new KeyValuePair<MediaPlayer, LibVLC>(new MediaPlayer(libVLC),libVLC);
      });


      semaphoreSlim.Release();

      return player;
    }

    #endregion

  }
}

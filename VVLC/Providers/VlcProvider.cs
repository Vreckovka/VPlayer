using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace VPlayer.Core.Providers
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

        var libVLC = new LibVLC("--freetype-background-opacity=150", "--freetype-background-color=0", "--freetype-rel-fontsize=22");

        return new KeyValuePair<MediaPlayer, LibVLC>(new MediaPlayer(libVLC),libVLC);
      });


      semaphoreSlim.Release();

      return player;
    }

    #endregion

  }
}

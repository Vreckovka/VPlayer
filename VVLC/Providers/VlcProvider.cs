using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;

namespace VVLC.Providers
{
  public class VlcProvider : IVlcProvider
  {
    private object lockObject = new object();
    private bool initilized;

    public VlcProvider()
    {

    }

    #region LoadVlc

    public LibVLC InitlizeVlc()
    {
      lock (lockObject)
      {
        if (!initilized)
        {
          Core.Initialize();
          initilized = true;
        }

        return GetLibVLC();
      }
    }

    public LibVLC GetLibVLC()
    {
      bool enableLogs = false;
#if DEBUG
      enableLogs = true;
#endif
      return new LibVLC(
        enableLogs,
        "--verbose=2",
        "--freetype-background-opacity=150", 
        "--freetype-background-color=0",
        "--freetype-rel-fontsize=22",
        "--codec=freetype",
        "--sub-filter=freetype",

        "--aout=directsound",   //Multiple Mediaplayers are sharing volume, this allows each volume for each player

        "--http-reconnect"   
        );
    }

    #endregion

   
  }
}

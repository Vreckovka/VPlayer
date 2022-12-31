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
      return new LibVLC("--freetype-background-opacity=150", "--freetype-background-color=0", "--freetype-rel-fontsize=22");
    }

    #endregion

   
  }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Vlc.DotNet.Wpf;

namespace VPlayer.WindowsPlayer.Providers
{
  public class VlcProvider : IVlcProvider
  {
    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);

    #region LoadVlc

    public async Task InitlizeVlc(VlcControl vlcControl)
    {
      await semaphoreSlim.WaitAsync();

      var currentAssembly = Assembly.GetEntryAssembly();

      if (currentAssembly != null)
      {
        var currentDirectory = new FileInfo(currentAssembly.Location).DirectoryName;

        var path = new DirectoryInfo(Path.Combine(currentDirectory, "libvlc", IntPtr.Size == 4 ? "win-x86" : "win-x64"));

        var libDirectory = new DirectoryInfo(path.FullName);

        vlcControl.SourceProvider.CreatePlayer(libDirectory);
      }

      semaphoreSlim.Release();
    }

    #endregion

  }
}

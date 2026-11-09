using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using LibVLCSharp.Shared;
using VCore.Standard.Providers;

namespace VVLC.Providers
{
  public class VlcProvider : IVlcProvider
  {
    private object lockObject = new object();
    private bool initilized;
    private readonly ISettingsProvider settingsProvider;

    public VlcProvider(ISettingsProvider settingsProvider = null)
    {
      this.settingsProvider = settingsProvider;
    }

    #region LoadVlc

    public LibVLC InitlizeVlc()
    {
      lock (lockObject)
      {
        if (!initilized)
        {
          using (VPLayer.Domain.Diagnostics.StartupMeasurements.Measure("VLC / core initialization"))
            Core.Initialize();
          initilized = true;
        }

        return GetLibVLC();
      }
    }

    public LibVLC GetLibVLC()
    {
      using var measurement = VPLayer.Domain.Diagnostics.StartupMeasurements.Measure("VLC / instance construction");
      bool enableLogs = false;
#if DEBUG
      enableLogs = true;
#endif
      var arguments = new[] {
        "--verbose=2",
        "--aout=directsound",   //Multiple Mediaplayers are sharing volume, this allows each volume for each player
        "--http-reconnect"
      }.Concat(SubtitleAppearance.Options(key =>
        settingsProvider?.Settings != null && settingsProvider.Settings.TryGetValue(key, out var setting)
          ? setting.Value : null)).ToArray();
      return new LibVLC(enableLogs, arguments);
    }

    #endregion

   
  }
}

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using LibVLCSharp.Shared;
using Logger;
using VCore.Helpers;
using VCore.Standard;
using VCore.Standard.ViewModels.TreeView;
using VPlayer.Core.Providers;
using MediaPlayer = LibVLCSharp.Shared.MediaPlayer;

namespace VPlayer.IPTV.ViewModels
{
  public class TVPlayerViewModel : ViewModel
  {
    private readonly IVlcProvider vlcProvider;
    private readonly ILogger logger;
    private LibVLC libVLC;

    public TVPlayerViewModel(IVlcProvider vlcProvider, ILogger logger)
    {
      this.vlcProvider = vlcProvider ?? throw new ArgumentNullException(nameof(vlcProvider));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));


    }

    #region MediaPlayer

    private MediaPlayer mediaPlayer;
    public MediaPlayer MediaPlayer
    {
      get { return mediaPlayer; }
      set
      {
        if (value != mediaPlayer)
        {
          mediaPlayer = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region ActualChannel

    private SerialDisposable channelLoadedSerialDisposable = new SerialDisposable();
    private TvChannelViewModel actualChannel;

    public TvChannelViewModel ActualChannel
    {
      get { return actualChannel; }
      set
      {
        if (value != actualChannel)
        {
          if (actualChannel != null)
          {
            actualChannel.IsSelected = false;
          }


          actualChannel = value;

          if (actualChannel is ISelectable actualSelectable)
          {
            actualSelectable.IsSelected = true;
          }

          if (actualChannel != null)
          {
            if (actualChannel.URL == null)
              channelLoadedSerialDisposable.Disposable = actualChannel.ObservePropertyChange(x => x.URL).Subscribe(x => OnChannelLoaded());

            actualChannel.State = TVChannelState.Loading;
          }



          PlayActualChannel(actualChannel?.URL);

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Initialize

    public override async void Initialize()
    {
      base.Initialize();

      await HookToVlcEvents();

      mediaPlayer.Volume = 100;
    }

    #endregion

    #region OnChannelLoaded

    private void OnChannelLoaded()
    {
      channelLoadedSerialDisposable.Disposable?.Dispose();
      PlayActualChannel(actualChannel?.URL);
    }

    #endregion

    #region HookToVlcEvents

    private async Task HookToVlcEvents()
    {
      await LoadVlc();

      if (MediaPlayer == null)
      {
        logger.Log(Logger.MessageType.Error, "VLC was not initlized!");
        return;
      }

      mediaPlayer.EncounteredError += (sender, e) =>
      {
        logger.Log(MessageType.Error, "Vlc playing error", true);

        Application.Current.Dispatcher.Invoke(() =>
        {
          ActualChannel.State = TVChannelState.Error;
        });
      };


      mediaPlayer.Buffering += MediaPlayer_Buffering;
    }



    #endregion

    #region MediaPlayer_Buffering

    private void MediaPlayer_Buffering(object sender, MediaPlayerBufferingEventArgs e)
    {
      if ((int)e.Cache == 100)
      {
        ActualChannel.State = TVChannelState.Playing;
      }
      else
        ActualChannel.State = TVChannelState.Loading;

      ActualChannel.BufferingValue = e.Cache;
    }

    #endregion


    #region LoadVlc

    private async Task LoadVlc()
    {
      var result = await vlcProvider.InitlizeVlc();

      MediaPlayer = result.Key;

      libVLC = result.Value;
    }

    #endregion

    #region PlayActualChannel

    private void PlayActualChannel(string connection)
    {
      try
      {
        if (!string.IsNullOrEmpty(connection))
        {
          var streamMedia = new Media(libVLC, new Uri(connection));

          mediaPlayer.Media = streamMedia;

          mediaPlayer.Play();
        }


      }
      catch (Exception ex)
      {
        if (ActualChannel != null)
          ActualChannel.State = TVChannelState.Error;
      }
    }

    #endregion


    public override void Dispose()
    {
      base.Dispose();

      channelLoadedSerialDisposable?.Dispose();
    }
  }
}

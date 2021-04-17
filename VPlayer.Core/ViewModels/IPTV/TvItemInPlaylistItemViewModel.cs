using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Prism.Events;
using VCore.Helpers;
using VCore.Standard.Helpers;
using VPlayer.AudioStorage.DomainClasses.IPTV;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels;
using VPLayer.Domain.Contracts.IPTV;

namespace VPlayer.IPTV.ViewModels
{
  public class TvItemInPlaylistItemViewModel : ItemInPlayList<TvItem>
  {
    private SerialDisposable serialDisposable = new SerialDisposable();
    public TvItemInPlaylistItemViewModel(
      TvItem model,
      IEventAggregator eventAggregator,
      IStorageManager storageManager,
      ITvPlayableItem tvPlayableItem) : base(model, eventAggregator, storageManager)
    {
      TvPlayableItem = tvPlayableItem ?? throw new ArgumentNullException(nameof(tvPlayableItem));

      serialDisposable.Disposable = Observable.FromAsync(x => tvPlayableItem.SelectedTvChannel.InitilizeUrl()).Subscribe(OnUrlInitilize);

      tvPlayableItem.ObservePropertyChange(x => x.SelectedTvChannel).ObserveOnDispatcher().Subscribe(x =>
      {
        if (x != null)
        {
          serialDisposable.Disposable.Dispose();
          State = TVChannelState.GettingData;
          Source = null;
          BufferingValue = 0;
          serialDisposable.Disposable = Observable.FromAsync(x => tvPlayableItem.SelectedTvChannel.InitilizeUrl()).Subscribe(OnUrlInitilize);
        }
      });

    }

    #region State

    private TVChannelState state;

    public TVChannelState State
    {
      get { return state; }
      set
      {
        if (value != state)
        {
          state = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region BufferingValue

    private float bufferingValue;

    public float BufferingValue
    {
      get { return bufferingValue; }
      set
      {
        if (value != bufferingValue)
        {
          bufferingValue = value;

          if ((int)bufferingValue == 100)
          {
            State = TVChannelState.Playing;
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Source

    public string Source
    {
      get { return Model.Source; }
      set
      {
        if (value != Model.Source)
        {
          Model.Source = value;

          if (State == TVChannelState.GettingData)
          {
            State = TVChannelState.Loading;
          }

          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region TvPlayableItem

    private ITvPlayableItem tvPlayableItem;

    public ITvPlayableItem TvPlayableItem
    {
      get { return tvPlayableItem; }
      set
      {
        if (value != tvPlayableItem)
        {
          tvPlayableItem = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region OnUrlInitilize

    private bool boolInitilizedValue = true;
    private void OnUrlInitilize(string url)
    {
      if (Source == null && url == null && !boolInitilizedValue)
      {
        State = TVChannelState.Error;

        if (TvPlayableItem?.SelectedTvChannel != null)
        {
          TvPlayableItem.SelectedTvChannel.RefreshSource();
        }

        serialDisposable.Disposable = Observable.FromAsync(x => tvPlayableItem.SelectedTvChannel.InitilizeUrl()).Subscribe(OnUrlInitilize);

        Debug.WriteLine("Getting url failed, refreshing");
      }

      Source = url;

      boolInitilizedValue = false;
    }

    #endregion


    public void RefreshSource()
    {
      //if (TvPlayableItem?.SelectedTvChannel != null)
      //{
      //  TvPlayableItem.SelectedTvChannel.RefreshSource();
      //}
    }

    #region RefreshConnection

    public void RefreshConnection()
    {
      BufferingValue = 0;
      serialDisposable.Disposable = Observable.FromAsync(x => tvPlayableItem.SelectedTvChannel.InitilizeUrl()).Subscribe(OnUrlInitilize);
    }

    #endregion

    protected override void PublishPlayEvent()
    {
      throw new System.NotImplementedException();
    }

    protected override void PublishRemoveFromPlaylist()
    {
      throw new System.NotImplementedException();
    }

    public override void Dispose()
    {
      base.Dispose();

      serialDisposable?.Dispose();
    }
  }
}
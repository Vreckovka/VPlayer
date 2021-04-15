using System;
using Prism.Events;
using VCore.Helpers;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.Core.ViewModels;

namespace VPlayer.IPTV.ViewModels
{
  public class TvPlaylistItemViewModel : ItemInPlayList<TvPlaylistItem>
  {
    private readonly TvChannelViewModel tvChannelViewModel;

    public TvPlaylistItemViewModel(
      TvPlaylistItem model, 
      IEventAggregator eventAggregator, 
      IStorageManager storageManager,
      TvChannelViewModel tvChannelViewModel) : base(model, eventAggregator, storageManager)
    {
      this.tvChannelViewModel = tvChannelViewModel ?? throw new ArgumentNullException(nameof(tvChannelViewModel));

      tvChannelViewModel.ObservePropertyChange(x => x.URL).Subscribe(x =>
      {
        Source = x;
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
          RaisePropertyChanged();
        }
      }
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
  }
}
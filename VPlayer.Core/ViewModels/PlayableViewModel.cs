using Prism.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using VCore;
using VCore.Standard;
using VCore.WPF.Misc;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.InfoDownloader;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.Artists;
using VPlayer.IPTV.ViewModels;

namespace VPlayer.Core.ViewModels
{
  public abstract class NamedEntityViewModel<TModel> : ViewModel<TModel>, INamedEntityViewModel<TModel> where TModel : INamedEntity
  {
    protected NamedEntityViewModel(TModel model) : base(model)
    {
    }

    #region Properties

    public int ModelId => Model.Id;
    public string Name => Model.Name;

    public abstract void Update(TModel updateItem);

    #endregion 
  }

  public abstract class PlayableViewModelWithThumbnail<TViewModel, TModel> : PlayableViewModel<TViewModel, TModel> where TModel : IDownloadableEntity, INamedEntity
  {
    protected PlayableViewModelWithThumbnail(TModel model, IEventAggregator eventAggregator) : base(model, eventAggregator)
    {
    }

    #region Properties

    #region HeaderText

    public string HeaderText => Model.Name;

    #endregion 

    public abstract string BottomText { get; }
    public abstract string ImageThumbnail { get; }

    #region IsInPlaylist

    private bool isInPlaylist;

    public bool IsInPlaylist
    {
      get { return isInPlaylist; }
      set
      {
        if (value != isInPlaylist)
        {
          isInPlaylist = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public InfoDownloadStatus InfoDownloadStatus => Model.InfoDownloadStatus;

    #region BottomPathData

    public virtual string BottomPathData
    {
      get
      {
        return null;
      }
    }

    #endregion

    #endregion

    #region GetEmptyImage

    public static string GetEmptyImage()
    {
      var directory = Path.Combine(AudioInfoDownloader.GetDefaultPicturesPath(), "Misc");

      if (!Directory.Exists(directory))
      {
        Directory.CreateDirectory(directory);
      }

      var finalPath = Path.Combine(directory, "emptyImage.jpg");

      if (!File.Exists(finalPath))
      {
        var bitmap = new Bitmap(1, 1, PixelFormat.Format24bppRgb);

        bitmap.Save(finalPath, ImageFormat.Jpeg);
      }


      return finalPath;
    }

    #endregion

    #region RaisePropertyChanges

    public virtual void RaisePropertyChanges()
    {
      RaisePropertyChanged(nameof(BottomText));
      RaisePropertyChanged(nameof(ImageThumbnail));
      RaisePropertyChanged(nameof(InfoDownloadStatus));
    }

    #endregion

    #region RaisePropertyChange

    public virtual void RaisePropertyChange(string propertyName)
    {
      RaisePropertyChanged(propertyName);
    }

    #endregion

  }

  public abstract class PlayableViewModel<TViewModelInPlaylist, TModel> : NamedEntityViewModel<TModel>, IBusy
    where TModel : INamedEntity
  {
    #region Fields

    protected readonly IEventAggregator eventAggregator;

    #endregion

    #region Constructors

    protected PlayableViewModel(TModel model, IEventAggregator eventAggregator) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    }

    #endregion

    #region IsPlaying

    private bool isPlaying;

    public bool IsPlaying
    {
      get { return isPlaying; }
      set
      {
        if (value != isPlaying)
        {
          isPlaying = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion IsPlaying

    #region IsBusy

    private bool isBusy;

    public bool IsBusy
    {
      get { return isBusy; }
      set
      {
        if (value != isBusy)
        {
          isBusy = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Commands

    #region Play

    private ActionCommand<EventAction> play;

    public ICommand Play
    {
      get
      {
        if (play == null)
        {
          play = new ActionCommand<EventAction>(OnPlayButton, EventAction.Play);
        }

        return play;
      }
    }

    protected virtual async void OnPlay(EventAction o)
    {
      try
      {
        if (o != EventAction.SetPlaylist)
          IsBusy = true;

        var data = await GetItemsToPlay();

        if (data != null)
          PublishPlayEvent(data, o);
      }
      finally
      {
        IsBusy = false;
      }
    }

    public virtual void OnPlayButton(EventAction o)
    {
      if (!IsPlaying)
      {
        OnPlay(o);
      }
      else
      {
        eventAggregator.GetEvent<PlayPauseEvent>().Publish();
      }
    }

    #endregion Play

    #region Detail

    private ActionCommand detail;

    public ICommand Detail
    {
      get
      {
        if (detail == null)
        {
          detail = new ActionCommand(OnDetail);
        }

        return detail;
      }
    }

    protected abstract void OnDetail();

    #endregion Detail

    #region AddToPlaylist

    private ActionCommand addToPlaylist;

    public ICommand AddToPlaylist
    {
      get
      {
        if (addToPlaylist == null)
        {
          addToPlaylist = new ActionCommand(OnAddToPlaylist);
        }

        return addToPlaylist;
      }
    }

    public async void OnAddToPlaylist()
    {
      try
      {
        IsBusy = true;

        var data = await GetItemsToPlay();

        if (data != null)
          PublishAddToPlaylistEvent(data);
      }
      finally
      {
        IsBusy = false;
      }

    }

    #endregion AddToPlaylist

    #endregion

    #region Methods

    public abstract Task<IEnumerable<TViewModelInPlaylist>> GetItemsToPlay();
    public abstract void PublishPlayEvent(IEnumerable<TViewModelInPlaylist> viewModels, EventAction eventAction);
    public abstract void PublishAddToPlaylistEvent(IEnumerable<TViewModelInPlaylist> viewModels);

    #endregion
  }

  public interface IBusy
  {
    public bool IsBusy { get; set; }
  }
}
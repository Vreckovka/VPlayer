using Prism.Events;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Input;
using VCore;
using VCore.ViewModels;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.Artists;

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

  public abstract class PlayableViewModel<TModel> : NamedEntityViewModel<TModel> where TModel : INamedEntity
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

    #region HeaderText

    public string HeaderText => Model.Name;

    #endregion HeaderText

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

    public abstract string BottomText { get; }
    public abstract byte[] ImageThumbnail { get; }
    public bool IsInPlaylist { get; set; }


    #region Commands

    #region Play

    private ActionCommand play;

    public ICommand Play
    {
      get
      {
        if (play == null)
        {
          play = new ActionCommand(OnPlayButton);
        }

        return play;
      }
    }

    public virtual void OnPlay()
    {
      var data = GetSongsToPlay();

      var e = new PlaySongsEventData()
      {
        PlaySongsAction = PlaySongsAction.Play,
        Songs = data
      };

      eventAggregator.GetEvent<PlaySongsEvent>().Publish(e);
    }

    private void OnPlayButton()
    {
      if (!IsPlaying)
      {
        OnPlay();
      }
      else
      {
        eventAggregator.GetEvent<PauseEvent>().Publish();
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

    public void OnAddToPlaylist()
    {
      var data = GetSongsToPlay();

      var e = new PlaySongsEventData()
      {
        PlaySongsAction = PlaySongsAction.Add,
        Songs = data
      };

      eventAggregator.GetEvent<PlaySongsEvent>().Publish(e);
    }

    #endregion AddToPlaylist

    #endregion

    #region Methods

    public abstract IEnumerable<SongInPlayList> GetSongsToPlay();


    #region GetEmptyImage

    public byte[] GetEmptyImage()
    {
      var bitmap = new Bitmap(1, 1, PixelFormat.Format24bppRgb);

      ImageConverter converter = new ImageConverter();
      return (byte[])converter.ConvertTo(bitmap, typeof(byte[]));
    }

    #endregion

    #region RaisePropertyChanges

    public virtual void RaisePropertyChanges()
    {
      RaisePropertyChanged(nameof(BottomText));
      RaisePropertyChanged(nameof(ImageThumbnail));
    }

    #endregion

    #endregion
  }
}
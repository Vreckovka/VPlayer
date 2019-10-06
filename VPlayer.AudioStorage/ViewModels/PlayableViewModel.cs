using Prism.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Input;
using VCore;
using VCore.ViewModels;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels.Artists;

namespace VPlayer.Core.ViewModels
{
  public abstract class PlayableViewModel<TModel> : ViewModel<TModel>, IPlayableViewModel<TModel> where TModel : INamedEntity
  {
    #region Fields

    protected readonly IEventAggregator eventAggregator;

    #endregion Fields

    #region Constructors

    protected PlayableViewModel(TModel model, IEventAggregator eventAggregator) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    }

    #endregion Constructors

    #region Properties

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
    public int ModelId => Model.Id;
    public string Name => Model.Name;

    public abstract void Update(TModel updateItem);

    #endregion Properties

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

    public void OnPlay()
    {
      eventAggregator.GetEvent<PlaySongsEvent>().Publish(GetSongsToPlay());
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
      eventAggregator.GetEvent<AddSongsEvent>().Publish(GetSongsToPlay());
    }

    #endregion AddToPlaylist

    #endregion Commands

    #region Methods

    public abstract IEnumerable<SongInPlayList> GetSongsToPlay();

    #endregion Methods

    public byte[] ConvertImageToByte(string path)
    {
      return File.ReadAllBytes(path);
    }
  }
}
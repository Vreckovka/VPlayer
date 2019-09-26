using System;
using System.Collections.Generic;
using System.Windows.Input;
using Prism.Events;
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

    #endregion

    #region Constructors

    protected PlayableViewModel(TModel model, IEventAggregator eventAggregator) : base(model)
    {
      this.eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
    }

    #endregion

    #region Properties

    #region HeaderText

    public string HeaderText => Model.Name;

    #endregion

    #region IsPlaying

    private bool isPlaying;

    public bool IsPlaying
    {
      get { return isPlaying;}
      set
      {
        if (value != isPlaying)
        {
          isPlaying = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    public abstract string BottomText { get; }
    public abstract byte[] ImageThumbnail { get; }
    public string Name => Model.Name;
    public int ModelId => Model.Id;
    public abstract void Update(TModel updateItem);
   

    #endregion

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

    public void OnPlay()
    {
      eventAggregator.GetEvent<PlaySongsEvent>().Publish(GetSongsToPlay());
    }
    #endregion

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

    #endregion

    #endregion

    #region Methods

    public abstract IEnumerable<SongInPlayList> GetSongsToPlay();

    #endregion
  }
}

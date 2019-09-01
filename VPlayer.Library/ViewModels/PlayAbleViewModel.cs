using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Media;
using Prism.Events;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.AudioStorage.Models;
using VPlayer.Core;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels;
using VPlayer.Library.ViewModels.ArtistsViewModels;

namespace VPlayer.Library.ViewModels
{
    public abstract class PlayableViewModel<TModel> : ViewModel<TModel>, IPlayableViewModel where TModel : INamedEntity
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

        public bool IsPlaying { get; set; }

        #endregion

        public abstract string BottomText { get; set; }
      
        public abstract ImageSource ImageThumbnail { get; set; }

        public string Name => Model.Name;

        #endregion

        public abstract IEnumerable<Song> GetSongsToPlay();

        #region Commands

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
                IsPlaying = true;
                OnPlay();
            }
            else
            {
                eventAggregator.GetEvent<PauseEvent>().Publish();
                IsPlaying = false;
            }
        }

        public void OnPlay()
        {
            eventAggregator.GetEvent<PlaySongsEvent>().Publish(GetSongsToPlay());
        }

        #endregion

       
    }
}

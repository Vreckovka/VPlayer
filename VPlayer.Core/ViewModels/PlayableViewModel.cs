using System;
using System.Collections.Generic;
using System.Windows.Input;
using Prism.Events;
using VCore;
using VCore.ViewModels;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.DomainClasses;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels;

namespace VPlayer.Library.ViewModels
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

        public bool IsPlaying { get; set; }

        #endregion

        public abstract string BottomText { get; }

        public abstract byte[] ImageThumbnail { get; }

        public string Name => Model.Name;
        public int ModelId => Model.Id;
        public abstract void Update(TModel updateItem);

        #endregion

        public abstract IEnumerable<Song> GetSongsToPlay();

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

    }
}

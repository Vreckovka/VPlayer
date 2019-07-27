using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Prism.Events;
using VPlayer.AudioStorage.Models;
using VPlayer.Core;
using VPlayer.Core.Events;

namespace VPlayer.Library.ViewModels.ArtistsViewModels
{
   
    public class ArtistViewModel : PlayableViewModel<Artist>
    {
        private readonly IEventAggregator _eventAggregator;

        public ArtistViewModel(Artist artist, IEventAggregator eventAggregator) : base(artist)
        {
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        }

        public override void OnPlay()
        {
            base.OnPlay();

            _eventAggregator.GetEvent<PlayArtistEvent>().Publish(Model.Albums?.SelectMany(x => x.Songs));
        }
    }
}
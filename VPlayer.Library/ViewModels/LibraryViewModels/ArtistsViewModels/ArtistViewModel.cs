using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using Prism.Events;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.AudioStorage.Models;
using VPlayer.Core;
using VPlayer.Core.Events;
using VPlayer.Core.ViewModels;
using VPlayer.Library.ViewModels.LibraryViewModels;

namespace VPlayer.Library.ViewModels.ArtistsViewModels
{
    public class ArtistViewModel : PlayableViewModel<Artist>
    {
        private readonly IStorage storage;

        public ArtistViewModel(Artist artist, IEventAggregator eventAggregator, IStorage storage) : base(artist, eventAggregator)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public override string BottomText => Model.Albums.Count.ToString();
        public override byte[] ImageThumbnail => Model.ArtistCover;

        public override IEnumerable<Song> GetSongsToPlay()
        {
            return storage.Artists.Where(x => x.ArtistId == Model.ArtistId).SelectMany(x => x.Albums).SelectMany(x => x.Songs);
        }
    }

    public interface IPlayableViewModel
    {
        string Name { get; }
    }
}
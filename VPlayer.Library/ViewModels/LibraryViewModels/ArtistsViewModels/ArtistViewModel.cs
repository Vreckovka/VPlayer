using System;
using System.Collections.Generic;
using System.Linq;
using Prism.Events;
using VPlayer.AudioStorage.Interfaces;
using VPlayer.Core.DomainClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data.Entity;
using System.Linq;
using System.Reactive.Linq;

namespace VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels
{
    public class ArtistViewModel : PlayableViewModel<Artist>
    {
        private readonly IStorageManager storage;

        public ArtistViewModel(Artist artist, IEventAggregator eventAggregator, IStorageManager storage) : base(artist, eventAggregator)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
        }

        public override string BottomText => Model.Albums?.Count.ToString();
        public override byte[] ImageThumbnail => Model.ArtistCover;

        public override void Update(Artist updateItem)
        {
            Model.Albums = Model.Albums;
        }

        public override IEnumerable<Song> GetSongsToPlay()
        {
            var songs = storage.GetRepository<Artist>().Include(x => x.Albums.Select(y => y.Songs)).SelectMany(z => z.Albums.SelectMany(x => x.Songs)).ToList();

            return songs;
        }
    }

    public interface IPlayableViewModel<TModel> where TModel : INamedEntity
    {
        string Name { get; }
        int ModelId { get; }
        void Update(TModel updateItem);
    }
}
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
using VCore.Annotations;
using VCore.Factories;

namespace VPlayer.Library.ViewModels.LibraryViewModels.ArtistsViewModels
{
    public class ArtistViewModel : PlayableViewModel<Artist>
    {
        private readonly IStorageManager storage;
        private readonly IViewModelsFactory viewModelsFactory;

        public ArtistViewModel(Artist artist, IEventAggregator eventAggregator, IStorageManager storage,
            [NotNull] IViewModelsFactory viewModelsFactory) : base(artist, eventAggregator)
        {
            this.storage = storage ?? throw new ArgumentNullException(nameof(storage));
            this.viewModelsFactory = viewModelsFactory ?? throw new ArgumentNullException(nameof(viewModelsFactory));
        }

        public override string BottomText => Model.Albums?.Count.ToString();
        public override byte[] ImageThumbnail => Model.ArtistCover;

        public override void Update(Artist updateItem)
        {
            Model.Albums = Model.Albums;
        }

        public override IEnumerable<Song> GetSongsToPlay()
        {
            var songs = storage.GetRepository<Artist>()
                .Include(x => x.Albums.Select(y => y.Songs)).ToList();

            return songs.SelectMany(x => x.Albums.SelectMany(y => y.Songs));
        }

        protected override void OnDetail()
        {
            var asd = viewModelsFactory.Create<ArtistDetailViewModel>(this);
            asd.IsActive = true;
        }
    }

    public interface IPlayableViewModel<TModel> where TModel : INamedEntity
    {
        string Name { get; }
        int ModelId { get; }
        void Update(TModel updateItem);
    }
}
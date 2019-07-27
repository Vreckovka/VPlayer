using System;
using System.Collections.Generic;
using Prism.Events;
using VPlayer.AudioStorage.Models;
using VPlayer.Core.ViewModels;
using VPlayer.Library.ViewModels;
using VPlayer.Library.ViewModels.ArtistsViewModels;
using VPlayer.Library.ViewModels.DesignViewModels;

namespace VPlayer.Library.Views
{
    public class ArtistsDesignViewModel : ArtistsViewModel
    {
        private static EventAggregator eventAggregator = new EventAggregator();
        public ArtistsDesignViewModel()
        {
            Artists = new List<ArtistViewModel>()
            {
                new ArtistViewModel(new Artist() { Name = "Metallica",
                                                   ArtistCover = DesignCoverImageSource.StAnger,
                                                   Albums = new List<Album>()
                                                   {
                                                        new Album(),
                                                        new Album(),
                                                        new Album(),
                                                    },
                                               },
                    eventAggregator )
                    {
                        IsPlaying = false
                    },
                new ArtistViewModel(new Artist() { Name = "AC/DC",
                        ArtistCover = DesignCoverImageSource.StAnger,
                        Albums = new List<Album>()
                        {
                            new Album(),
                            new Album(),
                            new Album(),
                            new Album(),
                            new Album(),
                            new Album(),
                        },
                    },
                    eventAggregator )
                {
                    IsPlaying = true
                },
                new ArtistViewModel(new Artist() { Name = "Unknown",
                        Albums = new List<Album>()
                        {
                            new Album(),
                        },
                    },
                    eventAggregator )
                {
                    IsPlaying = false
                },

            };
        }
    }
}

using System;
using System.Collections.ObjectModel;
using VPlayer.WebPlayer.Models;

namespace VPlayer.WebPlayer.ViewModels
{
    class WebPlayersViewModel
    {
        public ObservableCollection<InternetPlayer> InternetPlayers { get; } = new ObservableCollection<InternetPlayer>();
        public WebPlayersViewModel()
        {
            InternetPlayers.Add(new InternetPlayer()
            {
                Title = "Rock radio",
                Uri = new Uri("https://www.rockradio.com/melodicdeathmetal")
            });

            InternetPlayers.Add(new InternetPlayer()
            {
                Title = "Youtube",
                Uri = new Uri("https://www.youtube.com/watch?v=X78Q3AEvvyg")
            });

            InternetPlayers.Add(new InternetPlayer()
            {
                Title = "Spotify",
                Uri = new Uri("https://open.spotify.com/browse/podcasts")
            });
            
            InternetPlayers.Add(new InternetPlayer()
            {
                Title = "Jango",
                Uri = new Uri("https://jango.com")
            });
        }
    }
}

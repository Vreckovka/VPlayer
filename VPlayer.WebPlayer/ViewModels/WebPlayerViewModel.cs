//using System;
//using System.Collections.ObjectModel;
//using VCore.Modularity.RegionProviders;
//using VPlayer.Core.Modularity.Regions;
//using VPlayer.Core.ViewModels;
//using VPlayer.WebPlayer.Models;
//using VPlayer.WebPlayer.Views;

//namespace VPlayer.WebPlayer.ViewModels
//{
//  public class WebPlayerViewModel : PlayableRegionViewModel<WebPlayerPage>
//  {
//    public ObservableCollection<InternetPlayer> InternetPlayers { get; } = new ObservableCollection<InternetPlayer>();

//    public override void Play()
//    {
//      throw new NotImplementedException();
//    }

//    public override void PlayPause()
//    {
//      throw new NotImplementedException();
//    }

//    public override void PlayNext(int? songIndex, bool forcePlay)
//    {
//      throw new NotImplementedException();
//    }

//    public override void PlayPrevious()
//    {
//      throw new NotImplementedException();
//    }

//    public override void Stop()
//    {
//      throw new NotImplementedException();
//    }

//    public override bool IsPlaying { get; protected set; }
//    public override bool CanPlay { get; }

//    public override bool ContainsNestedRegions => false;
//    public override string RegionName { get; protected set; } = RegionNames.ContentRegion;

//    public WebPlayerViewModel(IRegionProvider regionProvider) : base(regionProvider)
//    {
//      InternetPlayers.Add(new InternetPlayer()
//      {
//        Title = "Rock radio",
//        Uri = new Uri("https://www.rockradio.com/melodicdeathmetal")
//      });

//      InternetPlayers.Add(new InternetPlayer()
//      {
//        Title = "Youtube",
//        Uri = new Uri("https://www.youtube.com/watch?v=X78Q3AEvvyg")
//      });

//      InternetPlayers.Add(new InternetPlayer()
//      {
//        Title = "Spotify",
//        Uri = new Uri("https://open.spotify.com/browse/podcasts")
//      });

//      InternetPlayers.Add(new InternetPlayer()
//      {
//        Title = "Jango",
//        Uri = new Uri("https://jango.com")
//      });
//    }
//  }
//}

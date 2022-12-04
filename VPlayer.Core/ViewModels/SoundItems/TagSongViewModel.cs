using System;
using System.Linq;
using TagLib;
using VCore.Standard;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Core.ViewModels.SoundItems
{
  public class TagSongViewModel : ViewModel<Song>
  {
    private File tagFile;

    public TagSongViewModel(Song model) : base(model)
    {
      tagFile = File.Create(model.Source);
    }

    #region Artists

    private string artists;
    public string Artists
    {
      get { return tagFile.Tag.AlbumArtists?.Aggregate((x,y) => x + "," + y); }
      set
      {
        if (value != artists)
        {
          artists = value;
          tagFile.Tag.AlbumArtists = value.Split(",");
          
          RaisePropertyChanged();
        }
      }
    }

    #endregion

    #region Album

    public string Album
    {
      get { return tagFile.Tag.Album; }
      set
      {
        if (value != tagFile.Tag.Album)
        {
          tagFile.Tag.Album = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion
  }
}
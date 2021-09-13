using System;
using VCore.Standard;

namespace VPlayer.AudioStorage.InfoDownloader.Models
{
  public class AlbumCoverViewModel : ViewModel<AlbumCover>
  {
    public AlbumCoverViewModel(AlbumCover model) : base(model)
    {
    }

    #region IsSelected

    private bool isSelected;

    public bool IsSelected
    {
      get { return isSelected; }
      set
      {
        if (value != isSelected)
        {
          isSelected = value;
          RaisePropertyChanged();
        }
      }
    }

    #endregion



  }

  public class AlbumCover 
  {
    #region Properties

    public string DownloadedCoverPath { get; set; }
    public string Mbid { get; set; }
    public long Size { get; set; }

    public string SizeString
    {
      get { return BytesToString(Size); }
    }

    public string Type { get; set; }
    public string Url { get; set; }


    #endregion Properties

    #region Methods

    #region BytesToString

    public string BytesToString(long byteCount)
    {
      string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
      if (byteCount == 0)
        return "0" + suf[0];
      long bytes = Math.Abs(byteCount);
      int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
      double num = Math.Round(bytes / Math.Pow(1024, place), 1);
      return (Math.Sign(byteCount) * num) + " " + suf[place];
    }

    #endregion

    #region ToString

    public override string ToString()
    {
      return $"{Mbid} {Type} {Url}";
    }

    #endregion

    #endregion Methods
  }
}
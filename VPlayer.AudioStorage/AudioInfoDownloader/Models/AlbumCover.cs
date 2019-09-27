using System;

namespace VPlayer.AudioStorage.AudioInfoDownloader.Models
{
  public class AlbumCover
  {
    public string Type { get; set; }
    public string Url { get; set; }
    public string Mbid { get; set; }
    public long Size { get; set; }
    public byte[] DownloadedCover { get; set; }

    public string SizeString
    {
      get { return BytesToString(Size); }
    }

    public override string ToString()
    {
      return $"{Mbid} {Type} {Url}";
    }


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
  }
}

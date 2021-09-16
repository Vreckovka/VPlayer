using System;
using System.Collections.Generic;
using System.Text;

namespace VPlayer.PCloud.JsonResponses
{
  public class GetFilePublicLinkResponse
  {
    public class Metadata
    {
      public string name { get; set; }
      public string created { get; set; }
      public string album { get; set; }
      public bool thumb { get; set; }
      public string modified { get; set; }
      public string artist { get; set; }
      public string genre { get; set; }
      public bool isfolder { get; set; }
      public long fileid { get; set; }
      public int trackno { get; set; }
      public bool isshared { get; set; }
      public ulong hash { get; set; }
      public int comments { get; set; }
      public int category { get; set; }
      public string id { get; set; }
      public string title { get; set; }
      public bool ismine { get; set; }
      public int size { get; set; }
      public int parentfolderid { get; set; }
      public string contenttype { get; set; }
      public string icon { get; set; }
    }

    public string code { get; set; }
    public string created { get; set; }
    public bool downloadenabled { get; set; }
    public int type { get; set; }
    public string modified { get; set; }
    public int downloads { get; set; }
    public string link { get; set; }
    public int result { get; set; }
    public int linkid { get; set; }
    public bool haspassword { get; set; }
    public int traffic { get; set; }
    public int views { get; set; }
    public Metadata metadata { get; set; }
  }
}

using System.Collections.Generic;
using VPlayer.AudioStorage.InfoDownloader.LRC.Domain;

namespace VPlayer.AudioStorage.InfoDownloader.Clients.PCloud
{
  public class PCloudLRCFile : LRCFile
  {
    public long Id { get; set; }
    public PCloudLRCFile(List<LRCLyricLine> lines) : base(lines)
    {
    }
  }
}
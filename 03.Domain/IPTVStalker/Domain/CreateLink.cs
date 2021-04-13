using System;
using System.Collections.Generic;
using System.Text;

namespace IPTVStalker.Domain
{
  public class Result
  {
    public string id { get; set; }
    public string cmd { get; set; }
  }

  public class CreateLinkResponse
  {
    public Result js { get; set; }
    public int streamer_id { get; set; }
    public int link_id { get; set; }
    public int load { get; set; }
    public string error { get; set; }
  }

}

using System;
using System.Collections.Generic;
using System.Text;

namespace IPTVStalker.Domain
{

  public class EpgInfo
  {
    public string id { get; set; }

    public List<Epg> data { get; set; }
  }

  public class Epg
  {
    public string id { get; set; }
    public string ch_id { get; set; }
    public string time { get; set; }
    public string time_to { get; set; }
    public int duration { get; set; }
    public string name { get; set; }
    public string descr { get; set; }
    public string real_id { get; set; }
    public string category { get; set; }
    public string director { get; set; }
    public string actor { get; set; }
    public long start_timestamp { get; set; }
    public long stop_timestamp { get; set; }
    public string t_time { get; set; }
    public string t_time_to { get; set; }
    public int display_duration { get; set; }
    public int larr { get; set; }
    public int rarr { get; set; }
    public int mark_rec { get; set; }
    public int mark_memo { get; set; }
    public int mark_archive { get; set; }
    public string on_date { get; set; }
  }
  
  public class EpgResponse
  {
    public List<EpgInfo> js { get; set; }
  }
}

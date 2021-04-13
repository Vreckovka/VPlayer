using System;
using System.Collections.Generic;
using System.Text;

namespace IPTVStalker.Domain
{
  public class Cmd
  {
    public string id { get; set; }
    public string ch_id { get; set; }
    public string priority { get; set; }
    public string url { get; set; }
    public string status { get; set; }
    public string use_http_tmp_link { get; set; }
    public string wowza_tmp_link { get; set; }
    public string user_agent_filter { get; set; }
    public string use_load_balancing { get; set; }
    public string changed { get; set; }
    public string enable_monitoring { get; set; }
    public string enable_balancer_monitoring { get; set; }
    public string nginx_secure_link { get; set; }
    public string flussonic_tmp_link { get; set; }
  }

  public class Channel
  {
    public string id { get; set; }
    public string name { get; set; }
    public string number { get; set; }
    public string censored { get; set; }
    public string cmd { get; set; }
    public string cost { get; set; }
    public string count { get; set; }
    public int status { get; set; }
    public string hd { get; set; }
    public string tv_genre_id { get; set; }
    public string base_ch { get; set; }
    public string xmltv_id { get; set; }
    public string service_id { get; set; }
    public string bonus_ch { get; set; }
    public string volume_correction { get; set; }
    public string mc_cmd { get; set; }
    public int enable_tv_archive { get; set; }
    public string wowza_tmp_link { get; set; }
    public string wowza_dvr { get; set; }
    public string use_http_tmp_link { get; set; }
    public string monitoring_status { get; set; }
    public string enable_monitoring { get; set; }
    public string enable_wowza_load_balancing { get; set; }
    public string cmd_1 { get; set; }
    public string cmd_2 { get; set; }
    public string cmd_3 { get; set; }
    public string logo { get; set; }
    public string correct_time { get; set; }
    public string nimble_dvr { get; set; }
    public int allow_pvr { get; set; }
    public int allow_local_pvr { get; set; }
    public int allow_remote_pvr { get; set; }
    public string modified { get; set; }
    public string allow_local_timeshift { get; set; }
    public string nginx_secure_link { get; set; }
    public int tv_archive_duration { get; set; }
    public int locked { get; set; }
    public int @lock { get; set; }
    public int fav { get; set; }
    public int archive { get; set; }
    public string genres_str { get; set; }
    public string cur_playing { get; set; }
    public List<object> epg { get; set; }
    public int open { get; set; }
    public List<Cmd> cmds { get; set; }
    public int use_load_balancing { get; set; }
    public int pvr { get; set; }
  }

  public class Channels
  {
    public int total_items { get; set; }
    public int max_page_items { get; set; }
    public int selected_item { get; set; }
    public int cur_page { get; set; }
    public List<Channel> data { get; set; }
  }

  public class ChannelsResponse
  {
    public Channels js { get; set; }
  }

}

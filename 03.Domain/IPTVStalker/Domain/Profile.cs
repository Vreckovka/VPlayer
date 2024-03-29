﻿using System;
using System.Collections.Generic;
using System.Text;

namespace IPTVStalker.Domain
{
  public class Updated
  {
    public string id { get; set; }
    public string uid { get; set; }
    public string anec { get; set; }
    public string vclub { get; set; }
  }

  public class Profile
  {
    public string id { get; set; }
    public string name { get; set; }
    public string sname { get; set; }
    public string pass { get; set; }
    public string parent_password { get; set; }
    public string bright { get; set; }
    public string contrast { get; set; }
    public string saturation { get; set; }
    public string video_out { get; set; }
    public string volume { get; set; }
    public string playback_buffer_bytes { get; set; }
    public string playback_buffer_size { get; set; }
    public string audio_out { get; set; }
    public string mac { get; set; }
    public string ip { get; set; }
    public object ls { get; set; }
    public string version { get; set; }
    public object lang { get; set; }
    public string locale { get; set; }
    public string city_id { get; set; }
    public string hd { get; set; }
    public string main_notify { get; set; }
    public string fav_itv_on { get; set; }
    public object now_playing_start { get; set; }
    public string now_playing_type { get; set; }
    public object now_playing_content { get; set; }
    public string additional_services_on { get; set; }
    public object time_last_play_tv { get; set; }
    public object time_last_play_video { get; set; }
    public string operator_id { get; set; }
    public string storage_name { get; set; }
    public string hd_content { get; set; }
    public string image_version { get; set; }
    public object last_change_status { get; set; }
    public object last_start { get; set; }
    public object last_active { get; set; }
    public object keep_alive { get; set; }
    public string screensaver_delay { get; set; }
    public string phone { get; set; }
    public string fname { get; set; }
    public string login { get; set; }
    public string password { get; set; }
    public string stb_type { get; set; }
    public string num_banks { get; set; }
    public string tariff_plan_id { get; set; }
    public object comment { get; set; }
    public object now_playing_link_id { get; set; }
    public object now_playing_streamer_id { get; set; }
    public string just_started { get; set; }
    public string last_watchdog { get; set; }
    public string created { get; set; }
    public string plasma_saving { get; set; }
    public string ts_enabled { get; set; }
    public string ts_enable_icon { get; set; }
    public object ts_path { get; set; }
    public string ts_max_length { get; set; }
    public string ts_buffer_use { get; set; }
    public string ts_action_on_exit { get; set; }
    public string ts_delay { get; set; }
    public string video_clock { get; set; }
    public string verified { get; set; }
    public string hdmi_event_reaction { get; set; }
    public string pri_audio_lang { get; set; }
    public string sec_audio_lang { get; set; }
    public string pri_subtitle_lang { get; set; }
    public string sec_subtitle_lang { get; set; }
    public string subtitle_color { get; set; }
    public string subtitle_size { get; set; }
    public string show_after_loading { get; set; }
    public string play_in_preview_by_ok { get; set; }
    public string hw_version { get; set; }
    public string openweathermap_city_id { get; set; }
    public string theme { get; set; }
    public string settings_password { get; set; }
    public string expire_billing_date { get; set; }
    public object reseller_id { get; set; }
    public string account_balance { get; set; }
    public string client_type { get; set; }
    public string hw_version_2 { get; set; }
    public string blocked { get; set; }
    public string units { get; set; }
    public object tariff_expired_date { get; set; }
    public object tariff_id_instead_expired { get; set; }
    public string activation_code_auto_issue { get; set; }
    public string last_itv_id { get; set; }
    public Updated updated { get; set; }
    public string rtsp_type { get; set; }
    public string rtsp_flags { get; set; }
    public string stb_lang { get; set; }
    public string display_menu_after_loading { get; set; }
    public string record_max_length { get; set; }
    public string web_proxy_host { get; set; }
    public string web_proxy_port { get; set; }
    public string web_proxy_user { get; set; }
    public string web_proxy_pass { get; set; }
    public string web_proxy_exclude_list { get; set; }
    public string demo_video_url { get; set; }
    public string tv_quality_filter { get; set; }
    public bool is_moderator { get; set; }
    public double timeslot_ratio { get; set; }
    public int timeslot { get; set; }
    public string kinopoisk_rating { get; set; }
    public string enable_tariff_plans { get; set; }
    public string strict_stb_type_check { get; set; }
    public int cas_type { get; set; }
    public object cas_params { get; set; }
    public object cas_web_params { get; set; }
    public List<object> cas_additional_params { get; set; }
    public int cas_hw_descrambling { get; set; }
    public string cas_ini_file { get; set; }
    public string logarithm_volume_control { get; set; }
    public string allow_subscription_from_stb { get; set; }
    public string deny_720p_gmode_on_mag200 { get; set; }
    public string enable_arrow_keys_setpos { get; set; }
    public string show_purchased_filter { get; set; }
    public int timezone_diff { get; set; }
    public bool enable_connection_problem_indication { get; set; }
    public string invert_channel_switch_direction { get; set; }
    public string play_in_preview_only_by_ok { get; set; }
    public string enable_stream_error_logging { get; set; }
    public string always_enabled_subtitles { get; set; }
    public string enable_service_button { get; set; }
    public string enable_setting_access_by_pass { get; set; }
    public string tv_archive_continued { get; set; }
    public string plasma_saving_timeout { get; set; }
    public string show_tv_only_hd_filter_option { get; set; }
    public string tv_playback_retry_limit { get; set; }
    public string fading_tv_retry_timeout { get; set; }
    public double epg_update_time_range { get; set; }
    public bool store_auth_data_on_stb { get; set; }
    public string account_page_by_password { get; set; }
    public bool tester { get; set; }
    public string enable_stream_losses_logging { get; set; }
    public string external_payment_page_url { get; set; }
    public string max_local_recordings { get; set; }
    public string tv_channel_default_aspect { get; set; }
    public string default_led_level { get; set; }
    public string standby_led_level { get; set; }
    public string show_version_in_main_menu { get; set; }
    public string disable_youtube_for_mag200 { get; set; }
    public bool auth_access { get; set; }
    public string epg_data_block_period_for_stb { get; set; }
    public string standby_on_hdmi_off { get; set; }
    public string force_ch_link_check { get; set; }
    public string stb_ntp_server { get; set; }
    public string overwrite_stb_ntp_server { get; set; }
    public object hide_tv_genres_in_fullscreen { get; set; }
    public object advert { get; set; }
    public string aspect { get; set; }
    //public bool playback_limit { get; set; }
    public object country { get; set; }
    public int watchdog_timeout { get; set; }
    public string play_token { get; set; }
    public int status { get; set; }
    public string update_url { get; set; }
    public string test_download_url { get; set; }
    public string default_timezone { get; set; }
    public string default_locale { get; set; }
    public List<string> allowed_stb_types { get; set; }
    public List<string> allowed_stb_types_for_local_recording { get; set; }
    public List<object> storages { get; set; }
    public bool show_tv_channel_logo { get; set; }
    public bool show_channel_logo_in_preview { get; set; }
    public string hls_fast_start { get; set; }
    public int check_ssl_certificate { get; set; }
    public int enable_buffering_indication { get; set; }
  }

  public class ProfileResult
  {
    public Profile js { get; set; }
  }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using IPTVStalker.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace IPTVStalker
{
  public static class ServiceMethods
  {
    public static class ServiceType
    {
      public const string STB = "stb";
      public const string ITV = "itv";
    }

    public const string Handshake = "handshake&prehash=";
    public const string Profile = "get_profile";
    public const string Generes = "get_genres";
    public const string AllChannels = "get_all_channels&force_ch_link_check";
    public const string Epg = "get_epg_info&period=";
  }

  public class IPTVStalkerService
  {
    private readonly string folder;
    private ConnectionProperties connectionProperties;
    private string bearerToken;
    private bool wasPrepared;

    #region Constructors

    public IPTVStalkerService(ConnectionProperties connectionProperties)
    {
      this.connectionProperties = connectionProperties ?? throw new ArgumentNullException(nameof(connectionProperties));

      if (connectionProperties.FolderToSave != null)
        System.IO.Directory.CreateDirectory(connectionProperties.FolderToSave);
    }


    public IPTVStalkerService(string folder)
    {
      this.folder = folder ?? throw new ArgumentNullException(nameof(folder));
    }

    #endregion

    #region Properties

    public Profile Profile { get; set; }
    public List<Genere> Generes { get; set; }
    public Channels Channels { get; set; }
    public List<EpgInfo> Epgs { get; set; }

    #endregion

    #region Methods

    private void Prepare()
    {
      bearerToken = GetToken();
      GetProfile();
      wasPrepared = true;
    }

    #region FetchData

    public void FetchData()
    {
      bool load = true;

      if (connectionProperties == null)
      {
        load = false;
        connectionProperties = JsonSerializer.Deserialize<ConnectionProperties>(File.ReadAllText(folder + "\\connection.json"));
      }

      bearerToken = GetToken();

      if (bearerToken == null)
      {
        return;
      }

      if (load)
      {
        Profile = GetProfile()?.js;
        Generes = GetGeneres()?.js;
        Channels = GetChannels()?.js;
      }
      else
      {
        Profile = GetProfile()?.js;
        Generes = GetGeneres(folder)?.js;
        Channels = GetChannels(folder)?.js;
      }

    }

    #endregion

    #region GetLink

    public CreateLinkResponse GetLink(string cmd)
    {
      if (!wasPrepared)
      {
        Prepare();
      }

      cmd = cmd.Replace(" ", "+");
      cmd = cmd.Replace(":", "%3a");
      cmd = cmd.Replace("/", "%2f");

      var result = GetRequest(ServiceMethods.ServiceType.ITV, $"create_link&cmd={cmd}", true);

      if (string.IsNullOrEmpty(result))
      {
        return null;
      }

      return JsonSerializer.Deserialize<CreateLinkResponse>(result);

    }

    #endregion

    #region GetRequest

    private string GetRequest(string type, string action, bool addReferer = false)
    {
      HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create($"{connectionProperties.Server}/server/load.php?type={type}&action={action}&JsHttpRequest=1-xml");


      request.Headers.Add(HttpRequestHeader.Cookie, "mac=" + connectionProperties.MAC + "; stb_lang=sk; timezone=" + connectionProperties.TimeZone);
      request.Headers.Add("X-User-Agent", "Model: MAG254; Link: Ethernet");

      if (bearerToken != null)
        request.Headers.Add("Authorization", $"Bearer {bearerToken}");


      if (addReferer)
      {
        request.Headers.Add("Referer", "http://fe7.flycany.me:8880/c/");
      }

      request.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:57.0) Gecko/20100101 Firefox/57.0");

      request.Method = "GET";

      using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
      {
        Stream dataStream = response.GetResponseStream();
        StreamReader reader = new StreamReader(dataStream);
        var result = reader.ReadToEnd();
        reader.Close();
        dataStream.Close();

        return result;
      }
    }

    #endregion

    #region GetToken

    private string GetToken()
    {
      var result = GetRequest(ServiceMethods.ServiceType.STB, ServiceMethods.Handshake);

      var split = result.Split("{\"js\":{\"token\":\"");
      var token = split[1].Substring(0, "4756976DA1D3BB85A9A5984A12F5F8A7".Length);

      var propertiesJson = JsonSerializer.Serialize(connectionProperties);

      if (connectionProperties.FolderToSave != null)
        File.WriteAllText(connectionProperties.FolderToSave + "\\connection.json", propertiesJson);

      return token;
    }

    #endregion

    #region GetProfile

    private ProfileResult GetProfile(string path = null)
    {
      string result = path;

      if (result == null)
      {
        result = GetRequest(ServiceMethods.ServiceType.STB, ServiceMethods.Profile);

        if (connectionProperties.FolderToSave != null)
          File.WriteAllText(connectionProperties.FolderToSave + "\\profile.json", result);
      }
      else
      {
        result = File.ReadAllText(path + "\\profile.json");
      }


      if (string.IsNullOrEmpty(result))
      {
        return null;
      }

      return JsonSerializer.Deserialize<ProfileResult>(result);
    }

    #endregion

    #region GetGeneres

    private GeneresResponse GetGeneres(string path = null)
    {
      string result = path;

      if (result == null)
      {
        result = GetRequest(ServiceMethods.ServiceType.ITV, ServiceMethods.Generes);

        if (connectionProperties.FolderToSave != null)
          File.WriteAllText(connectionProperties.FolderToSave + "\\generes.json", result);
      }
      else
      {
        result = File.ReadAllText(path + "\\generes.json");
      }

      if (string.IsNullOrEmpty(result))
      {
        return null;
      }

      return JsonSerializer.Deserialize<GeneresResponse>(result);
    }

    #endregion

    #region GetChannels

    private ChannelsResponse GetChannels(string path = null)
    {
      string result = path;

      if (result == null)
      {
        result = GetRequest(ServiceMethods.ServiceType.ITV, ServiceMethods.AllChannels);

        if (connectionProperties.FolderToSave != null)
          File.WriteAllText(connectionProperties.FolderToSave + "\\channels.json", result);
      }
      else
      {
        result = File.ReadAllText(path + "\\channels.json");
      }

      if (string.IsNullOrEmpty(result))
      {
        return null;
      }



      return JsonSerializer.Deserialize<ChannelsResponse>(result);
    }

    #endregion

    #region GetEpg

    private EpgResponse GetEpg(int period)
    {
      var result = GetRequest(ServiceMethods.ServiceType.ITV, ServiceMethods.Epg + period);

      if (string.IsNullOrEmpty(result))
      {
        return null;
      }

      var asd = JsonConvert.DeserializeObject<JObject>(result);

      var response = new EpgResponse();

      var epgDatas = new List<EpgInfo>();
      response.js = epgDatas;

      var root = asd.Root;
      var data = asd.Root.First.First.First.First;

      foreach (var node in data.Children())
      {
        var name = node.Path.Substring("js.data.".Length);

        var inf = new EpgInfo()
        {
          id = name,
          data = new List<Epg>()
        };

        foreach (var nodeChild in node.First.Children())
        {
          var value = nodeChild.ToObject<Epg>();
          inf.data.Add(value);
        }

        epgDatas.Add(inf);
      }

      return response;
    }

    #endregion


    #endregion

  }
}

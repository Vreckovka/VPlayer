using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IPTVStalker.Domain;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using VCore.Standard.Helpers;
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
    public string bearerToken;
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

    private async void Prepare()
    {
      bearerToken = await GetToken();
      await GetProfile();
      wasPrepared = true;
    }

    #region FetchData

    public async void FetchData()
    {
      bool load = true;

      if (connectionProperties == null)
      {
        load = false;
        connectionProperties = JsonSerializer.Deserialize<ConnectionProperties>(File.ReadAllText(folder + "\\connection.json"));
      }

      bearerToken = await GetToken();

      if (bearerToken == null)
      {
        return;
      }

      if (load)
      {
        Profile = (await GetProfile())?.js;
        Generes = (await GetGeneres())?.js;
        Channels = (await GetChannels())?.js;
      }
      else
      {
        Profile = (await GetProfile())?.js;
        Generes = (await GetGeneres(folder))?.js;
        Channels = (await GetChannels(folder))?.js;
      }

    }

    #endregion

    #region GetLink

    public async Task<string> GetLink(string cmd, CancellationToken? cancellationToken = null)
    {
      if (!wasPrepared)
      {
        Prepare();
      }

      cmd = cmd.Replace(" ", "+");
      cmd = cmd.Replace(":", "%3a");
      cmd = cmd.Replace("/", "%2f");

      var result = await GetRequest(ServiceMethods.ServiceType.ITV, $"create_link&cmd={cmd}", true, cancellationToken);

      Debug.WriteLine("Stalker created link: " + result);

      if (string.IsNullOrEmpty(result))
      {
        return null;
      }

      var desResult = JsonSerializer.Deserialize<CreateLinkResponse>(result);

      var nextConnection = desResult.js.cmd.Substring("ffmpeg ".Length);

      return nextConnection;

    }

    #endregion

    #region GetRequest

    private async Task<string> GetRequest(string type, string action, bool keepAlive = false, CancellationToken? cancellationToken = null)
    {
      try
      {
        HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create($"{connectionProperties.Server}/server/load.php?type={type}&action={action}&JsHttpRequest=1-xml");


        request.Headers.Add(HttpRequestHeader.Cookie, "mac=" + connectionProperties.MAC + "; stb_lang=sk; timezone=" + connectionProperties.TimeZone);
        request.Headers.Add("X-User-Agent", "Model: MAG254; Link: Ethernet");

        if (bearerToken != null)
          request.Headers.Add("Authorization", $"Bearer {bearerToken}");

        request.Headers.Add(HttpRequestHeader.KeepAlive, keepAlive.ToString());
        request.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:57.0) Gecko/20100101 Firefox/57.0");
        request.Headers.Add(HttpRequestHeader.Connection, "Keep-Alive");

        request.Method = "GET";
        request.Timeout = (int)TimeSpan.FromSeconds(5).TotalMilliseconds;

        HttpWebResponse response;
        if (cancellationToken != null)
          response = await request.GetResponseAsyncWithCancellationToken(cancellationToken.Value);
        else
          response = (HttpWebResponse)(await request.GetResponseAsync());


        using (response)
        {

          Stream dataStream = response.GetResponseStream();
          StreamReader reader = new StreamReader(dataStream);
          var result = reader.ReadToEnd();
          reader.Close();
          dataStream.Close();

          return result;
        }
      }
      catch (Exception ex)
      {
        return null;
      }
    }

    #endregion

    #region GetToken

    public async Task<string> GetToken()
    {
      var result = await GetRequest(ServiceMethods.ServiceType.STB, ServiceMethods.Handshake);

      if (result != null)
      {
        var split = result.Split("{\"js\":{\"token\":\"");
        var token = split[1].Substring(0, "4756976DA1D3BB85A9A5984A12F5F8A7".Length);

        var propertiesJson = JsonSerializer.Serialize(connectionProperties);

        if (connectionProperties.FolderToSave != null)
          File.WriteAllText(connectionProperties.FolderToSave + "\\connection.json", propertiesJson);

        return token;
      }

      return null;
    }

    #endregion

    #region GetProfile

    public async Task<ProfileResult> GetProfile(string path = null)
    {
      string result = path;

      if (result == null)
      {
        result = await GetRequest(ServiceMethods.ServiceType.STB, ServiceMethods.Profile);

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

    private async Task<GeneresResponse> GetGeneres(string path = null)
    {
      string result = path;

      if (result == null)
      {
        result = await GetRequest(ServiceMethods.ServiceType.ITV, ServiceMethods.Generes);

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

    private async Task<ChannelsResponse> GetChannels(string path = null)
    {
      string result = path;

      if (result == null)
      {
        result = await GetRequest(ServiceMethods.ServiceType.ITV, ServiceMethods.AllChannels);

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

    private async  Task<EpgResponse> GetEpg(int period)
    {
      var result = await GetRequest(ServiceMethods.ServiceType.ITV, ServiceMethods.Epg + period);

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

    #region RefreshService

    public void RefreshService()
    {
      Prepare();
    }

    #endregion

    #endregion

  }
}

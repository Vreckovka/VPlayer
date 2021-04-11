using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using UPnP.Common;
using UPnP.Device;
using UPnP.Utils;
using Timer = System.Timers.Timer;

namespace UPnP
{
  public class UPnPService
  {
    #region Events

    public event EventHandler<UPnPDiscoveryCompletedEventArgs> OnUPnPDiscoveryCompleted;
    public class UPnPDiscoveryCompletedEventArgs : EventArgs
    {
      public UPnPDiscoveryCompletedEventArgs()
      {
      }
    }
    public event EventHandler<MediaServerFoundEventArgs> OnMediaServerFound;
    public class MediaServerFoundEventArgs : EventArgs
    {
      public MediaServerFoundEventArgs(MediaServer mediaServer)
      {
        MediaServer = mediaServer;
      }

      public MediaServer MediaServer { get; set; }
    }
    public event EventHandler<MediaRendererFoundEventArgs> OnMediaRendererFound;
    public class MediaRendererFoundEventArgs : EventArgs
    {
      public MediaRendererFoundEventArgs(MediaRenderer mediaRenderer)
      {
        MediaRenderer = mediaRenderer;
      }

      public MediaRenderer MediaRenderer { get; set; }
    }
    public event EventHandler<OtherDeviceFoundEventArgs> OnOtherDeviceFound;
    public class OtherDeviceFoundEventArgs : EventArgs
    {
      public OtherDeviceFoundEventArgs(OtherDevice otherDevice)
      {
        OtherDevice = otherDevice;
      }

      public OtherDevice OtherDevice { get; set; }
    }

    #endregion

    private Dictionary<string, MediaServer> _mediaServers = new Dictionary<string, MediaServer>();
    private Dictionary<string, MediaRenderer> _mediaRenderers = new Dictionary<string, MediaRenderer>();
    private Dictionary<string, OtherDevice> _otherDevices = new Dictionary<string, OtherDevice>();

    private DatagramSocket _socket;
    private DateTime _upnpDiscoveryStart;
    private DateTime _upnpLastMessageReceived;
    private object batton = new object();

    public const string DiscoverMessage = "M-SEARCH * HTTP/1.1\r\n" +
                                          "HOST: 239.255.255.250:1900\r\n" +
                                          "MAN:\"ssdp:discover\"\r\n" +
                                          "ST:ssdp:all\r\n" +
                                          "MX:3\r\n\r\n";

    string SearchString = "M-SEARCH * HTTP/1.1\r\nHOST:239.255.255.250:1900\r\nMAN:\"ssdp:discover\"\r\nST:ssdp:all\r\nMX:3\r\n\r\n";

    #region StartUPnPDiscoveryAsync

    public async Task StartUPnPDiscoveryAsync()
    {
      _socket = new DatagramSocket();
      _socket.MessageReceived += _socket_MessageReceived;

      IOutputStream stream = await _socket.GetOutputStreamAsync(new HostName("239.255.255.250"), "1900");
      
      var writer = new DataWriter(stream) { UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8 };
      writer.WriteString(DiscoverMessage);

      _upnpDiscoveryStart = DateTime.Now;
   

      await writer.StoreAsync();
      await stream.FlushAsync();
      stream.Dispose();
    }

    #endregion
    

    #region _socket_MessageReceived

    private async void _socket_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
    {
      _upnpLastMessageReceived = DateTime.Now;

      DataReader reader = args.GetDataReader();

      uint count = reader.UnconsumedBufferLength;
      string data = reader.ReadString(count);
      var response = new Dictionary<string, string>();
      foreach (string x in data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
      {
        if (x.Contains(":"))
        {
          string[] strings = x.Split(':');
          response.Add(strings[0].ToLower(), x.Remove(0, strings[0].Length + 1));
        }
      }

      Console.WriteLine(data);
      if (response.ContainsKey("location"))
      {
        Uri myUri = new Uri(response["location"]);
        await AddDeviceAsync(myUri, false);
      }
    }

    #endregion

    #region AddDeviceAsync

    private async Task AddDeviceAsync(Uri aliasUrl, bool is_online_media_server)
    {
      try
      {
        Uri deviceDescriptionUrl = new Uri(aliasUrl.AbsoluteUri);
        if (is_online_media_server)
        {
          aliasUrl = new Uri(aliasUrl.AbsoluteUri + Parser.UseSlash(aliasUrl.AbsoluteUri));
          deviceDescriptionUrl = await DetectDeviceDescriptionUrlAsync(aliasUrl);
        }
        if (deviceDescriptionUrl == null)
          return;

        DeviceDescription deviceDescription = Deserializer.DeserializeXmlAsync<DeviceDescription>(deviceDescriptionUrl);
        if (deviceDescription != null)
        {
          switch (deviceDescription.Device.DeviceTypeText)
          {
            case DeviceTypes.MEDIASERVER:
              await AddMediaServerAsync(deviceDescription, aliasUrl, deviceDescriptionUrl, is_online_media_server);
              break;
            case DeviceTypes.MEDIARENDERER:
              await AddMediaRendererAsync(deviceDescription, deviceDescriptionUrl);
              break;
            default:
              AddOtherDevice(deviceDescription, deviceDescriptionUrl);
              break;
          }
        }
      }
      catch
      {
      }
    }

    #endregion

    #region AddMediaServerAsync

    private async Task AddMediaServerAsync(DeviceDescription deviceDescription, Uri aliasUrl, Uri deviceDescriptionUrl, bool is_online_media_server)
    {
      lock (batton)
      {

        if (_mediaServers.ContainsKey(deviceDescriptionUrl.AbsoluteUri))
          return;

        _mediaServers.Add(deviceDescriptionUrl.AbsoluteUri, null);
        MediaServer mediaServer = new MediaServer(deviceDescription, aliasUrl, deviceDescriptionUrl, is_online_media_server);
        mediaServer.InitAsync();
        _mediaServers[deviceDescriptionUrl.AbsoluteUri] = mediaServer;

        if (OnMediaServerFound != null)
          OnMediaServerFound(this, new MediaServerFoundEventArgs(mediaServer));
      }
    }

    #endregion

    #region AddMediaRendererAsync

    private async Task AddMediaRendererAsync(DeviceDescription deviceDescription, Uri deviceDescriptionUrl)
    {
      lock (batton)
      {

        if (_mediaRenderers.ContainsKey(deviceDescriptionUrl.AbsoluteUri))
          return;

        _mediaRenderers.Add(deviceDescriptionUrl.AbsoluteUri, null);
        MediaRenderer mediaRenderer = new MediaRenderer(deviceDescription, deviceDescriptionUrl);
        mediaRenderer.InitAsync();
        _mediaRenderers[deviceDescriptionUrl.AbsoluteUri] = mediaRenderer;

        if (OnMediaRendererFound != null)
          OnMediaRendererFound(this, new MediaRendererFoundEventArgs(mediaRenderer));
      }
    }

    #endregion

    #region AddOtherDevice

    private void AddOtherDevice(DeviceDescription deviceDescription, Uri deviceDescriptionUrl)
    {
      if (_otherDevices.ContainsKey(deviceDescriptionUrl.AbsoluteUri))
        return;

      OtherDevice otherDevice = new OtherDevice(deviceDescription, deviceDescriptionUrl);
      _otherDevices.Add(deviceDescriptionUrl.AbsoluteUri, otherDevice);

      if (OnOtherDeviceFound != null)
        OnOtherDeviceFound(this, new OtherDeviceFoundEventArgs(otherDevice));
    }

    #endregion

    #region DetectDeviceDescriptionUrlAsync

    private static async Task<Uri> DetectDeviceDescriptionUrlAsync(Uri url)
    {
      foreach (string description_url in SupportedOnlineDevices.Items)
      {
        Uri newUri = await Request.RequestUriAsync(new Uri(url.AbsoluteUri + Parser.UseSlash(url.AbsoluteUri) + description_url));
        if (newUri != null)
          return newUri;
      }
      return null;
    }

    #endregion
  }
}

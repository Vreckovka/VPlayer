//Dr Gadgit from the Code project http://www.codeproject.com/Articles/893791/DLNA-made-easy-and-Play-To-for-any-device
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

namespace DLNA
{
  #region HelperDNL
  public static class HelperDLNA
  {
    public static string MakeRequest(string Methord, string Url, int ContentLength, string SOAPAction, string IP, int Port)
    {//Make a request that is sent out to the DLNA server on the LAN using TCP
      string R = Methord.ToUpper() + " /" + Url + " HTTP/1.1" + Environment.NewLine;
      R += "Cache-Control: no-cache" + Environment.NewLine;
      R += "Connection: Close" + Environment.NewLine;
      R += "Pragma: no-cache" + Environment.NewLine;
      R += "Host: " + IP + ":" + Port + Environment.NewLine;
      R += "User-Agent: Microsoft-Windows/6.3 UPnP/1.0 Microsoft-DLNA DLNADOC/1.50" + Environment.NewLine;
      R += "FriendlyName.DLNA.ORG: " + Environment.MachineName + Environment.NewLine;
      if (ContentLength > 0)
      {
        R += "Content-Length: " + ContentLength + Environment.NewLine;
        R += "Content-Type: text/xml; charset=\"utf-8\"" + Environment.NewLine;
      }
      if (SOAPAction.Length > 0)
        R += "SOAPAction: \"" + SOAPAction + "\"" + Environment.NewLine;
      return R + Environment.NewLine;
    }

    public static Socket MakeSocket(string ip, int port)
    {//Just returns a TCP socket ready to use
      IPEndPoint IPWeb = new IPEndPoint(IPAddress.Parse(ip), port);
      Socket SocWeb = new Socket(IPWeb.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      SocWeb.ReceiveTimeout = 6000;
      SocWeb.Connect(IPWeb);
      return SocWeb;
    }

    public static string ReadSocket(Socket Soc, bool CloseOnExit, ref int ReturnCode)
    {//We have some data to read on the socket 
      ReturnCode = 0;
      int ContentLength = 0;
      int HeadLength = 0;
      Thread.Sleep(300);
      MemoryStream MS = new MemoryStream();
      byte[] buffer = new byte[8000];
      int Count = 0;
      while (Count < 8)
      {
        Count++;
        if (Soc.Available > 0)
        {
          int Size = Soc.Receive(buffer);
          string Head = UTF8Encoding.UTF32.GetString(buffer).ToLower();
          if (ContentLength == 0 && Head.IndexOf(Environment.NewLine + Environment.NewLine) > -1 && Head.IndexOf("content-length:") > -1)
          {//We have a contant length so we can test if we have all the page data.
            HeadLength = Head.LastIndexOf(Environment.NewLine + Environment.NewLine);
            string StrCL = Head.ChopOffBefore("content-length:").ChopOffAfter(Environment.NewLine);
            int.TryParse(StrCL, out ContentLength);
          }
          MS.Write(buffer, 0, Size);
          if (ContentLength > 0 && MS.Length >= HeadLength + ContentLength)
          {
            if (CloseOnExit) Soc.Close();
            return UTF8Encoding.UTF8.GetString(MS.ToArray());
          }
        }
        Thread.Sleep(200);
      }
      if (CloseOnExit) Soc.Close();
      string HTML = UTF8Encoding.UTF8.GetString(MS.ToArray());
      string Code = HTML.ChopOffBefore("HTTP/1.1").Trim().ChopOffAfter(" ");
      int.TryParse(Code, out ReturnCode);
      return HTML;
    }
  }
  #endregion

  #region DLNAService
  public class DLNAService
  {
    public string controlURL = "";
    public string Scpdurl = "";
    public string EventSubURL = "";
    public string ServiceType = "";
    public string ServiceID = "";
    public DLNAService(string HTML)
    {
      HTML = HTML.ChopOffBefore("<service>").Replace("url>/", "url>").Trim();
      HTML = HTML.Replace("URL>/", "URL>");
      if (HTML.ToLower().IndexOf("<servicetype>") > -1)
        ServiceType = HTML.ChopOffBefore("<servicetype>").ChopOffAfter("</servicetype>").Trim();
      if (HTML.ToLower().IndexOf("<serviceid>") > -1)
        ServiceID = HTML.ChopOffBefore("<serviceid>").ChopOffAfter("</serviceid>").Trim();
      if (HTML.ToLower().IndexOf("<controlurl>") > -1)
        controlURL = HTML.ChopOffBefore("<controlurl>").ChopOffAfter("</controlurl>").Trim();
      if (HTML.ToLower().IndexOf("<scpdurl>") > -1)
        Scpdurl = HTML.ChopOffBefore("<scpdurl>").ChopOffAfter("</scpdurl>").Trim();
      if (HTML.ToLower().IndexOf("<eventsuburl>") > -1)
        EventSubURL = HTML.ChopOffBefore("<eventsuburl>").ChopOffAfter("</eventsuburl>").Trim();
    }

    public static Dictionary<string, DLNAService> ReadServices(string HTML)
    {
      Dictionary<string, DLNAService> Dic = new Dictionary<string, DLNAService>();
      HTML = HTML.ChopOffBefore("<serviceList>").ChopOffAfter("</serviceList>").Replace("</service>", "¬");
      foreach (string Line in HTML.Split('¬'))
      {
        if (Line.Length > 20)
        {
          DLNAService S = new DLNAService(Line);
          Dic.Add(S.ServiceID, S);
        }
      }
      return Dic;
    }
  }
  #endregion

  public class DLNADevice
  {
    #region Constructors

    public DLNADevice(string url)
    {
      this.IP = url.ChopOffBefore("http://").ChopOffAfter(":");
      this.SMP = url.ChopOffBefore(this.IP).ChopOffBefore("/");
      string StrPort = url.ChopOffBefore(this.IP).ChopOffBefore(":").ChopOffAfter("/");
      int.TryParse(StrPort, out this.Port);
    }

    public DLNADevice(string ip, int port, string smp)
    {
      this.IP = ip;
      this.Port = port;
      this.SMP = smp;
    }

    #endregion

    public string ControlURL = "";
    public int ReturnCode = 0;
    public int Port = 0;
    public string IP = "";
    public string Location = "";
    public string ST = "";
    public string SMP = "";
    public Dictionary<string, DLNAService> Services = null;
    private string XMLHead = "<?xml version=\"1.0\"?>" + Environment.NewLine + "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" SOAP-ENV:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" + Environment.NewLine + "<SOAP-ENV:Body>" + Environment.NewLine;
    private string XMLFoot = "</SOAP-ENV:Body>" + Environment.NewLine + "</SOAP-ENV:Envelope>" + Environment.NewLine;

    //#region GetPosition

    //public string GetPosition()
    //{
    //  string XML = XMLHead + "<m:GetPositionInfo xmlns:m=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID xmlns:dt=\"urn:schemas-microsoft-com:datatypes\" dt:dt=\"ui4\">0</InstanceID></m:GetPositionInfo>" + XMLFoot + Environment.NewLine;
    //  Socket SocWeb = HelperDLNA.MakeSocket(this.IP, this.Port);
    //  string Request = HelperDLNA.MakeRequest("POST", ControlURL, XML.Length, "urn:schemas-upnp-org:service:AVTransport:1#GetPositionInfo", this.IP, this.Port) + XML;
    //  SocWeb.Send(UTF8Encoding.UTF8.GetBytes(Request), SocketFlags.None);
    //  string GG = HelperDLNA.ReadSocket(SocWeb, true, ref this.ReturnCode);
    //  return GG;
    //}

    //#endregion

    #region Desc

    public string Desc()
    {//Gets a description of the DLNA server
      string XML = "<DIDL-Lite xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:upnp=\"urn:schemas-upnp-org:metadata-1-0/upnp/\" xmlns:r=\"urn:schemas-rinconnetworks-com:metadata-1-0/\" xmlns=\"urn:schemas-upnp-org:metadata-1-0/DIDL-Lite/\">" + Environment.NewLine;
      XML += "<item>" + Environment.NewLine;
      XML += "<dc:title>Capital Edinburgh " + DateTime.Now.Millisecond + "</dc:title>" + Environment.NewLine;
      XML += "<upnp:class>object.item.audioItem.audioBroadcast</upnp:class>" + Environment.NewLine;
      XML += "<desc id=\"cdudn\" nameSpace=\"urn:schemas-rinconnetworks-com:metadata-1-0/\">SA_RINCON65031_</desc>" + Environment.NewLine;
      XML += "</item>" + Environment.NewLine;
      XML += "</DIDL-Lite>" + Environment.NewLine;
      return XML;
    }

    #endregion

    #region StartPlay

    public string StartPlay(int Instance)
    {
      string XML = XMLHead;
      XML += "<u:Play xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>" + Instance + "</InstanceID><Speed>1</Speed></u:Play>" + Environment.NewLine;
      XML += XMLFoot + Environment.NewLine;
      Socket SocWeb = HelperDLNA.MakeSocket(this.IP, this.Port);
      string Request = HelperDLNA.MakeRequest("POST", ControlURL, XML.Length, "urn:schemas-upnp-org:service:AVTransport:1#Play", this.IP, this.Port) + XML;
      SocWeb.Send(UTF8Encoding.UTF8.GetBytes(Request), SocketFlags.None);
      return HelperDLNA.ReadSocket(SocWeb, true, ref this.ReturnCode);
    }

    #endregion

    #region StopPlay

    public string StopPlay(int Instance)
    {//Called to stop playing a movie or a music track
      string XML = XMLHead;
      XML += "<u:Stop xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>" + Instance + "</InstanceID></u:Stop>" + Environment.NewLine;
      XML += XMLFoot + Environment.NewLine;
      Socket SocWeb = HelperDLNA.MakeSocket(this.IP, this.Port);
      string Request = HelperDLNA.MakeRequest("POST", ControlURL, XML.Length, "urn:schemas-upnp-org:service:AVTransport:1#Stop", this.IP, this.Port) + XML;
      SocWeb.Send(UTF8Encoding.UTF8.GetBytes(Request), SocketFlags.None);
      return HelperDLNA.ReadSocket(SocWeb, true, ref this.ReturnCode);
    }

    #endregion

    #region Pause

    public string Pause(int Instance)
    {//Called to pause playing a movie or a music track
      string XML = XMLHead;
      XML += "<u:Pause xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>" + Instance + "</InstanceID></u:Pause>" + Environment.NewLine;
      XML += XMLFoot + Environment.NewLine;
      Socket SocWeb = HelperDLNA.MakeSocket(this.IP, this.Port);
      string Request = HelperDLNA.MakeRequest("POST", ControlURL, XML.Length, "urn:schemas-upnp-org:service:AVTransport:1#Pause", this.IP, this.Port) + XML;
      SocWeb.Send(UTF8Encoding.UTF8.GetBytes(Request), SocketFlags.None);
      return HelperDLNA.ReadSocket(SocWeb, true, ref this.ReturnCode);
    }

    #endregion

    #region GetTransportInfo

    public string GetTransportInfo()
    {//Returns the current position for the track that is playing on the DLNA server
      string XML = XMLHead + "<m:GetTransportInfo xmlns:m=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID xmlns:dt=\"urn:schemas-microsoft-com:datatypes\" dt:dt=\"ui4\">0</InstanceID></m:GetTransportInfo>" + XMLFoot + Environment.NewLine;
      Socket SocWeb = HelperDLNA.MakeSocket(this.IP, this.Port);
      string Request = HelperDLNA.MakeRequest("POST", ControlURL, XML.Length, "urn:schemas-upnp-org:service:AVTransport:1#GetTransportInfo", this.IP, this.Port) + XML;
      if (SocWeb != null)
      {
        SocWeb.Send(Encoding.UTF8.GetBytes(Request), SocketFlags.None);
        return HelperDLNA.ReadSocket(SocWeb, true, ref this.ReturnCode);
      }
      else
        return "";
    }

    #endregion

    #region Seek

    public string Seek(int Instance, string position)
    {
      string XML = XMLHead;
      XML += "<u:Seek xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\"><InstanceID>" + Instance + "</InstanceID><Unit>REL_TIME</Unit><Target>" + position + "</Target></u:Seek>" + Environment.NewLine;
      XML += XMLFoot + Environment.NewLine;
      Socket SocWeb = HelperDLNA.MakeSocket(this.IP, this.Port);
      string Request = HelperDLNA.MakeRequest("POST", ControlURL, XML.Length, "urn:schemas-upnp-org:service:AVTransport:1#Seek", this.IP, this.Port) + XML;
      SocWeb.Send(Encoding.UTF8.GetBytes(Request), SocketFlags.None);
      return HelperDLNA.ReadSocket(SocWeb, true, ref this.ReturnCode);
    }

    #endregion

    #region UploadFileToPlay

    public string UploadFileToPlay(string UrlToPlay)
    {
      string XML = XMLHead;
      XML += "<u:SetAVTransportURI xmlns:u=\"urn:schemas-upnp-org:service:AVTransport:1\">" + Environment.NewLine;
      XML += "<InstanceID>0</InstanceID>" + Environment.NewLine;
      XML += "<CurrentURI>" + UrlToPlay + "</CurrentURI>" + Environment.NewLine;
      XML += "<CurrentURIMetaData>" + "</CurrentURIMetaData>" + Environment.NewLine;
      XML += "</u:SetAVTransportURI>" + Environment.NewLine;
      XML += XMLFoot + Environment.NewLine;
      Socket SocWeb = HelperDLNA.MakeSocket(this.IP, this.Port);
      string Request = HelperDLNA.MakeRequest("POST", ControlURL, XML.Length, "urn:schemas-upnp-org:service:AVTransport:1#SetAVTransportURI", this.IP, this.Port) + XML;
      SocWeb.Send(UTF8Encoding.UTF8.GetBytes(Request), SocketFlags.None);
      return HelperDLNA.ReadSocket(SocWeb, true, ref this.ReturnCode);
    }

    #endregion


  }
}

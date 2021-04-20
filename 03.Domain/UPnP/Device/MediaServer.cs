using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UPnP.Common;
using UPnP.Utils;

namespace UPnP.Device
{
  public abstract class UPnPDevice
  {
    public DeviceDescription DeviceDescription { get; set; }
  }

  public class MediaServer : UPnPDevice
  {
    #region Events

    public event EventHandler<ContentFoundEventArgs> OnContentFound;
    public class ContentFoundEventArgs : EventArgs
    {
      public ContentFoundEventArgs(Content content)
      {
        Content = content;
      }

      public Content Content { get; set; }
    }
    public event EventHandler<VideoContentScanCompletedEventArgs> OnVideoContentScanCompleted;
    public class VideoContentScanCompletedEventArgs : EventArgs
    {
      public VideoContentScanCompletedEventArgs(Content content)
      {
        Content = content;
      }

      public Content Content { get; set; }
    }
    public event EventHandler<AudioContentScanCompletedEventArgs> OnAudioContentScanCompleted;
    public class AudioContentScanCompletedEventArgs : EventArgs
    {
      public AudioContentScanCompletedEventArgs(Content content)
      {
        Content = content;
      }

      public Content Content { get; set; }
    }
    public event EventHandler<PhotoContentScanCompletedEventArgs> OnPhotoContentScanCompleted;
    public class PhotoContentScanCompletedEventArgs : EventArgs
    {
      public PhotoContentScanCompletedEventArgs(Content content)
      {
        Content = content;
      }

      public Content Content { get; set; }
    }

    #endregion

    #region Private variables

    private Dictionary<string, Content> _contents = new Dictionary<string, Content>();
    private ServiceAction _browseAction;

    #endregion

    #region Constructors

    public MediaServer()
    {
    }

    public MediaServer(DeviceDescription deviceDescription, Uri aliasUrl, Uri deviceDescriptionUrl, bool is_online_media_server)
    {
      this.DeviceDescription = deviceDescription;
      this.AliasURL = aliasUrl.AbsoluteUri;
      this.OnlineServer = is_online_media_server;

      if (this.OnlineServer)
      {
        this.PresentationURL = deviceDescriptionUrl.Authority;
        if (this.PresentationURL.IndexOf("http://") == -1)
          this.PresentationURL = "http://" + this.PresentationURL;
        this.PresentationURL = this.PresentationURL + Parser.UseSlash(this.PresentationURL);
      }
      else
      {
        if (string.IsNullOrEmpty(this.DeviceDescription.Device.PresentationURL))
        {
          this.DeviceDescription.Device.PresentationURL = deviceDescriptionUrl.Authority;
          if (this.DeviceDescription.Device.PresentationURL.IndexOf("http://") == -1)
            this.DeviceDescription.Device.PresentationURL = "http://" + this.DeviceDescription.Device.PresentationURL;
        }
        this.PresentationURL = this.DeviceDescription.Device.PresentationURL + Parser.UseSlash(this.DeviceDescription.Device.PresentationURL);
      }
    }

    #endregion

    #region Public properties

    public string AliasURL { get; set; }
    public string PresentationURL { get; set; }
    public string DefaultIconUrl { get; set; }
    public bool OnlineServer { get; set; }
    public MediaServer Self { get; private set; }
    public ConnectionManager ConnectionManager { get; private set; }
    public ContentDirectory ContentDirectory { get; private set; }
    public MediaReceiverRegistrar MediaReceiverRegistrar { get; private set; }
    public Uri ContentDirectoryControlUrl { get; set; }

    #endregion

    #region Public functions

    public void InitAsync()
    {
      foreach (Service serv in this.DeviceDescription.Device.ServiceList)
        switch (serv.ServiceType)
        {
          case ServiceTypes.CONNECTIONMANAGER:
            this.ConnectionManager = Deserializer.DeserializeXmlAsync<ConnectionManager>(new Uri(this.PresentationURL + serv.SCPDURL.Substring(1)));
            break;
          case ServiceTypes.CONTENTDIRECTORY:
            this.ContentDirectory = Deserializer.DeserializeXmlAsync<ContentDirectory>(new Uri(this.PresentationURL + serv.SCPDURL.Substring(1)));
            this.ContentDirectoryControlUrl = new Uri(this.PresentationURL + serv.ControlURL.Substring(1));
            break;
          case ServiceTypes.MEDIARECEIVERREGISTRAR:
            this.MediaReceiverRegistrar = Deserializer.DeserializeXmlAsync<MediaReceiverRegistrar>(new Uri(this.PresentationURL + serv.SCPDURL.Substring(1)));
            break;
        }

      SetDefaultIconUrl();
      _browseAction = FindBrowseAction();
      this.Self = this;
    }

    public async Task BrowseFolderAsync(string destination_id, string go_up_id)
    {
      try
      {
        Content content;
        if (!_contents.ContainsKey(destination_id))
        {
          content = new Content(await BrowseFolderAsync(destination_id), go_up_id, this.PresentationURL);
          _contents.Add(destination_id, content);
        }
        else
          content = _contents[destination_id];


        if (OnContentFound != null)
          OnContentFound(this, new ContentFoundEventArgs(content));
      }
      catch
      {
      }
    }

    public void GoUp(Content content)
    {
      if (_contents.ContainsKey(content.GoUpId))
      {
        if (OnContentFound != null)
          OnContentFound(this, new ContentFoundEventArgs(_contents[content.GoUpId]));
      }
    }

    public async Task StartScanVideoContentAsync()
    {
      try
      {
        //root
        DIDLLite didllite_root = await BrowseFolderAsync("0");
        foreach (Container cont_root in didllite_root.Containers)
        {
          if (cont_root.PersistentID == "video")
          {
            DIDLLite didllite_video = await BrowseFolderAsync(cont_root.Id);
            foreach (Container cont_video in didllite_video.Containers)
            {
              if (cont_video.PersistentID == "video/all")
              {
                DIDLLite didllite_allvideo = await BrowseFolderAsync(cont_video.Id);
                Content content = new Content(didllite_allvideo, cont_video.ParentID, this.PresentationURL);

                if (OnVideoContentScanCompleted != null)
                  OnVideoContentScanCompleted(this, new VideoContentScanCompletedEventArgs(content));
                return;
              }
            }
          }
        }
      }
      catch
      {
      }
    }

    public async Task StartScanAudioContentAsync()
    {
      try
      {
        //root
        DIDLLite didllite_root = await BrowseFolderAsync("0");
        foreach (Container cont_root in didllite_root.Containers)
        {
          if (cont_root.Title == "Music")
          {
            DIDLLite didllite_audio = await BrowseFolderAsync(cont_root.Id);

            foreach (Container cont_audio in didllite_audio.Containers)
            {
              if (cont_audio.Title == "Folders")
              {
                DIDLLite didllite_allaudio = await BrowseFolderAsync(cont_audio.Id);

                Content content = new Content(didllite_allaudio, cont_audio.ParentID, this.PresentationURL);

                if (OnAudioContentScanCompleted != null)
                  OnAudioContentScanCompleted(this, new AudioContentScanCompletedEventArgs(content));
                return;
              }
            }
          }
        }
      }
      catch (Exception ex)
      {

      }
    }

    public async Task StartScanPhotoContentAsync()
    {
      try
      {
        //root
        DIDLLite didllite_root = await BrowseFolderAsync("0");
        foreach (Container cont_root in didllite_root.Containers)
        {
          if (cont_root.PersistentID == "picture")
          {
            DIDLLite didllite_photo = await BrowseFolderAsync(cont_root.Id);
            foreach (Container cont_photo in didllite_photo.Containers)
            {
              if (cont_photo.PersistentID == "picture/all")
              {
                DIDLLite didllite_allphoto = await BrowseFolderAsync(cont_photo.Id);
                Content content = new Content(didllite_allphoto, cont_photo.ParentID, this.PresentationURL);

                if (OnPhotoContentScanCompleted != null)
                  OnPhotoContentScanCompleted(this, new PhotoContentScanCompletedEventArgs(content));
                return;
              }
            }
          }
        }
      }
      catch
      {
      }
    }

    #endregion

    #region Private functions

    private void SetDefaultIconUrl()
    {
      try
      {
        Icon[] iconList = DeviceDescription.Device.IconList;
        if (iconList != null)
        {
          foreach (Icon ic in iconList)
          {
            if (ic.MimeType.IndexOf("png") > -1)
            {
              this.DefaultIconUrl = ic.Url;
            }
          }
          if (string.IsNullOrEmpty(this.DefaultIconUrl) && (iconList != null))
            this.DefaultIconUrl = iconList[0].Url;

          if (!string.IsNullOrEmpty(this.DefaultIconUrl))
          {
            if (this.DefaultIconUrl.Substring(0, 1) == "/")
              this.DefaultIconUrl = this.DefaultIconUrl.Substring(1);
            this.DefaultIconUrl = this.PresentationURL + this.DefaultIconUrl;
          }
        }
      }
      catch
      {
      }
    }

    private ServiceAction FindBrowseAction()
    {
      if (this.ContentDirectory != null)
      {
        foreach (ServiceAction act in this.ContentDirectory.ActionList)
        {
          if (act.Name.ToUpper() == "BROWSE")
          {
            return act;
          }
        }
      }

      return null;
    }

    //4-127035
    public async Task<string> GetMusic(string id)
    {
      _browseAction.ClearArgumentsValue();
      _browseAction.SetArgumentValue("ObjectId", id);
      _browseAction.SetArgumentValue("BrowseFlag", "BrowseMetadata");
      _browseAction.SetArgumentValue("StartingIndex", "0");
      _browseAction.SetArgumentValue("RequestedCount", "0");
      await _browseAction.InvokeAsync(ServiceTypes.CONTENTDIRECTORY, this.ContentDirectoryControlUrl.AbsoluteUri);

      return _browseAction.GetArgumentValue("Result");
    }

    public async Task<DIDLLite> BrowseFolderAsync(string id)
    {
      try
      {
        if (_browseAction != null)
        {
          DIDLLite didllite = new DIDLLite();
          didllite.Containers = new List<Container>();
          didllite.Items = new List<Item>();
          int start_from = 0;
          int limit = 100;
          bool found = false;
          do
          {
            _browseAction.ClearArgumentsValue();
            _browseAction.SetArgumentValue("ObjectId", id);
            _browseAction.SetArgumentValue("BrowseFlag", "BrowseDirectChildren");
            _browseAction.SetArgumentValue("Filter", "*");
            _browseAction.SetArgumentValue("StartingIndex", start_from.ToString());
            _browseAction.SetArgumentValue("RequestedCount", limit.ToString());
            _browseAction.SetArgumentValue("SortCriteria", "");
            await _browseAction.InvokeAsync(ServiceTypes.CONTENTDIRECTORY, this.ContentDirectoryControlUrl.AbsoluteUri);
            DIDLLite tmp_didllite = Deserializer.DeserializeXml<DIDLLite>(_browseAction.GetArgumentValue("Result"));

            if (tmp_didllite == null)
            {
              return null;
            }

            foreach (Container container in tmp_didllite.Containers)
              didllite.Containers.Add(container);
            foreach (Item item in tmp_didllite.Items)
              didllite.Items.Add(item);
            found = (tmp_didllite.Containers.Count > 0 || tmp_didllite.Items.Count > 0);
            start_from += limit;
          }
          while (found);

          return didllite;
        }
        return null;
      }
      catch
      {
        return null;
      }
    }

    #endregion
  }
}

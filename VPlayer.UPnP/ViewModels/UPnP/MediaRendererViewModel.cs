using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows.Input;
using DLNA;
using UPnP.Common;
using UPnP.Device;
using VCore;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.UPnP.ViewModels.UPnP
{
  public class MediaRendererViewModel : UPnPViewModel<MediaRenderer>
  {
    private string XMLHead = "<?xml version=\"1.0\"?>" + Environment.NewLine + "<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" SOAP-ENV:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">" + Environment.NewLine + "<SOAP-ENV:Body>" + Environment.NewLine;
    private string XMLFoot = "</SOAP-ENV:Body>" + Environment.NewLine + "</SOAP-ENV:Envelope>" + Environment.NewLine;
    public MediaRendererViewModel(MediaRenderer model, IStorageManager storageManager) : base(model, storageManager)
    {
     
    }


    #region Discover

    private ActionCommand playCommand;

    public ICommand PlayCommand
    {
      get
      {
        if (playCommand == null)
        {
          playCommand = new ActionCommand(OnDiscover);
        }

        return playCommand;
      }
    }

    private void OnDiscover()
    {
      Play("http://127.0.0.1:10243/WMPNSSv4/2988159691/1_MTRfYjFmMzQ5YzVfNzU0YTdjNTVfZTUwNzg1NWEtMTI5Njg1.mp3");
    }

    #endregion

    public override void StoreData()
    {
      // Model.InitAsync();
      //var dbEntity = new UPnPMediaServer()
      //{
      //  AliasURL = Model.AliasURL,
      //  UPnPDevice = GetDeviceDbEntity(),
      //  PresentationURL = Model.PresentationURL,
      //  OnlineServer = Model.OnlineServer,
      //  ContentDirectoryControlUrl = Model.ContentDirectoryControlUrl,
      //  DefaultIconUrl = Model.DefaultIconUrl
      //};

      //var result = storageManager.StoreEntity<UPnPMediaServer>(dbEntity, out var uPnPMediaServer);


      //if (result)
      //{
      //  IsStored = true;
      //  save.RaiseCanExecuteChanged();
      //}
    }


    public async void Play(string fileUrl)
    {

      //"upnp/control/rendertransport1"
      var actions = Model.AVTransport.ActionList;

      var device = new DLNADevice(Model.PresentationURL);
      device.ControlURL = "upnp/control/rendertransport1";

      Console.WriteLine(device.StopPlay(true));
     

     Console.WriteLine(device.TryToPlayFile(fileUrl));

    }
  }
}
using System;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows.Input;
using DLNA;
using UPnP.Common;
using UPnP.Device;
using VCore;
using VPlayer.AudioStorage.DomainClasses.UPnP;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.UPnP.ViewModels.UPnP
{
  public class MediaRendererViewModel : UPnPViewModel<MediaRenderer>
  {
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
      //Play("http://127.0.0.1:10243/WMPNSSv4/2988159691/1_MTRfYjFmMzQ5YzVfNzU0YTdjNTVfZTUwNzg1NWEtMTI5Njg1.mp3");
    }

    #endregion

    #region StoreData

    public override void StoreData()
    {
      Model.Init();

      var dbEntity = new UPnPMediaRenderer()
      {
        UPnPDevice = Model.DeviceDescription.Device.GetDeviceDbEntity(),
        PresentationURL = Model.PresentationURL
      };

      var result = storageManager.StoreEntity<UPnPMediaRenderer>(dbEntity, out var stored);


      if (result)
      {
        IsStored = true;
        save.RaiseCanExecuteChanged();
      }
    }

    #endregion
    
  }
}
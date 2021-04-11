using UPnP.Device;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.UPnP.ViewModels.UPnP
{
  public class MediaRendererViewModel : UPnPViewModel<MediaRenderer>
  {
    public MediaRendererViewModel(MediaRenderer model, IStorageManager storageManager) : base(model, storageManager)
    {
    }


    public override void StoreData()
    {
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
  }
}
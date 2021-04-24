namespace VPlayer.AudioStorage.DomainClasses.UPnP
{
  public class UPnPMediaRenderer : DomainEntity
  {
    public string PresentationURL { get; set; }
    public UPnPDevice UPnPDevice { get; set; }
  }
}
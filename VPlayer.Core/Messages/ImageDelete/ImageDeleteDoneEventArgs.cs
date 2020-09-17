namespace VPlayer.Core.Messages.ImageDelete
{
  public class ImageDeleteDoneEventArgs : ImageDeleteRequestEventArgs
  {
    public bool Result { get; set; }
  }
}
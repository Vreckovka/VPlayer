using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Logger;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels;

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class VideoSliderPopupDetailViewModel : FileItemSliderPopupDetailViewModel<VideoItem>
  {
    private readonly ILogger logger;
    private VideoCapture videoCapture;
    double frameCount;
    private ImageConverter imageConverter;
    public VideoSliderPopupDetailViewModel(VideoItem model, ILogger logger) : base(model)
    {
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      videoCapture = new VideoCapture(model.Source, VideoCapture.API.Any, new Tuple<CapProp, int>(CapProp.HwAcceleration, 1));

      frameCount = videoCapture.Get(CapProp.FrameCount);
      imageConverter = new ImageConverter();

      videoCapture.Set(CapProp.FrameWidth, 460);
      videoCapture.Set(CapProp.FrameWidth, 320);
    }

    #region GetImage

    private async Task<byte[]> GetImage()
    {
      try
      {
       await semaphoreSlim.WaitAsync();

        if (isDiposed)
          return null;

        if (MaxValue > 0)
        {
          var frame = (ActualSliderValue / MaxValue) * frameCount;

          videoCapture.Set(CapProp.PosFrames, frame);

          var img = videoCapture.QuerySmallFrame();

          if (img == null)
            return null;

          return ImageToByte(img.ToBitmap());
        }


        return null;
      }
      catch (Exception ex)
      {
        logger.Log(ex);
        return null;
      }
      finally
      {
        semaphoreSlim.Release();
      }

    }

    #endregion


    public byte[] ImageToByte(System.Drawing.Image img)
    {
      return (byte[])imageConverter.ConvertTo(img, typeof(byte[]));
    }

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);


    protected override void Refresh()
    {
      base.Refresh();
    
      try
      {
        Task.Run(async () =>
        {
          if (semaphoreSlim.CurrentCount == 1)
          {
            if (videoCapture != null)
            {
              videoCapture.Stop();
              videoCapture.Dispose();
              videoCapture = new VideoCapture(Model.Source, VideoCapture.API.Any, new Tuple<CapProp, int>(CapProp.HwAcceleration, 1));
            }

            var image = await GetImage();

            Application.Current?.Dispatcher?.Invoke(() =>
            {
              Image = image;
            });
          }
        });
      }
      catch (Exception ex)
      {
        logger.Log(ex);
      }
    }

    private bool isDiposed = false;
    public override async void Dispose()
    {
      try
      {
        base.Dispose();

        await semaphoreSlim.WaitAsync();

        videoCapture.Stop();
        videoCapture.Dispose();
        isDiposed = true;
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }
  }
}
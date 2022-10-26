using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.Util;
using Logger;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.Core.ViewModels;

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class VideoSliderPopupDetailViewModel : FileItemSliderPopupDetailViewModel<VideoItem>
  {
    private readonly VideoItem model;
    private readonly ILogger logger;
    private VideoCapture videoCapture;
    double frameCount;
    private ImageConverter imageConverter;
    public VideoSliderPopupDetailViewModel(VideoItem model, ILogger logger) : base(model)
    {
      this.model = model ?? throw new ArgumentNullException(nameof(model));
      this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
      
      imageConverter = new ImageConverter();
    }

    #region GetImage

    private byte[] GetImage()
    {
      try
      {
        if (videoCapture == null)
        {
          videoCapture = new VideoCapture(model.Source, VideoCapture.API.Any, new Tuple<CapProp, int>(CapProp.HwAcceleration, 1));

          frameCount = videoCapture.Get(CapProp.FrameCount);

          videoCapture.Set(CapProp.FrameWidth, 460);
          videoCapture.Set(CapProp.FrameWidth, 320);
        }

        if (isDiposed)
          return null;

        if (MaxValue > 0)
        {
          var frame = (ActualSliderValue / MaxValue) * frameCount;

          videoCapture.Set(CapProp.FourCC, VideoWriter.Fourcc('M', 'J', 'P', 'G'));
          videoCapture.Set(CapProp.Buffersize, 1);
          videoCapture.Set(CapProp.PosFrames, frame);

          var img = videoCapture.QuerySmallFrame();

          if (img == null)
            return null;

          var bitMap = img.ToBitmap();
          img.Dispose();
          return ImageToByte(bitMap);
        }


        return null;
      }
      catch (Exception ex)
      {
        logger.Log(ex);
        return null;
      }


    }

    #endregion


    public byte[] ImageToByte(System.Drawing.Image img)
    {
      return (byte[])imageConverter.ConvertTo(img, typeof(byte[]));
    }

    private SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1, 1);


    protected override async void Refresh()
    {
      try
      {
        await semaphoreSlim.WaitAsync();

        base.Refresh();

        var task = Task.Run(() =>
        {
          if (isDiposed)
          {
            return null;
          }

          return GetImage();
        });

        Image = await task; 
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }

    private bool isDiposed = false;
    public override async void Dispose()
    {
      try
      {
        base.Dispose();
        await semaphoreSlim.WaitAsync();

        isDiposed = true;
        videoCapture?.Stop();
        videoCapture?.Dispose();
      }
      catch (Exception ex)
      {
        logger.Log(ex);
      }
      finally
      {
        semaphoreSlim.Release();
      }
    }
  }
}
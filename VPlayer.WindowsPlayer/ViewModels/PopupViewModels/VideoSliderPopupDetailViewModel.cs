using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.Util;
using FFMpegCore;
using FFMpegCore.Exceptions;
using Logger;
using VPlayer.AudioStorage.DomainClasses;
using VPlayer.AudioStorage.DomainClasses.Video;
using VPlayer.Core.Players;
using VPlayer.Core.ViewModels;
using Size = System.Drawing.Size;

namespace VPlayer.WindowsPlayer.ViewModels
{
  public class VideoSliderPopupDetailViewModel : FileItemSliderPopupDetailViewModel<VideoItem>
  {
    private readonly VideoItem model;
    private readonly ILogger logger;
    private ImageConverter imageConverter;
    private IMediaAnalysis mediaAnalysis;
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
        if (!DisablePopup)
        {
          if (isDiposed)
            return null;

          if (mediaAnalysis == null)
          {
            mediaAnalysis = FFProbe.Analyse(Model.Source);
          }

          var width = mediaAnalysis.VideoStreams[0].Width;
          var height = mediaAnalysis.VideoStreams[0].Height;

          double desiredWidth = 480.0;

          var sizeCoef = Math.Floor(width / desiredWidth) > 0 ? Math.Floor(width / desiredWidth) : 1;

          if (MaxValue > 0)
          {
            var bitMap = FFMpeg.Snapshot(model.Source, new Size((int)(width / sizeCoef), (int)(height / sizeCoef)), TimeSpan.FromSeconds(ActualSliderValue * model.Duration));

            return ImageToByte(bitMap);
          }
        }
        
        return null;
      }
      catch (FFMpegException ex)
      {
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


    private double lastActualSliderValue = 0;
    protected override async void Refresh()
    {
      try
      {
        await semaphoreSlim.WaitAsync();

        if (IsPopupOpened && ActualSliderValue != lastActualSliderValue)
        {
          lastActualSliderValue = ActualSliderValue;

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
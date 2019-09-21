using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Threading;
using Un4seen.Bass;
using Un4seen.BassWasapi;
using VCore.ViewModels;

namespace VPlayer.Player.ViewModels.AudioAnalyzer
{
  public class AudioAnalyzerViewModel : ViewModel
  {
    #region Fields

    private DispatcherTimer dispatcherTimer;
    private float[] buffer;

    private WASAPIPROC _process;        //callback function to obtain data
    private int _hanctr;                //last output level counter
    private int _lastlevel;             //last output level
    private bool _initialized;          //initialized flag 

    #endregion

    #region Constructors

    public AudioAnalyzerViewModel()
    {
      Initilization();
    }

    #endregion

    #region Properties

    public ObservableCollection<DeviceViewModel> Devices { get; set; } = new ObservableCollection<DeviceViewModel>();
    public bool IsEnabled { get; set; }
    public int Left { get; set; }
    public int Right { get; set; }
    public DeviceViewModel ActualDeviceViewModel { get; set; }
    public SpectrumViewModel Spectrum { get; set; }

    #endregion

    #region Enable

    //flag for enabling and disabling program functionality
    private bool _enable;

    #region Enable

    public bool Enable
    {
      get { return _enable; }
      set
      {
        _enable = value;
        if (value)
        {
          if (!_initialized)
          {

            bool result = BassWasapi.BASS_WASAPI_Init(ActualDeviceViewModel.BassWasapiIndex, 0, 0, BASSWASAPIInit.BASS_WASAPI_BUFFER, 1f, 0.05f, _process, IntPtr.Zero);
            if (!result)
            {
              var error = Bass.BASS_ErrorGetCode();
              MessageBox.Show(error.ToString());
            }
            else
            {
              _initialized = true;
            }
          }
          BassWasapi.BASS_WASAPI_Start();
        }
        else BassWasapi.BASS_WASAPI_Stop(true);
        System.Threading.Thread.Sleep(500);
        dispatcherTimer.IsEnabled = value;
      }
    }

    #endregion

    #endregion

    #region Initilization

    private void Initilization()
    {
      buffer = new float[1024];
      _process = new WASAPIPROC(Process);
      Spectrum = new SpectrumViewModel(8);

      SetDevices();
      SetTimer();

      Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATETHREADS, false);

      var result = Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);

      if (!result)
        throw new Exception("Init Error");
    }

    #endregion

    #region SetDevices

    private void SetDevices()
    {
      for (int i = 0; i < BassWasapi.BASS_WASAPI_GetDeviceCount(); i++)
      {
        var device = BassWasapi.BASS_WASAPI_GetDeviceInfo(i);

        if (device.IsEnabled && device.IsLoopback)
        {
          Devices.Add(new DeviceViewModel()
          {
            Name = string.Format("{0} - {1}", i, device.name),
            BassWasapiIndex = i
          });
        };
      }

      ActualDeviceViewModel = Devices[0];
    }

    #endregion

    #region SetTimer

    private void SetTimer()
    {
      dispatcherTimer = new DispatcherTimer();
      dispatcherTimer.Interval = TimeSpan.FromMilliseconds(25); //40hz refresh rate
      dispatcherTimer.IsEnabled = false;

      Observable.FromEventPattern(
        x => dispatcherTimer.Tick += x,
        x => dispatcherTimer.Tick -= x
        ).Subscribe(OnTick);
    }

    #endregion

    #region OnTick

    private void OnTick(EventPattern<object> eventPattern)
    {
      int ret = BassWasapi.BASS_WASAPI_GetData(buffer, (int)BASSData.BASS_DATA_FFT2048); //get channel fft data
      if (ret < -1) return;

      int x, y;
      int b0 = 0;

      byte[] spectrumData = new byte[Spectrum.Data.Length];
      //computes the spectrum data, the code is taken from a bass_wasapi sample.
      for (x = 0; x < Spectrum.LinesCount; x++)
      {
        float peak = 0;
        int b1 = (int)Math.Pow(2, x * 10.0 / (Spectrum.LinesCount - 1));
        if (b1 > 1023) b1 = 1023;
        if (b1 <= b0) b1 = b0 + 1;
        for (; b0 < b1; b0++)
        {
          if (peak < buffer[1 + b0]) peak = buffer[1 + b0];
        }
        y = (int)(Math.Sqrt(peak) * 3 * 255 - 4);
        if (y > 255) y = 255;
        if (y < 0) y = 0;

        spectrumData[x] = ((byte)y);
      }

      if (IsEnabled)
      {
        Spectrum.Set(spectrumData);
        Spectrum.ClearData();
      }


      int level = BassWasapi.BASS_WASAPI_GetLevel();
      Left = Utils.LowWord32(level);
      Right = Utils.HighWord32(level);
      if (level == _lastlevel && level != 0) _hanctr++;
      _lastlevel = level;

      //Required, because some programs hang the output. If the output hangs for a 75ms
      //this piece of code re initializes the output so it doesn't make a gliched sound for long.
      if (_hanctr > 3)
      {
        _hanctr = 0;
        Left = 0;
        Right = 0;
        Free();
        Bass.BASS_Init(0, 44100, BASSInit.BASS_DEVICE_DEFAULT, IntPtr.Zero);
        _initialized = false;
        Enable = true;
      }
    }

    #endregion

    #region Free

    public void Free()
    {
      BassWasapi.BASS_WASAPI_Free();
      Bass.BASS_Free();
    }

    #endregion

    #region Process

    // WASAPI callback, required for continuous recording
    private int Process(IntPtr buffer, int length, IntPtr user)
    {
      return length;
    }
    #endregion
  }
}

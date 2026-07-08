using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Ninject.Activation;
using VCore.WPF;
using VCore.WPF.Controls;
using VPlayer.Core.SoundVizualization;
using Windows.UI.Xaml.Media;
using Color = System.Windows.Media.Color;

namespace VPlayer.Player.UserControls
{
  public partial class BassImageVizualizer : UserControl
  {
    private BitmapSource originalImage;

    private float visualBass;

    #region ImagePath

    public string ImagePath
    {
      get => (string)GetValue(ImagePathProperty);
      set => SetValue(ImagePathProperty, value);
    }

    public static readonly DependencyProperty ImagePathProperty =
      DependencyProperty.Register(
        nameof(ImagePath),
        typeof(string),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
          null,
          ReloadImage));


    #endregion

    #region VizualizerLayerPath

    public string VizualizerLayerPath
    {
      get => (string)GetValue(VizualizerLayerPathProperty);
      set => SetValue(VizualizerLayerPathProperty, value);
    }

    public static readonly DependencyProperty VizualizerLayerPathProperty =
      DependencyProperty.Register(
        nameof(VizualizerLayerPath),
        typeof(string),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
          null,
          ReloadImage));


    #endregion

    #region StableVizualizerLayerPath

    public string StableVizualizerLayerPath
    {
      get => (string)GetValue(StableVizualizerLayerPathProperty);
      set => SetValue(StableVizualizerLayerPathProperty, value);
    }

    public static readonly DependencyProperty StableVizualizerLayerPathProperty =
      DependencyProperty.Register(
        nameof(StableVizualizerLayerPath),
        typeof(string),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
          null,
          ReloadImage));


    #endregion

    #region Stretch

    public System.Windows.Media.Stretch Stretch
    {
      get => (System.Windows.Media.Stretch)GetValue(StretchProperty);
      set => SetValue(StretchProperty, value);
    }

    public static readonly DependencyProperty StretchProperty =
      DependencyProperty.Register(
        nameof(Stretch),
        typeof(System.Windows.Media.Stretch),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
          System.Windows.Media.Stretch.Uniform,
          ReloadImage));


    #endregion

    #region LowBassImageColor

    public Color LowBassImageColor
    {
      get => (Color)GetValue(LowBassImageColorProperty);
      set => SetValue(LowBassImageColorProperty, value);
    }

    public static readonly DependencyProperty LowBassImageColorProperty =
      DependencyProperty.Register(
        nameof(LowBassImageColor),
        typeof(Color),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
          Color.FromRgb(0, 0, 255),
          ReloadImage));


    #endregion

    #region MidBassImageColor

    public Color MidBassImageColor
    {
      get => (Color)GetValue(MidBassImageColorProperty);
      set => SetValue(MidBassImageColorProperty, value);
    }

    public static readonly DependencyProperty MidBassImageColorProperty =
      DependencyProperty.Register(
        nameof(MidBassImageColor),
        typeof(Color),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
          Color.FromRgb(66, 245, 72),
          ReloadImage));


    #endregion

    #region HighBassImageColor

    public Color HighBassImageColor
    {
      get => (Color)GetValue(HighBassImageColorProperty);
      set => SetValue(HighBassImageColorProperty, value);
    }

    public static readonly DependencyProperty HighBassImageColorProperty =
      DependencyProperty.Register(
        nameof(HighBassImageColor),
        typeof(Color),
        typeof(BassImageVizualizer),
        new PropertyMetadata(
           Color.FromRgb(255, 0, 0),
          ReloadImage));


    #endregion

    private static void ReloadImage(DependencyObject d,
     DependencyPropertyChangedEventArgs e)
    {
      var visualizer =
      (BassImageVizualizer)d;

      visualizer.LoadPsychedelicImage();
      visualizer.LoadStableImage();
    }

    public bool Stablized { get; set; } = false;
    public float BassSensitivity { get; set; } = 1.35f;
    public float BassCurve { get; set; } = 0.20f;
    public float BassOutputGate { get; set; } = 0.06f;
    public float BassPeakDecay { get; set; } = 0.997f;
    public float ZoomStrength { get; set; } = 0.05f;
    public float ShakeStrength { get; set; } = 8f;

    public float OpacityModifier { get; set; } = 1;

    public BassImageVizualizer()
    {
      InitializeComponent();

      SpektrumAnalyzer.OnFFtTick += SpektrumAnalyzer_OnFFtTick;
    }

    private void SpektrumAnalyzer_OnFFtTick(object sender, float[] fftData)
    {
     
      VSynchronizationContext.PostOnUIThread(async () =>
      {
        if (!IsEnabled) return;
        if (Visibility != Visibility.Visible) return;

        float bass = SpektrumAnalyzer.AnalyzeBass(fftData, BassSensitivity, BassOutputGate, BassPeakDecay, BassCurve);

        float smoothing = bass > visualBass ? 0.50f : 0.14f;

        visualBass += (bass - visualBass) * smoothing;


        ApplyPsychedelicImageEffect(visualBass);
      }
       );
    }

    #region LoadStableImage

    private void LoadStableImage()
    {
      if (string.IsNullOrWhiteSpace(StableVizualizerLayerPath))
      {
        StableImage.Source = null;
        return;
      }

      var bitmap = new BitmapImage();

      bitmap.BeginInit();
      bitmap.UriSource = new Uri(StableVizualizerLayerPath, UriKind.RelativeOrAbsolute);
      bitmap.CacheOption = BitmapCacheOption.OnLoad;
      bitmap.EndInit();
      bitmap.Freeze();

      StableImage.Source = bitmap;
      StableImage.Stretch = Stretch;
    }

    #endregion

    #region LoadPsychedelicImage

    private void LoadPsychedelicImage()
    {
      string imageSource = VizualizerLayerPath;

      if (string.IsNullOrEmpty(imageSource))
        imageSource = ImagePath;

      if (string.IsNullOrWhiteSpace(imageSource))
      {
        originalImage = null;

        RedImage.Source = null;
        GreenImage.Source = null;
        BlueImage.Source = null;
        OriginalImage.Source = null;

        return;
      }

      try
      {
        var bitmap = new BitmapImage();

        bitmap.BeginInit();
        bitmap.UriSource = new Uri(imageSource, UriKind.RelativeOrAbsolute);
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.EndInit();
        bitmap.Freeze();

        originalImage = bitmap;

        OriginalImage.Source = originalImage;

        RedImage.Source = CreateTintedBitmap(originalImage, HighBassImageColor);
        GreenImage.Source = CreateTintedBitmap(originalImage, MidBassImageColor);
        BlueImage.Source = CreateTintedBitmap(originalImage, LowBassImageColor);


        OriginalImage.Stretch = Stretch;
        GreenImage.Stretch = Stretch;
        BlueImage.Stretch = Stretch;
        RedImage.Stretch = Stretch;
      }
      catch (Exception ex)
      {
        Debug.WriteLine(
          $"Could not load visualizer image: {ex}");

        originalImage = null;

        RedImage.Source = null;
        GreenImage.Source = null;
        BlueImage.Source = null;
        OriginalImage.Source = null;
      }
    }

    #endregion

    #region CreateTintedBitmap

    private static BitmapSource CreateTintedBitmap(BitmapSource source, Color tintColor)
    {
      BitmapSource formattedSource = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

      int width = formattedSource.PixelWidth;
      int height = formattedSource.PixelHeight;
      int stride = width * 4;
      byte[] pixels = new byte[height * stride];

      formattedSource.CopyPixels(pixels, stride, 0);

      for (int i = 0; i < pixels.Length; i += 4)
      {
        byte b = pixels[i];
        byte g = pixels[i + 1];
        byte r = pixels[i + 2];
        byte a = pixels[i + 3];
        byte brightness = (byte)(r * 0.299 + g * 0.587 + b * 0.114);
        pixels[i] = (byte)(brightness * tintColor.B / 255);
        pixels[i + 1] = (byte)(brightness * tintColor.G / 255);
        pixels[i + 2] = (byte)(brightness * tintColor.R / 255);
        pixels[i + 3] = a;
      }

      var tintedBitmap = new WriteableBitmap(width, height, formattedSource.DpiX, formattedSource.DpiY, PixelFormats.Bgra32, null);
      tintedBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
      tintedBitmap.Freeze();

      return tintedBitmap;
    }

    #endregion

    #region ApplyPsychedelicImageEffect

    private void ApplyPsychedelicImageEffect(float bass)
    {
      bass = SpektrumAnalyzer.Clamp(bass);

      double redOpacity;
      double greenOpacity;
      double blueOpacity;
      if (bass <= 0.5f)
      {
        float t = bass / 0.5f;
        blueOpacity = 1.0 - t;
        greenOpacity = t;
        redOpacity = 0.0;
      }
      else
      {
        float t = (bass - 0.5f) / 0.5f;
        blueOpacity = 0.0;
        greenOpacity = 1.0 - t;
        redOpacity = t;
      }

      RedImage.Opacity = redOpacity * OpacityModifier;
      GreenImage.Opacity = greenOpacity * OpacityModifier;
      BlueImage.Opacity = blueOpacity * OpacityModifier;

      if (!Stablized)
      {
        double offset = 4.0 + bass * ShakeStrength;
        RedImage.Margin = new Thickness(offset, 0, -offset, 0);
        GreenImage.Margin = new Thickness(0, -offset * 0.5, 0, offset * 0.5);
        BlueImage.Margin = new Thickness(-offset, 0, offset, 0);

        double scale = 1.0 + bass * ZoomStrength;
        BassImageScale.ScaleX = scale;
        BassImageScale.ScaleY = scale;
      }
    }

    #endregion
  }
}

using System;
using System.Windows;
using VPlayer.AudioStorage.Interfaces.Storage;

namespace VPlayer.Views
{
  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private readonly IStorageManager storageManager;

    public MainWindow(IStorageManager storageManager)
    {
      MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
      MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));
      InitializeComponent();
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
      await storageManager.ClearStorage();
    }
  }
}
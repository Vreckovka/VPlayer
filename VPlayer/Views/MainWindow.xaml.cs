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

    public MainWindow()
    {
      MaxHeight = SystemParameters.MaximizedPrimaryScreenHeight;
      MaxWidth = SystemParameters.MaximizedPrimaryScreenWidth;
      InitializeComponent();
    }

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
    }
  }
}
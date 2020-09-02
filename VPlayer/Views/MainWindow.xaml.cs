using System;
using System.ComponentModel;
using System.Windows;
using VPlayer.AudioStorage.Interfaces.Storage;
using VPlayer.ViewModels;

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


    private void OnClosing(object sender, CancelEventArgs e)
    {
      (DataContext as MainWindowViewModel)?.Dispose();
    }
  }
}
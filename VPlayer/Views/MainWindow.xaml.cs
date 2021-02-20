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
    private readonly IStorageManager storageManager;

    public MainWindow(IStorageManager storageManager)
    {
      this.storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));

      InitializeComponent();
    }


    private void OnClosing(object sender, CancelEventArgs e)
    {
      (DataContext as MainWindowViewModel)?.Dispose();
    }

    private void Button_Click(object sender, RoutedEventArgs e)
    {
      storageManager.ClearStorage();
    }
  }
}
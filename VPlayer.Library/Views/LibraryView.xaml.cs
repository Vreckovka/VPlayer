using System;
using System.Windows.Controls;
using Prism.Events;
using VCore.Modularity.Interfaces;
using VPlayer.AudioStorage.Models;
using VPlayer.Library.ViewModels;

namespace VPlayer.Library.Views
{
    /// <summary>
    /// Interaction logic for LibraryView.xaml
    /// </summary>
    public partial class LibraryView : UserControl, IView
    {
      public LibraryView()
      {
        InitializeComponent();
      } 
    }
}

﻿using System.Windows.Controls;
using VCore.Standard.Modularity.Interfaces;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Home.Views.Music.Albums
{
  /// <summary>
  /// Interaction logic for AlbumDetailView.xaml
  /// </summary>
  public partial class AlbumDetailView : UserControl, IView
  {
    #region Constructors

    public AlbumDetailView(Album album)
    {
      InitializeComponent();
    }

    #endregion Constructors
  }
}
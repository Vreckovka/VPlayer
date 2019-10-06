using System.Windows.Controls;
using VCore.Modularity.Interfaces;
using VPlayer.AudioStorage.DomainClasses;

namespace VPlayer.Library.Views
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
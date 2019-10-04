using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Prism.Regions;

namespace VCore.Design
{
  public class DesignTimeRegionProvider
  {
    #region ViewProviderProperty

    public static DesignTimeViewsProvider GetViewProvider(DependencyObject obj)
    {
      return (DesignTimeViewsProvider)obj.GetValue(ViewProviderProperty);
    }

    public static void SetViewProvider(DependencyObject obj, DesignTimeViewsProvider value)
    {
      obj.SetValue(ViewProviderProperty, value);
    }

    public static readonly DependencyProperty ViewProviderProperty =
      DependencyProperty.RegisterAttached("ViewProvider", typeof(DesignTimeViewsProvider), typeof(DesignTimeRegionProvider),
        new PropertyMetadata(null, OnViewProviderChanged));

    #endregion

    #region OnViewProviderChanged

    private static void OnViewProviderChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
    {
      if (DesignerProperties.GetIsInDesignMode(sender) == false) return;

      var viewProvider = e.NewValue as DesignTimeViewsProvider;

      if (viewProvider == null) return;

      var regionName = RegionManager.GetRegionName(sender);

      var designTimeViews = viewProvider.GetViewForRegion(regionName);

      if (sender is ContentControl)
      {
        (sender as ContentControl).Content = designTimeViews.FirstOrDefault();
      }
      else if (sender is Selector)
      {
        var selector = sender as Selector;
        selector.ItemsSource = designTimeViews;
        selector.SelectedItem = designTimeViews.FirstOrDefault();
      }
      else if (sender is ItemsControl)
      {
        (sender as ItemsControl).ItemsSource = designTimeViews;
      }
    }

    #endregion
  }
}

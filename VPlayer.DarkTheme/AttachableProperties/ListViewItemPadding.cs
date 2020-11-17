using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VCore.Helpers;

namespace TradingBroker.AttachableProperties
{
  public class ListViewAttachables 
  {
    public static readonly DependencyProperty ListViewItemPaddingProperty =
      DependencyProperty.RegisterAttached("ListViewItemPadding", typeof(Thickness), typeof(ListViewAttachables),
        new PropertyMetadata(new Thickness(0), new PropertyChangedCallback((x, y) =>
        {
          if (x is ListView listView)
          {
            if (!listView.IsLoaded)
            {
              listView.Loaded += ListView_Loaded;
              return;
            }

            var childs = listView.FindChildrenOfType<ListViewItem>();

            foreach (var child in childs)
            {
              child.Padding = (Thickness) y.NewValue;
            }
          }
        })));

    private static void ListView_Loaded(object sender, RoutedEventArgs e)
    {
      if (sender is ListView listView)
      {
        var childs = listView.FindChildrenOfType<ListViewItem>();

        foreach (var child in childs)
        {
          child.Padding = GetListViewItemPadding(listView);
        }

        listView.Loaded -= ListView_Loaded;
      }
    }

    public static Thickness GetListViewItemPadding(
      DependencyObject d)
    {
      return (Thickness)d.GetValue(ListViewItemPaddingProperty);
    }

    public static void SetListViewItemPadding(DependencyObject d, Thickness value)
    {
      d.SetValue(ListViewItemPaddingProperty, value);
    }

   
  }

}

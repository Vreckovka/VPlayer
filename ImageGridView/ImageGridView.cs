using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ImageGridView
{
    public class ImageGridView : ListView
    {
        public List<GridViewItem> GridViewItems { get; set; }

        //public double? TitleFontSize
        //{
        //    get { return GetValue(TitleFontSizeProperty) as double?; }
        //    set { SetValue(TitleFontSizeProperty, value); }
        //}

        //private static readonly DependencyProperty TitleFontSizeProperty =
        //    DependencyProperty.Register("TitleFontSizeProperty", typeof(double), typeof(ImageGridView), new FrameworkPropertyMetadata(12.0));


        //public double? SubTitleFontSize
        //{
        //    get { return GetValue(SubTitleFontSizeProperty) as double?; }
        //    set { SetValue(SubTitleFontSizeProperty, value); }
        //}

        //private static readonly DependencyProperty SubTitleFontSizeProperty =
        //    DependencyProperty.Register("SubTitleFontSizeProperty", typeof(double), typeof(ImageGridView), new FrameworkPropertyMetadata(15.0));



        public double? ItemHeight
        {
            get { return GetValue(ItemHeightProperty) as double?; }
            set { SetValue(ItemHeightProperty, value); }
        }

        private static readonly DependencyProperty ItemHeightProperty =
            DependencyProperty.Register("ItemHeightProperty", typeof(double), typeof(ImageGridView), new FrameworkPropertyMetadata(double.NaN));

        public double? ItemWitdth
        {
            get { return GetValue(ItemWitdthProperty) as double?; }
            set { SetValue(ItemWitdthProperty, value); }
        }

        private static readonly DependencyProperty ItemWitdthProperty =
            DependencyProperty.Register("ItemWitdthProperty", typeof(double), typeof(ImageGridView), new FrameworkPropertyMetadata(double.NaN));


        public string Title
        {
            get { return GetValue(TitleProperty) as string; }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(ImageGridView), new FrameworkPropertyMetadata(""));

        public string SubTitle
        {
            get { return GetValue(SubTitleProperty) as string; }
            set { SetValue(SubTitleProperty, value); }
        }

        public static readonly DependencyProperty SubTitleProperty =
            DependencyProperty.Register("SubTitle", typeof(string), typeof(ImageGridView), new FrameworkPropertyMetadata(""));


        public byte[] Image
        {
            get { return GetValue(ImageProperty) as byte[]; }
            set { SetValue(ImageProperty, value); }
        }

        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.Register("ImageProperty", typeof(byte[]), typeof(ImageGridView), new FrameworkPropertyMetadata(null));

        public ImageGridView()
        {
            var myResourceDictionary = new ResourceDictionary();
            myResourceDictionary.Source = new Uri("/ImageGridView;component/Resources/Style.xaml", UriKind.RelativeOrAbsolute);

            Style = myResourceDictionary["AlbumsPlayGridView"] as Style;

            Loaded += ImageGridView_Loaded;

        }

        private void ImageGridView_Loaded(object sender, RoutedEventArgs e)
        {
            GridViewItems = new List<GridViewItem>();

            BindingExpression titleExpression = GetBindingExpression(ImageGridView.TitleProperty);
            var titleBiding = titleExpression.ResolvedSourcePropertyName;

            BindingExpression subTitleExpression = GetBindingExpression(ImageGridView.SubTitleProperty);
            var subTitleBiding = titleExpression.ResolvedSourcePropertyName;

            foreach (var item in Items)
            {
                GridViewItems.Add(new GridViewItem()
                {
                    Title = item.GetType().GetProperty(titleBiding)?.GetValue(item, null).ToString(),
                    SubTitle = item.GetType().GetProperty(subTitleBiding)?.GetValue(item, null).ToString(),
                    Height = ItemHeight,
                    Width = ItemWitdth,
                });
            }

            ItemsSource = null;
            ItemsSource = GridViewItems;

        }
    }

    public class GridViewItem
    {
        public string Title { get; set; }
        public string SubTitle { get; set; }
        public byte[] Image { get; set; }

        public double? Width { get; set; }
        public double? Height { get; set; }

    }
}

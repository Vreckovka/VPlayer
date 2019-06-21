using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Media.Imaging;
using VPlayer.Models;
using VPlayer.ViewModels;

namespace VPlayer.Other.Converters
{
    public class ImageLazyLoadingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                BitmapImage image = new BitmapImage();
               
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri((value.ToString()), UriKind.Absolute);
                image.DecodePixelWidth = 1920; // should be enough, but you can experiment
                image.EndInit();
                return image;
            }
            else
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ActualTimeConverter : MarkupExtension, IMultiValueConverter
    {
        TimeSpan actualTrackDuration;
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] != DependencyProperty.UnsetValue &&
                values[1] != DependencyProperty.UnsetValue)
            {
                //If value changed based on Time
                if (values[0] is TimeSpan)
                {
                    actualTrackDuration = (TimeSpan) values[1];
                    return (100 * ((TimeSpan)values[0]).TotalMilliseconds) / ((TimeSpan)values[1]).TotalMilliseconds;

                }
                else
                {
                    //if value changed based on slider value
                    double valueInMilli = ((double)values[0] * ((TimeSpan)values[1]).TotalMilliseconds) / 100;
                    return TimeSpan.FromMilliseconds(valueInMilli).ToString("hh\\:mm\\:ss");
                }
            }
            else
                return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            if (actualTrackDuration != null)
            {
                double valueInMilli = ((double) value * actualTrackDuration.TotalMilliseconds) / 100;
                PlayerHandler.OnChangeTime(valueInMilli);
            }

            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}

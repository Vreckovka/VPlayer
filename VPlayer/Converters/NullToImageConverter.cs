using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using VPlayer.Core.Controls;

namespace VPlayer.Converters
{
    public class NullToImageConverter : BaseConverter
    {
        #region DefaultImage

        public ImageSource DefaultImage
        {
            get { return (ImageSource)GetValue(DefaultImageProperty); }
            set { SetValue(DefaultImageProperty, value); }
        }

        public static readonly DependencyProperty DefaultImageProperty =
            DependencyProperty.Register(
                nameof(DefaultImage),
                typeof(ImageSource),
                typeof(NullToImageConverter),
                new PropertyMetadata(null));


        #endregion

        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (DefaultImage == null)
            {
                throw new ArgumentNullException("Default image is null");
            }

            if (value == null)
            {
                return DefaultImage;
            }

            return value;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

// Inspired by https://blogs.msdn.microsoft.com/dditweb/2007/08/22/speeding-up-image-loading-in-wpf-using-thumbnails/
namespace UI
{
    class ThumbnailConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!File.Exists(value.ToString()))
                return null;

            try
            {
                BitmapImage image = new BitmapImage();
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                image.CacheOption = BitmapCacheOption.OnLoad; // Does this line still matter after the previous line?
                image.UriSource = new Uri(value.ToString());
                image.EndInit();
                return image;
            }
            catch (FileNotFoundException)
            {
                return null;
            }
            catch (NotSupportedException)
            {
                return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace GoLine
{
    public class ImagePathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || string.IsNullOrEmpty(value.ToString())) return null;
            Uri uri = null;
            if(Uri.TryCreate(value.ToString(), UriKind.RelativeOrAbsolute, out uri))
            {
                //if (uri.IsAbsoluteUri && !System.IO.File.Exists(uri.AbsolutePath)) return null;
                ImageBrush ib = new ImageBrush();
                ib.ImageSource = new BitmapImage(uri);
                return ib;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

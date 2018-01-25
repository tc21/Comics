using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;

namespace UI
{
    class RemainingWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ListView listView = value as ListView;
            GridView gridView = listView.View as GridView;

            double total = 0;
            for (int i = 0; i < gridView.Columns.Count - 1; i++)
                total += gridView.Columns[i].Width;

            return (listView.ActualWidth - total);

        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

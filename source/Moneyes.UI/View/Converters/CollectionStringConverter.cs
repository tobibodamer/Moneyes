using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Moneyes.UI.View
{
    public class CollectionStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable collection)
            {
                return string.Join(",", collection.Cast<object>().Select(item => item.ToString()));
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s) { return Binding.DoNothing; }

            return s.Split(",", 
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Cast<object>().ToList();
        }
    }
}

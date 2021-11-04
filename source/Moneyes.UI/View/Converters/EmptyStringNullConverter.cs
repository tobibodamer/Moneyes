using System;
using System.Globalization;
using System.Windows.Data;

namespace Moneyes.UI.View
{
    internal class EmptyStringNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string s || !string.IsNullOrEmpty(s))
            {
                return value;
            }

            return null;
        }
    }
}

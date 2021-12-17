using System;
using System.Globalization;
using System.Windows.Data;

namespace Moneyes.UI.View
{
    public class AlternativeValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            foreach (var value in values)
            {
                if (value is not null)
                {
                    return value;
                }
            }

            return null;
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

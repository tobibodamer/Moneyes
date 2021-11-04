using System;
using System.Collections;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace Moneyes.UI.View
{
    class EmptyItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var res = new CompositeCollection();

            if (value is IEnumerable && value != null)

                res.Add(new CollectionContainer()
                {
                    Collection = value as IEnumerable
                });

            res.Add(new ComboBoxItem() { Content = parameter });

            return res;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

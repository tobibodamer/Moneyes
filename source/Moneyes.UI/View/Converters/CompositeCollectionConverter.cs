using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace Moneyes.UI.View
{
    class CompositeCollectionConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var res = new CompositeCollection();
            foreach (var item in values)
                if (item is IEnumerable && item != null)
                    res.Add(new CollectionContainer()
                    {
                        Collection = item as IEnumerable
                    });
            return res;
        }
        public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

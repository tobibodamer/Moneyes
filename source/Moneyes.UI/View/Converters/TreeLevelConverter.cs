using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace Moneyes.UI.View
{
    public class TreeLevelConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var level = -1;
            if (value is DependencyObject)
            {
                var parent = VisualTreeHelper.GetParent(value as DependencyObject);
                while (!(parent is TreeView) && (parent != null))
                {
                    if (parent is TreeViewItem)
                        level++;
                    parent = VisualTreeHelper.GetParent(parent);
                }
            }
            return (parameter?.ToString() ?? "") + ((char)('A' + level)); // the group name has to be a letter, numbers didn't work
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new System.NotImplementedException();
        }
    }
}

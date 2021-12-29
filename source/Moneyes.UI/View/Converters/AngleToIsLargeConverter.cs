﻿using LiveCharts.Configurations;
using System;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Moneyes.UI.View
{
    public class AngleToIsLargeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double angle)
            {
                return angle > 180;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

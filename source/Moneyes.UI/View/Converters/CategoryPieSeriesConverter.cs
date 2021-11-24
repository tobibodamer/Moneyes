using LiveCharts;
using LiveCharts.Wpf;
using Moneyes.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Moneyes.UI.View
{
    public class CategoryPieSeriesConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is IEnumerable<CategoryExpenseViewModel> collection)
            {
                //var mapper = Mappers.Pie<CategoryExpenseViewModel>().Value(c => (double)c.TotalExpense);

                SeriesCollection col = new SeriesCollection();

                var totalExpense = collection
                    //.Where(c => !c.IsNoCategory)
                    .Sum(c => c.TotalExpense);

                if (totalExpense == 0)
                {
                    return null;
                }

                var biggestExpenses = collection
                        .Where(c => (c.TotalExpense / totalExpense) > 0.02m);
                        //.Where(c => !c.IsNoCategory);

                var restAmount = totalExpense - biggestExpenses.Sum(c => c.TotalExpense);

                col.AddRange(
                    biggestExpenses
                        .OrderByDescending(c => c.TotalExpense)
                        .Select(v => new PieSeries()
                        {
                            Values = new ChartValues<double>(new double[] { (double)v.TotalExpense }),
                            Title = v.Name,
                            DataLabels = true,
                            LabelPosition = (v.TotalExpense / totalExpense) > 0.11m ? PieLabelPosition.InsideSlice :
                                                PieLabelPosition.OutsideSlice,
                            Foreground = (v.TotalExpense / totalExpense) > 0.11m ? new SolidColorBrush(Colors.White) :
                                new SolidColorBrush(Colors.Black),
                            LabelPoint = p => v.Name + $"\n{Math.Round(p.Participation * 100)} %"
                        })
                        .Concat(new PieSeries[] {
                            new PieSeries() {
                                Values = new ChartValues<double>(new double[] { (double)restAmount }),
                                Title = "Other",
                                DataLabels = true,
                                Foreground = (restAmount / totalExpense) > 0.11m ? new SolidColorBrush(Colors.White) :
                                     new SolidColorBrush(Colors.Black),
                                LabelPosition = (restAmount / totalExpense) > 0.11m ? PieLabelPosition.InsideSlice :
                                                PieLabelPosition.OutsideSlice,
                                LabelPoint = p => "Other" + $"\n{Math.Round(p.Participation * 100)} %"
                            } }));

                return col;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

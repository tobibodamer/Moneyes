using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using Moneyes.UI.ViewModels;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

                List<ISeries> col = new();

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

                decimal calcPercentage(decimal value)
                {
                    return Math.Round((value / totalExpense) * 100);
                }

                col.AddRange(
                    biggestExpenses
                        .OrderByDescending(c => c.TotalExpense)
                        .Select(v => new PieSeries<decimal>()
                        {
                            HoverPushout = 5,
                            Values = new ObservableCollection<decimal>(new decimal[] { v.TotalExpense }),
                            Name = v.Name,
                            DataLabelsPosition = PolarLabelsPosition.Middle,
                            //DataLabelsPaint = new SolidColorPaint(SKColors.White),
                            DataLabelsSize = 10,
                            DataLabelsFormatter = p => $"{calcPercentage(p.Model)} %",
                            TooltipLabelFormatter = p => v.Name + $" {Math.Round(p.Model)} € ({calcPercentage(p.Model)} %)"
                        })
                        .Concat(new PieSeries<decimal>[] {
                            new PieSeries<decimal>() {
                                HoverPushout = 5,
                                Values = new ObservableCollection<decimal>(new decimal[] { restAmount }),
                                Name = "Other",
                                //DataLabelsPaint = new SolidColorPaint(SKColors.White),
                                DataLabelsSize = 10,
                                DataLabelsPosition = PolarLabelsPosition.Middle,
                                DataLabelsFormatter = p => "Other" + $" {calcPercentage(p.Model)} %",
                                TooltipLabelFormatter = p => "Other" + $" {Math.Round(p.Model)} € ({calcPercentage(p.Model)} %)"
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

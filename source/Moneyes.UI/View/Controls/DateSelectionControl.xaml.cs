using Moneyes.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Moneyes.UI.View
{
    /// <summary>
    /// Interaktionslogik für DateSelectionControl.xaml
    /// </summary>
    public partial class DateSelectionControl : UserControl
    {
        private DateTime? _customStartDate;
        private DateTime? _customEndDate;
        private DateTime? _monthDate;

        public DateSelectionControl()
        {
            InitializeComponent();

            DecrementCommand = new RelayCommand(() =>
            {
                IncrementDate(-1);

                IncrementCommand?.RaiseCanExecuteChanged();
            });

            IncrementCommand = new RelayCommand(() =>
            {
                IncrementDate(1);

                DecrementCommand?.RaiseCanExecuteChanged();
            }, () =>
            {
                switch (SelectionMode)
                {
                    case DateSelectionMode.Year:
                        DateTime nextYear = StartDate.AddYears(1);

                        return nextYear.Year <= DateTime.Now.Year;
                    case DateSelectionMode.Month:
                        DateTime nextMonth = StartDate.AddMonths(1);

                        return nextMonth.Year < DateTime.Now.Year
                            || (nextMonth.Year == DateTime.Now.Year && nextMonth.Month <= DateTime.Now.Month);
                    default:
                        return false;
                };
            });
        }

        private void IncrementDate(int amount)
        {
            switch (SelectionMode)
            {
                case DateSelectionMode.Year:
                    UpdateDateRange(StartDate.AddYears(amount));
                    break;
                case DateSelectionMode.Month:
                    UpdateDateRange(StartDate.AddMonths(amount));
                    break;
            }
        }

        private void RestoreDateRange()
        {
            switch (SelectionMode)
            {
                case DateSelectionMode.Year:
                    UpdateDateRange(StartDate);
                    // TODO
                    break;
                case DateSelectionMode.Month:
                    UpdateDateRange(_monthDate ?? StartDate);
                    break;
                case DateSelectionMode.Custom:
                    //if (_customStartDate.HasValue)
                    //{
                    //    StartDate = _customStartDate.Value;
                    //}

                    //if (_customEndDate.HasValue)
                    //{
                    //    EndDate = _customEndDate.Value;
                    //}
                    break;
            }
        }
        private void UpdateDateRange(DateTime date)
        {
            switch (SelectionMode)
            {
                case DateSelectionMode.Year:
                    StartDate = GetFirstDayOfYear(date);
                    EndDate = GetLastDayOfYear(date);
                    break;
                case DateSelectionMode.Month:
                    StartDate = GetFirstDayOfMonth(date);
                    EndDate = GetLastDayOfMonth(date);
                    break;
            }

            ApplyDateCommand?.Execute(null);
        }

        private static DateTime GetFirstDayOfMonth(DateTime date)
        {
            return new(date.Year, date.Month, 1);
        }
        private static DateTime GetLastDayOfMonth(DateTime date)
        {
            if (date.Year == DateTime.Now.Year && date.Month == DateTime.Now.Month)
            {
                return DateTime.Now;
            }
            else
            {
                int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);

                return new(date.Year, date.Month, daysInMonth);
            }
        }

        private static DateTime GetFirstDayOfYear(DateTime date)
        {
            return new(date.Year, 1, 1);
        }

        private static DateTime GetLastDayOfYear(DateTime date)
        {
            return new DateTime(date.Year, 1, 1).AddYears(1).AddDays(-1);
        }
        private RelayCommand DecrementCommand
        {
            get { return (RelayCommand)GetValue(DecrementCommandProperty); }
            set { SetValue(DecrementCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DecrementMonthCommand.  This enables animation, styling, binding, etc...
        private static readonly DependencyProperty DecrementCommandProperty =
            DependencyProperty.Register("DecrementCommand", typeof(RelayCommand), typeof(DateSelectionControl));


        private RelayCommand IncrementCommand
        {
            get { return (RelayCommand)GetValue(IncrementCommandProperty); }
            set { SetValue(IncrementCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DecrementMonthCommand.  This enables animation, styling, binding, etc...
        private static readonly DependencyProperty IncrementCommandProperty =
            DependencyProperty.Register("IncrementCommand", typeof(RelayCommand), typeof(DateSelectionControl));


        public DateTime StartDate
        {
            get { return (DateTime)GetValue(StartDateProperty); }
            set { SetValue(StartDateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StartDate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StartDateProperty =
            DependencyProperty.Register("StartDate", typeof(DateTime), typeof(DateSelectionControl),
                new FrameworkPropertyMetadata(new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1),
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public DateTime EndDate
        {
            get { return (DateTime)GetValue(EndDateProperty); }
            set { SetValue(EndDateProperty, value); }
        }

        // Using a DependencyProperty as the backing store for StartDate.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty EndDateProperty =
            DependencyProperty.Register("EndDate", typeof(DateTime), typeof(DateSelectionControl),
                new FrameworkPropertyMetadata(DateTime.Now,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public ICommand ApplyDateCommand
        {
            get { return (ICommand)GetValue(ApplyDateCommandProperty); }
            set { SetValue(ApplyDateCommandProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DecrementMonthCommand.  This enables animation, styling, binding, etc...
        private static readonly DependencyProperty ApplyDateCommandProperty =
            DependencyProperty.Register("ApplyDateCommand", typeof(ICommand), typeof(DateSelectionControl));

        public DateSelectionMode SelectionMode
        {
            get { return (DateSelectionMode)GetValue(SelectionModeProperty); }
            set { SetValue(SelectionModeProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectionMode.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectionModeProperty =
            DependencyProperty.Register("SelectionMode", typeof(DateSelectionMode), typeof(DateSelectionControl),
                new FrameworkPropertyMetadata(DateSelectionMode.Month,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    new PropertyChangedCallback(SelectionModeChanged)));

        private static void SelectionModeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            if (obj is not DateSelectionControl dateSelectionControl)
            {
                return;
            }

            if (e.OldValue is DateSelectionMode selectionMode)
            {
                if (selectionMode is DateSelectionMode.Custom)
                {
                    dateSelectionControl._customStartDate = dateSelectionControl.StartDate;
                    dateSelectionControl._customEndDate = dateSelectionControl.EndDate;
                }
                else if (selectionMode is DateSelectionMode.Month)
                {
                    dateSelectionControl._monthDate = dateSelectionControl.StartDate;
                }
            }

            dateSelectionControl.RestoreDateRange();
        }
    }

    public enum DateSelectionMode
    {
        Year,
        Month,
        Custom
    }
}

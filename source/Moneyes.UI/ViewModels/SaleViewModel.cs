using Moneyes.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI.ViewModels
{
    public class SaleViewModel : ViewModelBase
    {
        ISale _sale;
        string _categoryNames;
        public SaleViewModel(ISale sale, IEnumerable<Category> categories)
        {
            _sale = sale;
            _categoryNames = string.Join(", ", categories.Select(c => c.Name));
        }

        public string Name => _sale.Name;
        public string IntendedUse => _sale.IntendedUse;
        public string Amount => _sale.Amount.ToString();
        public DateTime PaymentDate => _sale.PaymentDate;
        public TransactionType SaleType => _sale.SaleType;

        public string CategoryNames {
            get => _categoryNames;
            set
            {
                _categoryNames = value;
                OnPropertyChanged();
            }
        }
    }

    public class SalesViewModel : ViewModelBase
    {
        ObservableCollection<SaleViewModel> _sales = new();
        public ObservableCollection<SaleViewModel> Sales {
            get => _sales; set
            {
                _sales = value;
                OnPropertyChanged();
            }
        }

        public SalesViewModel(IEnumerable<(ISale, List<Category>)> salesAndCategories)
        {
            Sales = new(salesAndCategories.Select(tuple =>
                new SaleViewModel(tuple.Item1, tuple.Item2)));
        }
    }
}

//using Moneyes.Core;
//using Moneyes.UI.Blazor.Stores;
//using Reactives;
//using ReactiveUI;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Reactive.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace Moneyes.UI.Blazor.Shared
//{
//    internal class TransactionsTabViewModel : ReactiveObject
//    {
//        private readonly TransactionStore _transactionStore;

//        private ObservableAsPropertyHelper<Guid?> _selectedCategory;
//        public Guid? SelectedCategory
//        {
//            get => _selectedCategory.Value;
//            set => _transactionStore.SetSelectedCategory(value);
//        }

//        ObservableAsPropertyHelper<IEnumerable<Transaction>> _transactions;
//        public IEnumerable<Transaction> Transactions => _transactions.Value;

//        public TransactionsTabViewModel(TransactionStore transactionStore)
//        {
//            _selectedCategory = _transactionStore.SelectedCategoryIdO
//                .ToProperty(this, x => x.SelectedCategory);
//            //.ToRef(initialValue: _transactionStore.GetSelectedCategory(),
//            //       setter: _transactionStore.SetSelectedCategory);

//            _transactions = _transactionStore.TransactionsO.Select(x => x.ToList()).ToProperty(this, x => x.Transactions);
//                                //.ToRef(initialValue: TransactionStore.GetTransactions().ToList());

//            //_categories = TransactionStore.ExpensesPerCategoryO
//            //    .Select(ToTreeItems)
//            //    .ToRef(ToTreeItems(TransactionStore.GetExpensesPerCategory()));

//            _categories = TransactionStore.CategoryTreeItemsO
//                .ToRef(TransactionStore.GetCategories());
//            _transactionStore = transactionStore;
//        }



//    }
//}

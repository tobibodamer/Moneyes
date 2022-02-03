using Moneyes.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Moneyes.UI.ViewModels.Dialogs
{
    public class CreateFilterViewModel : ViewModelBase, IDialogViewModel
    {
        public ICommand ApplyCommand { get; set; }
        public ICommand CancelCommand { get; set; }

        public event EventHandler<RequestCloseDialogEventArgs> RequestClose;

        public List<string> NameSuggestions { get; } = new();
        public List<string> PurposeSuggestions { get; } = new();

        public ObservableCollection<KeywordViewModel> Keywords { get; } = new();

        public ICommand AddKeywordCommand { get; }

        public CreateFilterViewModel(Transaction transaction)
        {
            ApplyCommand = new RelayCommand(() =>
            {
                RequestClose?.Invoke(this, new() { Result = true });
            });

            CancelCommand = new AsyncCommand(async ct =>
            {
                RequestClose?.Invoke(this, new() { Result = false });
            });

            var nameSplit = transaction.Name.Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();
            nameSplit.Insert(0, transaction.Name);
            nameSplit = nameSplit.Distinct().ToList();

            NameSuggestions = new(nameSplit);

            var purposeSplit = transaction.Purpose
                .Replace(",", "")
                .Split(" ", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList();

            PurposeSuggestions = purposeSplit;

            PurposeSuggestions.RemoveAll(x => x.Length < 2);
            PurposeSuggestions.RemoveAll(x => DateTime.TryParse(x, out _));
            PurposeSuggestions = PurposeSuggestions.OrderBy(x => x.Length < 3).ToList();

            AddKeywordCommand = new RelayCommand<string>((keyword) =>
            {
                Keywords.Add(new KeywordViewModel(keyword, x => Keywords.Remove(x)));
            },
            (keyword) => !Keywords.Any(x => x.Value.Equals(keyword)));
        }

        public class KeywordViewModel : ViewModelBase
        {
            public string Value { get; }

            public ICommand DeleteCommand { get; }

            public KeywordViewModel(string value, Action<KeywordViewModel> onDelete)
            {
                Value = value;
                DeleteCommand = new RelayCommand(() => onDelete(this));
            }
        }
    }
}

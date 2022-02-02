using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Moneyes.UI.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set
            {
                _displayName = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }
    }
}

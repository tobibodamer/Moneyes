using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Moneyes.UI.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public string DisplayName { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new(propertyName));
        }
    }
}

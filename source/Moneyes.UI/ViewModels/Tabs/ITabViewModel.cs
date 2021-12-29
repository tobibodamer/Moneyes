namespace Moneyes.UI.ViewModels
{
    public interface ITabViewModel
    {
        bool IsActive { get; set; }
        void OnSelect();
        void OnDeselect();
    }
}

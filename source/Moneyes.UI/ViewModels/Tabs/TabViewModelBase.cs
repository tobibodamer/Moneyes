namespace Moneyes.UI.ViewModels
{
    public class TabViewModelBase : ViewModelBase, ITabViewModel
    {
        protected bool NeedsUpdate { get; set; }
        public bool IsActive { get; set; }

        public virtual void OnDeselect()
        {
            IsActive = false;
        }

        public virtual void OnSelect()
        {
            IsActive = true;
        }

        /// <summary>
        /// Sets the <see cref="NeedsUpdate"/> to <see langword="true"/>, if the tab is inactive.
        /// </summary>
        /// <returns><see langword="true"/> if the update needs to be postponed.</returns>
        protected bool PostponeUpdate()
        {
            if (!IsActive)
            {
                NeedsUpdate = true;
                return true;
            }

            return false;
        }
    }
}

using Moneyes.Core;
using Moneyes.UI.Services;
using Moneyes.UI.ViewModels;
using System;
using System.Security;

namespace Moneyes.UI
{


    public partial class App
    {
        internal class MasterPasswordProvider
        {
            private readonly IDialogService<InitMasterPasswordDialogViewModel> _initDialogService;
            private readonly IDialogService<GetMasterPasswordDialogViewModel> _getDialogService;

            private readonly Func<InitMasterPasswordDialogViewModel> _createInitViewModel;
            private readonly Func<GetMasterPasswordDialogViewModel> _createGetViewModel;

            public SecureString CachedMasterPassword { get; private set; }

            public MasterPasswordProvider(
                IDialogService<InitMasterPasswordDialogViewModel> initDialogService,
                IDialogService<GetMasterPasswordDialogViewModel> getDialogService,
                Func<InitMasterPasswordDialogViewModel> createInitViewModel,
                Func<GetMasterPasswordDialogViewModel> createGetViewModel)
            {
                _initDialogService = initDialogService;
                _getDialogService = getDialogService;
                _createInitViewModel = createInitViewModel;
                _createGetViewModel = createGetViewModel;
            }

            /// <summary>
            /// Creates a new master password.
            /// </summary>
            /// <returns>Returns the new master password, 
            /// or <see langword="null"/> if not password returned.</returns>
            public SecureString CreateMasterPassword()
            {
                var passwordDialogVM = _createInitViewModel();
                var newMasterPasswordDialogService = _initDialogService;

                var dialogResult = newMasterPasswordDialogService.ShowDialog(passwordDialogVM);

                if (dialogResult == DialogResult.OK)
                {
                    CachedMasterPassword = passwordDialogVM.Password;

                    return passwordDialogVM.Password;
                }
                else if (passwordDialogVM.IsSkipped)
                {
                    CachedMasterPassword = null;

                    return string.Empty.ToSecuredString();
                }

                return null;
            }

            /// <summary>
            /// Request the master password.
            /// </summary>
            /// <returns>Returns the master password or <see langword="null"/> if not password returned.</returns>
            public SecureString RequestMasterPassword()
            {
                var passwordDialogVM = _createGetViewModel();
                var masterPasswordDialogService = _getDialogService;

                if (masterPasswordDialogService.ShowDialog(passwordDialogVM) == DialogResult.OK)
                {
                    CachedMasterPassword = passwordDialogVM.Password;

                    return passwordDialogVM.Password;
                }

                return null;
            }
        }
    }
}

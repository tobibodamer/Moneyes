using Moneyes.Core;
using Moneyes.Core.Filters;
using Moneyes.Data;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace Moneyes.UI
{
    public interface ITransactionService
    {
        Task<Result<IEnumerable<Transaction>>> GetTransactions(
            IEvaluable<Transaction> filter = null, params Category[] categories);
    }

    public class LiveDataService
    {
        private readonly TransactionRepository _transactionRepo;
        private readonly CategoryRepository _categoryRepo;
        private readonly IRepository<AccountDetails> _accountStore;
        private readonly OnlineBankingServiceFactory _bankingServiceFactory;
        private readonly IPasswordPrompt _passwordPrompt;

        private OnlineBankingService _bankingService;        

        public LiveDataService(
            TransactionRepository transactionStore,
            CategoryRepository categoryStore,
            IRepository<AccountDetails> accountStore,
            OnlineBankingServiceFactory bankingServiceFactory,
            IPasswordPrompt passwordPrompt)
        {
            _transactionRepo = transactionStore;
            _categoryRepo = categoryStore;
            _accountStore = accountStore;
            _bankingServiceFactory = bankingServiceFactory;
            _passwordPrompt = passwordPrompt;
        }

        private static DateTime FirstOfMonth => new(DateTime.Now.Year, DateTime.Now.Month, 1);

        private async Task ShowPasswordPromptIfNotSet()
        {
            if (_bankingService.BankingDetails.Pin == null || _bankingService.BankingDetails.Pin.Length == 0)
            {
                SecureString password = await _passwordPrompt.WaitForPasswordAsync();

                _bankingService.BankingDetails.Pin = password;
            }
        }

        public Result<IBankInstitute> FindBank(int bankCode)
        {
            try
            {
                IBankInstitute bank = BankInstitutes.GetInstitute(bankCode);

                if (bank != null)
                {
                    return Result.Successful(bank);
                }
            }
            catch
            {
                //TODO: Log
            }

            return Result.Failed<IBankInstitute>();
        }

        public async Task<Result> Initialize(OnlineBankingDetails bankingDetails, bool sync = false)
        {
            try
            {
                _bankingService = _bankingServiceFactory.CreateService(bankingDetails);

                if (sync)
                {
                    await ShowPasswordPromptIfNotSet();

                    return await _bankingService.Sync();
                }

                return Result.Successful();
            }
            catch (Exception)
            {
                //TODO: LOG
            }

            return Result.Failed();
        }

        public async Task<Result<int>> FetchOnlineTransactions(AccountDetails account)
        {
            await ShowPasswordPromptIfNotSet();

            Result<IEnumerable<Transaction>> result =
                await _bankingService.Transactions(account, startDate: FirstOfMonth, endDate: DateTime.Now);

            if (!result.IsSuccessful)
            {
                return Result.Failed<int>();
            }

            // Get new transactions
            List<Transaction> transactions = result.Data.ToList();


            // Assign categories

            List<Category> categories = _categoryRepo.All()
                .OrderBy(c => c.IsExlusive).ToList();

            foreach (Transaction transaction in transactions)
            {
                foreach (Category category in categories)
                {
                    if (category.IsExlusive && transaction.Categories.Any())
                    {
                        continue;
                    }

                    if (category.Filter != null && category.Filter.Evaluate(transaction))
                    {
                        transaction.Categories.Add(category);
                    }
                }
            }

            // Store

            return _transactionRepo.InsertAll(transactions);
        }

        public async Task<Result> FetchAndImportAccounts()
        {
            await ShowPasswordPromptIfNotSet();

            IEnumerable<AccountDetails> accounts = (await _bankingService.Accounts()).GetOrNull();

            if (accounts != null)
            {
                await _accountStore.SetAll(accounts);

                return Result.Successful(accounts);
            }

            return Result.Failed();
        }
    }
}

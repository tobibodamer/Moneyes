using libfintx.FinTS;
using libfintx.FinTS.Camt;
using libfintx.FinTS.Data;
using libfintx.FinTS.Swift;
using libfintx.Sepa;
using Moneyes.Core;
using Moneyes.Data;
using Moneyes.LiveData;
using Moneyes.UI;
using Moneyes.UI.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Moneyes.Tests
{
    public class UnitTest1
    {
        class SimpleBankConnectionStore : IBankConnectionStore
        {
            private OnlineBankingDetails _onlineBankingDetails;
            public bool HasBankingDetails => _onlineBankingDetails != null;

            public OnlineBankingDetails GetBankingDetails()
            {
                return _onlineBankingDetails;
            }

            public bool SetBankingDetails(OnlineBankingDetails bankingDetails)
            {
                bool hadValue = HasBankingDetails;

                _onlineBankingDetails = bankingDetails;

                return !hadValue;
            }
        }

        static string UserName = "testUser";
        static string Password = "testPIN";

        class MockOnlineBankingService : IOnlineBankingService
        {
            public OnlineBankingDetails BankingDetails { get; set; }

            public async Task<Result<IEnumerable<AccountDetails>>> Accounts()
            {
                if (VerifyDetails())
                {
                    IEnumerable<AccountDetails> accounts = new AccountDetails[] { CreateAccount() };
                    return Result.Successful(accounts);
                }

                throw new OnlineBankingException(OnlineBankingErrorCode.InvalidUsernameOrPin);
            }

            public Task<Result<Balance>> Balance(AccountDetails account)
            {
                throw new NotImplementedException();
            }

            public async Task Sync()
            {
                if (VerifyDetails())
                {
                    return;
                }

                throw new OnlineBankingException(OnlineBankingErrorCode.InvalidUsernameOrPin);
            }

            public async Task<Result<TransactionData>> Transactions(AccountDetails account, DateTime? startDate = null, DateTime? endDate = null)
            {
                if (VerifyDetails())
                {
                    return Result.Successful(CreateTransactionData());
                }

                throw new OnlineBankingException(OnlineBankingErrorCode.InvalidUsernameOrPin);
            }

            private bool VerifyDetails()
            {
                return BankingDetails.UserId == UserName
                    && BankingDetails.Pin.ToUnsecuredString() == Password;
            }

            public MockOnlineBankingService(OnlineBankingDetails bankingDetails)
            {
                BankingDetails = bankingDetails;
            }
        }
        class OBSFactory : IOnlineBankingServiceFactory
        {
            public IOnlineBankingService CreateService(OnlineBankingDetails bankingDetails)
            {
                return new MockOnlineBankingService(bankingDetails);
            }
        }

        class FakeStatusMessageService : IStatusMessageService
        {
            public event Action<string, string, Action> NewMessage;

            public void ShowMessage(string messageText, string actionText = null, Action action = null) { }
        }

        [Fact]
        public void Test_Find_Bank()
        {
            LiveDataService liveDataService = CreateLiveDataService();

            IBankInstitute bank = liveDataService.FindBank(66650085);

            Assert.NotNull(bank);
            Assert.Equal("Sparkasse Pforzheim Calw", bank.Name);
            Assert.Equal("66650085", bank.BankCode);

            _ = Assert.Throws<NotSupportedException>(() => liveDataService.FindBank(00000000));
        }

        static AccountDetails CreateAccount()
        {
            return new AccountDetails()
            {
                BankCode = "66650085",
                Number = "12345678",
                OwnerName = "Hans Jürgen"
            };
        }

        static TransactionData CreateTransactionData()
        {
            Balance[] balances = new[]
            {
                new Balance()
                {
                    Account = CreateAccount(),
                    Date = DateTime.Now,
                    Amount = 1337,
                    Currency = "EUR"
                }
            };

            Core.Transaction[] transactions = new[]
            {
                new Core.Transaction()
                {
                    Amount = 1337,
                    Currency = "EUR"
                }
            };

            return new TransactionData()
            {
                Balances = balances,
                Transactions = transactions
            };
        }

        [Fact]
        public async Task Test_auto_init_from_store()
        {
            var connectionStore = new SimpleBankConnectionStore();

            LiveDataService liveDataService = new LiveDataService(null, null, null, null, connectionStore,
                new OBSFactory(), null, new FakeStatusMessageService());

            Assert.Null(connectionStore.GetBankingDetails());

            var accountsResult = await liveDataService.FetchAccounts();
            var importAccountsResult = await liveDataService.FetchAndImportAccounts();
            var transactionsResult = await liveDataService.FetchTransactionsAndBalances(CreateAccount());

            Assert.False(accountsResult.IsSuccessful);
            Assert.False(importAccountsResult.IsSuccessful);
            Assert.False(transactionsResult.IsSuccessful);


            OnlineBankingDetails details = Details_correct();
            connectionStore.SetBankingDetails(details);
            Assert.Equal(details, connectionStore.GetBankingDetails());


            liveDataService = new LiveDataService(null, null, null, null, connectionStore,
                new OBSFactory(), null, new FakeStatusMessageService());

            accountsResult = await liveDataService.FetchAccounts();

            Assert.True(accountsResult.IsSuccessful);

            liveDataService = new LiveDataService(null, null, null, null, connectionStore,
                new OBSFactory(), null, new FakeStatusMessageService());

            importAccountsResult = await liveDataService.FetchAndImportAccounts();

            Assert.True(importAccountsResult.IsSuccessful);

            liveDataService = new LiveDataService(null, null, null, null, connectionStore,
                new OBSFactory(), null, new FakeStatusMessageService());

            transactionsResult = await liveDataService.FetchTransactionsAndBalances(CreateAccount());

            Assert.True(transactionsResult.IsSuccessful);
        }

        static OnlineBankingDetails Details_correct()
        {
            return new()
            {
                BankCode = 66650085,
                UserId = UserName,
                Pin = Password.ToSecuredString()
            };
        }

        static OnlineBankingDetails Details_WrongBankCode()
        {
            return new()
            {
                BankCode = 00000000,
                UserId = UserName,
                Pin = Password.ToSecuredString()
            };
        }

        static OnlineBankingDetails Details_WrongPassword()
        {
            return new()
            {
                BankCode = 66650085,
                UserId = UserName,
                Pin = "1234".ToSecuredString()
            };
        }

        LiveDataService CreateLiveDataService()
        {
            return new LiveDataService(null, null, null, null, new SimpleBankConnectionStore(),
                new OBSFactory(), null, new FakeStatusMessageService());
        }

        class FakeFinTsClient : IFinTsClient
        {
            ConnectionDetails _expectedDetails;
            public FakeFinTsClient(ConnectionDetails expectedDetails)
            {
                _expectedDetails = expectedDetails;
            }
            public AccountInformation activeAccount { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public bool Anonymous => throw new NotImplementedException();

            public ConnectionDetails ConnectionDetails { get; set; }

            public string HIRMS { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public string HITAB { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
            public int HITANS { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

            public string SystemId => throw new NotImplementedException();

            public Task<HBCIDialogResult<List<AccountInformation>>> Accounts(TANDialog tanDialog)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult<AccountBalance>> Balance(TANDialog tanDialog)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> Collect(TANDialog tanDialog, string payerName, string payerIBAN, string payerBIC, decimal amount, string purpose, DateTime settlementDate, string mandateNumber, DateTime mandateDate, string creditorIdNumber, string hirms)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> CollectiveCollect(TANDialog tanDialog, DateTime settlementDate, List<Pain00800202CcData> painData, string numberOfTransactions, decimal totalAmount, string hirms)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> CollectiveTransfer(TANDialog tanDialog, List<Pain00100203CtData> painData, string numberOfTransactions, decimal totalAmount, string hirms)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> CollectiveTransfer_Terminated(TANDialog tanDialog, List<Pain00100203CtData> painData, string numberOfTransactions, decimal totalAmount, DateTime executionDay, string hirms)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> DeleteBankersOrder(TANDialog tanDialog, string orderId, string receiverName, string receiverIBAN, string receiverBIC, decimal amount, string purpose, DateTime firstTimeExecutionDay, HKCDE.TimeUnit timeUnit, string rota, int executionDay, DateTime? lastExecutionDay, string hirms)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> DeleteTerminatedTransfer(TANDialog tanDialog, string orderId, string receiverName, string receiverIBAN, string receiverBIC, decimal amount, string usage, DateTime executionDay, string hirms)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult<List<BankersOrder>>> GetBankersOrders(TANDialog tanDialog)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult<List<TerminatedTransfer>>> GetTerminatedTransfers(TANDialog tanDialog)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> ModifyBankersOrder(TANDialog tanDialog, string OrderId, string receiverName, string receiverIBAN, string receiverBIC, decimal amount, string purpose, DateTime firstTimeExecutionDay, HKCDE.TimeUnit timeUnit, string rota, int executionDay, DateTime? lastExecutionDay, string hirms)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> ModifyTerminatedTransfer(TANDialog tanDialog, string orderId, string receiverName, string receiverIBAN, string receiverBIC, decimal amount, string usage, DateTime executionDay, string hirms)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> Prepaid(TANDialog tanDialog, int mobileServiceProvider, string phoneNumber, int amount, string hirms)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> Rebooking(TANDialog tanDialog, string receiverName, string receiverIBAN, string receiverBIC, decimal amount, string purpose, string hirms)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult<List<string>>> RequestTANMediumName()
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> SubmitBankersOrder(TANDialog tanDialog, string receiverName, string receiverIBAN, string receiverBIC, decimal amount, string purpose, DateTime firstTimeExecutionDay, HKCDE.TimeUnit timeUnit, string rota, int executionDay, DateTime? lastExecutionDay, string hirms)
            {
                throw new NotImplementedException();
            }

            public async Task<HBCIDialogResult<string>> Synchronization()
            {
                if (ConnectionDetails.Pin.Equals(_expectedDetails.Pin)
                    && ConnectionDetails.UserId.Equals(_expectedDetails.UserId))
                {
                    return new(CreateMessages(OnlineBankingErrorCode.Unknown), "");
                }

                return new(CreateMessages(OnlineBankingErrorCode.InvalidUsernameOrPin), "");
            }

            private IEnumerable<HBCIBankMessage> CreateMessages(OnlineBankingErrorCode errorCode)
            {
                int errorCodeInt = (int)errorCode;

                return new HBCIBankMessage[]
                {
                    new(errorCodeInt.ToString(), "")
                };
            }

            public Task<HBCIDialogResult> TAN(string TAN)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> TAN4(string TAN, string MediumName)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult<List<SwiftStatement>>> Transactions(TANDialog tanDialog, DateTime? startDate = null, DateTime? endDate = null, bool saveMt940File = false)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult<List<AccountTransaction>>> TransactionsSimple(TANDialog tanDialog, DateTime? startDate = null, DateTime? endDate = null)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult<List<CamtStatement>>> Transactions_camt(TANDialog tanDialog, CamtVersion camtVers, DateTime? startDate = null, DateTime? endDate = null, bool saveCamtFile = false)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> Transfer(TANDialog tanDialog, string receiverName, string receiverIBAN, string receiverBIC, decimal amount, string purpose, string hirms)
            {
                throw new NotImplementedException();
            }

            public Task<HBCIDialogResult> Transfer_Terminated(TANDialog tanDialog, string receiverName, string receiverIBAN, string receiverBIC, decimal amount, string purpose, DateTime executionDay, string hirms)
            {
                throw new NotImplementedException();
            }
        }
    }
}

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moneyes.BankService.Dtos;
using Moneyes.Core;
using Moneyes.LiveData;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Moneyes.BankService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BankingController : ControllerBase
    {
        private readonly ILogger<BankingController> _logger;
        private readonly OnlineBankingSessionStore _sessionStore;
        public BankingController(OnlineBankingSessionStore store, ILogger<BankingController> logger)
        {
            _sessionStore = store;
            _logger = logger;
        }


        [HttpPost("/login")]
        public async Task<IActionResult> CreateBankConnection(CreateConnectionDto createConnectionDto)
        {
            OnlineBankingDetails details = new()
            {
                BankCode = createConnectionDto.BankCode,
                Server = createConnectionDto.Server,
                UserId = createConnectionDto.UserId,
                Pin = createConnectionDto.Pin.ToSecuredString()
            };

            var service = await _sessionStore.CreateOrUpdateSession(details, createConnectionDto.TestConnection);

            if (service == null)
            {
                return Unauthorized();
            }

            //A claim is a statement about a subject by an issuer and    
            //represent attributes of the subject that are useful in the context of authentication and authorization operations.    
            var claims = new List<Claim>() {
                    new Claim("UserId", service.BankingDetails.UserId),
                    new Claim("BankCode", service.BankingDetails.BankCode.ToString())
            };
            
            //Initialize a new instance of the ClaimsIdentity with the claims and authentication scheme    
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            //Initialize a new instance of the ClaimsPrincipal with ClaimsIdentity    
            var principal = new ClaimsPrincipal(identity);
            //SignInAsync is a Extension method for Sign in a principal for the specified scheme.    
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, new AuthenticationProperties()
            {
                IsPersistent = true
            });
            
            return Ok();
        }

        private IOnlineBankingService GetBankingService()
        {
            string userId = User.FindFirst("UserId")?.Value;
            string bankCode = User.FindFirst("BankCode")?.Value;

            if (userId == null || bankCode == null)
            {
                return null;
            }

            return _sessionStore.GetSession(userId, bankCode);
        }

        [Authorize]
        [HttpGet("/accounts")]
        public async Task<ActionResult<IEnumerable<AccountDetails>>> GetAccounts()
        {
            var service = GetBankingService();

            if (service is null)
            {
                return Unauthorized();
            }

            var result = await service.Accounts();

            if (!result.IsSuccessful)
            {
                return StatusCode(500);
            }

            var accounts = result.Data;

            return Ok(accounts);
        }

        [Authorize]
        [HttpGet("/balance")]
        public async Task<ActionResult<BalanceDto>> GetBalance([FromQuery] GetBalanceDto getBalanceDto)
        {
            var service = GetBankingService();

            if (service is null)
            {
                return Unauthorized();
            }

            AccountDetails account = new()
            {
                BankCode = service.BankingDetails.BankCode.ToString(),
                IBAN = getBalanceDto.IBAN,
                Number = getBalanceDto.AccountNumber
            };

            var result = await service.Balance(account);

            if (!result.IsSuccessful)
            {
                return StatusCode(500);
            }

            var balance = new BalanceDto()
            {
                Date = result.Data.Date,
                Amount = result.Data.Amount
            };

            return Ok(balance);
        }


        [Authorize]
        [HttpGet("/transactions")]
        public async Task<ActionResult<TransactionBalanceListDto>> GetTransactions([FromQuery] GetTransactionsDto getTransactionsDto)
        {
            var service = GetBankingService();

            if (service is null)
            {
                return Unauthorized();
            }

            AccountDetails account = new()
            {
                BankCode = service.BankingDetails.BankCode.ToString(),
                IBAN = getTransactionsDto.IBAN,
                Number = getTransactionsDto.AccountNumber
            };

            var result = await service.Transactions(
                account, getTransactionsDto.StartDate, getTransactionsDto.EndDate);

            if (!result.IsSuccessful)
            {
                return StatusCode(500);
            }

            var transactions = result.Data.Transactions
                .Select(t => new TransactionDto()
                {
                    AltName = t.AltName,
                    Amount = t.Amount,
                    BookingType = t.BookingType,
                    BIC = t.BIC,
                    BookingDate = t.BookingDate,
                    Currency = t.Currency,
                    IBAN = t.IBAN,
                    Index = t.Index,
                    Name = t.Name,
                    PartnerIBAN = t.PartnerIBAN,
                    Purpose = t.Purpose,
                    ValueDate = t.ValueDate
                });

            var balances = result.Data.Balances
                .Select(b => new BalanceDto()
                {
                    Amount = b.Amount,
                    Date = b.Date
                });

            return Ok(
                new TransactionBalanceListDto()
                {
                    Balances = balances,
                    Transactions = transactions
                });
        }
    }
}

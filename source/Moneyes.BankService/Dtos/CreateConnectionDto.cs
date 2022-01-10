using Moneyes.Core;
using System;
using System.ComponentModel.DataAnnotations;

namespace Moneyes.BankService.Dtos
{
    public record CreateConnectionDto
    {
        [Required]
        [Range(1, 99999999)]
        /// <summary>
        /// The bank code of the bank to connect to.
        /// </summary>
        public int BankCode { get; init; }

        [Required(AllowEmptyStrings = false)]
        /// <summary>
        /// Online banking username for the bank account.
        /// </summary>
        public string UserId { get; init; }

        [Required(AllowEmptyStrings = false)]
        /// <summary>
        /// Online banking logon pin.
        /// </summary>
        public string Pin { get; init; }

        [Url]
        /// <summary>
        /// Online banking server uri.
        /// </summary>
        public Uri Server { get; init; }

        public bool TestConnection { get; init; } = true;
    }
}

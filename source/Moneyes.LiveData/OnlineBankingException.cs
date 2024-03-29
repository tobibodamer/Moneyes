﻿using System;

namespace Moneyes.LiveData
{
    public class OnlineBankingException : Exception
    {
        public OnlineBankingErrorCode ErrorCode { get; }

        public OnlineBankingException(OnlineBankingErrorCode errorCode, 
            string message = null, Exception innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
        }
    }
}

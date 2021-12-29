using System;

namespace Moneyes.LiveData
{
    public class BankingResult : Result
    {
        public OnlineBankingErrorCode? ErrorCode { get; }
        protected BankingResult(bool successful, OnlineBankingErrorCode? errorCode = null) : base(successful)
        {
            ErrorCode = errorCode;
        }
        public static new BankingResult Successful()
        {
            return new(true);
        }

        public static new BankingResult<T> Successful<T>(T data)
        {
            return new(data: data);
        }

        public static BankingResult Failed(OnlineBankingErrorCode? errorCode)
        {
            return new(false, errorCode);
        }

        public static BankingResult<T> Failed<T>(OnlineBankingErrorCode? errorCode = null)
        {
            return new(successful: false, errorCode: errorCode);
        }

        public static implicit operator BankingResult(OnlineBankingErrorCode errorCode)
        {
            return Failed(errorCode);
        }
    }

    public class BankingResult<T> : BankingResult
    {
        private readonly T _data;

        public T Data => !IsSuccessful
                    ? throw new InvalidOperationException("No data in error result.")
                    : _data;

        internal BankingResult(bool successful = true, T data = default, 
            OnlineBankingErrorCode? errorCode = null) : base(successful, errorCode)
        {
            _data = data;
        }

        public static implicit operator BankingResult<T>(T data)
        {
            return Successful(data);
        }

        public static implicit operator BankingResult<T>(OnlineBankingErrorCode errorCode)
        {
            return Failed<T>(errorCode);
        }
    }
    public class Result
    {
        public bool IsSuccessful { get; }

        protected Result(bool successful)
        {
            IsSuccessful = successful;
        }

        public static Result Successful()
        {
            return new(true);
        }

        public static Result<T> Successful<T>(T data)
        {
            return new(data: data);
        }

        public static Result Failed()
        {
            return new(false);
        }

        public static Result<T> Failed<T>()
        {
            return new(successful: false);
        }
    }

    public class Result<T> : Result
    {
        private readonly T _data;

        public T Data => !IsSuccessful
                    ? throw new InvalidOperationException("No data in error result.")
                    : _data;

        internal Result(bool successful = true, T data = default)
            : base(successful)
        {
            _data = data;
        }

        public static implicit operator Result<T>(T data)
        {
            return Successful(data);
        }
    }
}

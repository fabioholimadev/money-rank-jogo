namespace MoneyRank.Domain
{
    public readonly struct OperationResult
    {
        private OperationResult(bool succeeded, string errorCode, string message)
        {
            Succeeded = succeeded;
            ErrorCode = errorCode;
            Message = message;
        }

        public bool Succeeded { get; }
        public string ErrorCode { get; }
        public string Message { get; }

        public static OperationResult Success() => new OperationResult(true, string.Empty, string.Empty);

        public static OperationResult Failure(string errorCode, string message) =>
            new OperationResult(false, errorCode, message);
    }

    public readonly struct OperationResult<T>
    {
        private OperationResult(bool succeeded, T value, string errorCode, string message)
        {
            Succeeded = succeeded;
            Value = value;
            ErrorCode = errorCode;
            Message = message;
        }

        public bool Succeeded { get; }
        public T Value { get; }
        public string ErrorCode { get; }
        public string Message { get; }

        public static OperationResult<T> Success(T value) =>
            new OperationResult<T>(true, value, string.Empty, string.Empty);

        public static OperationResult<T> Failure(string errorCode, string message) =>
            new OperationResult<T>(false, default, errorCode, message);
    }
}


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
}


using System;

namespace MoneyRank.Domain.Events
{
    public sealed class RequirementCheckResult
    {
        private RequirementCheckResult(bool isSatisfied, string errorCode, string message)
        {
            IsSatisfied = isSatisfied;
            ErrorCode = errorCode;
            Message = message;
        }

        public bool IsSatisfied { get; }
        public string ErrorCode { get; }
        public string Message { get; }

        public static RequirementCheckResult Pass() =>
            new RequirementCheckResult(true, string.Empty, string.Empty);

        public static RequirementCheckResult Fail(string errorCode, string message)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
                throw new ArgumentException("Requirement error code cannot be empty.", nameof(errorCode));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Requirement error message cannot be empty.", nameof(message));

            return new RequirementCheckResult(false, errorCode, message);
        }
    }
}

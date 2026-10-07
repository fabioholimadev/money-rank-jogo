using System;

namespace MoneyRank.Domain.Events
{
    public sealed class EffectApplicationResult
    {
        private EffectApplicationResult(bool succeeded, string errorCode, string message)
        {
            Succeeded = succeeded;
            ErrorCode = errorCode;
            Message = message;
        }

        public bool Succeeded { get; }
        public string ErrorCode { get; }
        public string Message { get; }

        public static EffectApplicationResult Success() =>
            new EffectApplicationResult(true, string.Empty, string.Empty);

        public static EffectApplicationResult Failure(string errorCode, string message)
        {
            if (string.IsNullOrWhiteSpace(errorCode))
                throw new ArgumentException("Effect error code cannot be empty.", nameof(errorCode));
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Effect error message cannot be empty.", nameof(message));

            return new EffectApplicationResult(false, errorCode, message);
        }
    }
}

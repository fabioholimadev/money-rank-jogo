using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain.Economy
{
    public sealed class EconomyTransactionRecord
    {
        private readonly ReadOnlyCollection<EconomyTransactionEntry> _entries;

        public EconomyTransactionRecord(
            EconomyTransactionRequest request,
            EconomySnapshot beforeState,
            EconomySnapshot afterState)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            Id = request.Id;
            SourceId = request.SourceId;
            CreatedAt = request.CreatedAt;
            OperatorId = request.OperatorId;
            Description = request.Description;
            BeforeState = beforeState ?? throw new ArgumentNullException(nameof(beforeState));
            AfterState = afterState ?? throw new ArgumentNullException(nameof(afterState));
            _entries = request.Entries.ToList().AsReadOnly();
        }

        public string Id { get; }
        public string SourceId { get; }
        public DateTimeOffset CreatedAt { get; }
        public string OperatorId { get; }
        public string Description { get; }
        public EconomySnapshot BeforeState { get; }
        public EconomySnapshot AfterState { get; }
        public IReadOnlyList<EconomyTransactionEntry> Entries => _entries;
    }
}

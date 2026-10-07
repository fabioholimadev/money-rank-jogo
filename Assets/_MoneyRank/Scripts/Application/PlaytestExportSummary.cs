using System;
using MoneyRank.Domain;

namespace MoneyRank.Application
{
    public sealed class PlaytestExportSummary
    {
        public PlaytestExportSummary(
            SessionId sessionId,
            GamePhase phase,
            int playerCount,
            int actionCount,
            int eventResolutionCount,
            int economyTransactionCount,
            int processedAnnualCycles,
            int hospitalizedPlayerCount,
            int undoCount,
            TimeSpan? duration,
            DateTimeOffset exportedAt)
        {
            SessionId = sessionId;
            Phase = phase;
            PlayerCount = playerCount;
            ActionCount = actionCount;
            EventResolutionCount = eventResolutionCount;
            EconomyTransactionCount = economyTransactionCount;
            ProcessedAnnualCycles = processedAnnualCycles;
            HospitalizedPlayerCount = hospitalizedPlayerCount;
            UndoCount = undoCount;
            Duration = duration;
            ExportedAt = exportedAt;
        }

        public SessionId SessionId { get; }
        public GamePhase Phase { get; }
        public int PlayerCount { get; }
        public int ActionCount { get; }
        public int EventResolutionCount { get; }
        public int EconomyTransactionCount { get; }
        public int ProcessedAnnualCycles { get; }
        public int HospitalizedPlayerCount { get; }
        public int UndoCount { get; }
        public TimeSpan? Duration { get; }
        public DateTimeOffset ExportedAt { get; }
    }
}

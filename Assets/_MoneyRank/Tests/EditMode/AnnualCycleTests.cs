using System;
using MoneyRank.Content;
using MoneyRank.Domain;
using MoneyRank.Domain.Economy;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class AnnualCycleTests
    {
        [Test]
        public void ConfiguredLapCountDeterminesPendingCycles()
        {
            var service = CreateService(new SalaryPolicy());
            var definition = new AnnualCycleDefinition("fiscal-year", 2, "test-salary");
            var state = new AnnualCycleState(new PlayerId("p1"));

            var pending = service.GetPendingCycleCount(definition, state, 5);

            Assert.That(pending, Is.EqualTo(2));
        }

        [Test]
        public void ResolvingOneCycleAppliesAuditedTransactionAndMarksOnlyThatCycle()
        {
            var accounts = CreateAccounts();
            var economy = CreateEconomy(accounts);
            var history = new EconomyTransactionHistory();
            var service = new AnnualCycleService(
                new IAnnualCyclePolicy[] { new SalaryPolicy() },
                new EconomyTransactionService(accounts));
            var definition = new AnnualCycleDefinition("fiscal-year", 2, "test-salary");
            var state = new AnnualCycleState(new PlayerId("p1"));

            var result = service.ResolveNext(
                definition,
                state,
                5,
                economy,
                history,
                "annual-p1-1",
                new DateTimeOffset(2026, 10, 4, 12, 0, 0, TimeSpan.Zero),
                "facilitator");

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.CycleNumber, Is.EqualTo(1));
            Assert.That(result.Value.RemainingPendingCycles, Is.EqualTo(1));
            Assert.That(state.ProcessedCycles, Is.EqualTo(1));
            Assert.That(history.Records, Has.Count.EqualTo(1));
            Assert.That(economy.TryGetBalance("player-cash", out var playerCash), Is.True);
            Assert.That(playerCash, Is.EqualTo(10m));
            Assert.That(economy.TryGetBalance("bank", out var bankCash), Is.True);
            Assert.That(bankCash, Is.EqualTo(90m));
        }

        [Test]
        public void RejectedPolicyLeavesCycleEconomyAndHistoryUnchanged()
        {
            var accounts = CreateAccounts();
            var economy = CreateEconomy(accounts);
            var history = new EconomyTransactionHistory();
            var service = new AnnualCycleService(
                new IAnnualCyclePolicy[] { new RejectingPolicy() },
                new EconomyTransactionService(accounts));
            var state = new AnnualCycleState(new PlayerId("p1"));

            var result = service.ResolveNext(
                new AnnualCycleDefinition("fiscal-year", 1, "reject"),
                state,
                1,
                economy,
                history,
                "annual-p1-1",
                DateTimeOffset.UtcNow,
                "facilitator");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("TEST_POLICY_REJECTED"));
            Assert.That(state.ProcessedCycles, Is.Zero);
            Assert.That(history.Records, Is.Empty);
            economy.TryGetBalance("player-cash", out var playerCash);
            Assert.That(playerCash, Is.Zero);
        }

        [Test]
        public void MissingPolicyDoesNotMarkCycleProcessed()
        {
            var accounts = CreateAccounts();
            var service = new AnnualCycleService(
                Array.Empty<IAnnualCyclePolicy>(),
                new EconomyTransactionService(accounts));
            var state = new AnnualCycleState(new PlayerId("p1"));

            var result = service.ResolveNext(
                new AnnualCycleDefinition("fiscal-year", 4, "missing"),
                state,
                4,
                CreateEconomy(accounts),
                new EconomyTransactionHistory(),
                "annual-p1-1",
                DateTimeOffset.UtcNow,
                "facilitator");

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ANNUAL_CYCLE_POLICY_NOT_FOUND"));
            Assert.That(state.ProcessedCycles, Is.Zero);
        }

        [Test]
        public void SerializableDefinitionKeepsUnapprovedLapCountAsData()
        {
            var definition = new AnnualCycleDefinitionData("custom-year", 7, "custom-policy").ToDomain();

            Assert.That(definition.CompletedLapsPerCycle, Is.EqualTo(7));
            Assert.That(definition.PolicyId, Is.EqualTo("custom-policy"));
        }

        private static EconomyAccountDefinition[] CreateAccounts() =>
            new[]
            {
                new EconomyAccountDefinition("bank", "Bank", "bank", "cash", 0m),
                new EconomyAccountDefinition("player-cash", "Player cash", "p1", "cash", 0m)
            };

        private static EconomyState CreateEconomy(EconomyAccountDefinition[] accounts) =>
            new EconomyState(
                accounts,
                new[]
                {
                    new EconomyBalanceDefinition("bank", 100m),
                    new EconomyBalanceDefinition("player-cash", 0m)
                });

        private static AnnualCycleService CreateService(IAnnualCyclePolicy policy)
        {
            var accounts = CreateAccounts();
            return new AnnualCycleService(
                new[] { policy },
                new EconomyTransactionService(accounts));
        }

        private sealed class SalaryPolicy : IAnnualCyclePolicy
        {
            public string PolicyId => "test-salary";

            public OperationResult<EconomyTransactionRequest> CreateTransaction(
                AnnualCycleDefinition definition,
                AnnualCycleContext context) =>
                OperationResult<EconomyTransactionRequest>.Success(
                    new EconomyTransactionRequest(
                        context.TransactionId,
                        new[]
                        {
                            new EconomyTransactionEntry("bank", -10m),
                            new EconomyTransactionEntry("player-cash", 10m)
                        },
                        definition.Id,
                        context.CreatedAt,
                        context.OperatorId,
                        $"Configured annual cycle {context.CycleNumber}"));
        }

        private sealed class RejectingPolicy : IAnnualCyclePolicy
        {
            public string PolicyId => "reject";

            public OperationResult<EconomyTransactionRequest> CreateTransaction(
                AnnualCycleDefinition definition,
                AnnualCycleContext context) =>
                OperationResult<EconomyTransactionRequest>.Failure(
                    "TEST_POLICY_REJECTED",
                    "Rejected by the test policy.");
        }
    }
}

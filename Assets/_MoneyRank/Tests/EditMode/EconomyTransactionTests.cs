using System;
using MoneyRank.Content;
using MoneyRank.Domain.Economy;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class EconomyTransactionTests
    {
        [Test]
        public void BalancedTransactionMovesValueBetweenDistinctAuditableAccounts()
        {
            var accounts = CreateAccounts();
            var state = new EconomyState(
                accounts,
                new[]
                {
                    new EconomyBalanceDefinition("bank", 100m),
                    new EconomyBalanceDefinition("player-cash", 5m),
                    new EconomyBalanceDefinition("public-treasury", 0m)
                });
            var history = new EconomyTransactionHistory();
            var service = new EconomyTransactionService(accounts);
            var request = CreateRequest(
                "transaction-1",
                new EconomyTransactionEntry("bank", -10m),
                new EconomyTransactionEntry("player-cash", 7m),
                new EconomyTransactionEntry("public-treasury", 3m));

            var result = service.Apply(state, history, request);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(state.TryGetBalance("bank", out var bank), Is.True);
            Assert.That(state.TryGetBalance("player-cash", out var cash), Is.True);
            Assert.That(state.TryGetBalance("public-treasury", out var treasury), Is.True);
            Assert.That(bank, Is.EqualTo(90m));
            Assert.That(cash, Is.EqualTo(12m));
            Assert.That(treasury, Is.EqualTo(3m));
            Assert.That(result.Value.SourceId, Is.EqualTo("event:test@1"));
            Assert.That(history.Records, Has.Count.EqualTo(1));
        }

        [Test]
        public void AccountLimitFailureRollsBackWholeTransaction()
        {
            var accounts = CreateAccounts();
            var state = new EconomyState(
                accounts,
                new[]
                {
                    new EconomyBalanceDefinition("bank", 100m),
                    new EconomyBalanceDefinition("player-cash", 2m)
                });
            var history = new EconomyTransactionHistory();
            var service = new EconomyTransactionService(accounts);
            var request = CreateRequest(
                "transaction-1",
                new EconomyTransactionEntry("player-cash", -5m),
                new EconomyTransactionEntry("bank", 5m));

            var result = service.Apply(state, history, request);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ECONOMY_ACCOUNT_LIMIT_EXCEEDED"));
            state.TryGetBalance("player-cash", out var cash);
            state.TryGetBalance("bank", out var bank);
            Assert.That(cash, Is.EqualTo(2m));
            Assert.That(bank, Is.EqualTo(100m));
            Assert.That(history.Records, Is.Empty);
        }

        [Test]
        public void DuplicateTransactionIdIsRejectedBeforeMutation()
        {
            var accounts = CreateAccounts();
            var state = new EconomyState(
                accounts,
                new[]
                {
                    new EconomyBalanceDefinition("bank", 100m),
                    new EconomyBalanceDefinition("player-cash", 0m)
                });
            var history = new EconomyTransactionHistory();
            var service = new EconomyTransactionService(accounts);
            var request = CreateRequest(
                "same",
                new EconomyTransactionEntry("bank", -1m),
                new EconomyTransactionEntry("player-cash", 1m));

            service.Apply(state, history, request);
            var duplicate = service.Apply(state, history, request);

            Assert.That(duplicate.Succeeded, Is.False);
            Assert.That(duplicate.ErrorCode, Is.EqualTo("DUPLICATE_ECONOMY_TRANSACTION_ID"));
            state.TryGetBalance("player-cash", out var cash);
            Assert.That(cash, Is.EqualTo(1m));
        }

        [Test]
        public void TransactionMustBeBalanced()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateRequest(
                    "unbalanced",
                    new EconomyTransactionEntry("bank", -5m),
                    new EconomyTransactionEntry("player-cash", 4m)));
        }

        [Test]
        public void DifferentResourceTypesMustBalanceIndependently()
        {
            var accounts = new[]
            {
                new EconomyAccountDefinition("bank-cash", "Bank cash", "bank", "cash"),
                new EconomyAccountDefinition("player-asset", "Player asset", "player-1", "asset")
            };
            var state = new EconomyState(accounts, Array.Empty<EconomyBalanceDefinition>());
            var request = CreateRequest(
                "cross-resource",
                new EconomyTransactionEntry("bank-cash", -1m),
                new EconomyTransactionEntry("player-asset", 1m));

            var result = new EconomyTransactionService(accounts).Apply(
                state,
                new EconomyTransactionHistory(),
                request);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ECONOMY_UNIT_NOT_BALANCED"));
        }

        [Test]
        public void SerializableAccountDataKeepsResourceAndLimitsConfigurable()
        {
            var data = new EconomyAccountData(
                "player-reserve",
                "Reserve",
                "player-1",
                "reserve",
                "0",
                "12.5");

            var definition = data.ToDomain();

            Assert.That(definition.OwnerId, Is.EqualTo("player-1"));
            Assert.That(definition.ResourceTypeId, Is.EqualTo("reserve"));
            Assert.That(definition.MinimumBalance, Is.EqualTo(0m));
            Assert.That(definition.MaximumBalance, Is.EqualTo(12.5m));
        }

        private static EconomyAccountDefinition[] CreateAccounts() =>
            new[]
            {
                new EconomyAccountDefinition("bank", "Bank", "bank", "cash"),
                new EconomyAccountDefinition("player-cash", "Player cash", "player-1", "cash", 0m),
                new EconomyAccountDefinition("public-treasury", "Public treasury", "public-treasury", "public-funds", 0m, unitId: "cash")
            };

        private static EconomyTransactionRequest CreateRequest(
            string id,
            params EconomyTransactionEntry[] entries) =>
            new EconomyTransactionRequest(
                id,
                entries,
                "event:test@1",
                new DateTimeOffset(2026, 10, 3, 20, 0, 0, TimeSpan.Zero),
                "facilitator",
                "Test transaction");
    }
}

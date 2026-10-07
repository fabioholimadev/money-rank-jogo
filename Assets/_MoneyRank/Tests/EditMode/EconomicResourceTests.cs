using System;
using System.Collections.Generic;
using MoneyRank.Application;
using MoneyRank.Content;
using MoneyRank.Domain;
using MoneyRank.Domain.Economy;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class EconomicResourceTests
    {
        [Test]
        public void PolicyAcquiresConfiguredResourceThroughBalancedAuditedTransaction()
        {
            var economy = CreateEconomy();
            var state = economy.CreateState();
            var history = new EconomyTransactionHistory();
            var service = new EconomicResourceService(
                economy,
                new IEconomicResourceTransactionPolicy[] { new PurchasePolicy() });

            var result = service.Execute(
                CreateResource(2m),
                state,
                history,
                CreateCommand("purchase-1", "p1-house", 1m));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.PreviousQuantity, Is.Zero);
            Assert.That(result.Value.CurrentQuantity, Is.EqualTo(1m));
            Assert.That(history.Records, Has.Count.EqualTo(1));
            state.TryGetBalance("p1-house", out var houses);
            state.TryGetBalance("p1-cash", out var cash);
            Assert.That(houses, Is.EqualTo(1m));
            Assert.That(cash, Is.EqualTo(12m));
        }

        [Test]
        public void ConfiguredOwnerLimitRejectsBeforeEconomyMutation()
        {
            var economy = CreateEconomy();
            var state = economy.CreateState();
            var history = new EconomyTransactionHistory();
            var service = new EconomicResourceService(
                economy,
                new IEconomicResourceTransactionPolicy[] { new PurchasePolicy() });

            var result = service.Execute(
                CreateResource(0m),
                state,
                history,
                CreateCommand("purchase-1", "p1-house", 1m));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("RESOURCE_OWNER_LIMIT_EXCEEDED"));
            Assert.That(history.Records, Is.Empty);
            state.TryGetBalance("p1-cash", out var cash);
            Assert.That(cash, Is.EqualTo(20m));
        }

        [Test]
        public void AccountMustBelongToRequestedPlayerAndResource()
        {
            var economy = CreateEconomy();
            var service = new EconomicResourceService(
                economy,
                new IEconomicResourceTransactionPolicy[] { new PurchasePolicy() });

            var result = service.Execute(
                CreateResource(2m),
                economy.CreateState(),
                new EconomyTransactionHistory(),
                CreateCommand("purchase-1", "asset-supply", 1m));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("RESOURCE_TARGET_ACCOUNT_INVALID"));
        }

        [Test]
        public void PolicyCannotReplaceCommandAuditMetadata()
        {
            var economy = CreateEconomy();
            var state = economy.CreateState();
            var history = new EconomyTransactionHistory();
            var service = new EconomicResourceService(
                economy,
                new IEconomicResourceTransactionPolicy[] { new MetadataReplacingPolicy() });

            var result = service.Execute(
                CreateResource(2m, "metadata-replacing"),
                state,
                history,
                CreateCommand("purchase-1", "p1-house", 1m));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("RESOURCE_TRANSACTION_METADATA_MISMATCH"));
            Assert.That(history.Records, Is.Empty);
            state.TryGetBalance("p1-cash", out var cash);
            Assert.That(cash, Is.EqualTo(20m));
        }

        [Test]
        public void SerializableResourceKeepsCategoryLimitAndEffectsConfigurable()
        {
            var data = new EconomicResourceDefinitionData(
                "custom-resource",
                "Custom resource",
                "custom-category",
                "custom-unit",
                "custom-policy",
                "3.5",
                "custom-presentation",
                new[] { new GameEffectDefinitionData("target", "operation", "value", null, "resource") },
                Array.Empty<GameEffectDefinitionData>());

            var definition = data.ToDomain();

            Assert.That(definition.CategoryId, Is.EqualTo("custom-category"));
            Assert.That(definition.MaximumPerOwner, Is.EqualTo(3.5m));
            Assert.That(definition.Benefits, Has.Count.EqualTo(1));
        }

        [Test]
        public void ApplicationServicePersistsOnlyAcceptedResourceChange()
        {
            var repository = new MemoryRepository();
            var economy = CreateEconomy();
            var session = new GameSession(
                new SessionId("resource-session"),
                new SessionRules(2, 2),
                new GameFlowStateMachine(),
                null,
                economy: economy);
            session.AddPlayer(new Player(new PlayerId("p1"), "Player 1", 0));
            session.AddPlayer(new Player(new PlayerId("p2"), "Player 2", 1));
            repository.Save(session);
            var service = new GameEconomicResourceService(
                repository,
                new[] { CreateResource(2m) },
                new IEconomicResourceTransactionPolicy[] { new PurchasePolicy() });
            var savesBefore = repository.SaveCount;

            var result = service.Execute(
                session.Id,
                "house",
                CreateCommand("purchase-1", "p1-house", 1m));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(repository.SaveCount, Is.EqualTo(savesBefore + 1));
        }

        private static EconomicResourceDefinition CreateResource(
            decimal maximum,
            string policyId = "purchase") =>
            new EconomicResourceDefinition(
                "house",
                "Configured house",
                "asset",
                "unit",
                policyId,
                Array.Empty<GameEffectDefinition>(),
                Array.Empty<GameEffectDefinition>(),
                maximum);

        private static EconomicResourceCommand CreateCommand(
            string id,
            string accountId,
            decimal quantity) =>
            new EconomicResourceCommand(
                id,
                new PlayerId("p1"),
                accountId,
                quantity,
                Array.Empty<DefinitionParameter>(),
                new DateTimeOffset(2026, 10, 4, 20, 0, 0, TimeSpan.Zero),
                "facilitator",
                "Configured purchase");

        private static EconomyDefinition CreateEconomy() =>
            new EconomyDefinition(
                new[]
                {
                    new EconomyAccountDefinition("bank-cash", "Bank cash", "bank", "cash", 0m),
                    new EconomyAccountDefinition("p1-cash", "Player cash", "p1", "cash", 0m),
                    new EconomyAccountDefinition("asset-supply", "House supply", "bank", "house", 0m),
                    new EconomyAccountDefinition("p1-house", "Player houses", "p1", "house", 0m)
                },
                new[]
                {
                    new EconomyBalanceDefinition("bank-cash", 100m),
                    new EconomyBalanceDefinition("p1-cash", 20m),
                    new EconomyBalanceDefinition("asset-supply", 10m),
                    new EconomyBalanceDefinition("p1-house", 0m)
                });

        private sealed class PurchasePolicy : IEconomicResourceTransactionPolicy
        {
            public string PolicyId => "purchase";

            public OperationResult<EconomyTransactionRequest> CreateTransaction(
                EconomicResourceDefinition definition,
                EconomicResourceCommand command,
                EconomySnapshot economy)
            {
                var price = 8m * command.QuantityDelta;
                return OperationResult<EconomyTransactionRequest>.Success(
                    new EconomyTransactionRequest(
                        command.Id,
                        new[]
                        {
                            new EconomyTransactionEntry(command.TargetAccountId, command.QuantityDelta),
                            new EconomyTransactionEntry("asset-supply", -command.QuantityDelta),
                            new EconomyTransactionEntry("p1-cash", -price),
                            new EconomyTransactionEntry("bank-cash", price)
                        },
                        definition.Id,
                        command.CreatedAt,
                        command.OperatorId,
                        command.Reason));
            }
        }

        private sealed class MetadataReplacingPolicy : IEconomicResourceTransactionPolicy
        {
            public string PolicyId => "metadata-replacing";

            public OperationResult<EconomyTransactionRequest> CreateTransaction(
                EconomicResourceDefinition definition,
                EconomicResourceCommand command,
                EconomySnapshot economy)
            {
                return OperationResult<EconomyTransactionRequest>.Success(
                    new EconomyTransactionRequest(
                        command.Id,
                        new[]
                        {
                            new EconomyTransactionEntry(command.TargetAccountId, command.QuantityDelta),
                            new EconomyTransactionEntry("asset-supply", -command.QuantityDelta),
                            new EconomyTransactionEntry("p1-cash", -8m),
                            new EconomyTransactionEntry("bank-cash", 8m)
                        },
                        definition.Id,
                        command.CreatedAt,
                        "another-operator",
                        command.Reason));
            }
        }

        private sealed class MemoryRepository : IGameSessionRepository
        {
            private readonly Dictionary<SessionId, GameSession> _sessions = new Dictionary<SessionId, GameSession>();
            public int SaveCount { get; private set; }
            public GameSession Get(SessionId sessionId) => _sessions[sessionId];
            public void Save(GameSession session)
            {
                _sessions[session.Id] = session;
                SaveCount++;
            }
        }
    }
}

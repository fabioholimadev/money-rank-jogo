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
    public sealed class InstitutionOperationTests
    {
        [Test]
        public void LoanPolicyBalancesCashAndDebtUnitsIndependently()
        {
            var economy = CreateEconomy();
            var state = economy.CreateState();
            var history = new EconomyTransactionHistory();
            var service = new InstitutionOperationService(
                economy,
                new IInstitutionOperationPolicy[] { new LoanPolicy() });

            var result = service.Execute(
                CreateDefinition("bank-loan", "credit", "bank", "loan"),
                state,
                history,
                CreateCommand("loan-1"));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(history.Records, Has.Count.EqualTo(1));
            state.TryGetBalance("p1-cash", out var cash);
            state.TryGetBalance("p1-debt", out var debt);
            Assert.That(cash, Is.EqualTo(10m));
            Assert.That(debt, Is.EqualTo(10m));
        }

        [Test]
        public void PublicAidUsesTheConfiguredTreasuryOwnerInsteadOfTheBank()
        {
            var economy = CreateEconomy();
            var state = economy.CreateState();
            var service = new InstitutionOperationService(
                economy,
                new IInstitutionOperationPolicy[] { new PublicAidPolicy() });

            var result = service.Execute(
                CreateDefinition("public-aid", "public-aid", "treasury", "public-aid"),
                state,
                new EconomyTransactionHistory(),
                CreateCommand("aid-1"));

            Assert.That(result.Succeeded, Is.True);
            state.TryGetBalance("treasury-cash", out var treasury);
            state.TryGetBalance("bank-cash", out var bank);
            Assert.That(treasury, Is.EqualTo(46m));
            Assert.That(bank, Is.EqualTo(100m));
        }

        [Test]
        public void TransactionMustIncludeConfiguredInstitutionAndRequestingPlayer()
        {
            var economy = CreateEconomy();
            var state = economy.CreateState();
            var history = new EconomyTransactionHistory();
            var service = new InstitutionOperationService(
                economy,
                new IInstitutionOperationPolicy[] { new WrongParticipantsPolicy() });

            var result = service.Execute(
                CreateDefinition("invalid", "credit", "bank", "wrong-participants"),
                state,
                history,
                CreateCommand("invalid-1"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INSTITUTION_TRANSACTION_PARTICIPANTS_INVALID"));
            Assert.That(history.Records, Is.Empty);
        }

        [Test]
        public void PolicyCannotReplaceAuditMetadata()
        {
            var economy = CreateEconomy();
            var service = new InstitutionOperationService(
                economy,
                new IInstitutionOperationPolicy[] { new MetadataReplacingPolicy() });

            var result = service.Execute(
                CreateDefinition("invalid", "credit", "bank", "replace-metadata"),
                economy.CreateState(),
                new EconomyTransactionHistory(),
                CreateCommand("invalid-1"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("INSTITUTION_TRANSACTION_METADATA_MISMATCH"));
        }

        [Test]
        public void SerializableDefinitionKeepsInstitutionCategoryAndPolicyAsData()
        {
            var definition = new InstitutionOperationDefinitionData(
                "custom",
                "Custom operation",
                "renegotiation",
                "configured-institution",
                "configured-policy").ToDomain();

            Assert.That(definition.CategoryId, Is.EqualTo("renegotiation"));
            Assert.That(definition.InstitutionOwnerId, Is.EqualTo("configured-institution"));
            Assert.That(definition.PolicyId, Is.EqualTo("configured-policy"));
        }

        [Test]
        public void ApplicationServicePersistsOnlyAcceptedInstitutionOperation()
        {
            var repository = new MemoryRepository();
            var economy = CreateEconomy();
            var session = new GameSession(
                new SessionId("institution-session"),
                new SessionRules(2, 2),
                new GameFlowStateMachine(),
                null,
                economy: economy);
            session.AddPlayer(new Player(new PlayerId("p1"), "Player 1", 0));
            session.AddPlayer(new Player(new PlayerId("p2"), "Player 2", 1));
            repository.Save(session);
            var service = new GameInstitutionOperationService(
                repository,
                new[] { CreateDefinition("bank-loan", "credit", "bank", "loan") },
                new IInstitutionOperationPolicy[] { new LoanPolicy() });
            var savesBefore = repository.SaveCount;

            var accepted = service.Execute(session.Id, "bank-loan", CreateCommand("loan-1"));
            var rejected = service.Execute(session.Id, "missing", CreateCommand("loan-2"));

            Assert.That(accepted.Succeeded, Is.True);
            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(repository.SaveCount, Is.EqualTo(savesBefore + 1));
        }

        private static InstitutionOperationDefinition CreateDefinition(
            string id,
            string category,
            string owner,
            string policy) =>
            new InstitutionOperationDefinition(id, id, category, owner, policy);

        private static InstitutionOperationCommand CreateCommand(string id) =>
            new InstitutionOperationCommand(
                id,
                new PlayerId("p1"),
                Array.Empty<DefinitionParameter>(),
                new DateTimeOffset(2026, 10, 4, 22, 0, 0, TimeSpan.Zero),
                "facilitator",
                "Configured institution operation");

        private static EconomyDefinition CreateEconomy() =>
            new EconomyDefinition(
                new[]
                {
                    new EconomyAccountDefinition("bank-cash", "Bank cash", "bank", "bank-cash", 0m, unitId: "cash"),
                    new EconomyAccountDefinition("p1-cash", "Player cash", "p1", "cash", 0m, unitId: "cash"),
                    new EconomyAccountDefinition("p2-cash", "Other player cash", "p2", "cash", 0m, unitId: "cash"),
                    new EconomyAccountDefinition("treasury-cash", "Public treasury", "treasury", "public-funds", 0m, unitId: "cash"),
                    new EconomyAccountDefinition("bank-receivable", "Bank receivable", "bank", "receivable", maximumBalance: 0m, unitId: "debt"),
                    new EconomyAccountDefinition("p1-debt", "Player debt", "p1", "debt", 0m, unitId: "debt")
                },
                new[]
                {
                    new EconomyBalanceDefinition("bank-cash", 100m),
                    new EconomyBalanceDefinition("p1-cash", 0m),
                    new EconomyBalanceDefinition("p2-cash", 0m),
                    new EconomyBalanceDefinition("treasury-cash", 50m),
                    new EconomyBalanceDefinition("bank-receivable", 0m),
                    new EconomyBalanceDefinition("p1-debt", 0m)
                });

        private sealed class LoanPolicy : IInstitutionOperationPolicy
        {
            public string PolicyId => "loan";

            public OperationResult<EconomyTransactionRequest> CreateTransaction(
                InstitutionOperationDefinition definition,
                InstitutionOperationCommand command,
                EconomySnapshot economy) =>
                Success(
                    definition,
                    command,
                    new EconomyTransactionEntry("bank-cash", -10m),
                    new EconomyTransactionEntry("p1-cash", 10m),
                    new EconomyTransactionEntry("bank-receivable", -10m),
                    new EconomyTransactionEntry("p1-debt", 10m));
        }

        private sealed class PublicAidPolicy : IInstitutionOperationPolicy
        {
            public string PolicyId => "public-aid";

            public OperationResult<EconomyTransactionRequest> CreateTransaction(
                InstitutionOperationDefinition definition,
                InstitutionOperationCommand command,
                EconomySnapshot economy) =>
                Success(
                    definition,
                    command,
                    new EconomyTransactionEntry("treasury-cash", -4m),
                    new EconomyTransactionEntry("p1-cash", 4m));
        }

        private sealed class WrongParticipantsPolicy : IInstitutionOperationPolicy
        {
            public string PolicyId => "wrong-participants";

            public OperationResult<EconomyTransactionRequest> CreateTransaction(
                InstitutionOperationDefinition definition,
                InstitutionOperationCommand command,
                EconomySnapshot economy) =>
                Success(
                    definition,
                    command,
                    new EconomyTransactionEntry("p2-cash", -1m),
                    new EconomyTransactionEntry("p1-cash", 1m));
        }

        private sealed class MetadataReplacingPolicy : IInstitutionOperationPolicy
        {
            public string PolicyId => "replace-metadata";

            public OperationResult<EconomyTransactionRequest> CreateTransaction(
                InstitutionOperationDefinition definition,
                InstitutionOperationCommand command,
                EconomySnapshot economy) =>
                OperationResult<EconomyTransactionRequest>.Success(
                    new EconomyTransactionRequest(
                        command.Id,
                        new[]
                        {
                            new EconomyTransactionEntry("bank-cash", -1m),
                            new EconomyTransactionEntry("p1-cash", 1m)
                        },
                        definition.Id,
                        command.CreatedAt,
                        "another-operator",
                        command.Reason));
        }

        private static OperationResult<EconomyTransactionRequest> Success(
            InstitutionOperationDefinition definition,
            InstitutionOperationCommand command,
            params EconomyTransactionEntry[] entries) =>
            OperationResult<EconomyTransactionRequest>.Success(
                new EconomyTransactionRequest(
                    command.Id,
                    entries,
                    definition.Id,
                    command.CreatedAt,
                    command.OperatorId,
                    command.Reason));

        private sealed class MemoryRepository : IGameSessionRepository
        {
            private readonly Dictionary<SessionId, GameSession> _sessions =
                new Dictionary<SessionId, GameSession>();

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

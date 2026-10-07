using System;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class EventSelectionTests
    {
        [Test]
        public void RegisteredPolicySelectsAnExactVersionFromTheRequestedTable()
        {
            var catalog = CreateCatalog();
            var service = new EventSelectionService(
                catalog,
                new IEventSelectionPolicy[] { new IndexedPolicy("configured", 1) });

            var result = service.Select(
                "table-a",
                "configured",
                CreateContext(7));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.Definition.Id, Is.EqualTo("event-b"));
            Assert.That(result.Value.Definition.Version, Is.EqualTo(2));
            Assert.That(result.Value.TableId, Is.EqualTo("table-a"));
            Assert.That(result.Value.PolicyId, Is.EqualTo("configured"));
            Assert.That(result.Value.DrawIndex, Is.EqualTo(7));
        }

        [Test]
        public void MissingSelectionPolicyReturnsStableFailure()
        {
            var service = new EventSelectionService(
                CreateCatalog(),
                Array.Empty<IEventSelectionPolicy>());

            var result = service.Select("table-a", "missing", CreateContext(0));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("EVENT_SELECTION_POLICY_NOT_FOUND"));
        }

        [Test]
        public void PolicyCannotSelectAnEventOutsideTheRequestedTable()
        {
            var service = new EventSelectionService(
                CreateCatalog(),
                new IEventSelectionPolicy[] { new FixedPolicy("rogue", new EventDefinitionKey("event-c", 1)) });

            var result = service.Select("table-a", "rogue", CreateContext(0));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("EVENT_SELECTION_OUTSIDE_TABLE"));
        }

        [Test]
        public void RejectedPolicyDoesNotFallBackToAnInventedSelection()
        {
            var service = new EventSelectionService(
                CreateCatalog(),
                new IEventSelectionPolicy[] { new RejectingPolicy() });

            var result = service.Select("table-a", "rejecting", CreateContext(0));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("SELECTION_NOT_CONFIGURED"));
        }

        private static EventSelectionContext CreateContext(int drawIndex) =>
            new EventSelectionContext(
                new SessionId("session"),
                new PlayerId("player"),
                "space-01",
                drawIndex,
                Array.Empty<DefinitionParameter>());

        private static EventContentCatalog CreateCatalog()
        {
            var eventA = CreateEvent("event-a", 1);
            var eventB = CreateEvent("event-b", 2);
            var eventC = CreateEvent("event-c", 1);
            return new EventContentCatalog(
                new[] { eventA, eventB, eventC },
                new[]
                {
                    new EventTableDefinition(
                        "table-a",
                        new[]
                        {
                            new EventDefinitionKey(eventA.Id, eventA.Version),
                            new EventDefinitionKey(eventB.Id, eventB.Version)
                        }),
                    new EventTableDefinition(
                        "table-c",
                        new[] { new EventDefinitionKey(eventC.Id, eventC.Version) })
                });
        }

        private static EventDefinition CreateEvent(string id, int version) =>
            new EventDefinition(
                id,
                version,
                "category",
                id,
                "Educational content.",
                Array.Empty<string>(),
                Array.Empty<RequirementDefinition>(),
                new[]
                {
                    new EventOptionDefinition(
                        "continue",
                        "Continue",
                        string.Empty,
                        Array.Empty<RequirementDefinition>(),
                        Array.Empty<GameEffectDefinition>(),
                        "always")
                });

        private sealed class IndexedPolicy : IEventSelectionPolicy
        {
            private readonly int _index;

            public IndexedPolicy(string policyId, int index)
            {
                PolicyId = policyId;
                _index = index;
            }

            public string PolicyId { get; }

            public OperationResult<EventDefinitionKey> Select(
                EventTableDefinition table,
                EventSelectionContext context) =>
                OperationResult<EventDefinitionKey>.Success(table.Events[_index]);
        }

        private sealed class FixedPolicy : IEventSelectionPolicy
        {
            private readonly EventDefinitionKey _selection;

            public FixedPolicy(string policyId, EventDefinitionKey selection)
            {
                PolicyId = policyId;
                _selection = selection;
            }

            public string PolicyId { get; }

            public OperationResult<EventDefinitionKey> Select(
                EventTableDefinition table,
                EventSelectionContext context) =>
                OperationResult<EventDefinitionKey>.Success(_selection);
        }

        private sealed class RejectingPolicy : IEventSelectionPolicy
        {
            public string PolicyId => "rejecting";

            public OperationResult<EventDefinitionKey> Select(
                EventTableDefinition table,
                EventSelectionContext context) =>
                OperationResult<EventDefinitionKey>.Failure(
                    "SELECTION_NOT_CONFIGURED",
                    "No event selection has been configured for this context.");
        }
    }
}

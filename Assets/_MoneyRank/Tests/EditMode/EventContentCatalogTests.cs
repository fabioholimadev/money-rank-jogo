using System;
using MoneyRank.Content;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class EventContentCatalogTests
    {
        [Test]
        public void CatalogResolvesAnExactEventVersionFromATable()
        {
            var firstVersion = CreateEvent("event", 1);
            var secondVersion = CreateEvent("event", 2);
            var table = new EventTableDefinition(
                "events-test",
                new[] { new EventDefinitionKey("event", 2) });

            var catalog = new EventContentCatalog(
                new[] { firstVersion, secondVersion },
                new[] { table });

            Assert.That(catalog.GetTable("events-test"), Is.SameAs(table));
            Assert.That(catalog.GetEvent(table.Events[0]), Is.SameAs(secondVersion));
        }

        [Test]
        public void CatalogRejectsDuplicateEventVersions()
        {
            Assert.Throws<ArgumentException>(() =>
                new EventContentCatalog(
                    new[] { CreateEvent("event", 1), CreateEvent("event", 1) },
                    Array.Empty<EventTableDefinition>()));
        }

        [Test]
        public void CatalogRejectsReferencesToMissingVersions()
        {
            var table = new EventTableDefinition(
                "events-test",
                new[] { new EventDefinitionKey("event", 2) });

            var exception = Assert.Throws<ArgumentException>(() =>
                new EventContentCatalog(
                    new[] { CreateEvent("event", 1) },
                    new[] { table }));

            Assert.That(exception.Message, Does.Contain("event@2"));
        }

        [Test]
        public void EventTableCopiesReferencesAndRejectsDuplicates()
        {
            var references = new[] { new EventDefinitionKey("event", 1) };
            var table = new EventTableDefinition("events-test", references);

            Assert.That(table.Events, Is.EqualTo(references));
            Assert.Throws<ArgumentException>(() =>
                new EventTableDefinition("events-test", new[] { references[0], references[0] }));
        }

        [Test]
        public void SerializableAuthoringDataConvertsToImmutableDomainDefinitions()
        {
            var condition = new RequirementDefinitionData(
                "minimum-value",
                new[] { new DefinitionParameterData("value", "2") });
            var option = new EventOptionDefinitionData(
                "accept",
                "Accept",
                "Test option.",
                Array.Empty<RequirementDefinitionData>(),
                new[]
                {
                    new GameEffectDefinitionData(
                        "current-player/test-state",
                        "add",
                        "1",
                        null,
                        "event:test@2")
                },
                "always");
            var data = new EventDefinitionData(
                "test",
                2,
                "test-category",
                "Test title",
                "Educational text.",
                new[] { "test-tag" },
                new[] { condition },
                new[] { option },
                presentationId: "default-card");

            var definition = data.ToDomain();

            Assert.That(definition.Id, Is.EqualTo("test"));
            Assert.That(definition.Version, Is.EqualTo(2));
            Assert.That(definition.Conditions[0].Parameters["value"], Is.EqualTo("2"));
            Assert.That(definition.Options[0].Effects[0].OperationId, Is.EqualTo("add"));
            Assert.That(definition.PresentationId, Is.EqualTo("default-card"));
        }

        [Test]
        public void SerializableTableDataPreservesVersionedReferences()
        {
            var data = new EventTableDefinitionData(
                "events-test",
                new[] { new EventReferenceData("test", 4) });

            var definition = data.ToDomain();

            Assert.That(definition.Id, Is.EqualTo("events-test"));
            Assert.That(definition.Events[0], Is.EqualTo(new EventDefinitionKey("test", 4)));
        }

        private static EventDefinition CreateEvent(string id, int version) =>
            new EventDefinition(
                id,
                version,
                "test-category",
                "Test title",
                "Educational text.",
                Array.Empty<string>(),
                Array.Empty<RequirementDefinition>(),
                new[]
                {
                    new EventOptionDefinition(
                        "continue",
                        "Continue",
                        "Test option.",
                        Array.Empty<RequirementDefinition>(),
                        Array.Empty<GameEffectDefinition>(),
                        "always")
                });
    }
}

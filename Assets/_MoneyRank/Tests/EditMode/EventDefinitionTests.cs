using System;
using System.Collections.Generic;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class EventDefinitionTests
    {
        [Test]
        public void DefinitionPreservesVersionedEducationalContentAndGenericRules()
        {
            var requirement = new RequirementDefinition(
                "minimum-value",
                new[]
                {
                    new DefinitionParameter("state-id", "configurable-state"),
                    new DefinitionParameter("value", "2")
                });
            var effect = new GameEffectDefinition(
                "current-player/configurable-state",
                "add",
                "1",
                null,
                "event:test-event@3");
            var option = new EventOptionDefinition(
                "accept",
                "Accept",
                "Temporary test option.",
                new[] { requirement },
                new[] { effect },
                "always");

            var definition = new EventDefinition(
                "test-event",
                3,
                "test-category",
                "Test event",
                "Educational context remains part of the versioned definition.",
                new[] { "test", "education" },
                Array.Empty<RequirementDefinition>(),
                new[] { option },
                presentationId: "default-card");

            Assert.That(definition.Version, Is.EqualTo(3));
            Assert.That(definition.EducationalText, Does.Contain("versioned"));
            Assert.That(definition.GetOption("accept"), Is.SameAs(option));
            Assert.That(option.Requirements[0].Parameters["state-id"], Is.EqualTo("configurable-state"));
            Assert.That(option.Effects[0].TargetId, Is.EqualTo("current-player/configurable-state"));
        }

        [Test]
        public void ConstructorCopiesCollections()
        {
            var tags = new List<string> { "initial" };
            var options = new List<EventOptionDefinition> { CreateOption("one") };
            var definition = CreateEvent(tags, options);

            tags.Add("later");
            options.Add(CreateOption("two"));

            Assert.That(definition.Tags, Is.EqualTo(new[] { "initial" }));
            Assert.That(definition.Options, Has.Count.EqualTo(1));
        }

        [Test]
        public void VersionMustBePositive()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new EventDefinition(
                    "event",
                    0,
                    "category",
                    "Title",
                    "Educational text",
                    Array.Empty<string>(),
                    Array.Empty<RequirementDefinition>(),
                    new[] { CreateOption("one") }));
        }

        [Test]
        public void EventRequiresAtLeastOneOption()
        {
            Assert.Throws<ArgumentException>(() =>
                new EventDefinition(
                    "event",
                    1,
                    "category",
                    "Title",
                    "Educational text",
                    Array.Empty<string>(),
                    Array.Empty<RequirementDefinition>(),
                    Array.Empty<EventOptionDefinition>()));
        }

        [Test]
        public void OptionIdsMustBeUniqueWithinEvent()
        {
            Assert.Throws<ArgumentException>(() =>
                CreateEvent(
                    Array.Empty<string>(),
                    new[] { CreateOption("same"), CreateOption("same") }));
        }

        [Test]
        public void RequirementParameterKeysMustBeUnique()
        {
            Assert.Throws<ArgumentException>(() =>
                new RequirementDefinition(
                    "test-requirement",
                    new[]
                    {
                        new DefinitionParameter("same", "one"),
                        new DefinitionParameter("same", "two")
                    }));
        }

        [Test]
        public void EffectRequiresStableTargetOperationAndSourceIds()
        {
            Assert.Throws<ArgumentException>(() =>
                new GameEffectDefinition("", "add", "1", null, "event:test"));
            Assert.Throws<ArgumentException>(() =>
                new GameEffectDefinition("current-player/state", "", "1", null, "event:test"));
            Assert.Throws<ArgumentException>(() =>
                new GameEffectDefinition("current-player/state", "add", "1", null, ""));
        }

        private static EventDefinition CreateEvent(
            IEnumerable<string> tags,
            IEnumerable<EventOptionDefinition> options) =>
            new EventDefinition(
                "test-event",
                1,
                "test-category",
                "Test event",
                "Educational text",
                tags,
                Array.Empty<RequirementDefinition>(),
                options);

        private static EventOptionDefinition CreateOption(string id) =>
            new EventOptionDefinition(
                id,
                "Option",
                "Description",
                Array.Empty<RequirementDefinition>(),
                Array.Empty<GameEffectDefinition>(),
                "always");
    }
}

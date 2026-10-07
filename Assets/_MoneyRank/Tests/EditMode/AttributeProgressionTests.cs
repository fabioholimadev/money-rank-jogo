using System;
using MoneyRank.Content;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class AttributeProgressionTests
    {
        [Test]
        public void RegisteredPolicyControlsThresholdAndPreservesHistoricalCycles()
        {
            var definition = CreateDefinition();
            var state = new AttributeState("test-attribute", 2, 4);
            var service = new AttributeProgressionService(
                new IAttributeOverflowHandler[] { new CarryOverflowHandler() });

            var result = service.Apply(definition, state, 5);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(state.CurrentProgress, Is.EqualTo(1));
            Assert.That(state.CompletedCycles, Is.EqualTo(6));
            Assert.That(result.Value.CompletedCycles, Is.EqualTo(2));
            Assert.That(result.Value.TotalCompletedCycles, Is.EqualTo(6));
            Assert.That(result.Value.PlannedCompletionEffects, Has.Count.EqualTo(2));
        }

        [Test]
        public void MissingPolicyDoesNotMutateAttributeState()
        {
            var state = new AttributeState("test-attribute", 2, 1);
            var service = new AttributeProgressionService(Array.Empty<IAttributeOverflowHandler>());

            var result = service.Apply(CreateDefinition(), state, 1);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ATTRIBUTE_OVERFLOW_HANDLER_NOT_FOUND"));
            Assert.That(state.CurrentProgress, Is.EqualTo(2));
            Assert.That(state.CompletedCycles, Is.EqualTo(1));
        }

        [Test]
        public void RejectedDeltaDoesNotMutateAttributeState()
        {
            var state = new AttributeState("test-attribute", 1);
            var service = new AttributeProgressionService(
                new IAttributeOverflowHandler[] { new CarryOverflowHandler() });

            var result = service.Apply(CreateDefinition(), state, -2);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("ATTRIBUTE_PROGRESS_BELOW_ZERO"));
            Assert.That(state.CurrentProgress, Is.EqualTo(1));
        }

        [Test]
        public void SerializableDefinitionKeepsIdsConfigurable()
        {
            var data = new AttributeDefinitionData(
                "configurable-attribute",
                "Configurable attribute",
                "custom-type",
                7,
                "custom-overflow",
                Array.Empty<GameEffectDefinitionData>(),
                "icon-id",
                "presentation-id");

            var definition = data.ToDomain();

            Assert.That(definition.Id, Is.EqualTo("configurable-attribute"));
            Assert.That(definition.TypeId, Is.EqualTo("custom-type"));
            Assert.That(definition.Threshold, Is.EqualTo(7));
            Assert.That(definition.OverflowBehaviorId, Is.EqualTo("custom-overflow"));
        }

        [Test]
        public void DuplicatePolicyRegistrationIsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                new AttributeProgressionService(
                    new IAttributeOverflowHandler[]
                    {
                        new CarryOverflowHandler(),
                        new CarryOverflowHandler()
                    }));
        }

        private static AttributeDefinition CreateDefinition() =>
            new AttributeDefinition(
                "test-attribute",
                "Test attribute",
                "test-type",
                3,
                "carry",
                new[]
                {
                    new GameEffectDefinition(
                        "current-player/inner-board",
                        "test-progress",
                        "1",
                        null,
                        "attribute:test-attribute")
                });

        private sealed class CarryOverflowHandler : IAttributeOverflowHandler
        {
            public string BehaviorId => "carry";

            public OperationResult<AttributeProgressionCalculation> Calculate(
                int currentProgress,
                int delta,
                int threshold)
            {
                var total = currentProgress + delta;
                if (total < 0)
                {
                    return OperationResult<AttributeProgressionCalculation>.Failure(
                        "ATTRIBUTE_PROGRESS_BELOW_ZERO",
                        "The configured policy does not allow progress below zero.");
                }

                return OperationResult<AttributeProgressionCalculation>.Success(
                    new AttributeProgressionCalculation(total % threshold, total / threshold));
            }
        }
    }
}

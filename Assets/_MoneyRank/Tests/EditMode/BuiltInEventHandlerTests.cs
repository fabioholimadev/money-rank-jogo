using System;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class BuiltInEventHandlerTests
    {
        [Test]
        public void SetAndRemoveHandlersMutateOnlyTheConfiguredStateKey()
        {
            var state = new EventState(new[] { new DefinitionParameter("preserved", "yes") });
            var set = new SetEventStateEffectHandler();
            var remove = new RemoveEventStateEffectHandler();

            var setResult = set.Apply(
                new GameEffectDefinition("choice", set.OperationId, "accepted", null, "event"),
                state);
            var removeResult = remove.Apply(
                new GameEffectDefinition("choice", remove.OperationId, string.Empty, null, "event"),
                state);

            Assert.That(setResult.Succeeded, Is.True);
            Assert.That(removeResult.Succeeded, Is.True);
            Assert.That(state.TryGetValue("choice", out _), Is.False);
            Assert.That(state.TryGetValue("preserved", out var preserved), Is.True);
            Assert.That(preserved, Is.EqualTo("yes"));
        }

        [Test]
        public void RemoveMissingStateReturnsExplainableFailure()
        {
            var handler = new RemoveEventStateEffectHandler();

            var result = handler.Apply(
                new GameEffectDefinition("missing", handler.OperationId, string.Empty, null, "event"),
                new EventState(Array.Empty<DefinitionParameter>()));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("STATE_VALUE_NOT_FOUND"));
        }

        [Test]
        public void EqualsRequirementUsesConfiguredKeyAndValue()
        {
            var handler = new StateEqualsRequirementHandler();
            var context = new RequirementEvaluationContext(
                new SessionId("session"),
                new PlayerId("player"),
                new[] { new DefinitionParameter("knowledge", "2") });
            var requirement = new RequirementDefinition(
                handler.TypeId,
                new[]
                {
                    new DefinitionParameter("key", "knowledge"),
                    new DefinitionParameter("value", "2")
                });

            Assert.That(handler.Evaluate(requirement, context).IsSatisfied, Is.True);
        }

        [Test]
        public void ExistsRequirementExplainsMissingParameterAndMissingState()
        {
            var handler = new StateExistsRequirementHandler();
            var context = new RequirementEvaluationContext(
                new SessionId("session"),
                new PlayerId("player"),
                Array.Empty<DefinitionParameter>());

            var missingParameter = handler.Evaluate(
                new RequirementDefinition(handler.TypeId, Array.Empty<DefinitionParameter>()),
                context);
            var missingState = handler.Evaluate(
                new RequirementDefinition(
                    handler.TypeId,
                    new[] { new DefinitionParameter("key", "reserve") }),
                context);

            Assert.That(missingParameter.ErrorCode, Is.EqualTo("REQUIREMENT_PARAMETER_MISSING"));
            Assert.That(missingState.ErrorCode, Is.EqualTo("STATE_VALUE_NOT_FOUND"));
        }
    }
}

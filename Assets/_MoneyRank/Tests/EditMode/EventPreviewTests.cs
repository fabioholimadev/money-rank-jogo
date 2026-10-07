using System;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class EventPreviewTests
    {
        [Test]
        public void PreviewEvaluatesConditionsAndRequirementsWithoutApplyingEffects()
        {
            var service = CreateService();
            var definition = CreateEvent(
                new[] { CreateRequirement("status", "ready") },
                new[] { CreateRequirement("level", "2") });
            var context = CreateContext(
                new DefinitionParameter("status", "ready"),
                new DefinitionParameter("level", "2"));

            var result = service.Preview(definition, "accept", context);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.CanConfirm, Is.True);
            Assert.That(result.Value.ConditionResults, Has.Count.EqualTo(1));
            Assert.That(result.Value.RequirementResults, Has.Count.EqualTo(1));
            Assert.That(result.Value.PlannedEffects, Has.Count.EqualTo(1));
            Assert.That(result.Value.PlannedEffects[0].OperationId, Is.EqualTo("test-operation"));
            Assert.That(result.Value.HasProjectedState, Is.True);
            Assert.That(result.Value.ProjectedState.Values["current-player/test-state"], Is.EqualTo("1"));
        }

        [Test]
        public void UnsatisfiedRequirementKeepsPreviewButBlocksConfirmation()
        {
            var service = CreateService();
            var definition = CreateEvent(
                Array.Empty<RequirementDefinition>(),
                new[] { CreateRequirement("level", "3") });
            var context = CreateContext(new DefinitionParameter("level", "2"));

            var result = service.Preview(definition, "accept", context);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.CanConfirm, Is.False);
            Assert.That(result.Value.RequirementResults[0].Result.ErrorCode, Is.EqualTo("STATE_VALUE_MISMATCH"));
            Assert.That(result.Value.PlannedEffects, Has.Count.EqualTo(1));
        }

        [Test]
        public void MissingHandlerProducesStableDiagnosticAndBlocksConfirmation()
        {
            var evaluator = new RequirementEvaluator(Array.Empty<IRequirementHandler>());
            var service = new EventPreviewService(
                evaluator,
                new EffectExecutor(new IEffectHandler[] { new TestEffectHandler() }));
            var definition = CreateEvent(
                Array.Empty<RequirementDefinition>(),
                new[] { new RequirementDefinition("unknown", Array.Empty<DefinitionParameter>()) });

            var result = service.Preview(definition, "accept", CreateContext());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.CanConfirm, Is.False);
            Assert.That(
                result.Value.RequirementResults[0].Result.ErrorCode,
                Is.EqualTo("REQUIREMENT_HANDLER_NOT_FOUND"));
        }

        [Test]
        public void UnknownOptionDoesNotProducePreview()
        {
            var result = CreateService().Preview(
                CreateEvent(Array.Empty<RequirementDefinition>(), Array.Empty<RequirementDefinition>()),
                "missing",
                CreateContext());

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("EVENT_OPTION_NOT_FOUND"));
        }

        [Test]
        public void MissingEffectHandlerBlocksConfirmationAndExplainsThePreviewFailure()
        {
            var service = new EventPreviewService(
                new RequirementEvaluator(new IRequirementHandler[] { new StateEqualsRequirementHandler() }),
                new EffectExecutor(Array.Empty<IEffectHandler>()));

            var result = service.Preview(
                CreateEvent(Array.Empty<RequirementDefinition>(), Array.Empty<RequirementDefinition>()),
                "accept",
                CreateContext());

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.CanConfirm, Is.False);
            Assert.That(result.Value.HasProjectedState, Is.False);
            Assert.That(result.Value.EffectErrorCode, Is.EqualTo("EFFECT_HANDLER_NOT_FOUND"));
        }

        [Test]
        public void DuplicateHandlerRegistrationIsRejected()
        {
            Assert.Throws<ArgumentException>(() =>
                new RequirementEvaluator(
                    new IRequirementHandler[]
                    {
                        new StateEqualsRequirementHandler(),
                        new StateEqualsRequirementHandler()
                    }));
        }

        [Test]
        public void PreviewDoesNotMutateSessionState()
        {
            var board = new BoardDefinition(
                "board",
                new[]
                {
                    new BoardSpaceDefinition("start", 0, "start"),
                    new BoardSpaceDefinition("event", 1, "event")
                },
                0);
            var session = new GameSession(
                new SessionId("session"),
                new SessionRules(1, 1),
                new GameFlowStateMachine(),
                board);
            session.AddPlayer(new Player(new PlayerId("player"), "Player", 0));
            session.Start();
            session.BeginTurn();
            session.RegisterMove(1);
            session.TryGetBoardState(new PlayerId("player"), out var boardState);

            var phaseBeforePreview = session.Phase;
            var positionBeforePreview = boardState.Position;
            var preview = CreateService().Preview(
                CreateEvent(Array.Empty<RequirementDefinition>(), Array.Empty<RequirementDefinition>()),
                "accept",
                CreateContext());

            Assert.That(preview.Succeeded, Is.True);
            Assert.That(session.Phase, Is.EqualTo(phaseBeforePreview));
            Assert.That(boardState.Position, Is.EqualTo(positionBeforePreview));
        }

        private static EventPreviewService CreateService() =>
            new EventPreviewService(
                new RequirementEvaluator(
                    new IRequirementHandler[] { new StateEqualsRequirementHandler() }),
                new EffectExecutor(new IEffectHandler[] { new TestEffectHandler() }));

        private static RequirementEvaluationContext CreateContext(params DefinitionParameter[] values) =>
            new RequirementEvaluationContext(
                new SessionId("session"),
                new PlayerId("player"),
                values);

        private static RequirementDefinition CreateRequirement(string stateKey, string expectedValue) =>
            new RequirementDefinition(
                "state-equals",
                new[]
                {
                    new DefinitionParameter("state-key", stateKey),
                    new DefinitionParameter("expected-value", expectedValue)
                });

        private static EventDefinition CreateEvent(
            RequirementDefinition[] conditions,
            RequirementDefinition[] optionRequirements)
        {
            var effect = new GameEffectDefinition(
                "current-player/test-state",
                "test-operation",
                "1",
                null,
                "event:test@1");
            var option = new EventOptionDefinition(
                "accept",
                "Accept",
                "Test option",
                optionRequirements,
                new[] { effect },
                "always");

            return new EventDefinition(
                "test",
                1,
                "test-category",
                "Test",
                "Test educational text",
                Array.Empty<string>(),
                conditions,
                new[] { option });
        }

        private sealed class StateEqualsRequirementHandler : IRequirementHandler
        {
            public string TypeId => "state-equals";

            public RequirementCheckResult Evaluate(
                RequirementDefinition requirement,
                RequirementEvaluationContext context)
            {
                var stateKey = requirement.Parameters["state-key"];
                var expectedValue = requirement.Parameters["expected-value"];
                return context.StateValues.TryGetValue(stateKey, out var actualValue) &&
                       string.Equals(actualValue, expectedValue, StringComparison.Ordinal)
                    ? RequirementCheckResult.Pass()
                    : RequirementCheckResult.Fail(
                        "STATE_VALUE_MISMATCH",
                        $"State value '{stateKey}' does not match the expected value.");
            }
        }

        private sealed class TestEffectHandler : IEffectHandler
        {
            public string OperationId => "test-operation";

            public EffectApplicationResult Apply(GameEffectDefinition effect, EventState state)
            {
                state.SetValue(effect.TargetId, effect.Value);
                return EffectApplicationResult.Success();
            }
        }
    }
}

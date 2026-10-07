using System;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class EventConfirmationTests
    {
        [Test]
        public void ConfirmationCommitsEffectsAndAppendsVersionedHistory()
        {
            var state = CreateState("before");
            var history = new EventResolutionHistory();
            var preview = CreatePreview(canConfirm: true, "set-value");
            var service = CreateService(new SetValueEffectHandler());
            var createdAt = new DateTimeOffset(2026, 10, 3, 20, 0, 0, TimeSpan.Zero);

            var result = service.Confirm(
                preview,
                state,
                history,
                new EventConfirmationRequest("resolution-1", "space-01", "local-master", createdAt, "facilitator"));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(state.TryGetValue("test-target", out var value), Is.True);
            Assert.That(value, Is.EqualTo("after"));
            Assert.That(history.Records, Has.Count.EqualTo(1));
            Assert.That(result.Value.EventId, Is.EqualTo("event-test"));
            Assert.That(result.Value.EventVersion, Is.EqualTo(2));
            Assert.That(result.Value.BeforeState.Values["test-target"], Is.EqualTo("before"));
            Assert.That(result.Value.AfterState.Values["test-target"], Is.EqualTo("after"));
            Assert.That(result.Value.CreatedAt, Is.EqualTo(createdAt));
        }

        [Test]
        public void UnsatisfiedPreviewCannotBeConfirmed()
        {
            var state = CreateState("before");
            var history = new EventResolutionHistory();

            var result = CreateService(new SetValueEffectHandler()).Confirm(
                CreatePreview(canConfirm: false, "set-value"),
                state,
                history,
                CreateRequest("resolution-1"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("EVENT_REQUIREMENTS_NOT_SATISFIED"));
            AssertStateAndHistoryRemainUnchanged(state, history);
        }

        [Test]
        public void MissingEffectHandlerDoesNotMutateStateOrHistory()
        {
            var state = CreateState("before");
            var history = new EventResolutionHistory();

            var result = CreateService().Confirm(
                CreatePreview(canConfirm: true, "unknown-operation"),
                state,
                history,
                CreateRequest("resolution-1"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("EFFECT_HANDLER_NOT_FOUND"));
            AssertStateAndHistoryRemainUnchanged(state, history);
        }

        [Test]
        public void FailureAfterEarlierEffectRollsBackTheWholeConfirmation()
        {
            var state = CreateState("before");
            var history = new EventResolutionHistory();
            var preview = CreatePreview(
                true,
                new GameEffectDefinition("test-target", "set-value", "intermediate", null, "event:event-test@2"),
                new GameEffectDefinition("test-target", "fail", "ignored", null, "event:event-test@2"));

            var result = CreateService(new SetValueEffectHandler(), new FailingEffectHandler()).Confirm(
                preview,
                state,
                history,
                CreateRequest("resolution-1"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("TEST_EFFECT_FAILED"));
            AssertStateAndHistoryRemainUnchanged(state, history);
        }

        [Test]
        public void DuplicateResolutionIdIsRejectedBeforeApplyingEffects()
        {
            var state = CreateState("before");
            var history = new EventResolutionHistory();
            var service = CreateService(new SetValueEffectHandler());
            var preview = CreatePreview(canConfirm: true, "set-value");
            service.Confirm(preview, state, history, CreateRequest("same-resolution"));
            state.SetValue("test-target", "manually-changed");

            var duplicate = service.Confirm(preview, state, history, CreateRequest("same-resolution"));

            Assert.That(duplicate.Succeeded, Is.False);
            Assert.That(duplicate.ErrorCode, Is.EqualTo("DUPLICATE_RESOLUTION_ID"));
            Assert.That(state.TryGetValue("test-target", out var value), Is.True);
            Assert.That(value, Is.EqualTo("manually-changed"));
            Assert.That(history.Records, Has.Count.EqualTo(1));
        }

        [Test]
        public void StateChangeAfterPreviewRejectsStaleConfirmation()
        {
            var state = CreateState("before");
            var history = new EventResolutionHistory();
            var preview = CreatePreview(canConfirm: true, "set-value");
            state.SetValue("test-target", "changed-after-preview");

            var result = CreateService(new SetValueEffectHandler()).Confirm(
                preview,
                state,
                history,
                CreateRequest("resolution-1"));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("EVENT_PREVIEW_STALE"));
            Assert.That(state.TryGetValue("test-target", out var value), Is.True);
            Assert.That(value, Is.EqualTo("changed-after-preview"));
            Assert.That(history.Records, Is.Empty);
        }

        private static EventConfirmationService CreateService(params IEffectHandler[] handlers) =>
            new EventConfirmationService(new EffectExecutor(handlers));

        private static EventState CreateState(string value) =>
            new EventState(new[] { new DefinitionParameter("test-target", value) });

        private static EventConfirmationRequest CreateRequest(string resolutionId) =>
            new EventConfirmationRequest(
                resolutionId,
                "space-01",
                "local-master",
                new DateTimeOffset(2026, 10, 3, 20, 0, 0, TimeSpan.Zero),
                "facilitator");

        private static EventResolutionPreview CreatePreview(bool canConfirm, string operationId) =>
            CreatePreview(
                canConfirm,
                new GameEffectDefinition("test-target", operationId, "after", null, "event:event-test@2"));

        private static EventResolutionPreview CreatePreview(
            bool canConfirm,
            params GameEffectDefinition[] effects)
        {
            var option = new EventOptionDefinition(
                "choice",
                "Choice",
                "Test choice",
                Array.Empty<RequirementDefinition>(),
                effects,
                "always");
            var definition = new EventDefinition(
                "event-test",
                2,
                "test-category",
                "Test event",
                "Educational text",
                Array.Empty<string>(),
                Array.Empty<RequirementDefinition>(),
                new[] { option });
            var conditionResults = canConfirm
                ? Array.Empty<RequirementEvaluation>()
                : new[]
                {
                    new RequirementEvaluation(
                        new RequirementDefinition("blocked", Array.Empty<DefinitionParameter>()),
                        RequirementCheckResult.Fail("BLOCKED", "Test requirement is blocked."))
                };

            return new EventResolutionPreview(
                definition,
                option,
                new SessionId("session"),
                new PlayerId("player"),
                new EventStateSnapshot(
                    new[] { new DefinitionParameter("test-target", "before") }),
                conditionResults,
                Array.Empty<RequirementEvaluation>());
        }

        private static void AssertStateAndHistoryRemainUnchanged(
            EventState state,
            EventResolutionHistory history)
        {
            Assert.That(state.TryGetValue("test-target", out var value), Is.True);
            Assert.That(value, Is.EqualTo("before"));
            Assert.That(history.Records, Is.Empty);
        }

        private sealed class SetValueEffectHandler : IEffectHandler
        {
            public string OperationId => "set-value";

            public EffectApplicationResult Apply(GameEffectDefinition effect, EventState state)
            {
                state.SetValue(effect.TargetId, effect.Value);
                return EffectApplicationResult.Success();
            }
        }

        private sealed class FailingEffectHandler : IEffectHandler
        {
            public string OperationId => "fail";

            public EffectApplicationResult Apply(GameEffectDefinition effect, EventState state) =>
                EffectApplicationResult.Failure("TEST_EFFECT_FAILED", "The test effect failed.");
        }
    }
}

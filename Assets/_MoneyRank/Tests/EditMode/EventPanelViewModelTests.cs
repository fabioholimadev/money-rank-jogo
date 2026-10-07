using System;
using System.Collections.Generic;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using MoneyRank.Presentation.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class EventPanelViewModelTests
    {
        [Test]
        public void ShowBuildsOptionsFromTheVersionedDefinition()
        {
            var model = new EventPanelViewModel();

            model.Show(CreateEvent());

            Assert.That(model.IsVisible, Is.True);
            Assert.That(model.EventId, Is.EqualTo("test-event"));
            Assert.That(model.EventVersion, Is.EqualTo(3));
            Assert.That(model.Title, Is.EqualTo("Test event"));
            Assert.That(model.Options, Has.Count.EqualTo(1));
            Assert.That(model.Options[0].Id, Is.EqualTo("accept"));
            Assert.That(model.CanConfirm, Is.False);
        }

        [Test]
        public void PreviewBuildsSortedBeforeAfterRowsAndEnablesConfirmation()
        {
            var definition = CreateEvent();
            var model = new EventPanelViewModel();
            model.Show(definition);
            var before = new EventStateSnapshot(new[]
            {
                new DefinitionParameter("z-value", "same"),
                new DefinitionParameter("balance", "10")
            });
            var after = new EventStateSnapshot(new[]
            {
                new DefinitionParameter("balance", "12"),
                new DefinitionParameter("z-value", "same")
            });
            var preview = CreatePreview(definition, before, after);

            model.ShowPreview(preview);

            Assert.That(model.CanConfirm, Is.True);
            Assert.That(model.SelectedOptionId, Is.EqualTo("accept"));
            Assert.That(model.StateChanges, Has.Count.EqualTo(2));
            Assert.That(model.StateChanges[0].Key, Is.EqualTo("balance"));
            Assert.That(model.StateChanges[0].BeforeValue, Is.EqualTo("10"));
            Assert.That(model.StateChanges[0].AfterValue, Is.EqualTo("12"));
            Assert.That(model.StateChanges[0].Changed, Is.True);
            Assert.That(model.StateChanges[1].Changed, Is.False);
        }

        [Test]
        public void FailedEffectPreviewExplainsWhyConfirmationIsDisabled()
        {
            var definition = CreateEvent();
            var model = new EventPanelViewModel();
            model.Show(definition);
            var before = new EventStateSnapshot(Array.Empty<DefinitionParameter>());
            var preview = new EventResolutionPreview(
                definition,
                definition.Options[0],
                new SessionId("session"),
                new PlayerId("player"),
                before,
                Array.Empty<RequirementEvaluation>(),
                Array.Empty<RequirementEvaluation>(),
                effectErrorCode: "EFFECT_HANDLER_NOT_FOUND",
                effectErrorMessage: "Missing test handler.");

            model.ShowPreview(preview);

            Assert.That(model.CanConfirm, Is.False);
            Assert.That(model.StatusMessage, Does.Contain("Missing test handler"));
            Assert.That(model.StateChanges, Is.Empty);
        }

        [Test]
        public void CancelPreviewReturnsToOptionSelectionWithoutHidingEvent()
        {
            var definition = CreateEvent();
            var model = new EventPanelViewModel();
            model.Show(definition);
            model.ShowPreview(
                CreatePreview(
                    definition,
                    new EventStateSnapshot(new[] { new DefinitionParameter("balance", "10") }),
                    new EventStateSnapshot(new[] { new DefinitionParameter("balance", "12") })));

            model.CancelPreview();

            Assert.That(model.IsVisible, Is.True);
            Assert.That(model.EventId, Is.EqualTo(definition.Id));
            Assert.That(model.HasPreview, Is.False);
            Assert.That(model.SelectedOptionId, Is.Empty);
            Assert.That(model.CanConfirm, Is.False);
            Assert.That(model.StateChanges, Is.Empty);
            Assert.That(model.StatusMessage, Is.Empty);
        }

        [Test]
        public void PreviewForAnotherEventIsRejected()
        {
            var model = new EventPanelViewModel();
            model.Show(CreateEvent());
            var other = CreateEvent("other-event");

            Assert.Throws<InvalidOperationException>(() =>
                model.ShowPreview(
                    CreatePreview(
                        other,
                        new EventStateSnapshot(Array.Empty<DefinitionParameter>()),
                        new EventStateSnapshot(Array.Empty<DefinitionParameter>()))));
        }

        [Test]
        public void ShowMarksAnOptionUnavailableAndProvidesItsDiagnostic()
        {
            var definition = CreateEvent();
            var state = new EventStateSnapshot(Array.Empty<DefinitionParameter>());
            var unavailable = new EventResolutionPreview(
                definition,
                definition.Options[0],
                new SessionId("session"),
                new PlayerId("player"),
                state,
                Array.Empty<RequirementEvaluation>(),
                Array.Empty<RequirementEvaluation>(),
                effectErrorCode: "EFFECT_HANDLER_NOT_FOUND",
                effectErrorMessage: "Missing test handler.");
            var model = new EventPanelViewModel();

            model.Show(
                definition,
                new Dictionary<string, EventResolutionPreview>
                {
                    ["accept"] = unavailable
                });

            Assert.That(model.Options[0].IsAvailable, Is.False);
            Assert.That(model.Options[0].UnavailableReason, Does.Contain("Missing test handler"));
        }

        private static EventResolutionPreview CreatePreview(
            EventDefinition definition,
            EventStateSnapshot before,
            EventStateSnapshot after) =>
            new EventResolutionPreview(
                definition,
                definition.Options[0],
                new SessionId("session"),
                new PlayerId("player"),
                before,
                Array.Empty<RequirementEvaluation>(),
                Array.Empty<RequirementEvaluation>(),
                after);

        private static EventDefinition CreateEvent(string id = "test-event") =>
            new EventDefinition(
                id,
                3,
                "test-category",
                "Test event",
                "Educational text.",
                Array.Empty<string>(),
                Array.Empty<RequirementDefinition>(),
                new[]
                {
                    new EventOptionDefinition(
                        "accept",
                        "Accept",
                        "Test option.",
                        Array.Empty<RequirementDefinition>(),
                        new[]
                        {
                            new GameEffectDefinition(
                                "balance",
                                "add",
                                "2",
                                null,
                                $"event:{id}@3")
                        },
                        "always")
                });
    }
}

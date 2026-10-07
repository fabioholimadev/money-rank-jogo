using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Application;
using MoneyRank.Domain.Events;

namespace MoneyRank.Presentation.Events
{
    public sealed class EventPanelViewModel
    {
        private ReadOnlyCollection<EventOptionViewModel> _options =
            new List<EventOptionViewModel>().AsReadOnly();
        private ReadOnlyCollection<EventStateChangeViewModel> _stateChanges =
            new List<EventStateChangeViewModel>().AsReadOnly();

        public bool IsVisible { get; private set; }
        public string EventId { get; private set; } = string.Empty;
        public int EventVersion { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string EducationalText { get; private set; } = string.Empty;
        public string SelectedOptionId { get; private set; } = string.Empty;
        public string StatusMessage { get; private set; } = string.Empty;
        public bool CanConfirm { get; private set; }
        public bool IsConfirmed { get; private set; }
        public bool HasPreview => !string.IsNullOrEmpty(SelectedOptionId) && !IsConfirmed;
        public IReadOnlyList<EventOptionViewModel> Options => _options;
        public IReadOnlyList<EventStateChangeViewModel> StateChanges => _stateChanges;

        public void Show(
            EventDefinition definition,
            IReadOnlyDictionary<string, EventResolutionPreview> optionPreviews = null)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));

            IsVisible = true;
            EventId = definition.Id;
            EventVersion = definition.Version;
            Title = definition.Title;
            EducationalText = definition.EducationalText;
            SelectedOptionId = string.Empty;
            StatusMessage = string.Empty;
            CanConfirm = false;
            IsConfirmed = false;
            _stateChanges = new List<EventStateChangeViewModel>().AsReadOnly();
            _options = definition.Options
                .Select(option =>
                {
                    EventResolutionPreview preview = null;
                    optionPreviews?.TryGetValue(option.Id, out preview);
                    return new EventOptionViewModel(
                        option.Id,
                        option.Label,
                        option.Description,
                        option.VisibilityId,
                        preview == null || preview.CanConfirm,
                        preview != null && !preview.CanConfirm ? BuildPreviewStatus(preview) : string.Empty);
                })
                .ToList()
                .AsReadOnly();
        }

        public void ShowPreview(EventResolutionPreview preview)
        {
            if (preview == null)
                throw new ArgumentNullException(nameof(preview));
            EnsureMatchesOpenEvent(preview.EventDefinition.Id, preview.EventDefinition.Version);

            SelectedOptionId = preview.Option.Id;
            CanConfirm = preview.CanConfirm;
            IsConfirmed = false;
            StatusMessage = BuildPreviewStatus(preview);
            _stateChanges = BuildStateChanges(preview.EvaluatedState, preview.ProjectedState);
        }

        public void ShowConfirmation(EventResolutionRecord record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            EnsureMatchesOpenEvent(record.EventId, record.EventVersion);

            SelectedOptionId = record.ChoiceId;
            CanConfirm = false;
            IsConfirmed = true;
            StatusMessage = "Event confirmed.";
            _stateChanges = BuildStateChanges(record.BeforeState, record.AfterState);
        }

        public void CancelPreview()
        {
            if (!IsVisible)
                throw new InvalidOperationException("Cannot cancel a preview while the event panel is hidden.");
            if (!HasPreview)
                throw new InvalidOperationException("There is no event preview to cancel.");

            SelectedOptionId = string.Empty;
            StatusMessage = string.Empty;
            CanConfirm = false;
            _stateChanges = new List<EventStateChangeViewModel>().AsReadOnly();
        }

        public void ShowError(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                throw new ArgumentException("Error message cannot be empty.", nameof(message));

            StatusMessage = message;
            CanConfirm = false;
        }

        public void Hide()
        {
            IsVisible = false;
            CanConfirm = false;
        }

        private void EnsureMatchesOpenEvent(string eventId, int version)
        {
            if (!IsVisible ||
                !string.Equals(EventId, eventId, StringComparison.Ordinal) ||
                EventVersion != version)
            {
                throw new InvalidOperationException(
                    $"Event panel is not showing definition '{eventId}@{version}'.");
            }
        }

        private static string BuildPreviewStatus(EventResolutionPreview preview)
        {
            var diagnostics = preview.ConditionResults
                .Concat(preview.RequirementResults)
                .Where(evaluation => !evaluation.IsSatisfied)
                .Select(evaluation => evaluation.Result.Message)
                .ToList();
            if (!string.IsNullOrEmpty(preview.EffectErrorMessage))
                diagnostics.Add(preview.EffectErrorMessage);

            return diagnostics.Count == 0
                ? "Preview ready for confirmation."
                : string.Join(Environment.NewLine, diagnostics);
        }

        private static ReadOnlyCollection<EventStateChangeViewModel> BuildStateChanges(
            EventStateSnapshot before,
            EventStateSnapshot after)
        {
            if (before == null)
                throw new ArgumentNullException(nameof(before));
            if (after == null)
                return new List<EventStateChangeViewModel>().AsReadOnly();

            var keys = before.Values.Keys
                .Concat(after.Values.Keys)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal);
            return keys
                .Select(key => new EventStateChangeViewModel(
                    key,
                    before.Values.TryGetValue(key, out var beforeValue) ? beforeValue : string.Empty,
                    after.Values.TryGetValue(key, out var afterValue) ? afterValue : string.Empty))
                .ToList()
                .AsReadOnly();
        }
    }

    public sealed class EventOptionViewModel
    {
        public EventOptionViewModel(
            string id,
            string label,
            string description,
            string visibilityId,
            bool isAvailable = true,
            string unavailableReason = "")
        {
            Id = id;
            Label = label;
            Description = description;
            VisibilityId = visibilityId;
            IsAvailable = isAvailable;
            UnavailableReason = unavailableReason ?? string.Empty;
        }

        public string Id { get; }
        public string Label { get; }
        public string Description { get; }
        public string VisibilityId { get; }
        public bool IsAvailable { get; }
        public string UnavailableReason { get; }
    }

    public sealed class EventStateChangeViewModel
    {
        public EventStateChangeViewModel(string key, string beforeValue, string afterValue)
        {
            Key = key;
            BeforeValue = beforeValue;
            AfterValue = afterValue;
        }

        public string Key { get; }
        public string BeforeValue { get; }
        public string AfterValue { get; }
        public bool Changed => !string.Equals(BeforeValue, AfterValue, StringComparison.Ordinal);
    }
}

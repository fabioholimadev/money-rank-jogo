using System;
using System.Collections.Generic;
using System.Linq;
using MoneyRank.Application;
using MoneyRank.Domain.Events;
using UnityEngine;

namespace MoneyRank.Presentation.Events
{
    [DisallowMultipleComponent]
    public sealed class EventPanelView : MonoBehaviour
    {
        private static int _nextWindowId;

        [SerializeField] private Rect panelRect = new Rect(410f, 24f, 460f, 620f);

        private Vector2 _scrollPosition;
        private int _windowId;

        public EventPanelViewModel Model { get; } = new EventPanelViewModel();
        public event Action<string> OptionRequested;
        public event Action ConfirmationRequested;
        public event Action CancellationRequested;

        private void OnEnable()
        {
            if (_windowId == 0)
                _windowId = ++_nextWindowId;
        }

        public void Show(
            EventDefinition definition,
            IReadOnlyDictionary<string, EventResolutionPreview> optionPreviews = null) =>
            Model.Show(definition, optionPreviews);
        public void ShowPreview(EventResolutionPreview preview) => Model.ShowPreview(preview);
        public void ShowConfirmation(EventResolutionRecord record) => Model.ShowConfirmation(record);
        public void ShowError(string message) => Model.ShowError(message);
        public void Hide() => Model.Hide();

        public void RequestOption(string optionId)
        {
            if (!Model.IsVisible)
                throw new InvalidOperationException("Cannot choose an option while the event panel is hidden.");
            if (string.IsNullOrWhiteSpace(optionId))
                throw new ArgumentException("Option ID cannot be empty.", nameof(optionId));
            var option = Model.Options.FirstOrDefault(item => string.Equals(item.Id, optionId, StringComparison.Ordinal));
            if (option == null)
                throw new InvalidOperationException($"Option '{optionId}' is not shown by the event panel.");
            if (!option.IsAvailable)
                throw new InvalidOperationException($"Option '{optionId}' is unavailable: {option.UnavailableReason}");

            OptionRequested?.Invoke(optionId);
        }

        public void RequestConfirmation()
        {
            if (!Model.CanConfirm)
                throw new InvalidOperationException("The current event preview cannot be confirmed.");

            ConfirmationRequested?.Invoke();
        }

        public void RequestCancellation()
        {
            if (!Model.HasPreview || Model.IsConfirmed)
                throw new InvalidOperationException("There is no event preview to cancel.");

            CancellationRequested?.Invoke();
        }

        private void OnGUI()
        {
            if (!Model.IsVisible) return;
            panelRect = GUI.Window(_windowId, panelRect, DrawWindow, Model.Title);
        }

        private void DrawWindow(int windowId)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            GUILayout.Label(Model.EducationalText);
            GUILayout.Space(8f);

            foreach (var option in Model.Options)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUI.enabled = option.IsAvailable;
                if (GUILayout.Button(option.Label))
                    RequestOption(option.Id);
                GUI.enabled = true;
                if (!string.IsNullOrEmpty(option.Description))
                    GUILayout.Label(option.Description);
                if (!option.IsAvailable)
                    GUILayout.Label($"Indisponível: {option.UnavailableReason}");
                GUILayout.EndVertical();
            }

            if (Model.StateChanges.Count > 0)
            {
                GUILayout.Space(8f);
                GUILayout.Label("Before / After");
                foreach (var change in Model.StateChanges)
                {
                    var marker = change.Changed ? "→" : "=";
                    GUILayout.Label($"{change.Key}: {change.BeforeValue} {marker} {change.AfterValue}");
                }
            }

            if (!string.IsNullOrEmpty(Model.StatusMessage))
            {
                GUILayout.Space(8f);
                GUILayout.Label(Model.StatusMessage);
            }

            GUI.enabled = Model.CanConfirm;
            if (GUILayout.Button("Confirm"))
                RequestConfirmation();
            GUI.enabled = true;
            if (Model.HasPreview && !Model.IsConfirmed && GUILayout.Button("Cancel preview"))
                RequestCancellation();

            GUILayout.EndScrollView();
            GUI.DragWindow(new Rect(0f, 0f, panelRect.width, 24f));
        }
    }
}

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
        private bool _prototypeChoiceLayout;

        public EventPanelViewModel Model { get; } = new EventPanelViewModel();
        public event Action<string> OptionRequested;
        public event Action ConfirmationRequested;
        public event Action CancellationRequested;
        public event Action CloseRequested;

        private void OnEnable()
        {
            if (_windowId == 0)
                _windowId = ++_nextWindowId;
        }

        public void Show(
            EventDefinition definition,
            IReadOnlyDictionary<string, EventResolutionPreview> optionPreviews = null) =>
            ShowInternal(definition, optionPreviews, false);
        public void ShowPrototype(EventDefinition definition) => ShowInternal(definition, null, true);
        private void ShowInternal(EventDefinition definition,
            IReadOnlyDictionary<string, EventResolutionPreview> optionPreviews, bool prototypeChoiceLayout)
        {
            _prototypeChoiceLayout = prototypeChoiceLayout;
            Model.Show(definition, optionPreviews);
        }
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

            if (_prototypeChoiceLayout)
            {
                DrawPrototypeOverlay();
                return;
            }

            panelRect = GUI.Window(_windowId, panelRect, DrawWindow, Model.Title);
        }

        private void DrawWindow(int windowId)
        {
            var closeRequested = false;
            string selectedOptionId = null;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            closeRequested = GUILayout.Button("X", GUILayout.Width(42f), GUILayout.Height(32f));
            GUILayout.EndHorizontal();

            _scrollPosition = GUILayout.BeginScrollView(
                _scrollPosition,
                GUILayout.Height(Mathf.Max(120f, panelRect.height - 150f)));
            GUILayout.Label(Model.EducationalText);
            GUILayout.Space(8f);
            foreach (var option in Model.Options)
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUI.enabled = option.IsAvailable;
                if (GUILayout.Button(option.Label))
                    selectedOptionId = option.Id;
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
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (Model.HasPreview && !Model.IsConfirmed &&
                GUILayout.Button("CANCELAR", GUILayout.Height(48f)))
                RequestCancellation();

            GUI.enabled = Model.CanConfirm;
            if (GUILayout.Button("CONFIRMAR", GUILayout.Height(48f)))
                RequestConfirmation();
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0f, 0f, Mathf.Max(0f, panelRect.width - 100f), 24f));

            if (closeRequested)
            {
                if (CloseRequested != null) CloseRequested.Invoke();
                else Model.Hide();
            }
            else if (!string.IsNullOrEmpty(selectedOptionId))
                RequestOption(selectedOptionId);
        }

        private void DrawPrototypeOverlay()
        {
            var width = Mathf.Max(1f, Mathf.Min(760f, Screen.width - 24f));
            var height = Mathf.Max(1f, Mathf.Min(360f, Screen.height - 24f));
            var panel = new Rect(
                (Screen.width - width) * 0.5f,
                (Screen.height - height) * 0.5f,
                width,
                height);
            GUI.Box(panel, GUIContent.none);

            var titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold,
                fontSize = 16
            };
            GUI.Label(new Rect(panel.x + 12f, panel.y + 4f, panel.width - 72f, 28f), Model.Title, titleStyle);

            var closeRect = new Rect(panel.xMax - 48f, panel.y + 4f, 40f, 30f);
            var closeRequested = GUI.Button(closeRect, "X");

            GUI.Label(
                new Rect(panel.x + 18f, panel.y + 44f, panel.width - 36f, 38f),
                Model.EducationalText);

            var margin = 18f;
            var gap = 12f;
            var cardWidth = (panel.width - margin * 2f - gap * 2f) / 3f;
            var cardY = panel.y + 92f;
            var cardHeight = Mathf.Max(72f, panel.height - 110f);
            var cardStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(cardHeight * 0.28f), 24, 64),
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };

            string selectedOptionId = null;
            for (var index = 0; index < Model.Options.Count; index++)
            {
                var option = Model.Options[index];
                var cardRect = new Rect(
                    panel.x + margin + index * (cardWidth + gap),
                    cardY,
                    cardWidth,
                    cardHeight);
                GUI.enabled = option.IsAvailable;
                if (GUI.Button(cardRect, option.Id, cardStyle))
                    selectedOptionId = option.Id;
                GUI.enabled = true;
            }

            if (closeRequested)
            {
                if (CloseRequested != null) CloseRequested.Invoke();
                else Model.Hide();
            }
            else if (!string.IsNullOrEmpty(selectedOptionId))
                RequestOption(selectedOptionId);
        }
    }
}

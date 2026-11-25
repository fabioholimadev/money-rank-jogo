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
        private bool _paused;
        private IReadOnlyList<EventDefinition> _opportunityCards;
        private EventDefinition _opportunityPreview;

        public EventPanelViewModel Model { get; } = new EventPanelViewModel();
        public event Action<string> OptionRequested;
        public event Action ConfirmationRequested;
        public event Action CancellationRequested;
        public event Action CloseRequested;
        public event Action<EventDefinition> OpportunityChosen;
        public event Action OpportunityDeclined;

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
        public void ShowOpportunityOffer(IReadOnlyList<EventDefinition> cards)
        {
            _opportunityCards = cards ?? throw new ArgumentNullException(nameof(cards));
            _opportunityPreview = null;
            _prototypeChoiceLayout = false;
            Model.Show(new EventDefinition(
                "opportunity-offer", 1, "opportunity", "Oportunidades disponíveis",
                cards.Count == 0 ? "Não há cartas disponíveis para esta profissão." : "Consulte uma carta para ver os detalhes. Você pode voltar e consultar outra antes de escolher.",
                Array.Empty<string>(), Array.Empty<RequirementDefinition>(),
                new[] { new EventOptionDefinition("offer", "Cartas", "", Array.Empty<RequirementDefinition>(), Array.Empty<GameEffectDefinition>(), "always") }));
        }
        public void ShowOpportunityPreview(EventDefinition card) => _opportunityPreview = card ?? throw new ArgumentNullException(nameof(card));
        private void ShowInternal(EventDefinition definition,
            IReadOnlyDictionary<string, EventResolutionPreview> optionPreviews, bool prototypeChoiceLayout)
        {
            _prototypeChoiceLayout = prototypeChoiceLayout;
            Model.Show(definition, optionPreviews);
        }
        public void ShowPreview(EventResolutionPreview preview) => Model.ShowPreview(preview);
        public void ShowConfirmation(EventResolutionRecord record) => Model.ShowConfirmation(record);
        public void ShowError(string message) => Model.ShowError(message);
        public void Hide()
        {
            _opportunityCards = null;
            _opportunityPreview = null;
            Model.Hide();
        }
        public void SetPaused(bool paused) => _paused = paused;

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
            if (_paused || !Model.IsVisible) return;

            if (_opportunityCards != null)
            {
                DrawOpportunityPanel();
                return;
            }

            if (UnityEngine.Application.isMobilePlatform)
                FitPanelToMobileSafeArea();

            if (_prototypeChoiceLayout)
            {
                DrawPrototypeOverlay();
                return;
            }

            panelRect = GUI.Window(_windowId, panelRect, DrawWindow, Model.Title);
        }

        private void DrawOpportunityPanel()
        {
            var safeArea = UnityEngine.Application.isMobilePlatform ? Screen.safeArea : new Rect(0f, 0f, Screen.width, Screen.height);
            var width = Mathf.Max(1f, Mathf.Min(760f, safeArea.width - 24f));
            var height = Mathf.Max(1f, Mathf.Min(540f, safeArea.height - 24f));
            var panel = new Rect(safeArea.x + (safeArea.width - width) * .5f,
                Screen.height - safeArea.yMax + (safeArea.height - height) * .5f, width, height);
            GUI.Box(panel, GUIContent.none);
            var heading = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold, fontSize = 22 };
            GUI.Label(new Rect(panel.x + 16, panel.y + 12, panel.width - 32, 38),
                _opportunityPreview == null ? "CARTAS DE OPORTUNIDADE" : _opportunityPreview.Title, heading);
            if (GUI.Button(new Rect(panel.xMax - 48, panel.y + 12, 32, 32), "X"))
            {
                _opportunityCards = null;
                _opportunityPreview = null;
                OpportunityDeclined?.Invoke();
                return;
            }

            if (_opportunityPreview == null)
            {
                var y = panel.y + 62;
                foreach (var card in _opportunityCards)
                {
                    var row = new Rect(panel.x + 24, y, panel.width - 48, 66);
                    GUI.Box(row, GUIContent.none);
                    GUI.Label(new Rect(row.x + 12, row.y + 8, row.width * .62f, 48), card.Title);
                    if (GUI.Button(new Rect(row.xMax - 164, row.y + 12, 148, 42), "CONSULTAR"))
                        _opportunityPreview = card;
                    y += 76;
                }
                if (GUI.Button(new Rect(panel.x + 24, panel.yMax - 68, panel.width - 48, 48), "RECUSAR TODAS"))
                {
                    _opportunityCards = null;
                    OpportunityDeclined?.Invoke();
                }
            }
            else
            {
                var textRect = new Rect(panel.x + 28, panel.y + 64, panel.width - 56, panel.height - 164);
                var cardDetails = string.Join("\n\n", _opportunityPreview.Options.Select(option =>
                {
                    var effects = option.Effects.Count == 0
                        ? "Efeito não cadastrado."
                        : string.Join("\n", option.Effects.Select(effect => $"{effect.OperationId}: {effect.Value}"));
                    return $"{option.Label}\n{option.Description}\n{effects}";
                }));
                var detailStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, alignment = TextAnchor.UpperLeft };
                GUI.Label(textRect, _opportunityPreview.EducationalText + "\n\n" + cardDetails, detailStyle);
                var buttonY = panel.yMax - 72;
                if (GUI.Button(new Rect(panel.x + 24, buttonY, panel.width * .42f, 48), "VOLTAR ÀS CARTAS"))
                    _opportunityPreview = null;

                if (GUI.Button(new Rect(panel.x + panel.width * .48f, buttonY, panel.width * .48f - 24, 48), "ESCOLHER ESTA CARTA"))
                {
                    var selected = _opportunityPreview;
                    _opportunityCards = null;
                    _opportunityPreview = null;
                    OpportunityChosen?.Invoke(selected);
                }
                if (_opportunityPreview.Tags.Contains("effect:pending-approval"))
                    GUI.Label(new Rect(panel.x + 24, panel.yMax - 108, panel.width - 48, 28), "Efeito aguardando aprovação; nenhum efeito de jogo será aplicado.");
            }
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
            var safeArea = UnityEngine.Application.isMobilePlatform ? Screen.safeArea :
                new Rect(0f, 0f, Screen.width, Screen.height);
            var width = Mathf.Max(1f, Mathf.Min(760f, safeArea.width - 24f));
            var height = Mathf.Max(1f, Mathf.Min(360f, safeArea.height - 24f));
            var panel = new Rect(
                safeArea.x + (safeArea.width - width) * 0.5f,
                Screen.height - safeArea.yMax + (safeArea.height - height) * 0.5f,
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

        private void FitPanelToMobileSafeArea()
        {
            var safeArea = Screen.safeArea;
            var screenSafeAreaTop = Screen.height - safeArea.yMax;
            panelRect.width = Mathf.Min(Mathf.Max(320f, panelRect.width), safeArea.width - 24f);
            panelRect.height = Mathf.Min(Mathf.Max(260f, panelRect.height), safeArea.height - 24f);
            panelRect.x = safeArea.x + (safeArea.width - panelRect.width) * 0.5f;
            panelRect.y = screenSafeAreaTop + (safeArea.height - panelRect.height) * 0.5f;
        }
    }
}

using System;
using UnityEngine;

namespace MoneyRank.Presentation.Board
{
    [DisallowMultipleComponent]
    public sealed class BoardStartMenuView : MonoBehaviour
    {
        private enum MenuAction
        {
            None,
            OpenSetup,
            StartNewGame,
            ContinueGame
        }

        private static readonly Color BackgroundColor = new Color32(17, 17, 17, 255); // #111111
        private static readonly Color GoldColor = new Color32(245, 197, 66, 255); // #F5C542
        private static readonly Color YellowColor = new Color32(255, 217, 102, 255); // #FFD966
        private static readonly Color OrangeColor = new Color32(242, 140, 40, 255); // #F28C28
        private static readonly Color WhiteColor = new Color32(255, 255, 255, 255); // #FFFFFF
        private static readonly Color GrayColor = new Color32(230, 230, 230, 255); // #E6E6E6
        private BoardSandboxController _controller;
        private GUIStyle _titleStyle;
        private GUIStyle _subtitleStyle;
        private GUIStyle _newGameStyle;
        private GUIStyle _continueStyle;
        private GUIStyle _onlineStyle;
        private GUIStyle _statusStyle;
        private GUIStyle _setupTitleStyle;
        private GUIStyle _playerCountStyle;
        private GUIStyle _selectedPlayerCountStyle;
        private GUIStyle _setupDescriptionStyle;
        private float _styleScale = -1f;
        private bool _canContinue;
        private bool _showSetup;
        private int _selectedPlayerCount = 2;
        private string _statusMessage = string.Empty;
        private Texture2D _logo;

        public void Bind(BoardSandboxController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _canContinue = controller.HasSavedGame;
            _logo = Resources.Load<Texture2D>("MoneyRankLogo");
        }

        private void OnGUI()
        {
            if (_controller == null || _controller.Session != null)
                return;

            var scale = Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.62f, 1.2f);
            EnsureStyles(scale);

            if (_showSetup)
            {
                DrawSetupScreen(scale);
                return;
            }

            var previousColor = GUI.color;
            var previousBackground = GUI.backgroundColor;
            var panelWidth = Mathf.Min(400f * scale, Screen.width);
            var panelRect = new Rect(0f, 0f, panelWidth, Screen.height);

            GUI.color = Color.white;
            var action = MenuAction.None;
            GUILayout.BeginArea(panelRect);
            var contentRect = new Rect(28f * scale, 26f * scale,
                panelWidth - 56f * scale, Screen.height - 52f * scale);
            GUILayout.BeginArea(contentRect);
            GUILayout.Space(30f * scale);
            if (_logo != null)
            {
                var logoWidth = contentRect.width;
                var logoHeight = logoWidth / 2.5f;
                var logoRect = GUILayoutUtility.GetRect(logoWidth, logoHeight, GUILayout.ExpandWidth(false));
                GUI.DrawTexture(logoRect, _logo, ScaleMode.ScaleToFit, true);
            }
            else
            {
                GUILayout.Label("MONEY RANK", _titleStyle, GUILayout.Height(76f * scale));
            }
            GUILayout.Space(16f * scale);
            GUILayout.Label("JOGO DE EDUCAÇÃO FINANCEIRA", _subtitleStyle, GUILayout.Height(34f * scale));
            var accentWidth = 92f * scale;
            var accentHeight = 4f * scale;
            GUI.color = OrangeColor;
            GUI.DrawTexture(new Rect(0f,
                30f * scale + (panelWidth - 56f * scale) / 2.5f + 16f * scale + 34f * scale,
                accentWidth, accentHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(24f * scale);

            GUI.backgroundColor = GoldColor;
            if (GUILayout.Button("NOVO JOGO", _newGameStyle, GUILayout.Height(48f * scale)))
                action = MenuAction.OpenSetup;

            GUILayout.Space(14f * scale);
            GUI.enabled = _canContinue;
            GUI.backgroundColor = YellowColor;
            if (GUILayout.Button("CONTINUAR", _continueStyle, GUILayout.Height(48f * scale)))
                action = MenuAction.ContinueGame;
            GUI.enabled = true;

            GUILayout.Space(14f * scale);
            GUI.enabled = false;
            GUI.backgroundColor = GrayColor;
            GUILayout.Button("JOGO ONLINE", _onlineStyle, GUILayout.Height(48f * scale));
            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            var status = !string.IsNullOrEmpty(_statusMessage)
                ? _statusMessage
                : _canContinue
                    ? "Sua última partida está salva neste computador."
                    : "Ainda não há uma partida salva para continuar.";
            GUILayout.Label(status, _statusStyle, GUILayout.MinHeight(42f * scale));
            GUILayout.Space(30f * scale);
            GUILayout.EndArea();
            GUILayout.EndArea();

            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;

            if (action == MenuAction.OpenSetup)
                _showSetup = true;
            else if (action != MenuAction.None)
                StartSelectedGame(action);
        }

        private void DrawSetupScreen(float scale)
        {
            var previousColor = GUI.color;
            var previousBackground = GUI.backgroundColor;
            var panelWidth = Mathf.Min(620f * scale, Screen.width - 40f * scale);
            var panelHeight = Mathf.Min(570f * scale, Screen.height - 40f * scale);
            var panelRect = new Rect(
                (Screen.width - panelWidth) * 0.5f,
                (Screen.height - panelHeight) * 0.5f,
                panelWidth,
                panelHeight);

            GUI.color = new Color(0.05f, 0.07f, 0.09f, 0.97f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = OrangeColor;
            GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, 4f * scale), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var padding = 38f * scale;
            var contentRect = new Rect(
                panelRect.x + padding,
                panelRect.y + 28f * scale,
                panelRect.width - padding * 2f,
                panelRect.height - 56f * scale);
            GUILayout.BeginArea(contentRect);
            GUILayout.Label("CONFIGURAR JOGO", _setupTitleStyle, GUILayout.Height(58f * scale));
            GUILayout.Space(12f * scale);
            GUILayout.Label("Quantidade de jogadores", _subtitleStyle, GUILayout.Height(34f * scale));
            GUILayout.Space(18f * scale);

            GUILayout.BeginHorizontal();
            for (var playerCount = 2; playerCount <= 4; playerCount++)
            {
                var isSelected = _selectedPlayerCount == playerCount;
                GUI.backgroundColor = isSelected ? GoldColor : new Color32(65, 73, 80, 255);
                var labelStyle = isSelected ? _selectedPlayerCountStyle : _playerCountStyle;
                if (GUILayout.Button($"{playerCount}\nJOGADORES", labelStyle,
                        GUILayout.ExpandWidth(true), GUILayout.Height(82f * scale)))
                    _selectedPlayerCount = playerCount;
                if (playerCount < 4)
                    GUILayout.Space(14f * scale);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(20f * scale);
            GUILayout.Label(
                $"{_selectedPlayerCount} capivaras na trilha externa\ne {_selectedPlayerCount * 4} peões no tabuleiro interno.",
                _setupDescriptionStyle,
                GUILayout.MinHeight(58f * scale));
            GUILayout.FlexibleSpace();
            GUI.backgroundColor = GoldColor;
            if (GUILayout.Button("COMEÇAR PARTIDA", _newGameStyle, GUILayout.Height(54f * scale)))
            {
                GUILayout.EndArea();
                GUI.backgroundColor = previousBackground;
                GUI.color = previousColor;
                StartSelectedGame(MenuAction.StartNewGame);
                return;
            }
            GUILayout.Space(12f * scale);
            GUI.backgroundColor = new Color32(65, 73, 80, 255);
            if (GUILayout.Button("VOLTAR", _playerCountStyle, GUILayout.Height(44f * scale)))
                _showSetup = false;
            GUILayout.EndArea();

            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
        }

        private void StartSelectedGame(MenuAction action)
        {
            try
            {
                var started = action == MenuAction.StartNewGame
                    ? _controller.StartNewGame(_selectedPlayerCount)
                    : _controller.ContinueSavedGame();
                if (!started)
                {
                    _canContinue = _controller.HasSavedGame;
                    _statusMessage = action == MenuAction.ContinueGame
                        ? "Não foi possível carregar a partida salva."
                        : "Não foi possível iniciar a partida.";
                }
            }
            catch (Exception exception)
            {
                _statusMessage = $"Falha ao iniciar: {exception.Message}";
                Debug.LogException(exception);
            }
        }

        private void EnsureStyles(float scale)
        {
            if (_titleStyle != null && Mathf.Approximately(_styleScale, scale))
                return;

            _styleScale = scale;
            _titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(42f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = GoldColor }
            };
            _subtitleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(16f * scale),
                normal = { textColor = WhiteColor }
            };
            _newGameStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(19f * scale),
                fontStyle = FontStyle.Bold,
                wordWrap = true
            };
            _continueStyle = new GUIStyle(_newGameStyle);
            _onlineStyle = new GUIStyle(_newGameStyle)
            {
                normal = { textColor = BackgroundColor },
                focused = { textColor = BackgroundColor },
                hover = { textColor = BackgroundColor },
                active = { textColor = BackgroundColor }
            };
            SetFlatButtonBackgrounds(_newGameStyle);
            SetFlatButtonBackgrounds(_continueStyle);
            SetFlatButtonBackgrounds(_onlineStyle);
            SetButtonTextColor(_newGameStyle, BackgroundColor);
            SetButtonTextColor(_continueStyle, BackgroundColor);
            _statusStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(14f * scale),
                wordWrap = true,
                normal = { textColor = GrayColor }
            };
            _setupTitleStyle = new GUIStyle(_titleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(34f * scale),
                wordWrap = false
            };
            _playerCountStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(16f * scale),
                fontStyle = FontStyle.Bold,
                wordWrap = true,
                normal = { textColor = WhiteColor },
                hover = { textColor = WhiteColor },
                active = { textColor = WhiteColor },
                focused = { textColor = WhiteColor }
            };
            _selectedPlayerCountStyle = new GUIStyle(_playerCountStyle)
            {
                normal = { textColor = BackgroundColor },
                hover = { textColor = BackgroundColor },
                active = { textColor = BackgroundColor },
                focused = { textColor = BackgroundColor }
            };
            _setupDescriptionStyle = new GUIStyle(_statusStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(18f * scale),
                normal = { textColor = GrayColor }
            };
            SetFlatButtonBackgrounds(_playerCountStyle);
            SetFlatButtonBackgrounds(_selectedPlayerCountStyle);
        }

        private static void SetFlatButtonBackgrounds(GUIStyle style)
        {
            style.normal.background = Texture2D.whiteTexture;
            style.hover.background = Texture2D.whiteTexture;
            style.active.background = Texture2D.whiteTexture;
            style.focused.background = Texture2D.whiteTexture;
            style.onNormal.background = Texture2D.whiteTexture;
            style.onHover.background = Texture2D.whiteTexture;
            style.onActive.background = Texture2D.whiteTexture;
            style.onFocused.background = Texture2D.whiteTexture;
        }

        private static void SetButtonTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.focused.textColor = color;
            style.onNormal.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
            style.onFocused.textColor = color;
        }
    }
}

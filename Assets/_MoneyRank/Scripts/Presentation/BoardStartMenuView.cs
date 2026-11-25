using System;
using System.Collections.Generic;
using System.Linq;
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
            OpenOnline,
            StartNewGame,
            ContinueGame,
            BeginConfiguredGame
        }

        private static readonly Color BackgroundColor = new Color32(17, 17, 17, 255); // #111111
        private static readonly Color GoldColor = new Color32(245, 197, 66, 255); // #F5C542
        private static readonly Color YellowColor = new Color32(255, 217, 102, 255); // #FFD966
        private static readonly Color OrangeColor = new Color32(242, 140, 40, 255); // #F28C28
        private static readonly Color WhiteColor = new Color32(255, 255, 255, 255); // #FFFFFF
        private static readonly Color GrayColor = new Color32(230, 230, 230, 255); // #E6E6E6
        private const string NicknamePreferenceKey = "MoneyRank.PlayerNickname";
        private const int NicknameMaximumLength = 24;
        private BoardSandboxController _controller;
        private OnlineSessionCoordinator _onlineCoordinator;
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
        private GUIStyle _characterNameStyle;
        private GUIStyle _characterOptionStyle;
        private GUIStyle _nicknameFieldStyle;
        private GUIStyle _nicknamePlaceholderStyle;
        private GUIStyle _joinCodeStyle;
        private GUIStyle _waitingHeaderStyle;
        private GUIStyle _waitingSectionStyle;
        private GUIStyle _waitingMemberStyle;
        private GUIStyle _waitingCodeStyle;
        private GUIStyle _waitingButtonStyle;
        private GUIStyle _nicknameButtonStyle;
        private GUIStyle _waitingCrownStyle;
        private float _styleScale = -1f;
        private bool _canContinue;
        private bool _showSetup;
        private bool _showCharacterSelection;
        private bool _showNicknameScreen;
        private bool _showOnlineScreen;
        private int _openCharacterMenu = -1;
        private int _openTurnOrderMenu = -1;
        private int _selectedPlayerCount = 2;
        private int[] _selectedCharacters = { -1, -1, -1, -1 };
        private int[] _turnOrderPlayerIndices = { 0, 1, 2, 3 };
        private string _statusMessage = string.Empty;
        private string _nickname = string.Empty;
        private string _joinCode = string.Empty;
        private TouchScreenKeyboard _mobileKeyboard;
        private MobileInputTarget _mobileKeyboardTarget;
        private Texture2D _logo;
        private Texture2D _crownIcon;

        private enum MobileInputTarget
        {
            None,
            Nickname,
            JoinCode
        }

        public void Bind(BoardSandboxController controller)
        {
            _controller = controller ?? throw new ArgumentNullException(nameof(controller));
            _onlineCoordinator = GetComponent<OnlineSessionCoordinator>();
            if (_onlineCoordinator == null)
                _onlineCoordinator = gameObject.AddComponent<OnlineSessionCoordinator>();
            _canContinue = controller.HasSavedGame;
            _logo = Resources.Load<Texture2D>("MoneyRankLogo");
            _crownIcon = CreateCrownTexture();
            _nickname = PlayerPrefs.GetString(NicknamePreferenceKey, string.Empty);
            _showNicknameScreen = string.IsNullOrWhiteSpace(_nickname);
        }

        private void OnDestroy()
        {
            if (_crownIcon != null)
                Destroy(_crownIcon);
        }

        private void OnGUI()
        {
            if (_controller == null || _controller.Session != null)
                return;

            UpdateMobileKeyboardText();

            var scale = UnityEngine.Application.isMobilePlatform
                ? Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.72f, 1.2f)
                : Mathf.Clamp(Mathf.Min(Screen.width / 1280f, Screen.height / 720f), 0.62f, 1.2f);
            EnsureStyles(scale);

            if (_showNicknameScreen)
            {
                DrawNicknameScreen(scale);
                return;
            }

            if (_showOnlineScreen)
            {
                DrawOnlineScreen(scale);
                return;
            }

            if (_showCharacterSelection)
            {
                DrawCharacterSelectionScreen(scale);
                return;
            }

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
            GUILayout.Space(10f * scale);
            GUILayout.Label($"Olá, {_nickname}", _statusStyle, GUILayout.Height(24f * scale));
            GUI.backgroundColor = OrangeColor;
            if (GUILayout.Button("ALTERAR NICKNAME", _nicknameButtonStyle, GUILayout.Height(36f * scale)))
                _showNicknameScreen = true;
            GUI.backgroundColor = Color.white;
            var accentWidth = 92f * scale;
            var accentHeight = 4f * scale;
            GUI.color = OrangeColor;
            GUI.DrawTexture(new Rect(0f,
                30f * scale + (panelWidth - 56f * scale) / 2.5f + 16f * scale + 34f * scale,
                accentWidth, accentHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUILayout.Space(24f * scale);

            GUI.backgroundColor = GoldColor;
            GUI.enabled = !UnityEngine.Application.isMobilePlatform;
            if (GUILayout.Button("NOVO JOGO", _newGameStyle, GUILayout.Height(48f * scale)))
                action = MenuAction.OpenSetup;
            GUI.enabled = true;

            GUILayout.Space(14f * scale);
            GUI.enabled = _canContinue && !UnityEngine.Application.isMobilePlatform;
            GUI.backgroundColor = YellowColor;
            if (GUILayout.Button("CONTINUAR", _continueStyle, GUILayout.Height(48f * scale)))
                action = MenuAction.ContinueGame;
            GUI.enabled = true;

            GUILayout.Space(14f * scale);
            GUI.backgroundColor = OrangeColor;
            if (GUILayout.Button(UnityEngine.Application.isMobilePlatform ? "JOGAR ONLINE" : "JOGO ONLINE",
                    _onlineStyle, GUILayout.Height(48f * scale)))
                action = MenuAction.OpenOnline;

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

            if (action == MenuAction.OpenOnline)
                _showOnlineScreen = true;
            else if (action == MenuAction.OpenSetup)
                _showSetup = true;
            else if (action != MenuAction.None)
                StartSelectedGame(action);
        }

        private void DrawOnlineScreen(float scale)
        {
            var previousColor = GUI.color;
            var previousBackground = GUI.backgroundColor;
            if (_onlineCoordinator.IsInRoom)
            {
                DrawWaitingRoom();
                GUI.color = previousColor;
                GUI.backgroundColor = previousBackground;
                return;
            }

            var available = GetKeyboardSafeArea();
            var keyboardActive = IsKeyboardVisibleFor(MobileInputTarget.JoinCode);
            var panelWidth = Mathf.Min(680f * scale, available.width - 24f * scale);
            var preferredHeight = keyboardActive ? 250f * scale : 560f * scale;
            var panelHeight = Mathf.Min(preferredHeight, available.height - 16f * scale);
            var panelRect = new Rect(available.x + (available.width - panelWidth) * 0.5f,
                available.y + (available.height - panelHeight) * 0.5f, panelWidth, panelHeight);
            GUI.color = new Color(0.05f, 0.07f, 0.09f, 0.98f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = GoldColor;
            GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, 4f * scale), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var padding = 32f * scale;
            GUILayout.BeginArea(new Rect(panelRect.x + padding, panelRect.y + 22f * scale,
                panelRect.width - padding * 2f, panelRect.height - 44f * scale));
            GUILayout.Label(keyboardActive ? "CÓDIGO DA SALA" : "JOGO ONLINE",
                _setupTitleStyle, GUILayout.Height((keyboardActive ? 36f : 54f) * scale));

            if (!_onlineCoordinator.IsInRoom)
            {
                if (!keyboardActive && _onlineCoordinator.CanHost)
                {
                    GUILayout.Label("No PC, crie a sala para os jogadores entrarem pelo celular.",
                        _setupDescriptionStyle, GUILayout.MinHeight(48f * scale));
                    GUI.enabled = !_onlineCoordinator.IsBusy;
                    GUI.backgroundColor = GoldColor;
                    if (GUILayout.Button(_onlineCoordinator.IsBusy ? "CONECTANDO..." : "CRIAR SALA NO PC",
                            _newGameStyle, GUILayout.Height(50f * scale)))
                        _onlineCoordinator.CreateRoom(_nickname);
                    GUI.enabled = true;
                    GUILayout.Space(10f * scale);
                }

                if (!keyboardActive)
                    GUILayout.Label("No celular, digite o código da sala do PC:",
                        _setupDescriptionStyle, GUILayout.MinHeight(36f * scale));
                GUI.SetNextControlName("MoneyRankJoinCode");
                _joinCode = DrawInputField(_joinCode, MobileInputTarget.JoinCode,
                    "Digite o código", 16, TouchScreenKeyboardType.ASCIICapable, scale,
                    GUILayout.ExpandWidth(true), GUILayout.Height(48f * scale));
                GUI.enabled = !_onlineCoordinator.IsBusy && !string.IsNullOrWhiteSpace(_joinCode);
                GUI.backgroundColor = GoldColor;
                if (GUILayout.Button(_onlineCoordinator.IsBusy ? "CONECTANDO..." : "ENTRAR NA SALA",
                        _newGameStyle, GUILayout.Height(50f * scale)))
                    _onlineCoordinator.JoinRoom(_nickname, _joinCode);
                GUI.enabled = true;
                if (!keyboardActive && UnityEngine.Application.isMobilePlatform)
                {
                    GUI.backgroundColor = OrangeColor;
                    if (GUILayout.Button(_onlineCoordinator.IsScanningQr ? "LENDO QR CODE..." : "LER QR CODE",
                            _newGameStyle, GUILayout.Height(48f * scale)))
                        _onlineCoordinator.StartQrScan();
                    if (_onlineCoordinator.IsScanningQr)
                    {
                        var previewRect = GUILayoutUtility.GetRect(220f * scale, 120f * scale,
                            GUILayout.ExpandWidth(true));
                        if (_onlineCoordinator.QrCameraTexture != null)
                            GUI.DrawTexture(previewRect, _onlineCoordinator.QrCameraTexture, ScaleMode.ScaleToFit);
                        if (GUILayout.Button("CANCELAR LEITURA", _playerCountStyle,
                                GUILayout.Height(38f * scale)))
                            _onlineCoordinator.StopQrScan();
                    }
                }
            }
            if (!keyboardActive && !string.IsNullOrWhiteSpace(_onlineCoordinator.Status))
                GUILayout.Label(_onlineCoordinator.Status, _statusStyle, GUILayout.MinHeight(42f * scale));
            GUI.backgroundColor = new Color32(65, 73, 80, 255);
            if (!_onlineCoordinator.IsInRoom && GUILayout.Button(keyboardActive ? "FECHAR TECLADO / VOLTAR" : "VOLTAR AO MENU", _playerCountStyle,
                    GUILayout.Height((keyboardActive ? 36f : 42f) * scale)))
            {
                if (keyboardActive && _mobileKeyboard != null)
                    _mobileKeyboard.active = false;
                _showOnlineScreen = false;
            }
            GUILayout.EndArea();
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
        }

        private void DrawNicknameScreen(float scale)
        {
            var previousColor = GUI.color;
            var previousBackground = GUI.backgroundColor;
            var available = GetKeyboardSafeArea();
            var keyboardActive = IsKeyboardVisibleFor(MobileInputTarget.Nickname);
            var panelWidth = Mathf.Min(560f * scale, available.width - 24f * scale);
            var panelHeight = Mathf.Min((keyboardActive ? 205f : 300f) * scale, available.height - 16f * scale);
            var panelRect = new Rect(available.x + (available.width - panelWidth) * 0.5f,
                available.y + (available.height - panelHeight) * 0.5f, panelWidth, panelHeight);

            GUI.color = new Color(0.05f, 0.07f, 0.09f, 0.98f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = GoldColor;
            GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, 4f * scale), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var padding = 24f * scale;
            GUILayout.BeginArea(new Rect(panelRect.x + padding, panelRect.y + (keyboardActive ? 10f : 24f) * scale,
                panelRect.width - padding * 2f, panelRect.height - (keyboardActive ? 20f : 48f) * scale));
            GUILayout.Label("SEU NICKNAME", _setupTitleStyle, GUILayout.Height((keyboardActive ? 34f : 50f) * scale));
            if (!keyboardActive)
            {
                GUILayout.Label("Esse nome aparecerá na sala de espera e na partida online.",
                    _setupDescriptionStyle, GUILayout.Height(44f * scale));
                GUILayout.Space(14f * scale);
            }
            GUI.SetNextControlName("MoneyRankNickname");
            _nickname = DrawInputField(_nickname, MobileInputTarget.Nickname,
                "Seu nickname", NicknameMaximumLength, TouchScreenKeyboardType.Default, scale,
                GUILayout.ExpandWidth(true), GUILayout.Height(48f * scale));
            GUILayout.Space((keyboardActive ? 4f : 10f) * scale);
            if (!keyboardActive && string.IsNullOrWhiteSpace(_nickname))
                GUILayout.Label("Digite um nome para continuar.", _statusStyle, GUILayout.Height(26f * scale));
            GUILayout.FlexibleSpace();
            GUI.enabled = !string.IsNullOrWhiteSpace(_nickname);
            GUI.backgroundColor = GoldColor;
            if (GUILayout.Button("CONFIRMAR", _newGameStyle, GUILayout.Height((keyboardActive ? 38f : 48f) * scale)))
            {
                if (_mobileKeyboard != null)
                    _mobileKeyboard.active = false;
                _nickname = _nickname.Trim();
                PlayerPrefs.SetString(NicknamePreferenceKey, _nickname);
                PlayerPrefs.Save();
                _showNicknameScreen = false;
            }
            GUI.enabled = true;
            GUILayout.EndArea();
            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
        }

        private void DrawWaitingRoom()
        {
            var safe = UnityEngine.Application.isMobilePlatform
                ? Screen.safeArea
                : new Rect(0f, 0f, Screen.width, Screen.height);
            var safeTop = Screen.height - safe.yMax;
            var uiScale = Mathf.Clamp(Mathf.Min(safe.width / 1280f, safe.height / 720f), 0.55f, 1.15f);
            var totalWidth = Mathf.Min(safe.width * 0.92f, 1180f * uiScale);
            var totalHeight = Mathf.Min(safe.height * 0.88f, 640f * uiScale);
            var gap = 16f * uiScale;
            var codeWidth = totalWidth * (UnityEngine.Application.isMobilePlatform ? 0.32f : 0.27f);
            var mainWidth = totalWidth - codeWidth - gap;
            var left = safe.x + (safe.width - totalWidth) * 0.5f;
            var top = safeTop + (safe.height - totalHeight) * 0.5f;
            var main = new Rect(left, top, mainWidth, totalHeight);
            var codePanel = new Rect(main.xMax + gap, top + totalHeight * 0.21f,
                codeWidth, totalHeight * 0.65f);

            GUI.color = new Color(0.015f, 0.025f, 0.04f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            DrawFramedPanel(main, new Color32(8, 24, 39, 248), GoldColor, 2f * uiScale);
            DrawFramedPanel(codePanel, new Color32(8, 24, 39, 250), new Color32(113, 145, 169, 255), 2f * uiScale);
            GUI.color = Color.white;

            var inset = 24f * uiScale;
            var headerHeight = totalHeight * 0.13f;
            GUI.Label(new Rect(main.x + inset, main.y + 8f * uiScale, main.width - inset * 2f, headerHeight),
                "SALA DE ESPERA", _waitingHeaderStyle);
            GUI.color = Color.white;

            var members = _onlineCoordinator.Members;
            OnlineSessionCoordinator.RoomMember? host = null;
            var guests = new List<OnlineSessionCoordinator.RoomMember>();
            foreach (var member in members)
            {
                if (member.IsHost && !host.HasValue) host = member;
                else guests.Add(member);
            }

            var masterLabel = new Rect(main.x + inset, main.y + headerHeight + 6f * uiScale,
                main.width - inset * 2f, 28f * uiScale);
            GUI.Label(masterLabel, "M E S T R E", _waitingSectionStyle);
            var rowGap = 7f * uiScale;
            var rowLeft = main.x + inset;
            var rowWidth = main.width - inset * 2f;
            var masterY = masterLabel.yMax + 4f * uiScale;
            var masterHeight = Mathf.Max(38f * uiScale, totalHeight * 0.115f);
            DrawMemberRow(new Rect(rowLeft, masterY, rowWidth, masterHeight),
                host.HasValue ? host.Value.Nickname : _nickname, true, uiScale);

            var playersTitleY = masterY + masterHeight + 12f * uiScale;
            GUI.Label(new Rect(main.x + inset, playersTitleY, main.width - inset * 2f, 26f * uiScale),
                $"JOGADORES  ({guests.Count}/4)", _waitingSectionStyle);
            var playersStartY = playersTitleY + 28f * uiScale;
            var footerHeight = Mathf.Max(38f * uiScale, totalHeight * 0.105f);
            var availableForRows = main.yMax - footerHeight - 16f * uiScale - playersStartY;
            var playerRowHeight = Mathf.Max(32f * uiScale, (availableForRows - rowGap * 3f) / 4f);
            for (var index = 0; index < 4; index++)
            {
                var row = new Rect(rowLeft, playersStartY + index * (playerRowHeight + rowGap),
                    rowWidth, playerRowHeight);
                var occupied = index < guests.Count;
                DrawMemberRow(row, occupied ? guests[index].Nickname : "Aguardando jogador...",
                    false, uiScale, !occupied);
            }

            var leaveRect = new Rect(main.x + main.width * 0.21f, main.yMax - footerHeight - 6f * uiScale,
                main.width * 0.58f, footerHeight);
            GUI.color = Color.white;
            GUI.backgroundColor = new Color32(190, 38, 48, 255);
            if (GUI.Button(leaveRect, "SAIR DA SALA", _waitingButtonStyle))
                _onlineCoordinator.LeaveRoom();
            GUI.backgroundColor = Color.white;

            var codeInset = 16f * uiScale;
            GUI.Label(new Rect(codePanel.x + codeInset, codePanel.y + 18f * uiScale,
                codePanel.width - codeInset * 2f, 40f * uiScale), "CÓDIGO DA SALA", _waitingSectionStyle);
            var codeRect = new Rect(codePanel.x + codeInset, codePanel.y + 62f * uiScale,
                codePanel.width - codeInset * 2f, Mathf.Min(64f * uiScale, codePanel.height * 0.19f));
            GUI.color = Color.white;
            GUI.backgroundColor = new Color32(17, 31, 44, 255);
            GUI.Box(codeRect, _onlineCoordinator.JoinCode, _waitingCodeStyle);
            GUI.backgroundColor = Color.white;
            if (_onlineCoordinator.JoinCodeQrTexture != null)
            {
                var qrTop = codeRect.yMax + 12f * uiScale;
                var statusHeight = 24f * uiScale;
                var qrAvailableHeight = Mathf.Max(1f, codePanel.yMax - statusHeight - 8f * uiScale - qrTop);
                var qrSize = Mathf.Min(codePanel.width - codeInset * 2f, qrAvailableHeight);
                var qrRect = new Rect(codePanel.center.x - qrSize * 0.5f, qrTop, qrSize, qrSize);
                var qrBacking = new Rect(qrRect.x - 8f * uiScale, qrRect.y - 8f * uiScale,
                    qrRect.width + 16f * uiScale, qrRect.height + 16f * uiScale);
                GUI.color = Color.white;
                GUI.DrawTexture(qrBacking, Texture2D.whiteTexture);
                GUI.DrawTexture(qrRect, _onlineCoordinator.JoinCodeQrTexture, ScaleMode.ScaleToFit);
            }
            else
            {
                GUI.Label(new Rect(codePanel.x + codeInset, codeRect.yMax + 16f * uiScale,
                    codePanel.width - codeInset * 2f, 44f * uiScale), "Compartilhe o código com os jogadores.",
                    _statusStyle);
            }
            GUI.Label(new Rect(codePanel.x + codeInset, codePanel.yMax - 34f * uiScale,
                codePanel.width - codeInset * 2f, 24f * uiScale), _onlineCoordinator.Status, _statusStyle);
        }

        private void DrawMemberRow(Rect rect, string nickname, bool isHost, float scale, bool waiting = false)
        {
            var fill = waiting ? new Color32(15, 31, 46, 210) : new Color32(17, 39, 56, 255);
            var border = isHost ? GoldColor : (Color)new Color32(63, 91, 112, 255);
            DrawFramedPanel(rect, fill, border, isHost ? 1.6f * scale : 1f * scale);
            GUI.color = Color.white;
            var iconRect = new Rect(rect.x + 15f * scale, rect.y + rect.height * 0.15f,
                rect.height * 0.7f, rect.height * 0.7f);
            if (isHost && _crownIcon != null)
                GUI.DrawTexture(iconRect, _crownIcon, ScaleMode.ScaleToFit, true);
            else if (!isHost)
                GUI.Label(iconRect, "●", _waitingMemberStyle);
            GUI.color = waiting ? (Color)new Color32(133, 148, 160, 255) : Color.white;
            GUI.Label(new Rect(iconRect.xMax + 10f * scale, rect.y,
                    rect.width - iconRect.width - 82f * scale, rect.height), nickname,
                _waitingMemberStyle);
            GUI.color = new Color32(112, 226, 111, 255);
            GUI.Label(new Rect(rect.xMax - 86f * scale, rect.y,
                    76f * scale, rect.height), waiting ? "—" : "ONLINE", _statusStyle);
            GUI.color = Color.white;
        }

        private static void DrawFramedPanel(Rect rect, Color fill, Color border, float thickness)
        {
            GUI.color = border;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = fill;
            GUI.DrawTexture(new Rect(rect.x + thickness, rect.y + thickness,
                Mathf.Max(1f, rect.width - thickness * 2f), Mathf.Max(1f, rect.height - thickness * 2f)),
                Texture2D.whiteTexture);
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
            for (var playerCount = 1; playerCount <= 4; playerCount++)
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
            if (GUILayout.Button("PRONTO", _newGameStyle, GUILayout.Height(54f * scale)))
            {
                _selectedCharacters = new[] { -1, -1, -1, -1 };
                _turnOrderPlayerIndices = new[] { 0, 1, 2, 3 };
                _showCharacterSelection = true;
            }
            GUILayout.Space(12f * scale);
            GUI.backgroundColor = new Color32(65, 73, 80, 255);
            if (GUILayout.Button("VOLTAR", _playerCountStyle, GUILayout.Height(44f * scale)))
                _showSetup = false;
            GUILayout.EndArea();

            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
        }

        private void DrawCharacterSelectionScreen(float scale)
        {
            var previousColor = GUI.color;
            var previousBackground = GUI.backgroundColor;
            var safe = UnityEngine.Application.isMobilePlatform
                ? Screen.safeArea
                : new Rect(0f, 0f, Screen.width, Screen.height);
            var safeTop = Screen.height - safe.yMax;
            var panelWidth = Mathf.Min(1120f * scale, safe.width - 32f * scale);
            var characterMenuHeight = _openCharacterMenu >= 0
                ? Mathf.CeilToInt(GetAvailableCharacterIndices(_openCharacterMenu).Length / 2f) * 31f
                : 0f;
            var orderMenuHeight = _openTurnOrderMenu >= 0 ? 34f : 0f;
            var rowHeight = 60f;
            var panelHeight = Mathf.Min((215f + _selectedPlayerCount * rowHeight + characterMenuHeight + orderMenuHeight) * scale,
                safe.height - 24f * scale);
            var panelRect = new Rect(safe.x + (safe.width - panelWidth) * 0.5f,
                safeTop + (safe.height - panelHeight) * 0.5f, panelWidth, panelHeight);

            GUI.color = new Color(0.05f, 0.07f, 0.09f, 0.98f);
            GUI.DrawTexture(panelRect, Texture2D.whiteTexture);
            GUI.color = GoldColor;
            GUI.DrawTexture(new Rect(panelRect.x, panelRect.y, panelRect.width, 4f * scale), Texture2D.whiteTexture);
            GUI.color = Color.white;

            var padding = 28f * scale;
            var left = panelRect.x + padding;
            var contentWidth = panelRect.width - padding * 2f;
            var playerWidth = contentWidth * 0.20f;
            var characterWidth = contentWidth * 0.55f;
            var orderWidth = contentWidth - playerWidth - characterWidth - 14f * scale;
            GUI.Label(new Rect(left, panelRect.y + 14f * scale, contentWidth, 48f * scale),
                "SELEÇÃO DE PERSONAGENS", _setupTitleStyle);
            GUI.Label(new Rect(left, panelRect.y + 62f * scale, contentWidth, 28f * scale),
                "Escolha uma capivara para cada pessoa e defina a ordem dos turnos.", _setupDescriptionStyle);

            var headerY = panelRect.y + 106f * scale;
            GUI.Label(new Rect(left, headerY, playerWidth, 28f * scale), "JOGADOR", _subtitleStyle);
            GUI.Label(new Rect(left + playerWidth, headerY, characterWidth, 28f * scale), "PERSONAGEM", _subtitleStyle);
            GUI.Label(new Rect(left + playerWidth + characterWidth + 8f * scale, headerY,
                orderWidth, 28f * scale), "ORDEM DO TURNO", _subtitleStyle);
            DrawSelectionDivider(new Rect(left, headerY + 28f * scale, contentWidth, 1f * scale));

            var characterNames = GetCharacterNames();
            var rowY = headerY + 35f * scale;
            for (var playerIndex = 0; playerIndex < _selectedPlayerCount; playerIndex++)
            {
                var rowHeightScaled = 48f * scale;
                var assignedCharacter = _selectedCharacters[playerIndex];
                var selectionLabel = assignedCharacter >= 0 && assignedCharacter < characterNames.Length
                    ? characterNames[assignedCharacter]
                    : "Selecionar capivara...  ▾";
                GUI.Label(new Rect(left, rowY, playerWidth, rowHeightScaled), $"Pessoa {playerIndex + 1}", _characterNameStyle);

                GUI.backgroundColor = assignedCharacter >= 0 ? GoldColor : new Color32(24, 34, 43, 255);
                var characterRect = new Rect(left + playerWidth, rowY, characterWidth, rowHeightScaled);
                if (GUI.Button(characterRect, selectionLabel, _characterOptionStyle))
                {
                    _openCharacterMenu = _openCharacterMenu == playerIndex ? -1 : playerIndex;
                    _openTurnOrderMenu = -1;
                }

                var orderIndex = Array.IndexOf(_turnOrderPlayerIndices, playerIndex);
                GUI.backgroundColor = new Color32(24, 34, 43, 255);
                var orderRect = new Rect(characterRect.xMax + 8f * scale, rowY, orderWidth, rowHeightScaled);
                if (GUI.Button(orderRect, $"{orderIndex + 1}º  ▾", _characterOptionStyle))
                {
                    _openTurnOrderMenu = _openTurnOrderMenu == playerIndex ? -1 : playerIndex;
                    _openCharacterMenu = -1;
                }
                rowY += rowHeightScaled + 5f * scale;

                if (_openCharacterMenu == playerIndex)
                {
                    var available = GetAvailableCharacterIndices(playerIndex);
                    var optionHeight = 28f * scale;
                    for (var optionIndex = 0; optionIndex < available.Length; optionIndex += 2)
                    {
                        for (var column = 0; column < 2 && optionIndex + column < available.Length; column++)
                        {
                            var characterIndex = available[optionIndex + column];
                            var optionRect = new Rect(characterRect.x + column * (characterRect.width * 0.5f),
                                rowY + (optionIndex / 2) * (optionHeight + 2f * scale),
                                characterRect.width * 0.5f - 2f * scale, optionHeight);
                            GUI.backgroundColor = characterIndex == assignedCharacter
                                ? GoldColor
                                : new Color32(35, 48, 59, 255);
                            if (GUI.Button(optionRect, characterNames[characterIndex], _characterOptionStyle))
                            {
                                _selectedCharacters[playerIndex] = characterIndex;
                                _openCharacterMenu = -1;
                            }
                        }
                    }
                    rowY += Mathf.CeilToInt(available.Length / 2f) * (optionHeight + 2f * scale);
                }
                else if (_openTurnOrderMenu == playerIndex)
                {
                    var optionWidth = orderRect.width / _selectedPlayerCount;
                    for (var order = 0; order < _selectedPlayerCount; order++)
                    {
                        var requestedOrder = order;
                        GUI.backgroundColor = order == orderIndex ? GoldColor : new Color32(35, 48, 59, 255);
                        if (GUI.Button(new Rect(orderRect.x + order * optionWidth, rowY, optionWidth - 2f * scale,
                                28f * scale), $"{order + 1}º", _characterOptionStyle))
                        {
                            SetPlayerTurnOrder(playerIndex, requestedOrder);
                            _openTurnOrderMenu = -1;
                        }
                    }
                    rowY += 31f * scale;
                }

                DrawSelectionDivider(new Rect(left, rowY, contentWidth, 1f * scale));
                rowY += 5f * scale;
            }

            var footerY = panelRect.yMax - 60f * scale;
            var footerGap = 14f * scale;
            var backWidth = contentWidth * 0.36f;
            GUI.backgroundColor = new Color32(65, 73, 80, 255);
            if (GUI.Button(new Rect(left, footerY, backWidth, 46f * scale), "VOLTAR", _playerCountStyle))
            {
                _showCharacterSelection = false;
                _openCharacterMenu = -1;
                _openTurnOrderMenu = -1;
            }
            GUI.enabled = HasAllCharacterSelections();
            GUI.backgroundColor = GoldColor;
            if (GUI.Button(new Rect(left + backWidth + footerGap, footerY,
                    contentWidth - backWidth - footerGap, 46f * scale), "COMEÇAR PARTIDA", _newGameStyle))
                StartSelectedGame(MenuAction.BeginConfiguredGame);
            GUI.enabled = true;

            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
        }

        private static void DrawSelectionDivider(Rect rect)
        {
            GUI.color = new Color32(65, 73, 80, 255);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private string[] GetCharacterNames() => _controller.CharacterDefinitions
            .Select(character => character.DisplayName)
            .ToArray();

        private int[] GetAvailableCharacterIndices(int playerIndex)
        {
            var characters = GetCharacterNames();
            var takenByOtherPlayers = new HashSet<int>();
            for (var i = 0; i < _selectedPlayerCount; i++)
                if (i != playerIndex && _selectedCharacters[i] >= 0)
                    takenByOtherPlayers.Add(_selectedCharacters[i]);
            return Enumerable.Range(0, characters.Length)
                .Where(index => !takenByOtherPlayers.Contains(index))
                .ToArray();
        }

        private bool HasAllCharacterSelections()
        {
            if (_controller.CharacterDefinitions.Count < 6)
                return false;
            var assigned = new HashSet<int>();
            for (var i = 0; i < _selectedPlayerCount; i++)
                if (_selectedCharacters[i] < 0 || _selectedCharacters[i] >= _controller.CharacterDefinitions.Count ||
                    !assigned.Add(_selectedCharacters[i]))
                    return false;
            return true;
        }

        private void StartSelectedGame(MenuAction action)
        {
            try
            {
                var turnOrdersByPerson = new int[_selectedPlayerCount];
                for (var orderIndex = 0; orderIndex < _selectedPlayerCount; orderIndex++)
                    turnOrdersByPerson[_turnOrderPlayerIndices[orderIndex]] = orderIndex;
                var started = action == MenuAction.StartNewGame || action == MenuAction.BeginConfiguredGame
                    ? action == MenuAction.BeginConfiguredGame
                        ? _controller.StartNewGame(_selectedPlayerCount, _selectedCharacters, turnOrdersByPerson)
                        : _controller.StartNewGame(_selectedPlayerCount)
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

        private void SwapTurnOrder(int first, int second)
        {
            if (first < 0 || second < 0 || first >= _selectedPlayerCount || second >= _selectedPlayerCount)
                return;
            var value = _turnOrderPlayerIndices[first];
            _turnOrderPlayerIndices[first] = _turnOrderPlayerIndices[second];
            _turnOrderPlayerIndices[second] = value;
        }

        private void SetPlayerTurnOrder(int playerIndex, int requestedOrder)
        {
            var currentOrder = Array.IndexOf(_turnOrderPlayerIndices, playerIndex);
            if (currentOrder < 0 || requestedOrder < 0 || requestedOrder >= _selectedPlayerCount)
                return;
            SwapTurnOrder(currentOrder, requestedOrder);
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
            _nicknameButtonStyle = new GUIStyle(_newGameStyle)
            {
                fontSize = Mathf.RoundToInt(15f * scale),
                padding = new RectOffset(16, 16, 4, 4)
            };
            SetFlatButtonBackgrounds(_nicknameButtonStyle);
            SetButtonTextColor(_nicknameButtonStyle, BackgroundColor);
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
            _characterNameStyle = new GUIStyle(_playerCountStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(15f * scale),
                normal = { textColor = YellowColor }
            };
            _characterOptionStyle = new GUIStyle(_playerCountStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(14f * scale),
                wordWrap = true,
                padding = new RectOffset(4, 4, 3, 3)
            };
            _nicknameFieldStyle = new GUIStyle(GUI.skin.textField)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(19f * scale),
                padding = new RectOffset(14, 14, 8, 8),
                normal = { textColor = WhiteColor },
                focused = { textColor = WhiteColor },
                hover = { textColor = WhiteColor },
                active = { textColor = WhiteColor }
            };
            SetTextColor(_nicknameFieldStyle, WhiteColor);
            _nicknamePlaceholderStyle = new GUIStyle(_nicknameFieldStyle);
            SetTextColor(_nicknamePlaceholderStyle, new Color32(165, 178, 188, 255));
            _joinCodeStyle = new GUIStyle(_titleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(23f * scale),
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = GoldColor }
            };
            _waitingHeaderStyle = new GUIStyle(_titleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(40f * scale),
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                normal = { textColor = WhiteColor }
            };
            _waitingSectionStyle = new GUIStyle(_subtitleStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(18f * scale),
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                normal = { textColor = YellowColor }
            };
            _waitingMemberStyle = new GUIStyle(_subtitleStyle)
            {
                alignment = TextAnchor.MiddleLeft,
                fontSize = Mathf.RoundToInt(19f * scale),
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                normal = { textColor = WhiteColor }
            };
            _waitingCodeStyle = new GUIStyle(GUI.skin.box)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(30f * scale),
                fontStyle = FontStyle.Bold,
                wordWrap = false,
                clipping = TextClipping.Clip,
                normal = { textColor = GoldColor }
            };
            _waitingButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(20f * scale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = WhiteColor },
                hover = { textColor = WhiteColor },
                active = { textColor = WhiteColor },
                focused = { textColor = WhiteColor }
            };
            _waitingCrownStyle = new GUIStyle(_waitingMemberStyle)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(30f * scale),
                normal = { textColor = GoldColor }
            };
            SetFlatButtonBackgrounds(_waitingButtonStyle);
            SetFlatButtonBackgrounds(_playerCountStyle);
            SetFlatButtonBackgrounds(_selectedPlayerCountStyle);
        }

        private string DrawInputField(string value, MobileInputTarget target, string placeholder,
            int characterLimit, TouchScreenKeyboardType keyboardType, float scale,
            params GUILayoutOption[] layoutOptions)
        {
            var previousColor = GUI.color;
            var previousBackground = GUI.backgroundColor;
            var previousCursorColor = GUI.skin.settings.cursorColor;
            var previousSelectionColor = GUI.skin.settings.selectionColor;
            GUI.color = Color.white;
            GUI.backgroundColor = new Color32(22, 35, 47, 255);
            GUI.skin.settings.cursorColor = GoldColor;
            GUI.skin.settings.selectionColor = new Color32(245, 197, 66, 150);
            if (!UnityEngine.Application.isMobilePlatform || !TouchScreenKeyboard.isSupported)
            {
                var editedValue = GUILayout.TextField(value, characterLimit, _nicknameFieldStyle, layoutOptions);
                GUI.color = previousColor;
                GUI.backgroundColor = previousBackground;
                GUI.skin.settings.cursorColor = previousCursorColor;
                GUI.skin.settings.selectionColor = previousSelectionColor;
                return editedValue;
            }

            var display = string.IsNullOrEmpty(value) ? placeholder : value;
            if (_mobileKeyboardTarget == target && _mobileKeyboard != null)
                display = string.IsNullOrEmpty(value) ? placeholder : value;

            var isPlaceholder = string.IsNullOrEmpty(value);
            if (GUILayout.Button(display, isPlaceholder ? _nicknamePlaceholderStyle : _nicknameFieldStyle, layoutOptions))
            {
                _mobileKeyboardTarget = target;
                _mobileKeyboard = TouchScreenKeyboard.Open(value, keyboardType,
                    autocorrection: false, multiline: false, secure: false, alert: false,
                    textPlaceholder: placeholder, characterLimit: characterLimit);
            }

            GUI.color = previousColor;
            GUI.backgroundColor = previousBackground;
            GUI.skin.settings.cursorColor = previousCursorColor;
            GUI.skin.settings.selectionColor = previousSelectionColor;
            return value;
        }

        private void UpdateMobileKeyboardText()
        {
            if (_mobileKeyboard == null)
                return;

            var editedText = _mobileKeyboard.text ?? string.Empty;
            if (_mobileKeyboardTarget == MobileInputTarget.Nickname)
                _nickname = editedText;
            else if (_mobileKeyboardTarget == MobileInputTarget.JoinCode)
                _joinCode = editedText;

            if (_mobileKeyboard.status == TouchScreenKeyboard.Status.Visible)
                return;

            _mobileKeyboard = null;
            _mobileKeyboardTarget = MobileInputTarget.None;
        }

        private bool IsKeyboardVisibleFor(MobileInputTarget target) =>
            _mobileKeyboardTarget == target && _mobileKeyboard != null &&
            (_mobileKeyboard.active || _mobileKeyboard.status == TouchScreenKeyboard.Status.Visible);

        private Rect GetKeyboardSafeArea()
        {
            var safe = UnityEngine.Application.isMobilePlatform
                ? Screen.safeArea
                : new Rect(0f, 0f, Screen.width, Screen.height);
            var guiSafe = new Rect(safe.xMin, Screen.height - safe.yMax, safe.width, safe.height);
            if (_mobileKeyboard == null || !_mobileKeyboard.active)
                return guiSafe;

            var keyboard = TouchScreenKeyboard.area;
            if (keyboard.height <= 0f)
                return guiSafe;

            var visibleHeightFromBottom = keyboard.yMin > 0f
                ? keyboard.yMin
                : Screen.height - keyboard.height;
            var visibleHeight = Mathf.Clamp(visibleHeightFromBottom - (Screen.height - safe.yMax), 1f, guiSafe.height);
            return new Rect(guiSafe.x, guiSafe.y, guiSafe.width, visibleHeight);
        }

        private static Texture2D CreateCrownTexture()
        {
            const int width = 128;
            const int height = 96;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "MoneyRank Master Crown",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var outline = new[] { new Vector2(8, 24), new Vector2(4, 68), new Vector2(34, 48),
                new Vector2(64, 88), new Vector2(94, 48), new Vector2(124, 68), new Vector2(120, 24) };
            var crown = new[] { new Vector2(13, 29), new Vector2(10, 60), new Vector2(36, 43),
                new Vector2(64, 79), new Vector2(92, 43), new Vector2(118, 60), new Vector2(115, 29) };
            var band = new[] { new Vector2(13, 25), new Vector2(115, 25), new Vector2(115, 39), new Vector2(13, 39) };
            var pixels = new Color32[width * height];
            var gold = new Color32(245, 197, 66, 255);
            var darkGold = new Color32(106, 70, 10, 255);
            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var point = new Vector2(x + 0.5f, y + 0.5f);
                if (PointInPolygon(point, outline) || PointInPolygon(point, band))
                    pixels[y * width + x] = darkGold;
                if (PointInPolygon(point, crown) || PointInPolygon(point, band))
                    pixels[y * width + x] = gold;
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static bool PointInPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
        {
            var inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) &&
                    point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x)
                    inside = !inside;
            }
            return inside;
        }

        private static void SetTextColor(GUIStyle style, Color color)
        {
            style.normal.textColor = color;
            style.focused.textColor = color;
            style.hover.textColor = color;
            style.active.textColor = color;
            style.onNormal.textColor = color;
            style.onFocused.textColor = color;
            style.onHover.textColor = color;
            style.onActive.textColor = color;
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

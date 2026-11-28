using System;
using System.Collections.Generic;
using System.Linq;
using MoneyRank.Domain.V4;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace MoneyRank.V4.Presentation
{
    /// <summary>Master HUD (uGUI built in code): setup, match, decision modal, fiscal/history panels. Never calculates rules.</summary>
    public sealed partial class V4Hud : MonoBehaviour
    {
        private enum SidePanel { None, Fiscal, History, Rules, Players, YearSummary }

        public Action<V4Session> SessionStarted;
        public Action BackToSetup;

        private V4Session _session;
        private Canvas _canvas;
        private RectTransform _setupRoot, _matchRoot, _modalRoot, _panelRoot, _endRoot;
        private RectTransform _cardsRoot, _sideRoot, _controlRoot;
        private Text _toast;
        private SidePanel _panel = SidePanel.None;
        private bool _modalDismissed;
        private string _lastDecisionId;
        private string _selectedOption;
        private string _swapFrom;
        private bool _swapMode;

        public void Init()
        {
            var cgo = new GameObject("V4Canvas");
            cgo.transform.SetParent(transform, false);
            _canvas = cgo.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = cgo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            // Expand keeps the whole 1920x1080 design area visible on any aspect ratio (4:3 tablet, 16:10, ultrawide): no clipping/overlap.
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            cgo.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<InputSystemUIInputModule>();
            }
            _setupRoot = V4Ui.Rect(_canvas.transform, "Setup", UiAnchor.Stretch, 0, 0, 0, 0);
            _matchRoot = V4Ui.Rect(_canvas.transform, "Match", UiAnchor.Stretch, 0, 0, 0, 0);
            _panelRoot = V4Ui.Rect(_canvas.transform, "SidePanel", UiAnchor.Stretch, 0, 0, 0, 0);
            _modalRoot = V4Ui.Rect(_canvas.transform, "Modal", UiAnchor.Stretch, 0, 0, 0, 0);
            _endRoot = V4Ui.Rect(_canvas.transform, "End", UiAnchor.Stretch, 0, 0, 0, 0);
            _matchRoot.gameObject.SetActive(false);
        }

        // ================================================================== SETUP

        private int _setupCount = 2;
        private bool _setupSolo;
        private string _setupDifficulty = "medio";
        private int _setupBattery = 1;
        private readonly string[] _setupProf = { "P01", "P02", "P03", "P04" };
        private readonly string[] _setupColor = { "verde", "vermelho", "azul", "amarelo" };
        private readonly string[] _setupName = { "Jogador 1", "Jogador 2", "Jogador 3", "Jogador 4" };
        private string _setupMessage;

        public void ShowSetup(string message = null)
        {
            _session = null;
            _setupMessage = message;
            _matchRoot.gameObject.SetActive(false);
            V4Ui.Clear(_modalRoot); V4Ui.Clear(_panelRoot); V4Ui.Clear(_endRoot);
            _setupRoot.gameObject.SetActive(true);
            BuildSetup();
        }

        private void BuildSetup()
        {
            V4Ui.Clear(_setupRoot);
            var content = V4Assets.Content;
            V4Ui.Box(_setupRoot, "Bg", new Color(0.02f, 0.02f, 0.025f, 0.97f), UiAnchor.Stretch, 0, 0, 0, 0);
            V4Ui.Label(_setupRoot, "MONEY RANK", 88, V4Ui.Gold, TextAnchor.MiddleCenter, UiAnchor.TC, 0, 28, 1200, 110, FontStyle.Bold);
            V4Ui.Label(_setupRoot, "Painel do mestre · V4 (pré-teste) · educação financeira, fiscal e cidadania", 26, V4Ui.TextMuted, TextAnchor.MiddleCenter, UiAnchor.TC, 0, 134, 1400, 40);

            V4Ui.Label(_setupRoot, "Jogadores", 28, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 140, 200, 200, 44, FontStyle.Bold);
            for (var n = 2; n <= 4; n++)
            {
                var count = n;
                V4Ui.Btn(_setupRoot, n.ToString(), 28, _setupCount == n && !_setupSolo ? V4Ui.Gold : V4Ui.PanelLight, _setupCount == n && !_setupSolo ? V4Ui.Ink : V4Ui.TextMain,
                    UiAnchor.TL, 330 + (n - 2) * 70, 200, 60, 44, () => { _setupSolo = false; _setupCount = count; BuildSetup(); });
            }
            V4Ui.Btn(_setupRoot, "Solo (demonstração)", 22, _setupSolo ? V4Ui.Gold : V4Ui.PanelLight, _setupSolo ? V4Ui.Ink : V4Ui.TextMain,
                UiAnchor.TL, 560, 200, 260, 44, () => { _setupSolo = true; _setupCount = 1; BuildSetup(); });

            for (var i = 0; i < _setupCount; i++)
            {
                var idx = i;
                var y = 275 + i * 112;
                V4Ui.Box(_setupRoot, "Row" + i, V4Ui.Panel, UiAnchor.TL, 120, y - 10, 1680, 100);
                V4Ui.Box(_setupRoot, "Stripe" + i, V4Assets.PawnColor(_setupColor[i]), UiAnchor.TL, 120, y - 10, 10, 100);
                V4Ui.Label(_setupRoot, "Jogador " + (i + 1), 24, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 150, y, 150, 34, FontStyle.Bold);
                V4Ui.Input(_setupRoot, _setupName[i], 24, UiAnchor.TL, 150, y + 40, 250, 44, s => _setupName[idx] = string.IsNullOrWhiteSpace(s) ? "Jogador " + (idx + 1) : s.Trim());
                var k = 0;
                foreach (var prof in content.Professions)
                {
                    var pid = prof.Id;
                    var taken = Enumerable.Range(0, _setupCount).Any(j => j != idx && _setupProf[j] == pid);
                    var selected = _setupProf[idx] == pid;
                    var b = V4Ui.Btn(_setupRoot, prof.Name, 18, selected ? V4Ui.Gold : (taken ? new Color(0.12f, 0.17f, 0.22f) : V4Ui.PanelLight),
                        selected ? V4Ui.Ink : (taken ? V4Ui.TextMuted : V4Ui.TextMain), UiAnchor.TL, 430 + k * 168, y + 6, 160, 38, () => { _setupProf[idx] = pid; BuildSetup(); });
                    b.interactable = !taken;
                    V4Ui.Label(_setupRoot, "Hab.: " + prof.Ability, 14, V4Ui.TextMuted, TextAnchor.UpperCenter, UiAnchor.TL, 430 + k * 168, y + 48, 160, 40);
                    k++;
                }
                var ck = 0;
                foreach (var col in V4Engine.Colors)
                {
                    var cname = col;
                    var takenColor = Enumerable.Range(0, _setupCount).Any(j => j != idx && _setupColor[j] == cname);
                    var sel = _setupColor[idx] == cname;
                    var cb = V4Ui.Btn(_setupRoot, sel ? "✓" : string.Empty, 22, V4Assets.PawnColor(cname), V4Ui.Ink, UiAnchor.TL, 1450 + ck * 86, y + 6, 76, 38, () => { _setupColor[idx] = cname; BuildSetup(); });
                    cb.interactable = !takenColor;
                    ck++;
                }
                V4Ui.Label(_setupRoot, "Cor do peão", 14, V4Ui.TextMuted, TextAnchor.UpperLeft, UiAnchor.TL, 1450, y + 48, 300, 24);
            }

            var by = 275 + _setupCount * 112 + 20;
            V4Ui.Label(_setupRoot, "Dificuldade", 26, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 140, by, 200, 44, FontStyle.Bold);
            var diffs = new[] { ("facil", "Fácil"), ("medio", "Médio"), ("dificil", "Difícil") };
            for (var d = 0; d < diffs.Length; d++)
            {
                var id = diffs[d].Item1; var sel = _setupDifficulty == id;
                V4Ui.Btn(_setupRoot, diffs[d].Item2, 24, sel ? V4Ui.Gold : V4Ui.PanelLight, sel ? V4Ui.Ink : V4Ui.TextMain, UiAnchor.TL, 340 + d * 150, by, 140, 44, () => { _setupDifficulty = id; BuildSetup(); });
            }
            V4Ui.Label(_setupRoot, "Bateria", 26, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 820, by, 120, 44, FontStyle.Bold);
            for (var b = 1; b <= 2; b++)
            {
                var bat = b;
                V4Ui.Btn(_setupRoot, "B" + b, 24, _setupBattery == b ? V4Ui.Gold : V4Ui.PanelLight, _setupBattery == b ? V4Ui.Ink : V4Ui.TextMain, UiAnchor.TL, 950 + (b - 1) * 90, by, 80, 44, () => { _setupBattery = bat; BuildSetup(); });
            }
            V4Ui.Label(_setupRoot, "Modo", 26, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 1180, by, 100, 44, FontStyle.Bold);
            V4Ui.Btn(_setupRoot, "Local físico-digital", 22, V4Ui.Gold, V4Ui.Ink, UiAnchor.TL, 1290, by, 260, 44, null);
            var online = V4Ui.Btn(_setupRoot, "Online (em breve)", 22, V4Ui.PanelLight, V4Ui.TextMuted, UiAnchor.TL, 1560, by, 240, 44, null);
            online.interactable = false;

            V4Ui.Btn(_setupRoot, "INICIAR PARTIDA", 40, V4Ui.Gold, V4Ui.Ink, UiAnchor.BC, 0, 150, 520, 84, StartMatch);
            var saves = _session == null ? V4Session.NewSaveList() : new List<string>();
            if (saves.Count > 0)
                V4Ui.Btn(_setupRoot, "Continuar última partida", 26, V4Ui.Teal, V4Ui.TextMain, UiAnchor.BC, 0, 70, 520, 56, ContinueLast);
            if (!string.IsNullOrEmpty(_setupMessage))
                V4Ui.Label(_setupRoot, _setupMessage, 22, V4Ui.Bad, TextAnchor.MiddleCenter, UiAnchor.BC, 0, 20, 1500, 40);
        }

        private void StartMatch()
        {
            var content = V4Assets.Content;
            var diffId = "B" + _setupBattery + "_" + _setupDifficulty;
            var setups = new List<V4PlayerSetup>();
            for (var i = 0; i < _setupCount; i++)
                setups.Add(new V4PlayerSetup { Id = "p" + (i + 1), Name = _setupName[i], ProfessionId = _setupProf[i], Color = _setupColor[i] });
            string error;
            var session = V4Session.StartNew(setups, new V4Config { DifficultyId = diffId, Mode = "local", SoloDemo = _setupSolo }, out error);
            if (session == null) { _setupMessage = error; BuildSetup(); return; }
            SessionStarted?.Invoke(session);
        }

        private void ContinueLast()
        {
            var ids = V4Session.NewSaveList();
            if (ids.Count == 0) return;
            string error;
            var session = V4Session.Resume(ids[ids.Count - 1], out error);
            if (session == null) { _setupMessage = error; BuildSetup(); return; }
            SessionStarted?.Invoke(session);
        }

        // ================================================================== MATCH

        public void Bind(V4Session session)
        {
            if (_session != null) _session.Changed -= Refresh;
            _session = session;
            _session.Changed += Refresh;
            _setupRoot.gameObject.SetActive(false);
            _matchRoot.gameObject.SetActive(true);
            _panel = SidePanel.None; _modalDismissed = false; _lastDecisionId = null; _selectedOption = null; _swapMode = false;
            _seenHistory = session.S.History.Count;
            _summaryYearShown = session.S.YearIndex - 1;
            // Initial reading (playtest): a new match opens on the rules, attributes and abilities of each player.
            if (!session.S.History.Any(h => h.Kind == "move")) _panel = SidePanel.Rules;
            V4Ui.Clear(_matchRoot);
            BuildMatchFrame();
            // A new session starts with no display link: reuse the port of the previous match, or look for the display.
            var previousPort = _serialPort;
            if (_detect != null) { StopCoroutine(_detect); _detect = null; _detectingPort = null; }
            _serial?.Dispose(); _serial = null; _serialPort = null; _lastLinkKey = null;
            Refresh();
            if (previousPort != null) ConnectDisplay(previousPort, quiet: true);
            else if (!Application.isEditor) AutoDetectDisplayOnStart();   // in the Editor (tests) the master clicks "Detectar display"
        }

        private void BuildMatchFrame()
        {
            // Opaque: the 3D laptop's "MONEY RANK" screen sits behind the bar and showed through, cut by the buttons.
            var bar = V4Ui.Panel; bar.a = 1f;
            V4Ui.Box(_matchRoot, "TopBar", bar, UiAnchor.TL, 0, 0, 1920, 72);
            V4Ui.Label(_matchRoot, "MONEY RANK", 34, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 24, 0, 300, 72, FontStyle.Bold);
            V4Ui.Btn(_matchRoot, "Menu", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 16, 14, 100, 44, () => BackToSetup?.Invoke());
            V4Ui.Btn(_matchRoot, "Histórico", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 124, 14, 120, 44, () => { _panel = _panel == SidePanel.History ? SidePanel.None : SidePanel.History; Refresh(); });
            V4Ui.Btn(_matchRoot, "Fiscal", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 252, 14, 100, 44, () => { _panel = _panel == SidePanel.Fiscal ? SidePanel.None : SidePanel.Fiscal; Refresh(); });
            V4Ui.Btn(_matchRoot, "Exportar", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 360, 14, 120, 44, DoExport);
            V4Ui.Btn(_matchRoot, "Desfazer", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 488, 14, 120, 44, DoUndo);
            V4Ui.Btn(_matchRoot, "Salvar", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 616, 14, 100, 44, () => { _session.Persist(); Toast(_session.LastSaveError == null ? "Partida salva." : "Falha ao salvar: " + _session.LastSaveError); });
            V4Ui.Btn(_matchRoot, "Jogadores", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 724, 14, 130, 44, () => TogglePanel(SidePanel.Players));
            V4Ui.Btn(_matchRoot, "Regras", 20, V4Ui.Gold, V4Ui.Ink, UiAnchor.TR, 862, 14, 110, 44, () => TogglePanel(SidePanel.Rules));
            _cardsRoot = V4Ui.Rect(_matchRoot, "Cards", UiAnchor.TL, 12, 84, 392, 980);
            _sideRoot = V4Ui.Rect(_matchRoot, "Side", UiAnchor.TR, 12, 84, 392, 980);
            _controlRoot = V4Ui.Rect(_matchRoot, "Control", UiAnchor.BC, 0, 8, 980, 172);
            _toast = V4Ui.Label(_matchRoot, string.Empty, 22, V4Ui.Gold, TextAnchor.MiddleCenter, UiAnchor.BC, 0, 184, 1100, 40, FontStyle.Bold);
            _banner = V4Ui.Label(_matchRoot, string.Empty, 54, Color.white, TextAnchor.MiddleCenter, UiAnchor.TC, 0, 96, 1300, 150, FontStyle.Bold);
            _banner.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3f, -3f);
            _banner.gameObject.SetActive(false);
        }

        private void Toast(string text) { if (_toast != null) _toast.text = text; }

        private void DoUndo()
        {
            var r = _session.Undo("correção do mestre");
            Toast(r.Ok ? "Última ação desfeita (histórico e ledger preservados)." : r.Error);
        }

        private void DoExport()
        {
            try { var files = _session.Export(); Toast("Exportado: " + files[0].Replace('\\', '/')); }
            catch (Exception ex) { Toast("Falha na exportação: " + ex.Message); }
        }

        private PlayerState Current => _session.S.TurnOrder.Count == 0 ? null : _session.Engine.CurrentPlayer;

        private void Refresh()
        {
            if (_session == null || _cardsRoot == null) return;
            var s = _session.S;
            AnnounceNewEvents();
            BuildTopBarInfo();
            BuildCards();
            BuildSide();
            BuildControl();
            BuildPanel();
            if (s.Pending != null && s.Pending.Id != _lastDecisionId) { _lastDecisionId = s.Pending.Id; _modalDismissed = false; _selectedOption = null; _swapMode = false; }
            if (s.Pending == null) { _lastDecisionId = null; _selectedOption = null; _swapMode = false; }
            BuildModal();
            BuildEnd();
            if (!string.IsNullOrEmpty(_session.LastMessage) && _toast != null && string.IsNullOrEmpty(_toast.text)) _toast.text = _session.LastMessage;
        }

        private Transform _topInfo;
        private void BuildTopBarInfo()
        {
            if (_topInfo != null) Destroy(_topInfo.gameObject);
            var s = _session.S;
            var root = V4Ui.Rect(_matchRoot, "TopInfo", UiAnchor.TL, 300, 0, 640, 72);
            _topInfo = root;
            var diff = _session.Engine.Difficulty;
            var cur = Current;
            var turn = cur == null ? "—" : cur.Name + " · " + _session.Content.Profession(cur.ProfessionId).Name;
            V4Ui.Label(root, "Rodada " + s.RoundIndex + " · Ano " + s.YearIndex + " (fecha em " + (_session.Engine.Rules.RoundsPerYear - (s.CompletedRounds % _session.Engine.Rules.RoundsPerYear)) + " rodada[s])", 20, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 0, 6, 640, 30);
            V4Ui.Label(root, "VEZ: " + turn + "  ·  " + diff.Difficulty.ToUpperInvariant() + " (B" + diff.Battery + ")", 20, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 0, 36, 640, 30, FontStyle.Bold);
        }

        // ------------------------------------------------------------------ player cards

        private static readonly string[] AttrNames = { "Conhecimento", "Investimento", "Cidadania", "Toxina", "Cortisol", "Dívidas" };

        private void BuildCards()
        {
            V4Ui.Clear(_cardsRoot);
            var s = _session.S;
            var cur = Current;
            var n = s.Players.Count;
            var h = Mathf.Min(236f, 970f / n - 6f);
            for (var i = 0; i < n; i++)
            {
                var p = s.Players[i];
                var y = i * (h + 6f);
                var isTurn = cur != null && p.Id == cur.Id;
                var card = V4Ui.Rect(_cardsRoot, "Card " + p.Name, UiAnchor.TL, 0, y, 392, h);
                V4Ui.Box(card, "Bg", isTurn ? new Color(0.20f, 0.16f, 0.07f, 0.97f) : V4Ui.Panel, UiAnchor.Stretch, 0, 0, 0, 0);
                V4Ui.Box(card, "Stripe", V4Assets.PawnColor(p.Color), UiAnchor.TL, 0, 0, 10, h);
                if (isTurn) V4Ui.Box(card, "Turn", V4Ui.Gold, UiAnchor.TL, 10, 0, 382, 3);
                var prof = _session.Content.Profession(p.ProfessionId);
                V4Ui.Label(card, (isTurn ? "▶ " : string.Empty) + p.Name, 24, p.Eliminated ? V4Ui.TextMuted : V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, 4, 250, 32, FontStyle.Bold);
                V4Ui.Label(card, prof.Name + " · nível " + p.Level, 16, V4Ui.Gold, TextAnchor.MiddleRight, UiAnchor.TR, 10, 6, 190, 28);
                V4Ui.Label(card, "Carteira " + V4Ui.Money(p.Wallet) + "  ·  Reserva " + V4Ui.Money(p.ReserveBalance), 17, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, 36, 365, 24);
                V4Ui.Label(card, "Salário " + V4Ui.Money(p.Salary) + "/ano", 15, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, 20, 58, 365, 22);
                var barTop = 82f;
                var rowH = Mathf.Min(26f, (h - 150f) / 3f);
                for (var a = 0; a < 6; a++)
                {
                    var col = a / 3; var row = a % 3;
                    var bx = 20 + col * 184; var by = barTop + row * rowH;
                    var bar = p.Attributes[a];
                    var positive = a <= 2;
                    // One segment per point of the bar (threshold 3): full bar = pawn moves (positive) or retreats (negative).
                    var threshold = _session.Engine.Rules.AttributeThreshold;
                    V4Ui.Label(card, AttrNames[a], 13, positive ? new Color(0.62f, 0.86f, 0.70f) : new Color(1f, 0.66f, 0.66f), TextAnchor.MiddleLeft, UiAnchor.TL, bx, by, 92, rowH, FontStyle.Bold);
                    for (var seg = 0; seg < threshold; seg++)
                    {
                        var filled = bar.Progress >= seg + 1;
                        var partial = !filled && bar.Progress > seg;
                        V4Ui.Box(card, "Seg", filled ? (positive ? V4Ui.Good : V4Ui.Bad) : (partial ? new Color(0.5f, 0.5f, 0.3f) : new Color(0.02f, 0.02f, 0.025f, 1f)),
                            UiAnchor.TL, bx + 92 + seg * 20, by + 5, 17, rowH - 10);
                    }
                    V4Ui.Label(card, V4Ui.Dec(bar.Progress) + "/" + threshold, 13, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, bx + 94 + threshold * 20, by, 40, rowH, FontStyle.Bold);
                }
                var fy = barTop + 3 * rowH + 4;
                for (var k = 0; k < p.Pawns.Length; k++)
                {
                    var pw = p.Pawns[k];
                    var fill = pw.Completed ? V4Ui.Gold : (pw.RouteIndex >= 0 ? V4Assets.PawnColor(p.Color) : new Color(0.02f, 0.02f, 0.025f, 1f));
                    V4Ui.Box(card, "Pawn" + k, fill, UiAnchor.TL, 20 + k * 28, fy + 2, 22, 22);
                    if (k == p.ActivePawn && !pw.Completed) V4Ui.Box(card, "Active", V4Ui.TextMain, UiAnchor.TL, 20 + k * 28, fy + 24, 22, 3);
                }
                var active = p.ActivePawn >= 0 ? p.Pawns[p.ActivePawn] : null;
                V4Ui.Label(card, "Ludo: peão " + (p.ActivePawn >= 0 ? (p.ActivePawn + 1).ToString() : "—") + "/4 · casa " + (active == null ? "—" : (active.RouteIndex < 0 ? "base" : active.RouteIndex + "/56")), 14, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, 140, fy, 245, 26);
                var asset = "Imóveis " + p.Properties + " · Checkpoints " + p.CheckpointRights + " · " + (_session.Engine.VehicleActive(p) ? "carro até rodada " + p.VehicleExpiresAfterRound : "sem carro");
                V4Ui.Label(card, asset, 14, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, fy + 30, 365, 22);
                var status = p.Left ? "SAIU DA PARTIDA" : p.Eliminated ? "ELIMINADO(A)" : (p.HospitalTurnsRemaining > 0 ? "HOSPITAL: perde " + p.HospitalTurnsRemaining + " turno(s)" : prof.Ability + ": " + (p.AbilityUsedThisYear ? "usada neste ano" : "disponível neste ano"));
                V4Ui.Label(card, status, 14, p.Eliminated || p.HospitalTurnsRemaining > 0 ? V4Ui.Bad : V4Ui.Good, TextAnchor.MiddleLeft, UiAnchor.TL, 20, fy + 50, 365, 22);
            }
        }

        // ------------------------------------------------------------------ side column (bank, cofre, log)

        private void BuildSide()
        {
            V4Ui.Clear(_sideRoot);
            var s = _session.S;
            var bank = V4Ui.Rect(_sideRoot, "Bank", UiAnchor.TL, 0, 0, 392, 256);
            V4Ui.Box(bank, "Bg", V4Ui.Panel, UiAnchor.Stretch, 0, 0, 0, 0);
            V4Ui.Label(bank, "BANCO E COFRE", 18, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 16, 6, 360, 28, FontStyle.Bold);
            V4Ui.Label(bank, "Banco  " + V4Ui.Money(s.BankCash), 24, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 16, 36, 360, 32, FontStyle.Bold);
            V4Ui.Label(bank, "Cofre público  " + V4Ui.Money(s.FundCash), 24, V4Ui.Good, TextAnchor.MiddleLeft, UiAnchor.TL, 16, 70, 360, 32, FontStyle.Bold);
            var reserves = s.Players.Sum(p => p.ReserveBalance);
            var receivables = s.Players.Sum(p => p.Receivables.Where(r => !r.Paid).Sum(r => r.TotalReturn));
            V4Ui.Label(bank, "Reservas (passivo)  " + V4Ui.Money(reserves) + "\nParcerias a pagar  " + V4Ui.Money(receivables), 16, V4Ui.TextMuted, TextAnchor.UpperLeft, UiAnchor.TL, 16, 106, 360, 48);
            var ok = _session.Engine.TotalCash() == _session.Engine.Rules.PhysicalMoneyTotal;
            V4Ui.Label(bank, "Caixa total " + V4Ui.Money(_session.Engine.TotalCash()) + (ok ? "  ✓ conferido" : "  ✗ DIVERGENTE"), 16, ok ? V4Ui.Good : V4Ui.Bad, TextAnchor.MiddleLeft, UiAnchor.TL, 16, 158, 360, 24);
            Color linkColor;
            var link = DisplayLinkText(out linkColor);
            V4Ui.Label(bank, link, 14, linkColor, TextAnchor.MiddleLeft, UiAnchor.TL, 16, 184, 368, 24, FontStyle.Bold);
            BuildDisplayPicker(bank);
            if (s.LiquidityBlocked)
                V4Ui.Btn(_sideRoot, "PAUSADO: falta de troco/caixa — ajustei fisicamente", 16, V4Ui.Bad, V4Ui.Ink, UiAnchor.TL, 0, 264, 392, 46, () => Toast(_session.Exec(e => e.ClearLiquidityBlock(V4Session.NewCommandId(), "ajuste físico pelo mestre")).Ok ? "Pausa liberada." : "Falha."));

            var logTop = s.LiquidityBlocked ? 318 : 264;
            var log = V4Ui.Rect(_sideRoot, "Log", UiAnchor.TL, 0, logTop, 392, 980 - logTop);
            V4Ui.Box(log, "Bg", V4Ui.Panel, UiAnchor.Stretch, 0, 0, 0, 0);
            V4Ui.Label(log, "EVENTOS RECENTES", 18, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 16, 6, 360, 28, FontStyle.Bold);
            var lines = s.History.Where(x => !x.Undone && !string.IsNullOrEmpty(x.Detail)).Reverse().Take(14).ToList();
            var y = 40f;
            foreach (var h in lines)
            {
                V4Ui.Label(log, "• " + h.Detail, 15, h.Kind == "undo" ? V4Ui.Gold : V4Ui.TextMain, TextAnchor.UpperLeft, UiAnchor.TL, 16, y, 362, 52);
                y += 52f;
                if (y > 900 - logTop) break;
            }
        }

        // ------------------------------------------------------------------ coffer display (serial)

        private IDisposable _serial;
        private string _serialPort;
        private string _detectingPort;
        private Coroutine _detect;
        private float _nextLinkCheck;
        private string _lastLinkKey;
        /// <summary>Ticks connected without any valid reply before the HUD says the Arduino is not answering (~5 s).</summary>
        private const int NoReplyTicks = 300;

        /// <summary>Real link state for the master: port, whether the coffer sketch answered and what the display confirmed.</summary>
        private string DisplayLinkText(out Color color)
        {
            var c = _session.Coffer;
            color = V4Ui.TextMuted;
            if (_detectingPort != null) { color = V4Ui.Gold; return "Procurando o display… testando " + _detectingPort; }
            if (_serialPort == null) return "Display do cofre: sem display (saldo só na tela)";
            if (c.Status != CofferLinkStatus.Connected) { color = V4Ui.Bad; return "Display (" + _serialPort + "): porta indisponível, tentando de novo…"; }
            if (c.IsConfirmed) { color = V4Ui.Good; return "Display (" + _serialPort + "): ✓ mostrando " + V4Ui.Money(c.LastAckedBalance); }
            if (!c.DisplayAnswered && c.TicksWithoutReply > NoReplyTicks) { color = V4Ui.Bad; return "Display (" + _serialPort + "): porta aberta, Arduino não responde"; }
            color = V4Ui.Gold;
            return "Display (" + _serialPort + "): conectando…";
        }

        private void BuildDisplayPicker(Transform bank)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var detect = V4Ui.Btn(bank, _detect != null ? "Procurando…" : "Detectar display", 14, V4Ui.Gold, V4Ui.Ink, UiAnchor.TL, 16, 214, 138, 30, StartDisplayDetection);
            detect.interactable = _detect == null;
            // Only real serial/USB ports: the Bluetooth virtual ports (COM3..COM8 on this PC) can never be the Arduino.
            var ports = MoneyRank.Infrastructure.V4.WindowsSerialCofferTransport.ListCandidatePorts();
            var x = 160f;
            foreach (var port in ports.Take(2))
            {
                var name = port;
                var selected = name == _serialPort;
                V4Ui.Btn(bank, name, 14, selected ? V4Ui.Good : V4Ui.PanelLight, selected ? V4Ui.Ink : V4Ui.TextMain, UiAnchor.TL, x, 214, 66, 30, () => ConnectDisplay(name));
                x += 70f;
            }
            if (ports.Count == 0) V4Ui.Label(bank, "Nenhuma porta USB", 13, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, 160, 214, 136, 30);
            V4Ui.Btn(bank, "Sem display", 13, _serialPort == null ? V4Ui.Teal : V4Ui.PanelLight, V4Ui.TextMuted, UiAnchor.TR, 8, 214, 84, 30, () => ConnectDisplay(null));
#endif
        }

        private void ConnectDisplay(string port, bool quiet = false)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (!quiet && _detect != null) { StopCoroutine(_detect); _detect = null; _detectingPort = null; }
            _serial?.Dispose();
            _serial = null;
            _serialPort = port;
            if (port == null) { _session.Coffer.SetTransport(null); if (!quiet) Toast("Display do cofre desconectado."); }
            else
            {
                var transport = new MoneyRank.Infrastructure.V4.WindowsSerialCofferTransport(port);
                _serial = transport;
                _session.Coffer.SetTransport(transport);
                if (!quiet) Toast("Conectando ao display em " + port + "… o status mostra quando ele confirmar o saldo.");
            }
            Refresh();
#endif
        }

        /// <summary>Tries every USB/serial port until the coffer sketch answers (PRONTO/ACK), so the master never needs to know the COM number.</summary>
        private void StartDisplayDetection()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_detect != null) return;
            _detect = StartCoroutine(DetectDisplay());
#endif
        }

        private System.Collections.IEnumerator DetectDisplay()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var candidates = MoneyRank.Infrastructure.V4.WindowsSerialCofferTransport.ListCandidatePorts();
            foreach (var port in candidates)
            {
                _detectingPort = port;
                ConnectDisplay(port, quiet: true);
                var until = Time.realtimeSinceStartup + 4.5f;      // the Uno resets when the port opens (~2 s of bootloader)
                while (Time.realtimeSinceStartup < until && !_session.Coffer.DisplayAnswered) yield return null;
                if (_session.Coffer.DisplayAnswered)
                {
                    _detectingPort = null; _detect = null;
                    Toast("Display do cofre encontrado na " + port + ".");
                    Refresh();
                    yield break;
                }
            }
            _detectingPort = null; _detect = null;
            ConnectDisplay(null, quiet: true);
            Toast(candidates.Count == 0
                ? "Nenhuma porta USB encontrada: confira o cabo do Arduino."
                : "O display não respondeu em " + string.Join(", ", candidates) + ". Feche o Serial Monitor/Arduino IDE e confira se o sketch do cofre está gravado.");
            Refresh();
#else
            yield break;
#endif
        }

        /// <summary>The link changes without engine events (ACKs arrive on the serial thread): repaint the side column when it does.</summary>
        private void RefreshDisplayLinkIfChanged()
        {
            if (_session == null || _sideRoot == null || Time.unscaledTime < _nextLinkCheck) return;
            _nextLinkCheck = Time.unscaledTime + 0.4f;
            var c = _session.Coffer;
            var key = _serialPort + "|" + _detectingPort + "|" + c.Status + "|" + c.DisplayAnswered + "|" + c.IsConfirmed + "|" + c.LastAckedBalance + "|" + (c.TicksWithoutReply > NoReplyTicks);
            if (key == _lastLinkKey) return;
            _lastLinkKey = key;
            BuildSide();
        }

        /// <summary>At match start, connect by itself when there is exactly one USB/serial port (the usual single-Arduino table).</summary>
        private void AutoDetectDisplayOnStart()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            if (_serialPort == null && _detect == null && MoneyRank.Infrastructure.V4.WindowsSerialCofferTransport.ListCandidatePorts().Count == 1)
                StartDisplayDetection();
#endif
        }

        private void OnDestroy() { _serial?.Dispose(); }

        // ------------------------------------------------------------------ dice, coin and result banner

        /// <summary>Source of virtual dice (tests inject fixed values; default is random).</summary>
        public Func<(int, int)> DiceSource = () => (UnityEngine.Random.Range(1, 7), UnityEngine.Random.Range(1, 7));
        public Func<bool> CoinSource = () => UnityEngine.Random.value < 0.5f;
        public Action<int, int, Action> AnimateDice;
        public Action<bool, Action> AnimateCoin;

        private bool _busy;
        private int _physA, _physB;
        private Text _banner;
        private float _bannerUntil;

        public bool Busy => _busy;

        private void ShowBanner(string text, float seconds = 2.6f)
        {
            if (_banner == null) return;
            _banner.text = text;
            _bannerUntil = Time.unscaledTime + seconds;
            _banner.gameObject.SetActive(true);
        }

        private void Update()
        {
            if (_banner != null && _banner.gameObject.activeSelf && Time.unscaledTime > _bannerUntil) _banner.gameObject.SetActive(false);
            RefreshDisplayLinkIfChanged();
        }

        /// <summary>Virtual roll: the dice are thrown on the board and the result is applied when they settle.</summary>
        public void RollVirtual()
        {
            if (_busy || _session == null || _session.S.Phase != V4Phase.AwaitRoll) return;
            var dice = DiceSource();
            var cur = Current;
            _busy = true;
            Toast("Lançando os dados…");
            Refresh();
            var a = dice.Item1; var b = dice.Item2;
            Action apply = () => { _busy = false; ExecRoll(cur.Id, a, b); };
            if (AnimateDice != null) AnimateDice(a, b, apply); else apply();
        }

        private void ExecRoll(string playerId, int a, int b)
        {
            var r = _session.Exec(e => e.RegisterRoll(V4Session.NewCommandId(), playerId, a, b));
            if (!r.Ok) { Toast(r.Error); return; }
            Toast(string.Empty);                       // drops the "rolling the dice" notice once the result is in
            var extra = _session.S.RollAgain;
            ShowBanner(a + " + " + b + " = " + (a + b) + (a == b ? "   DADOS IGUAIS!" : string.Empty) + (extra ? "\nJoga de novo depois desta casa (extra " + _session.S.ExtraRollsUsed + "/" + _session.Engine.Rules.DoublesExtraRollsMax + ")" : (a == b ? "\nLimite de jogadas extras atingido: a vez passa." : string.Empty)));
        }

        private void RollPhysical()
        {
            if (_busy || _physA < 1 || _physB < 1) return;
            var cur = Current;
            var a = _physA; var b = _physB;
            _physA = 0; _physB = 0;
            ExecRoll(cur.Id, a, b);
        }

        /// <summary>Virtual coin: flips on the board, then registers FELIZ/TRISTE for the pending fiscal decision.</summary>
        private void FlipVirtualCoin(PendingDecision d)
        {
            if (_busy) return;
            var happy = CoinSource();
            _busy = true;
            Refresh();   // hides the modal so the coin is visible
            Action apply = () =>
            {
                _busy = false;
                ShowBanner(happy ? "MOEDA: FELIZ" : "MOEDA: TRISTE", 2.2f);
                Run(e => e.RegisterCoin(V4Session.NewCommandId(), d.Id, d.PlayerId, happy));
            };
            if (AnimateCoin != null) AnimateCoin(happy, apply); else apply();
        }

        // ------------------------------------------------------------------ control bar

        private void BuildControl()
        {
            V4Ui.Clear(_controlRoot);
            var s = _session.S;
            var cur = Current;
            if (cur == null || s.Phase == V4Phase.GameEnd) return;
            V4Ui.Box(_controlRoot, "Bg", V4Ui.Panel, UiAnchor.Stretch, 0, 0, 0, 0);
            var prof = _session.Content.Profession(cur.ProfessionId);
            if (s.Phase == V4Phase.AwaitRoll)
            {
                var extra = s.ExtraRollsUsed > 0 ? "  ·  jogada extra " + s.ExtraRollsUsed + "/" + _session.Engine.Rules.DoublesExtraRollsMax + " (dados iguais)" : string.Empty;
                V4Ui.Label(_controlRoot, cur.Name + ", sua vez de lançar os dois dados" + extra, 22, extra.Length > 0 ? V4Ui.Gold : V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, 6, 940, 34, FontStyle.Bold);
                var roll = V4Ui.Btn(_controlRoot, _busy ? "Lançando…" : "LANÇAR DADOS", 30, V4Ui.Gold, V4Ui.Ink, UiAnchor.TL, 20, 48, 300, 74, RollVirtual);
                roll.interactable = !_busy;

                V4Ui.Label(_controlRoot, "Ou registre os dados físicos:", 15, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, 346, 42, 260, 22);
                for (var d = 1; d <= 6; d++)
                {
                    var face = d;
                    V4Ui.Btn(_controlRoot, d.ToString(), 18, _physA == d ? V4Ui.Gold : V4Ui.PanelLight, _physA == d ? V4Ui.Ink : V4Ui.TextMain, UiAnchor.TL, 346 + (d - 1) * 40, 66, 36, 30, () => { _physA = face; BuildControl(); });
                    V4Ui.Btn(_controlRoot, d.ToString(), 18, _physB == d ? V4Ui.Gold : V4Ui.PanelLight, _physB == d ? V4Ui.Ink : V4Ui.TextMain, UiAnchor.TL, 346 + (d - 1) * 40, 100, 36, 30, () => { _physB = face; BuildControl(); });
                }
                var reg = V4Ui.Btn(_controlRoot, "Registrar", 18, V4Ui.Teal, V4Ui.TextMain, UiAnchor.TL, 596, 66, 110, 64, RollPhysical);
                reg.interactable = !_busy && _physA > 0 && _physB > 0;

                var y = 40f;
                if (prof.Id == V4Engine.AbilityMotorista && !cur.AbilityUsedThisYear)
                {
                    V4Ui.Btn(_controlRoot, "Pausa: −2 Cortisol", 15, V4Ui.Teal, V4Ui.TextMain, UiAnchor.TL, 730, y, 230, 28, () => Run(e => e.UseDriverFlex(V4Session.NewCommandId(), cur.Id, AttributeId.Cortisol))).interactable = !_busy;
                    y += 32f;
                    V4Ui.Btn(_controlRoot, "Pausa: −2 Toxina", 15, V4Ui.Teal, V4Ui.TextMain, UiAnchor.TL, 730, y, 230, 28, () => Run(e => e.UseDriverFlex(V4Session.NewCommandId(), cur.Id, AttributeId.Toxina))).interactable = !_busy;
                    y += 32f;
                }
                if (cur.ReserveBalance > 0)
                {
                    V4Ui.Btn(_controlRoot, "Resgatar reserva (" + V4Ui.Money(cur.ReserveBalance) + ")", 15, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TL, 730, y, 230, 28, () => Redeem(cur, cur.ReserveBalance));
                    y += 32f;
                }
                if (cur.CheckpointRights > 0)
                    V4Ui.Btn(_controlRoot, "Instalar checkpoint (" + cur.CheckpointRights + ")", 15, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TL, 730, y, 230, 28, () => Run(e => e.InstallCheckpoint(V4Session.NewCommandId(), cur.Id)));
                V4Ui.Label(_controlRoot, prof.Ability + ": " + prof.AbilityText, 14, V4Ui.TextMuted, TextAnchor.UpperLeft, UiAnchor.TL, 20, 132, 700, 36);
            }
            else if (s.Phase == V4Phase.AwaitDecision)
            {
                V4Ui.Label(_controlRoot, "Decisão pendente de " + s.Players.First(p => p.Id == s.Pending.PlayerId).Name + ".", 24, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, 20, 700, 40, FontStyle.Bold);
                V4Ui.Btn(_controlRoot, "Abrir decisão", 28, V4Ui.Gold, V4Ui.Ink, UiAnchor.TL, 20, 80, 300, 64, () => { _modalDismissed = false; Refresh(); });
            }
            else
            {
                V4Ui.Label(_controlRoot, "Fechamento do ano em andamento…", 24, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, 20, 700, 40, FontStyle.Bold);
            }
        }

        private void Redeem(PlayerState p, int amount)
        {
            var value = Mathf.Min(amount, p.ReserveBalance);
            var r = _session.Exec(e => e.RedeemReserve(V4Session.NewCommandId(), p.Id, value));
            Toast(r.Ok ? "Resgate de " + V4Ui.Money(value) + "." : r.Error);
        }

        private void Run(Func<V4Engine, V4Result> command)
        {
            var r = _session.Exec(command);
            Toast(r.Ok ? string.Empty : r.Error);
        }
    }
}

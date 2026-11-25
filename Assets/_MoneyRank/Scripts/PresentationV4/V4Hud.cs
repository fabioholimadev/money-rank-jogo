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
        private enum SidePanel { None, Fiscal, History }

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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
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
            V4Ui.Box(_setupRoot, "Bg", new Color(0.04f, 0.08f, 0.12f, 0.97f), UiAnchor.Stretch, 0, 0, 0, 0);
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
            V4Ui.Clear(_matchRoot);
            BuildMatchFrame();
            Refresh();
        }

        private void BuildMatchFrame()
        {
            V4Ui.Box(_matchRoot, "TopBar", V4Ui.Panel, UiAnchor.TL, 0, 0, 1920, 72);
            V4Ui.Label(_matchRoot, "MONEY RANK", 34, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 24, 0, 300, 72, FontStyle.Bold);
            V4Ui.Btn(_matchRoot, "Menu", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 16, 14, 100, 44, () => BackToSetup?.Invoke());
            V4Ui.Btn(_matchRoot, "Histórico", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 124, 14, 120, 44, () => { _panel = _panel == SidePanel.History ? SidePanel.None : SidePanel.History; Refresh(); });
            V4Ui.Btn(_matchRoot, "Fiscal", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 252, 14, 100, 44, () => { _panel = _panel == SidePanel.Fiscal ? SidePanel.None : SidePanel.Fiscal; Refresh(); });
            V4Ui.Btn(_matchRoot, "Exportar", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 360, 14, 120, 44, DoExport);
            V4Ui.Btn(_matchRoot, "Desfazer", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 488, 14, 120, 44, DoUndo);
            V4Ui.Btn(_matchRoot, "Salvar", 20, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TR, 616, 14, 100, 44, () => { _session.Persist(); Toast(_session.LastSaveError == null ? "Partida salva." : "Falha ao salvar: " + _session.LastSaveError); });
            _cardsRoot = V4Ui.Rect(_matchRoot, "Cards", UiAnchor.TL, 12, 84, 392, 980);
            _sideRoot = V4Ui.Rect(_matchRoot, "Side", UiAnchor.TR, 12, 84, 392, 980);
            _controlRoot = V4Ui.Rect(_matchRoot, "Control", UiAnchor.BC, 0, 14, 980, 190);
            _toast = V4Ui.Label(_matchRoot, string.Empty, 22, V4Ui.Gold, TextAnchor.MiddleCenter, UiAnchor.BC, 0, 212, 1100, 44, FontStyle.Bold);
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
            var root = V4Ui.Rect(_matchRoot, "TopInfo", UiAnchor.TL, 360, 0, 880, 72);
            _topInfo = root;
            var diff = _session.Engine.Difficulty;
            var cur = Current;
            var turn = cur == null ? "—" : cur.Name + " · " + _session.Content.Profession(cur.ProfessionId).Name;
            V4Ui.Label(root, "Rodada " + s.RoundIndex + " · Ano " + s.YearIndex + " (fecha em " + (_session.Engine.Rules.RoundsPerYear - (s.CompletedRounds % _session.Engine.Rules.RoundsPerYear)) + " rodada[s])", 22, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 0, 6, 880, 30);
            V4Ui.Label(root, "VEZ: " + turn + "   ·   " + diff.Difficulty.ToUpperInvariant() + " (bateria " + diff.Battery + ")", 22, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 0, 36, 880, 30, FontStyle.Bold);
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
                V4Ui.Box(card, "Bg", isTurn ? new Color(0.16f, 0.27f, 0.36f, 0.97f) : V4Ui.Panel, UiAnchor.Stretch, 0, 0, 0, 0);
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
                    V4Ui.Label(card, AttrNames[a], 13, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, bx, by, 90, rowH);
                    V4Ui.Box(card, "BarBg", new Color(0.04f, 0.08f, 0.12f, 1f), UiAnchor.TL, bx + 92, by + 5, 62, rowH - 10);
                    V4Ui.Box(card, "BarFill", positive ? V4Ui.Good : V4Ui.Bad, UiAnchor.TL, bx + 92, by + 5, 62f * Mathf.Clamp01((float)bar.Progress / 3f), rowH - 10);
                    V4Ui.Label(card, V4Ui.Dec(bar.Progress) + (bar.LifetimeCycles > 0 ? " ×" + bar.LifetimeCycles : string.Empty), 12, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, bx + 156, by, 40, rowH);
                }
                var fy = barTop + 3 * rowH + 4;
                for (var k = 0; k < p.Pawns.Length; k++)
                {
                    var pw = p.Pawns[k];
                    var fill = pw.Completed ? V4Ui.Gold : (pw.RouteIndex >= 0 ? V4Assets.PawnColor(p.Color) : new Color(0.04f, 0.08f, 0.12f, 1f));
                    V4Ui.Box(card, "Pawn" + k, fill, UiAnchor.TL, 20 + k * 28, fy + 2, 22, 22);
                    if (k == p.ActivePawn && !pw.Completed) V4Ui.Box(card, "Active", V4Ui.TextMain, UiAnchor.TL, 20 + k * 28, fy + 24, 22, 3);
                }
                var active = p.ActivePawn >= 0 ? p.Pawns[p.ActivePawn] : null;
                V4Ui.Label(card, "Ludo: peão " + (p.ActivePawn >= 0 ? (p.ActivePawn + 1).ToString() : "—") + "/4 · casa " + (active == null ? "—" : (active.RouteIndex < 0 ? "base" : active.RouteIndex + "/56")), 14, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, 140, fy, 245, 26);
                var asset = "Imóveis " + p.Properties + " · Checkpoints " + p.CheckpointRights + " · " + (_session.Engine.VehicleActive(p) ? "carro até rodada " + p.VehicleExpiresAfterRound : "sem carro");
                V4Ui.Label(card, asset, 14, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, fy + 30, 365, 22);
                var status = p.Eliminated ? "ELIMINADO(A)" : (p.HospitalTurnsRemaining > 0 ? "HOSPITAL: perde " + p.HospitalTurnsRemaining + " turno(s)" : (p.AbilityUsedThisYear ? "Habilidade do ano: usada" : "Habilidade do ano: disponível"));
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
            var link = _session.Coffer.Status == CofferLinkStatus.Connected ? "Display do cofre: conectado" : (_session.Coffer.Status == CofferLinkStatus.Reconnecting ? "Display do cofre: reconectando…" : "Display do cofre: desconectado (saldo na tela)");
            V4Ui.Label(bank, link, 14, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, 16, 184, 360, 22);
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

        private void BuildDisplayPicker(Transform bank)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var ports = MoneyRank.Infrastructure.V4.WindowsSerialCofferTransport.ListPorts();
            var x = 16f;
            foreach (var port in ports.Take(4))
            {
                var name = port;
                V4Ui.Btn(bank, name, 14, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TL, x, 212, 70, 30, () => ConnectDisplay(name));
                x += 76f;
            }
            if (ports.Count == 0) V4Ui.Label(bank, "Nenhuma porta COM encontrada.", 13, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, 16, 212, 250, 30);
            V4Ui.Btn(bank, "Sem display", 14, V4Ui.PanelLight, V4Ui.TextMuted, UiAnchor.TR, 14, 212, 90, 30, () => ConnectDisplay(null));
#endif
        }

        private void ConnectDisplay(string port)
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            _serial?.Dispose();
            _serial = null;
            if (port == null) { _session.Coffer.SetTransport(null); Toast("Display do cofre desconectado."); }
            else
            {
                var transport = new MoneyRank.Infrastructure.V4.WindowsSerialCofferTransport(port);
                _serial = transport;
                _session.Coffer.SetTransport(transport);
                Toast("Conectando ao display em " + port + "… (saldo absoluto será reenviado)");
            }
            Refresh();
#endif
        }

        private void OnDestroy() { _serial?.Dispose(); }

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
                V4Ui.Label(_controlRoot, cur.Name + ", registre o dado físico (1–6):", 24, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, 8, 700, 36, FontStyle.Bold);
                for (var d = 1; d <= 6; d++)
                {
                    var die = d;
                    V4Ui.Btn(_controlRoot, d.ToString(), 36, V4Ui.Gold, V4Ui.Ink, UiAnchor.TL, 20 + (d - 1) * 74, 50, 66, 66, () => Roll(die));
                }
                var x = 500f;
                if (prof.Id == V4Engine.AbilityMotorista && !cur.AbilityUsedThisYear)
                {
                    V4Ui.Btn(_controlRoot, "Pausa: −2 Cortisol", 18, V4Ui.Teal, V4Ui.TextMain, UiAnchor.TL, x, 50, 220, 30, () => Run(e => e.UseDriverFlex(V4Session.NewCommandId(), cur.Id, AttributeId.Cortisol)));
                    V4Ui.Btn(_controlRoot, "Pausa: −2 Toxina", 18, V4Ui.Teal, V4Ui.TextMain, UiAnchor.TL, x, 86, 220, 30, () => Run(e => e.UseDriverFlex(V4Session.NewCommandId(), cur.Id, AttributeId.Toxina)));
                    x += 232;
                }
                if (cur.ReserveBalance > 0)
                {
                    V4Ui.Btn(_controlRoot, "Resgatar 50", 18, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TL, x, 50, 130, 30, () => Redeem(cur, 50));
                    V4Ui.Btn(_controlRoot, "Resgatar tudo", 18, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TL, x, 86, 130, 30, () => Redeem(cur, cur.ReserveBalance));
                    x += 142;
                }
                if (cur.CheckpointRights > 0)
                    V4Ui.Btn(_controlRoot, "Instalar checkpoint (" + cur.CheckpointRights + ")", 18, V4Ui.PanelLight, V4Ui.TextMain, UiAnchor.TL, x, 50, 240, 30, () => Run(e => e.InstallCheckpoint(V4Session.NewCommandId(), cur.Id)));
                V4Ui.Label(_controlRoot, prof.Ability + ": " + prof.AbilityText, 15, V4Ui.TextMuted, TextAnchor.UpperLeft, UiAnchor.TL, 20, 128, 940, 54);
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

        private void Roll(int die)
        {
            var cur = Current;
            var r = _session.Exec(e => e.RegisterRoll(V4Session.NewCommandId(), cur.Id, die));
            Toast(r.Ok ? string.Empty : r.Error);
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

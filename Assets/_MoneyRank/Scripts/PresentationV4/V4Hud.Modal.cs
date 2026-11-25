using System;
using System.Collections.Generic;
using System.Linq;
using MoneyRank.Domain.V4;
using UnityEngine;
using UnityEngine.UI;

namespace MoneyRank.V4.Presentation
{
    public sealed partial class V4Hud
    {
        private sealed class CardData
        {
            public string Id, Title, Text, Reason;
            public bool Blocked;
            public List<(string text, Color color)> Lines = new List<(string, Color)>();
        }

        private static string CategoryLabel(string cat)
        {
            switch (cat)
            {
                case "essencial": return "Despesa essencial (obrigatória)";
                case "nao_essencial": return "Despesa não essencial";
                case "oportunidade": return "Oportunidade";
                case "divida": return "Dívidas";
                case "lazer": return "Lazer e bem-estar";
                case "investimento": return "Investimento e patrimônio";
                case "imposto": return "Tributo";
                case "progressao": return "Progressão de vida";
                default: return "Casa especial";
            }
        }

        // ------------------------------------------------------------------ modal

        private void BuildModal()
        {
            V4Ui.Clear(_modalRoot);
            var s = _session.S;
            var d = s.Pending;
            if (d == null || _modalDismissed || _busy || s.Phase == V4Phase.GameEnd) return;
            var player = s.Players.First(p => p.Id == d.PlayerId);
            var isIr = d.HouseId == "SYS_IR";
            var house = isIr ? null : _session.Content.House(d.HouseId);

            V4Ui.Box(_modalRoot, "Dim", V4Ui.Dim, UiAnchor.Stretch, 0, 0, 0, 0).raycastTarget = true;
            var win = V4Ui.Rect(_modalRoot, "Window", UiAnchor.C, 0, -10, 1560, 900);
            V4Ui.Box(win, "Bg", new Color(0.05f, 0.05f, 0.055f, 0.99f), UiAnchor.Stretch, 0, 0, 0, 0);
            var band = isIr ? V4Ui.Teal : V4Assets.HouseColor(house.Id);
            V4Ui.Box(win, "Band", band, UiAnchor.TL, 0, 0, 1560, 104);
            if (house != null)
            {
                var sprite = V4Assets.IconSprite(house.Id);
                if (sprite != null) { var img = V4Ui.Box(win, "Icon", Color.white, UiAnchor.TL, 16, 12, 80, 80); img.sprite = sprite; img.preserveAspect = true; }
            }
            var title = isIr ? "Imposto de renda anual" : house.Id + " — " + house.Title;
            var dark = new Color(0.05f, 0.08f, 0.12f);
            V4Ui.Label(win, title, 40, dark, TextAnchor.MiddleLeft, UiAnchor.TL, 112, 8, 1100, 52, FontStyle.Bold);
            V4Ui.Label(win, (isIr ? "Ano " + s.YearIndex + " · alíquota " + _session.Engine.Difficulty.IncomeTaxPct + "% do salário" : CategoryLabel(house.Category)) + "  ·  decide: " + player.Name + " (" + _session.Content.Profession(player.ProfessionId).Name + ")",
                22, dark, TextAnchor.MiddleLeft, UiAnchor.TL, 112, 58, 1250, 36, FontStyle.Bold);
            V4Ui.Btn(win, "Voltar ao tabuleiro", 18, new Color(0f, 0f, 0f, 0.35f), V4Ui.TextMain, UiAnchor.TR, 16, 30, 230, 44, () => { _modalDismissed = true; Refresh(); });

            switch (d.Kind)
            {
                case DecisionKind.Choice: BuildChoiceBody(win, d, player); break;
                case DecisionKind.TaxCoin:
                case DecisionKind.FiscalCoin: BuildCoinBody(win, d, player); break;
                case DecisionKind.HospitalModality: BuildHospitalBody(win, d, player); break;
                case DecisionKind.AbilityChoice: BuildAbilityBody(win, d, player); break;
            }
        }

        private void BuildChoiceBody(RectTransform win, PendingDecision d, PlayerState player)
        {
            var cards = new List<CardData>();
            foreach (var id in d.OptionIds) cards.Add(MakeCard(d, player, id, false, null));
            for (var i = 0; i < d.BlockedOptionIds.Count; i++) cards.Add(MakeCard(d, player, d.BlockedOptionIds[i], true, d.BlockedReasons[i]));

            var n = cards.Count;
            var w = Mathf.Min(340f, (1500f - (n - 1) * 16f) / n);
            var total = n * w + (n - 1) * 16f;
            var x0 = (1560f - total) / 2f;
            var tax = !string.IsNullOrEmpty(d.Tag) && d.Tag.StartsWith("tax:", StringComparison.Ordinal);
            if (tax) V4Ui.Label(win, "Tributo devido: " + V4Ui.Money(d.Amount) + (d.Tag == "tax:IR" ? " (sobre o salário bruto anual)" : string.Empty), 26, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 24, 114, 900, 40, FontStyle.Bold);
            if (d.Mandatory && !tax) V4Ui.Label(win, "Despesa essencial: escolha uma das opções acessíveis.", 22, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 24, 114, 900, 40, FontStyle.Bold);
            if (d.Delegated) V4Ui.Label(win, "Investimento livre: apenas UMA operação por visita (ou recusar).", 22, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 24, 114, 900, 40, FontStyle.Bold);
            if (_swapMode) V4Ui.Label(win, "Visão de Negócios: escolha a opção substituta (seleção atual: " + _swapFrom + ").", 22, V4Ui.Good, TextAnchor.MiddleLeft, UiAnchor.TL, 24, 114, 1200, 40, FontStyle.Bold);

            for (var i = 0; i < n; i++)
            {
                var c = cards[i];
                var id = c.Id;
                var selected = _selectedOption == id && !_swapMode;
                var card = V4Ui.Rect(win, "Card " + id, UiAnchor.TL, x0 + i * (w + 16f), 164, w, 456);
                if (selected) V4Ui.Box(card, "Sel", V4Ui.Gold, UiAnchor.TL, -5, -5, w + 10, 466);
                V4Ui.Box(card, "Bg", c.Blocked ? new Color(0.16f, 0.19f, 0.23f, 1f) : V4Ui.Paper, UiAnchor.Stretch, 0, 0, 0, 0);
                V4Ui.Box(card, "Top", c.Blocked ? new Color(0.3f, 0.32f, 0.36f) : V4Ui.Teal, UiAnchor.TL, 0, 0, w, 40);
                V4Ui.Label(card, "Opção " + (i + 1) + (c.Blocked ? " · indisponível" : string.Empty), 17, Color.white, TextAnchor.MiddleLeft, UiAnchor.TL, 12, 0, w - 24, 40, FontStyle.Bold);
                var iconSprite = d.HouseId != null && d.HouseId != "SYS_IR" ? V4Assets.IconSprite(d.HouseId) : null;
                if (iconSprite != null) { var icon = V4Ui.Box(card, "Icon", c.Blocked ? new Color(1f, 1f, 1f, 0.45f) : Color.white, UiAnchor.TR, 6, 4, 32, 32); icon.sprite = iconSprite; icon.preserveAspect = true; }
                var ink = c.Blocked ? V4Ui.TextMuted : new Color(0.07f, 0.11f, 0.15f);
                V4Ui.Label(card, c.Title, 25, ink, TextAnchor.UpperLeft, UiAnchor.TL, 14, 50, w - 28, 66, FontStyle.Bold);
                V4Ui.Label(card, c.Text, 16, c.Blocked ? V4Ui.TextMuted : new Color(0.25f, 0.31f, 0.38f), TextAnchor.UpperLeft, UiAnchor.TL, 14, 118, w - 28, 120);
                var ly = 246f;
                foreach (var line in c.Lines)
                {
                    V4Ui.Label(card, line.text, 19, c.Blocked ? V4Ui.TextMuted : line.color, TextAnchor.MiddleLeft, UiAnchor.TL, 14, ly, w - 28, 28, FontStyle.Bold);
                    ly += 28f;
                    if (ly > 410) break;
                }
                if (c.Blocked) V4Ui.Label(card, c.Reason, 16, V4Ui.Bad, TextAnchor.LowerLeft, UiAnchor.TL, 14, 408, w - 28, 42);
                else
                {
                    var capture = id;
                    var btn = V4Ui.Btn(card, selected ? "Selecionada" : (_swapMode ? "Trocar por esta" : "Selecionar"), 20, selected ? V4Ui.Gold : V4Ui.Teal, selected ? V4Ui.Ink : Color.white,
                        UiAnchor.BL, 14, 12, w - 28, 40, () => OnCardClicked(capture));
                    btn.interactable = !(_swapMode && capture == _swapFrom);
                }
            }

            // preview + actions
            var pv = V4Ui.Rect(win, "Preview", UiAnchor.TL, 24, 636, 1100, 250);
            V4Ui.Box(pv, "Bg", V4Ui.Panel, UiAnchor.Stretch, 0, 0, 0, 0);
            V4Ui.Label(pv, "PRÉVIA (nada é aplicado até confirmar)", 16, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 14, 4, 700, 26, FontStyle.Bold);
            var lines = _selectedOption == null ? new List<string> { "Selecione uma opção para ver o efeito calculado (carteira, banco, cofre, barras, ciclos e peão)." } : PreviewLines(d, _selectedOption);
            var yy = 34f;
            foreach (var l in lines.Take(8)) { V4Ui.Label(pv, l, 17, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 14, yy, 1070, 26); yy += 26f; }

            var confirm = V4Ui.Btn(win, "CONFIRMAR", 34, V4Ui.Gold, V4Ui.Ink, UiAnchor.TR, 24, 650, 380, 84, ConfirmChoice);
            confirm.interactable = _selectedOption != null && !_swapMode;
            var empreend = player.ProfessionId == V4Engine.AbilityEmpreendedora && !player.AbilityUsedThisYear && !tax && _selectedOption != null;
            if (empreend)
            {
                V4Ui.Btn(win, _swapMode ? "Cancelar troca" : "Visão de Negócios: trocar opção", 20, V4Ui.Teal, V4Ui.TextMain, UiAnchor.TR, 24, 750, 380, 48, () =>
                {
                    _swapMode = !_swapMode; _swapFrom = _selectedOption; Refresh();
                });
            }
        }

        private void OnCardClicked(string id)
        {
            var d = _session.S.Pending;
            if (_swapMode)
            {
                var from = _swapFrom;
                var r = _session.Exec(e => e.SwapWithEntrepreneur(V4Session.NewCommandId(), d.Id, d.PlayerId, from, id));
                _swapMode = false;
                Toast(r.Ok ? string.Empty : r.Error);
                return;
            }
            _selectedOption = id;
            Refresh();
        }

        private void ConfirmChoice()
        {
            var d = _session.S.Pending;
            if (d == null || _selectedOption == null) return;
            var opt = _selectedOption;
            var r = _session.Exec(e => e.ChooseOption(V4Session.NewCommandId(), d.Id, d.PlayerId, opt));
            Toast(r.Ok ? string.Empty : r.Error);
        }

        private CardData MakeCard(PendingDecision d, PlayerState player, string id, bool blocked, string reason)
        {
            var card = new CardData { Id = id, Blocked = blocked, Reason = reason };
            if (id.StartsWith("IR:", StringComparison.Ordinal))
            {
                card.Title = id == "IR:pay" ? "Pagar o imposto" : (id == "IR:evade" ? "Tentar sonegar" : "Sem recursos para pagar");
                card.Text = id == "IR:pay" ? "Transfere o tributo ao cofre público (+1 Cidadania base)." :
                    (id == "IR:evade" ? "Lance a moeda: FELIZ registra pendência fiscal; TRISTE cobra o dobro e +1 Cortisol." : "Registra pendência e +3 Dívidas base.");
                card.Lines.Add(("Tributo: " + V4Ui.Money(d.Amount), V4Ui.Gold));
                return card;
            }
            var o = _session.Content.Options.First(x => x.Id == id);
            card.Title = o.Title;
            card.Text = o.Text;
            var cost = _session.Engine.CostOf(player, o);
            var reward = _session.Engine.RewardOf(player, o);
            var tax = !string.IsNullOrEmpty(d.Tag) && d.Tag.StartsWith("tax:", StringComparison.Ordinal);
            if (tax && (o.Action == "pagar_imposto" || o.Action == "sonegar_imposto" || o.Action == "imposto_sem_recursos"))
                card.Lines.Add(("Tributo: " + V4Ui.Money(d.Amount), V4Ui.Gold));
            if (cost > 0) card.Lines.Add(("Custo: " + V4Ui.Money(cost), new Color(0.75f, 0.2f, 0.2f)));
            if (reward > 0) card.Lines.Add(("Recebe: " + V4Ui.Money(reward), new Color(0.1f, 0.5f, 0.25f)));
            if (o.Action == "parceria") card.Lines.Add(("Retorno no fechamento: " + V4Ui.Money(o.ParamInt("retorno_total_umr")), new Color(0.1f, 0.5f, 0.25f)));
            if (o.Action == "progressao") { var pc = _session.Content.ProgressionOf(player.ProfessionId); if (player.Level < pc.Count) { var c = pc[player.Level]; card.Lines.Add(("Salário → " + V4Ui.Money(c.SalaryAfter), new Color(0.1f, 0.5f, 0.25f))); card.Title = c.Title; card.Text = c.Text; card.Lines.Add((string.Format("+{0} Conhecimento  +{1} Investimento  +{2} Cidadania (base)", c.Conhecimento, c.Investimento, c.Cidadania), new Color(0.1f, 0.5f, 0.25f))); } }
            for (var i = 0; i < AttrNames.Length; i++)
            {
                var v = o.Deltas[i];
                if (v == 0) continue;
                var positiveAttr = i <= 2;
                var good = positiveAttr ? v > 0 : v < 0;
                card.Lines.Add(((v > 0 ? "+" : string.Empty) + v + " " + AttrNames[i] + " (base)", good ? new Color(0.1f, 0.5f, 0.25f) : new Color(0.75f, 0.2f, 0.2f)));
            }
            if (card.Lines.Count == 0) card.Lines.Add(("Sem custo nem efeito", V4Ui.TextMuted));
            return card;
        }

        private List<string> PreviewLines(PendingDecision d, string optionId)
        {
            var lines = new List<string>();
            var p = _session.S.Players.First(x => x.Id == d.PlayerId);
            var pv = _session.Engine.PreviewOption(d.Id, d.PlayerId, optionId);
            if (!pv.Ok) { lines.Add("Indisponível: " + pv.Error); return lines; }
            var money = new List<string>();
            if (pv.WalletDelta != 0) money.Add("Carteira " + Signed(pv.WalletDelta));
            if (pv.ReserveDelta != 0) money.Add("Reserva " + Signed(pv.ReserveDelta));
            if (pv.BankDelta != 0) money.Add("Banco " + Signed(pv.BankDelta));
            if (pv.FundDelta != 0) money.Add("Cofre " + Signed(pv.FundDelta));
            lines.Add(money.Count == 0 ? "Dinheiro: sem movimentação." : "Dinheiro: " + string.Join(" · ", money));
            for (var i = 0; i < AttrNames.Length; i++)
            {
                var before = p.Attributes[i].Progress; var after = pv.AttributeProgressAfter[i];
                if (before == after && pv.CyclesGained[i] == 0) continue;
                lines.Add(AttrNames[i] + ": barra " + V4Ui.Dec(before) + " → " + V4Ui.Dec(after) + (pv.CyclesGained[i] > 0 ? "  (+" + pv.CyclesGained[i] + " ciclo" + (pv.CyclesGained[i] > 1 ? "s" : string.Empty) + (i <= 2 ? ": avança" : ": recua") + ")" : string.Empty));
            }
            var cur = p.ActivePawn >= 0 ? p.Pawns[p.ActivePawn].RouteIndex : -1;
            if (cur != pv.PawnIndexAfter) lines.Add("Peão ativo: casa interna " + (cur < 0 ? "base" : cur.ToString()) + " → " + (pv.PawnIndexAfter < 0 ? "base" : pv.PawnIndexAfter.ToString()));
            if (pv.HospitalWillTrigger) lines.Add("ATENÇÃO: isto causa internação hospitalar.");
            return lines;
        }

        private static string Signed(int v) => (v > 0 ? "+" : "−") + Math.Abs(v).ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + " UMR";

        // ------------------------------------------------------------------ other decision kinds

        private void BuildCoinBody(RectTransform win, PendingDecision d, PlayerState player)
        {
            var isAudit = d.Kind == DecisionKind.FiscalCoin;
            V4Ui.Label(win, isAudit ? "FISCALIZAÇÃO" : "SONEGAÇÃO", 52, V4Ui.Gold, TextAnchor.MiddleCenter, UiAnchor.TL, 0, 150, 1560, 70, FontStyle.Bold);
            var text = isAudit
                ? "Há pendências fiscais anteriores. Lance a moeda de duas faces.\nFELIZ: as pendências são mantidas.\nTRISTE: paga o dobro dos principais ainda não autuados (ou o valor já autuado), +1 Cortisol base, e regulariza os registros pagos."
                : "Tributo de " + V4Ui.Money(d.Amount) + " (" + d.Tag + ").\nFELIZ: não paga agora e registra a pendência fiscal.\nTRISTE: paga o dobro (" + V4Ui.Money(d.Amount * 2) + ") e recebe +1 Cortisol base.";
            V4Ui.Label(win, text, 28, V4Ui.TextMain, TextAnchor.UpperCenter, UiAnchor.TL, 160, 250, 1240, 220);
            V4Ui.Label(win, "Lance a moeda animada ou registre o resultado da moeda física:", 24, V4Ui.TextMuted, TextAnchor.MiddleCenter, UiAnchor.TL, 0, 480, 1560, 36);
            V4Ui.Btn(win, "LANÇAR MOEDA", 40, V4Ui.Gold, V4Ui.Ink, UiAnchor.TL, 480, 520, 600, 90, () => FlipVirtualCoin(d));
            V4Ui.Label(win, "ou registre a moeda física:", 20, V4Ui.TextMuted, TextAnchor.MiddleCenter, UiAnchor.TL, 0, 626, 1560, 30);
            V4Ui.Btn(win, "FELIZ", 48, V4Ui.Good, V4Ui.Ink, UiAnchor.TL, 360, 660, 360, 110, () => Run(e => e.RegisterCoin(V4Session.NewCommandId(), d.Id, d.PlayerId, true)));
            V4Ui.Btn(win, "TRISTE", 48, V4Ui.Bad, V4Ui.Ink, UiAnchor.TL, 840, 660, 360, 110, () => Run(e => e.RegisterCoin(V4Session.NewCommandId(), d.Id, d.PlayerId, false)));
        }

        private void BuildHospitalBody(RectTransform win, PendingDecision d, PlayerState player)
        {
            var diff = _session.Engine.Difficulty;
            V4Ui.Label(win, "Internação: escolha a modalidade de tratamento", 34, V4Ui.TextMain, TextAnchor.MiddleCenter, UiAnchor.TL, 0, 130, 1560, 56, FontStyle.Bold);
            var opts = new[]
            {
                ("public", "Tratamento público", "O cofre público paga " + V4Ui.Money(d.Amount) + " ao banco.\nO jogador perde " + diff.PublicHospitalTurns + " turnos.", true),
                ("private", "Tratamento particular", "Paga " + V4Ui.Money(d.Amount2) + " (carteira; reserva pode ser resgatada).\nO jogador perde " + (player.ProfessionId == V4Engine.AbilityMedica && !player.AbilityUsedThisYear ? "1 turno (Atendimento Especializado: metade do custo, mínimo de 1 turno)" : diff.PrivateHospitalTurns + " turno") + ".", true)
            };
            for (var i = 0; i < opts.Length; i++)
            {
                var id = opts[i].Item1;
                var available = d.OptionIds.Contains(id);
                var card = V4Ui.Rect(win, "Hosp " + id, UiAnchor.TL, 220 + i * 600, 220, 540, 480);
                V4Ui.Box(card, "Bg", available ? V4Ui.Paper : new Color(0.16f, 0.19f, 0.23f), UiAnchor.Stretch, 0, 0, 0, 0);
                V4Ui.Box(card, "Top", available ? V4Ui.Teal : new Color(0.3f, 0.32f, 0.36f), UiAnchor.TL, 0, 0, 540, 56);
                V4Ui.Label(card, opts[i].Item2, 28, Color.white, TextAnchor.MiddleLeft, UiAnchor.TL, 16, 0, 500, 56, FontStyle.Bold);
                V4Ui.Label(card, opts[i].Item3, 22, available ? new Color(0.15f, 0.2f, 0.27f) : V4Ui.TextMuted, TextAnchor.UpperLeft, UiAnchor.TL, 16, 76, 508, 250);
                if (!available) V4Ui.Label(card, "Indisponível agora (sem recursos suficientes).", 20, V4Ui.Bad, TextAnchor.MiddleLeft, UiAnchor.TL, 16, 330, 508, 60);
                var btn = V4Ui.Btn(card, "Escolher", 30, V4Ui.Gold, V4Ui.Ink, UiAnchor.BL, 16, 16, 508, 70, () => Run(e => e.ChooseHospital(V4Session.NewCommandId(), d.Id, d.PlayerId, id)));
                btn.interactable = available;
            }
        }

        private void BuildAbilityBody(RectTransform win, PendingDecision d, PlayerState player)
        {
            V4Ui.Label(win, "Fôlego Extra (Atleta)", 40, V4Ui.Gold, TextAnchor.MiddleCenter, UiAnchor.TL, 0, 130, 1560, 60, FontStyle.Bold);
            V4Ui.Label(win, "Uma barra positiva foi concluída. Use a habilidade anual: avance duas casas OU reduza 1 ponto-base de um atributo negativo. Recusar não consome a habilidade.", 24, V4Ui.TextMain, TextAnchor.UpperCenter, UiAnchor.TL, 200, 200, 1160, 120);
            var y = 340f;
            foreach (var id in d.OptionIds)
            {
                var opt = id;
                string label;
                if (id == "advance2") label = "Avançar duas casas neste ciclo";
                else if (id == "decline") label = "Recusar (avanço normal; mantém a habilidade)";
                else label = "Reduzir 1 ponto-base de " + char.ToUpperInvariant(id.Substring("reduce:".Length)[0]) + id.Substring("reduce:".Length + 1);
                V4Ui.Btn(win, label, 28, id == "decline" ? V4Ui.PanelLight : V4Ui.Teal, V4Ui.TextMain, UiAnchor.TL, 420, y, 720, 72, () => Run(e => e.ChooseAbility(V4Session.NewCommandId(), d.Id, opt)));
                y += 88f;
            }
        }

        // ------------------------------------------------------------------ side panels

        private void BuildPanel()
        {
            V4Ui.Clear(_panelRoot);
            if (_panel == SidePanel.None || _session.S.Pending != null && !_modalDismissed) return;
            var s = _session.S;
            var win = V4Ui.Rect(_panelRoot, "Win", UiAnchor.C, 0, 10, 1000, 820);
            V4Ui.Box(win, "Bg", new Color(0.05f, 0.05f, 0.055f, 0.99f), UiAnchor.Stretch, 0, 0, 0, 0);
            V4Ui.Box(win, "Band", V4Ui.Teal, UiAnchor.TL, 0, 0, 1000, 60);
            V4Ui.Label(win, _panel == SidePanel.Fiscal ? "Painel fiscal (pendências)" : "Histórico de eventos", 30, Color.white, TextAnchor.MiddleLeft, UiAnchor.TL, 20, 0, 700, 60, FontStyle.Bold);
            V4Ui.Btn(win, "Fechar", 20, new Color(0f, 0f, 0f, 0.35f), V4Ui.TextMain, UiAnchor.TR, 14, 8, 120, 44, () => { _panel = SidePanel.None; Refresh(); });
            var y = 72f;
            if (_panel == SidePanel.Fiscal)
            {
                V4Ui.Label(win, _session.Engine.IsFiscalPublic ? "Online: situação fiscal visível a todos os jogadores." : "Local: visível apenas ao mestre — não exiba no painel coletivo.", 18, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 20, y, 960, 30, FontStyle.Bold);
                y += 40f;
                foreach (var p in s.Players)
                {
                    var open = p.Fiscal.Where(f => f.Status != FiscalStatus.Regularizado).ToList();
                    V4Ui.Label(win, p.Name + " — " + (open.Count == 0 ? "sem pendências" : open.Count + " pendência(s), total devido " + V4Ui.Money(open.Sum(f => f.Status == FiscalStatus.Autuado ? f.Outstanding : f.Principal * _session.Engine.Rules.TaxEvasionPenaltyMultiplier)) + " se fiscalizado"), 20, open.Count == 0 ? V4Ui.TextMuted : V4Ui.Bad, TextAnchor.MiddleLeft, UiAnchor.TL, 20, y, 960, 30, FontStyle.Bold);
                    y += 32f;
                    foreach (var f in p.Fiscal)
                    {
                        V4Ui.Label(win, "   " + f.Kind + " (ano " + f.SourceYear + ") · principal " + V4Ui.Money(f.Principal) + " · devido " + V4Ui.Money(f.Outstanding) + " · " + f.Status, 17, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, y, 960, 26);
                        y += 26f;
                    }
                    foreach (var r in p.Receivables.Where(x => !x.Paid))
                    {
                        V4Ui.Label(win, "   Parceria a receber: " + V4Ui.Money(r.TotalReturn) + " no fechamento do ano " + r.DueYear, 17, V4Ui.Good, TextAnchor.MiddleLeft, UiAnchor.TL, 20, y, 960, 26);
                        y += 26f;
                    }
                    y += 8f;
                    if (y > 770) break;
                }
            }
            else
            {
                foreach (var h in s.History.Where(x => !string.IsNullOrEmpty(x.Detail)).Reverse().Take(26))
                {
                    V4Ui.Label(win, "R" + h.Round + "/A" + h.Year + "  " + h.Detail + (h.Undone ? "  (desfeito)" : string.Empty), 17, h.Undone ? V4Ui.TextMuted : V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, y, 960, 26);
                    y += 28f;
                }
            }
        }

        // ------------------------------------------------------------------ end screen

        private void BuildEnd()
        {
            V4Ui.Clear(_endRoot);
            var s = _session.S;
            if (s.Phase != V4Phase.GameEnd) return;
            V4Ui.Box(_endRoot, "Dim", V4Ui.Dim, UiAnchor.Stretch, 0, 0, 0, 0);
            var win = V4Ui.Rect(_endRoot, "Win", UiAnchor.C, 0, 0, 1100, 700);
            V4Ui.Box(win, "Bg", new Color(0.05f, 0.05f, 0.055f, 0.99f), UiAnchor.Stretch, 0, 0, 0, 0);
            V4Ui.Box(win, "Band", V4Ui.Gold, UiAnchor.TL, 0, 0, 1100, 110);
            var winner = s.WinnerId == null ? null : s.Players.First(p => p.Id == s.WinnerId);
            V4Ui.Label(win, winner == null ? "Partida encerrada sem vencedor" : "Vitória de " + winner.Name + "!", 56, V4Ui.Ink, TextAnchor.MiddleCenter, UiAnchor.TL, 0, 0, 1100, 110, FontStyle.Bold);
            V4Ui.Label(win, s.EndReason == "survival" ? "Vencedor(a) por sobrevivência (único participante não eliminado)." : (s.EndReason == "victory" ? "Concluiu os quatro peões do Ludo." : "Todos foram eliminados."), 24, V4Ui.TextMain, TextAnchor.MiddleCenter, UiAnchor.TL, 0, 124, 1100, 40);
            var y = 190f;
            foreach (var p in s.Players.OrderByDescending(x => x.Pawns.Count(k => k.Completed)).ThenByDescending(x => x.Wallet + x.ReserveBalance))
            {
                V4Ui.Label(win, p.Name + " (" + _session.Content.Profession(p.ProfessionId).Name + ") — peões " + p.Pawns.Count(k => k.Completed) + "/4 · carteira " + V4Ui.Money(p.Wallet) + " · reserva " + V4Ui.Money(p.ReserveBalance) + " · imóveis " + p.Properties + (p.Eliminated ? " · ELIMINADO" : string.Empty), 22, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 40, y, 1020, 36);
                y += 40f;
            }
            V4Ui.Label(win, "Rodadas completas: " + s.CompletedRounds + " · Ano " + s.YearIndex + " · Sessão " + s.SessionId, 18, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, 40, y + 20, 1020, 30);
            V4Ui.Btn(win, "Exportar dados", 28, V4Ui.Teal, V4Ui.TextMain, UiAnchor.BL, 40, 40, 400, 70, DoExport);
            V4Ui.Btn(win, "Nova partida", 28, V4Ui.Gold, V4Ui.Ink, UiAnchor.BR, 40, 40, 400, 70, () => BackToSetup?.Invoke());
        }
    }
}

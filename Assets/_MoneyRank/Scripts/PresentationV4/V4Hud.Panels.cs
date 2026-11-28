using System.Linq;
using MoneyRank.Domain.V4;
using UnityEngine;

namespace MoneyRank.V4.Presentation
{
    /// <summary>
    /// Panels added after the 27/11/2026 playtest: rules/attributes/abilities reading, players joining and leaving,
    /// and the annual close summary. The HUD only reads engine state and sends commands; it never calculates rules.
    /// </summary>
    public sealed partial class V4Hud
    {
        private int _seenHistory;
        private int _summaryYearShown;
        private int _summaryYear;
        private string _confirmRemoveId;
        private string _joinName = "Novo jogador";
        private string _joinProfession;
        private string _joinColor;

        private void TogglePanel(SidePanel panel)
        {
            _panel = _panel == panel ? SidePanel.None : panel;
            _confirmRemoveId = null;
            Refresh();
        }

        private string PanelTitle()
        {
            switch (_panel)
            {
                case SidePanel.Fiscal: return "Painel fiscal (pendências)";
                case SidePanel.Rules: return "Regras, atributos e habilidades";
                case SidePanel.Players: return "Jogadores: entrada e saída";
                case SidePanel.YearSummary: return "Fechamento do ano " + _summaryYear + ": o que cada um recebeu e pagou";
                default: return "Histórico de eventos";
            }
        }

        /// <summary>Shows notices for automatic abilities and opens the year summary when an annual close finishes.</summary>
        private void AnnounceNewEvents()
        {
            var s = _session.S;
            for (var i = _seenHistory; i < s.History.Count; i++)
            {
                var h = s.History[i];
                if (h.Undone || h.PlayerId == null) continue;
                var p = s.Players.FirstOrDefault(x => x.Id == h.PlayerId);
                if (p == null) continue;
                if (h.Kind == "ability" && h.Detail != null && h.Detail.Contains("automaticamente"))
                    ShowBanner("HABILIDADE: " + _session.Content.Profession(p.ProfessionId).Ability.ToUpperInvariant() + "\n" + p.Name + " avança o dobro de casas nesta barra", 4f);
                else if (h.Kind == "join" || h.Kind == "leave") Toast(h.Detail);
            }
            _seenHistory = s.History.Count;

            var closed = s.YearSummaries.Where(l => l.Year < s.YearIndex && l.Year > _summaryYearShown).Select(l => l.Year).DefaultIfEmpty(0).Max();
            if (closed > 0 && s.Phase != V4Phase.YearClose && s.Pending == null)
            {
                _summaryYearShown = closed;
                _summaryYear = closed;
                _panel = SidePanel.YearSummary;
            }
        }

        private void BuildExtraPanel(RectTransform win, float winW)
        {
            switch (_panel)
            {
                case SidePanel.Rules: BuildRulesPanel(win); break;
                case SidePanel.Players: BuildPlayersPanel(win); break;
                case SidePanel.YearSummary: BuildYearSummaryPanel(win); break;
            }
        }

        // ------------------------------------------------------------------ rules, attributes and abilities

        private void BuildRulesPanel(RectTransform win)
        {
            var e = _session.Engine;
            var r = e.Rules;
            var diff = e.Difficulty;
            var steps = r.PositiveCycleSteps;
            var route = _session.Content.RouteOf(V4Engine.Colors[0]).Count;
            var blocks = new[]
            {
                ("PONTOS E ATRIBUTOS",
                    "Positivos — Conhecimento, Investimento e Cidadania: a cada " + r.AttributeThreshold + " pontos a barra enche e o peão ativo avança " + steps + " casas no Ludo (com carro: " + steps * r.VehicleAdvanceMultiplier + ").\n" +
                    "Negativos — Toxina, Cortisol e Dívidas: barra cheia = o peão recua " + r.NegativeCycleSteps + " casa. " + r.HealthCyclesToHospital + " barras de Toxina OU de Cortisol desde a última alta = internação. Dívidas nunca interna."),
                ("COMO OS PONTOS SÃO CALCULADOS",
                    "Pontos ganhos = valor da carta × multiplicador da profissão × multiplicador da dificuldade (sempre números inteiros). Reduções (valores negativos) não são multiplicadas. O que sobra numa barra continua para a próxima."),
                ("TURNO",
                    "Lance os 2 dados e ande a soma no tabuleiro; só a casa de chegada vale. Dados iguais: resolve a casa e joga de novo (até " + r.DoublesExtraRollsMax + " jogadas extras)."),
                ("ANO E IMPOSTO DE RENDA",
                    r.RoundsPerYear + " rodadas = 1 ano. No fechamento: todos recebem o salário, depois cada um decide o IR (" + diff.IncomeTaxPct + "% do salário): pagar ao cofre ou sonegar (moeda). Também entram o retorno das parcerias e o rendimento da reserva. Novos jogadores só entram no início de um ano."),
                ("HOSPITAL",
                    "Público: o cofre paga " + V4Ui.Money(diff.PublicHospitalCost) + " e o jogador perde " + diff.PublicHospitalTurns + " turnos. Particular: " + V4Ui.Money(diff.PrivateHospitalCost) + " e perde " + diff.PrivateHospitalTurns + " turno. " +
                    (diff.EliminateWithoutResources ? "Nesta dificuldade, sem recursos para tratar = eliminação." : "Ficar sem pagar: perde " + diff.EmergencyTurns + " turnos" + (diff.EmergencyDebtBase > 0 ? " e ganha +" + diff.EmergencyDebtBase + " Dívidas." : ", sem custo."))),
                ("VITÓRIA",
                    "Vence quem concluir os " + r.PawnCount + " peões do Ludo, um de cada vez (" + (route - 1) + " casas por peão).")
            };
            var y = 74f;
            foreach (var b in blocks)
            {
                V4Ui.Label(win, b.Item1, 17, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 24, y, 640, 24, FontStyle.Bold);
                V4Ui.Label(win, b.Item2, 16, V4Ui.TextMain, TextAnchor.UpperLeft, UiAnchor.TL, 24, y + 24, 640, 100);
                y += b.Item2.Length > 230 ? 126f : (b.Item2.Length > 130 ? 104f : 80f);
            }

            V4Ui.Label(win, "JOGADORES: MULTIPLICADORES (PROFISSÃO × " + diff.Difficulty.ToUpperInvariant() + ") E HABILIDADE", 17, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 690, 74, 630, 24, FontStyle.Bold);
            var py = 104f;
            foreach (var p in _session.S.Players.Where(x => !x.Left))
            {
                var prof = _session.Content.Profession(p.ProfessionId);
                var card = V4Ui.Rect(win, "Rules " + p.Id, UiAnchor.TL, 690, py, 630, 178);
                V4Ui.Box(card, "Bg", V4Ui.Panel, UiAnchor.Stretch, 0, 0, 0, 0);
                V4Ui.Box(card, "Stripe", V4Assets.PawnColor(p.Color), UiAnchor.TL, 0, 0, 8, 178);
                V4Ui.Label(card, p.Name + " — " + prof.Name + " · salário " + V4Ui.Money(p.Salary) + "/ano", 19, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 18, 4, 600, 28, FontStyle.Bold);
                for (var a = 0; a < AttrNames.Length; a++)
                {
                    var mult = prof.Multipliers[a] * diff.MultiplierFor((AttributeId)a);
                    var col = a % 3; var row = a / 3;
                    V4Ui.Label(card, AttrNames[a] + " ×" + V4Ui.Dec(mult), 16, a <= 2 ? V4Ui.Good : V4Ui.Bad, TextAnchor.MiddleLeft, UiAnchor.TL, 18 + col * 200, 36 + row * 24, 196, 24, FontStyle.Bold);
                }
                V4Ui.Label(card, prof.Ability + (p.AbilityUsedThisYear ? " (usada neste ano)" : " (disponível neste ano)"), 16, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 18, 88, 600, 24, FontStyle.Bold);
                V4Ui.Label(card, prof.AbilityText, 15, V4Ui.TextMuted, TextAnchor.UpperLeft, UiAnchor.TL, 18, 112, 600, 62);
                py += 186f;
                if (py > 800) break;
            }
        }

        // ------------------------------------------------------------------ players joining / leaving

        private void BuildPlayersPanel(RectTransform win)
        {
            var e = _session.Engine;
            var s = _session.S;
            V4Ui.Label(win, "NA PARTIDA", 17, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, 24, 74, 600, 24, FontStyle.Bold);
            var y = 104f;
            foreach (var p in s.Players)
            {
                var prof = _session.Content.Profession(p.ProfessionId);
                var row = V4Ui.Rect(win, "Row " + p.Id, UiAnchor.TL, 24, y, 620, 70);
                V4Ui.Box(row, "Bg", V4Ui.Panel, UiAnchor.Stretch, 0, 0, 0, 0);
                V4Ui.Box(row, "Stripe", V4Assets.PawnColor(p.Color), UiAnchor.TL, 0, 0, 8, 70);
                var state = p.Left ? "saiu da partida" : (p.Eliminated ? "eliminado(a)" : "carteira " + V4Ui.Money(p.Wallet));
                V4Ui.Label(row, p.Name + " — " + prof.Name, 19, p.Eliminated ? V4Ui.TextMuted : V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 18, 4, 400, 30, FontStyle.Bold);
                V4Ui.Label(row, state, 15, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, 18, 36, 400, 28);
                if (!p.Eliminated)
                {
                    var id = p.Id;
                    var block = e.LeaveBlockReason(id);
                    var confirming = _confirmRemoveId == id;
                    var btn = V4Ui.Btn(row, confirming ? "Confirmar saída" : "Retirar", 16, confirming ? V4Ui.Bad : V4Ui.PanelLight, confirming ? V4Ui.Ink : V4Ui.TextMain, UiAnchor.TR, 10, 14, 180, 42, () =>
                    {
                        if (_confirmRemoveId != id) { _confirmRemoveId = id; Refresh(); return; }
                        _confirmRemoveId = null;
                        Run(x => x.RemovePlayer(V4Session.NewCommandId(), id, "decisão do mestre"));
                    });
                    btn.interactable = block == null;
                    if (block != null && confirming) _confirmRemoveId = null;
                }
                y += 78f;
            }
            V4Ui.Label(win, "Quem sai devolve a carteira ao banco; reserva, parcerias, imóveis, carro e checkpoints são encerrados e as peças saem do tabuleiro. Fica registrado no histórico. A partida precisa de pelo menos " + e.Rules.PlayersMin + " jogadores ativos.",
                15, V4Ui.TextMuted, TextAnchor.UpperLeft, UiAnchor.TL, 24, y + 4, 620, 80);

            // join form
            var fx = 690f;
            V4Ui.Label(win, "ADICIONAR JOGADOR", 17, V4Ui.Gold, TextAnchor.MiddleLeft, UiAnchor.TL, fx, 74, 620, 24, FontStyle.Bold);
            var joinBlock = e.JoinBlockReason();
            V4Ui.Label(win, joinBlock ?? "Entra agora, no início do ano " + s.YearIndex + ": começa com " + V4Ui.Money(e.Rules.StartingWallet) + " (pagos pelo banco), na casa H01, peões na base, salário inicial, e joga por último na ordem.",
                16, joinBlock == null ? V4Ui.Good : V4Ui.Bad, TextAnchor.UpperLeft, UiAnchor.TL, fx, 102, 620, 70);
            V4Ui.Label(win, "Nome", 16, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, fx, 180, 120, 30);
            V4Ui.Input(win, _joinName, 20, UiAnchor.TL, fx + 80, 178, 360, 40, v => _joinName = string.IsNullOrWhiteSpace(v) ? "Novo jogador" : v.Trim());

            var profs = e.FreeProfessions();
            if (_joinProfession == null || !profs.Contains(_joinProfession)) _joinProfession = profs.FirstOrDefault();
            V4Ui.Label(win, "Profissão (livres)", 16, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, fx, 230, 300, 26);
            var k = 0;
            foreach (var pid in profs)
            {
                var capture = pid;
                var sel = _joinProfession == pid;
                V4Ui.Btn(win, _session.Content.Profession(pid).Name, 16, sel ? V4Ui.Gold : V4Ui.PanelLight, sel ? V4Ui.Ink : V4Ui.TextMain, UiAnchor.TL, fx + (k % 3) * 206, 260 + (k / 3) * 48, 198, 40, () => { _joinProfession = capture; Refresh(); });
                k++;
            }
            var colors = e.FreeColors();
            if (_joinColor == null || !colors.Contains(_joinColor)) _joinColor = colors.FirstOrDefault();
            V4Ui.Label(win, "Cor do peão (livres)", 16, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, fx, 364, 300, 26);
            var c = 0;
            foreach (var col in colors)
            {
                var capture = col;
                V4Ui.Btn(win, _joinColor == col ? "✓" : string.Empty, 22, V4Assets.PawnColor(col), V4Ui.Ink, UiAnchor.TL, fx + c * 92, 394, 82, 40, () => { _joinColor = capture; Refresh(); });
                c++;
            }
            var add = V4Ui.Btn(win, "ADICIONAR À PARTIDA", 24, V4Ui.Gold, V4Ui.Ink, UiAnchor.TL, fx, 460, 420, 64, () =>
            {
                var setup = new V4PlayerSetup { Name = _joinName, ProfessionId = _joinProfession, Color = _joinColor };
                Run(x => x.AddPlayer(V4Session.NewCommandId(), setup));
            });
            add.interactable = joinBlock == null && _joinProfession != null && _joinColor != null;
        }

        // ------------------------------------------------------------------ annual close summary

        private void BuildYearSummaryPanel(RectTransform win)
        {
            var s = _session.S;
            var pct = _session.Engine.Difficulty.IncomeTaxPct;
            V4Ui.Label(win, "Salário recebido, imposto de renda (" + pct + "% do salário), retorno de parcerias e rendimento da reserva de cada jogador.", 17, V4Ui.TextMuted, TextAnchor.MiddleLeft, UiAnchor.TL, 24, 70, 1290, 30);
            var y = 108f;
            foreach (var line in s.YearSummaries.Where(l => l.Year == _summaryYear))
            {
                var p = s.Players.FirstOrDefault(x => x.Id == line.PlayerId);
                if (p == null) continue;
                var row = V4Ui.Rect(win, "Year " + p.Id, UiAnchor.TL, 24, y, 1290, 150);
                V4Ui.Box(row, "Bg", V4Ui.Panel, UiAnchor.Stretch, 0, 0, 0, 0);
                V4Ui.Box(row, "Stripe", V4Assets.PawnColor(p.Color), UiAnchor.TL, 0, 0, 8, 150);
                V4Ui.Label(row, p.Name + " — " + _session.Content.Profession(p.ProfessionId).Name, 22, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, 6, 700, 32, FontStyle.Bold);
                var net = line.Salary - line.IncomeTaxPaid + line.PartnershipReturns;
                V4Ui.Label(row, "Na carteira: " + (net >= 0 ? "+" : "−") + V4Ui.Money(Mathf.Abs(net)), 22, net >= 0 ? V4Ui.Good : V4Ui.Bad, TextAnchor.MiddleRight, UiAnchor.TR, 20, 6, 500, 32, FontStyle.Bold);
                V4Ui.Label(row, "Salário recebido: +" + V4Ui.Money(line.Salary), 18, V4Ui.Good, TextAnchor.MiddleLeft, UiAnchor.TL, 20, 44, 620, 28);
                V4Ui.Label(row, "Imposto de renda: " + IncomeTaxText(line), 18, line.IncomeTaxPaid > 0 ? V4Ui.Bad : V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, 74, 1250, 28);
                V4Ui.Label(row, "Parcerias: " + (line.PartnershipReturns > 0 ? "+" + V4Ui.Money(line.PartnershipReturns) : "nenhum retorno") + "     ·     Reserva: " + (line.ReserveInterest > 0 ? "+" + V4Ui.Money(line.ReserveInterest) + " de rendimento (fica na reserva)" : "sem rendimento"),
                    18, V4Ui.TextMain, TextAnchor.MiddleLeft, UiAnchor.TL, 20, 104, 1250, 28);
                y += 158f;
                if (y > 760) break;
            }
            V4Ui.Btn(win, "Continuar a partida", 24, V4Ui.Gold, V4Ui.Ink, UiAnchor.BC, 0, 20, 420, 60, () => { _panel = SidePanel.None; Refresh(); });
        }

        private static string IncomeTaxText(YearSummaryLine line)
        {
            switch (line.IncomeTaxOutcome)
            {
                case "pago": return "pagou " + V4Ui.Money(line.IncomeTaxPaid) + " ao cofre";
                case "sonegado": return "sonegou " + V4Ui.Money(line.IncomeTaxDue) + " (moeda FELIZ: pendência registrada)";
                case "multa": return "tentou sonegar e pagou multa de " + V4Ui.Money(line.IncomeTaxPaid) + " (moeda TRISTE)";
                case "autuado": return "autuado(a): tentou sonegar sem recursos para a multa";
                case "sem_recursos": return "sem recursos para " + V4Ui.Money(line.IncomeTaxDue) + " (pendência registrada)";
                default: return "devido " + V4Ui.Money(line.IncomeTaxDue);
            }
        }
    }
}

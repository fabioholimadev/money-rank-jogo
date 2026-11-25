using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MoneyRank.Domain.V4
{
    /// <summary>Preview of an option's effects computed on a throw-away copy (never mutates the real state).</summary>
    public sealed class V4Preview
    {
        public bool Ok;
        public string Error;
        public int WalletDelta, BankDelta, FundDelta, ReserveDelta;
        public decimal[] AttributeProgressAfter = new decimal[AttributeIds.Count];
        public int[] CyclesGained = new int[AttributeIds.Count];
        public int PawnIndexAfter;
        public bool HospitalWillTrigger;
        public List<string> Events = new List<string>();
    }

    public sealed partial class V4Engine
    {
        // ------------------------------------------------------------------ house dispatch

        private void ResolveHouse(PlayerState p, HouseDefinition h)
        {
            switch (h.Handler)
            {
                case "inicio":
                case "visita_hospital":
                    Resolve(p);
                    return;
                case "internar":
                    p.HospitalPending = true;
                    Resolve(p);
                    return;
                case "imposto":
                    OpenTaxHouse(p, h);
                    return;
                case "automatico":
                    ApplyOption(p, h, Content.OptionsOf(h.Id)[0]);
                    Resolve(p);
                    return;
                default:
                    OpenChoice(p, h);
                    return;
            }
        }

        // ------------------------------------------------------------------ eligibility

        private static bool IsPaidAction(string a) => a == "normal" || a == "parceria" || a == "progressao";

        public int CostOf(PlayerState p, OptionDefinition o)
        {
            if (o.Action == "progressao")
            {
                var cards = Content.ProgressionOf(p.ProfessionId);
                return p.Level < cards.Count ? cards[p.Level].Cost : 0;
            }
            return o.Cost;
        }

        public int RewardOf(PlayerState p, OptionDefinition o) => o.RewardFixed + CeilPct(p.Salary, o.RewardPctSalary);

        /// <summary>Returns null when the option is selectable, otherwise the human reason it is blocked.</summary>
        public string BlockReason(PlayerState p, OptionDefinition o)
        {
            switch (o.Condition)
            {
                case "etapa_menor_que_4": if (p.Level >= 4) return "Carreira completa."; break;
                case "etapa_igual_4": if (p.Level < 4) return "Ainda há evoluções disponíveis."; break;
                case "saldo_e_teto":
                    if (p.Wallet < o.Cost) return "Saldo insuficiente na carteira.";
                    if (p.ReserveBalance + o.Cost > p.Salary) return "Acima do teto da reserva (salário atual).";
                    break;
                case "veiculo_ativo": if (!VehicleActive(p)) return "Sem veículo ativo."; break;
                case "sem_veiculo_ativo": if (VehicleActive(p)) return "Já possui veículo ativo."; break;
                case "barra_dividas_maior_zero": if (p.Attributes[(int)AttributeId.Dividas].Progress <= 0m) return "Sem dívidas na barra atual."; break;
                case "barra_dividas_zero": if (p.Attributes[(int)AttributeId.Dividas].Progress > 0m) return "Há dívidas na barra atual."; break;
            }
            var cost = CostOf(p, o);
            if (cost > 0 && o.Condition != "saldo_e_teto" && Capacity(p) < cost) return "Saldo insuficiente (carteira + reserva).";
            var reward = RewardOf(p, o);
            if (reward > 0 && State.BankCash + cost < reward) return "Banco sem caixa para honrar o benefício.";
            return null;
        }

        // ------------------------------------------------------------------ choice building

        private void OpenChoice(PlayerState p, HouseDefinition h)
        {
            var d = NewDecision(DecisionKind.Choice, p, h.Id);
            BuildOptions(p, h, d, Content.OptionsOf(h.Id));
            d.Mandatory = h.Category == "essencial";
            if (d.OptionIds.Count == 0) { Resolve(p); return; }
            State.Pending = d;
            State.Phase = V4Phase.AwaitDecision;
            Note(p.Name + " precisa decidir em " + h.Id + " — " + h.Title + ".");
        }

        private void BuildOptions(PlayerState p, HouseDefinition h, PendingDecision d, IEnumerable<OptionDefinition> options)
        {
            var list = options.ToList();
            var anyPaidAccessible = list.Any(o => IsPaidAction(o.Action) && CostOf(p, o) > 0 && BlockReason(p, o) == null);
            foreach (var o in list)
            {
                if (o.Action == "inadimplencia")
                {
                    if (!anyPaidAccessible) d.OptionIds.Add(o.Id);
                    continue;
                }
                var reason = BlockReason(p, o);
                if (reason == null) d.OptionIds.Add(o.Id);
                else { d.BlockedOptionIds.Add(o.Id); d.BlockedReasons.Add(reason); }
            }
        }

        // ------------------------------------------------------------------ choose

        public V4Result ChooseOption(string commandId, string decisionId, string playerId, string optionId)
        {
            return Run(commandId, () =>
            {
                var d = State.Pending;
                if (d == null || d.Kind != DecisionKind.Choice || d.Id != decisionId) return V4Result.Fail("Decisão inválida ou desatualizada.");
                if (d.PlayerId != playerId) return V4Result.Fail("Não é a vez deste jogador.");
                if (!d.OptionIds.Contains(optionId))
                {
                    var i = d.BlockedOptionIds.IndexOf(optionId);
                    return V4Result.Fail(i >= 0 ? d.BlockedReasons[i] : "Opção indisponível.");
                }
                CommitChoice(d, optionId);
                return V4Result.Success();
            });
        }

        private void CommitChoice(PendingDecision d, string optionId)
        {
            var p = Player(d.PlayerId);
            State.Pending = null;
            State.Phase = V4Phase.AwaitRoll;
            if (!string.IsNullOrEmpty(d.Tag) && d.Tag.StartsWith("tax:", StringComparison.Ordinal))
            {
                HandleTaxChoice(p, d, optionId);
                return;
            }
            var o = Content.Options.First(x => x.Id == optionId);
            var h = Content.House(d.HouseId);
            ApplyOption(p, h, o, d);
            if (State.Pending == null) Resolve(p);
        }

        /// <summary>Entrepreneur "Visão de Negócios": once per year swap an unfavourable selection for another eligible option.</summary>
        public V4Result SwapWithEntrepreneur(string commandId, string decisionId, string playerId, string fromOptionId, string toOptionId)
        {
            return Run(commandId, () =>
            {
                var d = State.Pending;
                if (d == null || d.Kind != DecisionKind.Choice || d.Id != decisionId) return V4Result.Fail("Decisão inválida ou desatualizada.");
                if (d.PlayerId != playerId) return V4Result.Fail("Não é a vez deste jogador.");
                var p = Player(playerId);
                if (p.ProfessionId != AbilityEmpreendedora || !State.Config.EmpreendedoraSwapEnabled) return V4Result.Fail("Habilidade indisponível.");
                if (p.AbilityUsedThisYear) return V4Result.Fail("Habilidade já usada neste ano.");
                if (!string.IsNullOrEmpty(d.Tag) && d.Tag.StartsWith("tax:", StringComparison.Ordinal)) return V4Result.Fail("A habilidade não afeta impostos.");
                var h = Content.House(d.HouseId);
                if (h.Category != "nao_essencial" && h.Category != "divida" && h.Category != "oportunidade") return V4Result.Fail("Evento não elegível.");
                if (!d.OptionIds.Contains(fromOptionId) || !d.OptionIds.Contains(toOptionId) || fromOptionId == toOptionId) return V4Result.Fail("Opções inválidas.");
                var from = Content.Options.First(x => x.Id == fromOptionId);
                var unfavourable = from.Cost > 0 || from.Deltas[3] > 0 || from.Deltas[4] > 0 || from.Deltas[5] > 0;
                if (!unfavourable) return V4Result.Fail("A seleção original não é desfavorável.");
                p.AbilityUsedThisYear = true;
                Log(p, "ability", d.HouseId, toOptionId, p.Name + " usou Visão de Negócios: " + fromOptionId + " → " + toOptionId + ".");
                CommitChoice(d, toOptionId);
                return V4Result.Success();
            });
        }

        // ------------------------------------------------------------------ option application

        private void ApplyOption(PlayerState p, HouseDefinition h, OptionDefinition o, PendingDecision context = null)
        {
            var deltas = DeltasOf(o);
            switch (o.Action)
            {
                case "recusar":
                case "neutro":
                case "isento":
                case "consulta_cofre":
                    Log(p, "option", h.Id, o.Id, p.Name + ": " + o.Title + ".");
                    return;

                case "normal":
                case "automatico":
                    Pay(p, CostOf(p, o), "bank", "despesa", o.Id);
                    Receive(p, RewardOf(p, o), "recompensa", o.Id);
                    break;

                case "inadimplencia":
                    break;

                case "depositar_reserva":
                {
                    var oldRecord = p.ReserveRecord;
                    Transfer("w:" + p.Id, "bank", o.Cost, "deposito_reserva", o.Id);
                    p.ReserveBalance += o.Cost;
                    p.ReservePrincipal += o.Cost;
                    p.ReserveRecord = Math.Max(p.ReserveRecord, p.ReservePrincipal);
                    var fresh = Math.Max(0, p.ReservePrincipal - oldRecord);
                    deltas[(int)AttributeId.Investimento] = o.Cost == 0 ? 0m : o.Deltas[(int)AttributeId.Investimento] * ((decimal)fresh / o.Cost);
                    break;
                }

                case "comprar_veiculo":
                    Pay(p, o.Cost, "bank", "compra_veiculo", o.Id);
                    p.HasVehicle = true;
                    p.VehicleExpiresAfterRound = State.CompletedRounds + Rules.VehicleDurationRounds + 1;
                    break;

                case "manter_veiculo":
                    Pay(p, o.Cost, "bank", "manutencao_veiculo", o.Id);
                    p.VehicleExpiresAfterRound += Rules.VehicleMaintenanceExtensionRounds;
                    break;

                case "comprar_imovel":
                    Pay(p, o.Cost, "bank", "compra_imovel", o.Id);
                    p.Properties += 1;
                    p.CheckpointRights += o.ParamInt("direitos_checkpoint", 1);
                    break;

                case "parceria":
                    Pay(p, o.Cost, "bank", "parceria", o.Id);
                    p.Receivables.Add(new Receivable
                    {
                        Id = p.Id + "-R" + (p.Receivables.Count + 1), Investment = o.Cost,
                        TotalReturn = o.ParamInt("retorno_total_umr"), DueYear = State.YearIndex
                    });
                    break;

                case "progressao":
                {
                    var card = Content.ProgressionOf(p.ProfessionId)[p.Level];
                    Pay(p, card.Cost, "bank", "progressao", card.Id);
                    p.Salary = card.SalaryAfter;
                    p.Level += 1;
                    deltas = new decimal[AttributeIds.Count];
                    deltas[0] = card.Conhecimento; deltas[1] = card.Investimento; deltas[2] = card.Cidadania;
                    break;
                }

                case "doar_cofre":
                    Pay(p, o.Cost, "fund", "contribuicao_voluntaria", o.Id);
                    break;

                case "delegar":
                {
                    var dest = Content.House(o.ParamString("casa_destino"));
                    var d = NewDecision(DecisionKind.Choice, p, dest.Id);
                    d.Delegated = true;
                    d.Tag = "delegated:" + h.Id;
                    BuildOptions(p, dest, d, Content.OptionsOf(dest.Id));
                    Log(p, "option", h.Id, o.Id, p.Name + " delegou a " + dest.Id + " — " + dest.Title + ".");
                    State.Pending = d;
                    State.Phase = V4Phase.AwaitDecision;
                    return;
                }

                case "internar":
                    p.HospitalPending = true;
                    break;

                default:
                    throw new InvalidOperationException("Ação desconhecida: " + o.Action);
            }
            Log(p, "option", h.Id, o.Id, p.Name + " aplicou: " + o.Title + ".");
            ApplyEffects(p, deltas);
        }

        // ------------------------------------------------------------------ checkpoint

        public V4Result InstallCheckpoint(string commandId, string playerId)
        {
            return Run(commandId, () =>
            {
                if (State.Phase != V4Phase.AwaitRoll && State.Phase != V4Phase.AwaitDecision) return V4Result.Fail("Checkpoint indisponível nesta fase.");
                var p = Player(playerId);
                if (CurrentPlayer.Id != playerId) return V4Result.Fail("Só durante o próprio turno.");
                var block = V4Ludo.CheckpointBlockReason(Content, p);
                if (block != null) return V4Result.Fail(block);
                V4Ludo.TryInstallCheckpoint(Content, p);
                Log(p, "checkpoint", null, null, p.Name + " instalou um checkpoint (restam " + p.CheckpointRights + " direito[s]).");
                return V4Result.Success();
            });
        }

        // ------------------------------------------------------------------ preview

        /// <summary>Computes what choosing an option would do, on a copy. Never mutates this engine.</summary>
        public V4Preview PreviewOption(string decisionId, string playerId, string optionId)
        {
            var sim = new V4Engine(Content, StateClone.Clone(State));
            var p0 = Player(playerId);
            var w = p0.Wallet; var b = State.BankCash; var f = State.FundCash; var r = p0.ReserveBalance;
            var result = sim.ChooseOption("preview", decisionId, playerId, optionId);
            var preview = new V4Preview { Ok = result.Ok, Error = result.Error };
            if (!result.Ok) return preview;
            var p1 = sim.Player(playerId);
            preview.WalletDelta = p1.Wallet - w;
            preview.BankDelta = sim.State.BankCash - b;
            preview.FundDelta = sim.State.FundCash - f;
            preview.ReserveDelta = p1.ReserveBalance - r;
            for (var i = 0; i < AttributeIds.Count; i++)
            {
                preview.AttributeProgressAfter[i] = p1.Attributes[i].Progress;
                preview.CyclesGained[i] = p1.Attributes[i].LifetimeCycles - p0.Attributes[i].LifetimeCycles;
            }
            preview.PawnIndexAfter = p1.ActivePawn < 0 ? -1 : p1.Pawns[p1.ActivePawn].RouteIndex;
            preview.HospitalWillTrigger = p1.HospitalTurnsRemaining > 0 || (sim.State.Pending != null && sim.State.Pending.Kind == DecisionKind.HospitalModality);
            preview.Events.AddRange(result.Events);
            return preview;
        }
    }
}

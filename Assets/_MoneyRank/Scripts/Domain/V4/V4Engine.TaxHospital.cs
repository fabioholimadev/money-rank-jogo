using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MoneyRank.Domain.V4
{
    public sealed partial class V4Engine
    {
        public bool IsFiscalPublic => State.Config.Mode == "online" || !State.Config.FiscalPrivateToMaster;

        // ------------------------------------------------------------------ tax houses (IPTU / IPVA / ISS / ICMS)

        private static string TaxKind(string houseId)
        {
            switch (houseId)
            {
                case "H07": return "IPTU";
                case "H11": return "IPVA";
                case "H25": return "ISS";
                case "H32": return "ICMS";
                default: return houseId;
            }
        }

        private int TaxDue(PlayerState p, HouseDefinition h, out bool exempt)
        {
            var pay = Content.OptionsOf(h.Id).First(o => o.Action == "pagar_imposto");
            exempt = false;
            if (pay.Condition == "imoveis_maior_que_zero" && p.Properties == 0) { exempt = true; return 0; }
            if (pay.Condition == "veiculo_ativo" && !VehicleActive(p)) { exempt = true; return 0; }
            var multiplier = pay.ParamString("multiplicar_por") == "imoveis" ? p.Properties : 1;
            return pay.ParamInt("base_umr") * multiplier;
        }

        private void OpenTaxHouse(PlayerState p, HouseDefinition h)
        {
            // Pending fiscal records are audited first, even when exempt from this specific tax (HANDOFF §10).
            if (p.Fiscal.Any(f => f.Status != FiscalStatus.Regularizado))
            {
                var d = NewDecision(DecisionKind.FiscalCoin, p, h.Id);
                d.Tag = "after:" + h.Id;
                State.Pending = d;
                State.Phase = V4Phase.AwaitDecision;
                Note("Fiscalização: lance a moeda (FELIZ mantém pendências, TRISTE cobra).");
                return;
            }
            ContinueTaxHouse(p, h);
        }

        private void ContinueTaxHouse(PlayerState p, HouseDefinition h)
        {
            bool exempt;
            var due = TaxDue(p, h, out exempt);
            if (exempt)
            {
                Log(p, "tax", h.Id, null, p.Name + " está isento(a) de " + TaxKind(h.Id) + ".");
                Resolve(p);
                return;
            }
            OpenTaxChoice(p, TaxKind(h.Id), h.Id, due);
        }

        private void OpenTaxChoice(PlayerState p, string kind, string houseId, int due)
        {
            var d = NewDecision(DecisionKind.Choice, p, houseId);
            d.Tag = "tax:" + kind;
            d.Amount = due;
            d.Mandatory = true;
            var canPay = Capacity(p) >= due;
            if (houseId == "SYS_IR")
            {
                if (canPay) { d.OptionIds.Add("IR:pay"); d.OptionIds.Add("IR:evade"); }
                else d.OptionIds.Add("IR:none");
            }
            else
            {
                foreach (var o in Content.OptionsOf(houseId))
                {
                    if (o.Action == "pagar_imposto" || o.Action == "sonegar_imposto") { if (canPay) d.OptionIds.Add(o.Id); }
                    else if (o.Action == "imposto_sem_recursos") { if (!canPay) d.OptionIds.Add(o.Id); }
                }
            }
            State.Pending = d;
            State.Phase = V4Phase.AwaitDecision;
            Note(p.Name + " deve " + kind + ": " + due + " UMR.");
        }

        private string TaxActionOf(string optionId)
        {
            if (optionId.StartsWith("IR:", StringComparison.Ordinal))
            {
                switch (optionId) { case "IR:pay": return "pagar_imposto"; case "IR:evade": return "sonegar_imposto"; default: return "imposto_sem_recursos"; }
            }
            return Content.Options.First(x => x.Id == optionId).Action;
        }

        private void HandleTaxChoice(PlayerState p, PendingDecision d, string optionId)
        {
            var kind = d.Tag.Substring("tax:".Length);
            var due = d.Amount;
            switch (TaxActionOf(optionId))
            {
                case "pagar_imposto":
                    Pay(p, due, "fund", "imposto_" + kind, d.HouseId);
                    if (State.Config.VoluntaryTaxCitizenship && Rules.VoluntaryTaxCitizenshipBase > 0)
                        ApplyEffects(p, OneDelta(AttributeId.Cidadania, Rules.VoluntaryTaxCitizenshipBase));
                    Log(p, "tax", d.HouseId, optionId, p.Name + " pagou " + due + " UMR de " + kind + " ao cofre.");
                    break;
                case "sonegar_imposto":
                {
                    var coin = NewDecision(DecisionKind.TaxCoin, p, d.HouseId);
                    coin.Tag = kind;
                    coin.Amount = due;
                    State.Pending = coin;
                    State.Phase = V4Phase.AwaitDecision;
                    Note("Sonegação de " + kind + ": lance a moeda.");
                    return;
                }
                case "imposto_sem_recursos":
                    AddFiscal(p, kind, due, due, FiscalStatus.Inadimplente);
                    ApplyEffects(p, OneDelta(AttributeId.Dividas, Rules.UnpaidTaxDebtBase));
                    Log(p, "tax", d.HouseId, optionId, p.Name + " sem recursos para " + kind + ": inadimplente, +" + Rules.UnpaidTaxDebtBase + " Dívidas.");
                    break;
                default:
                    Log(p, "tax", d.HouseId, optionId, p.Name + ": isento.");
                    break;
            }
            Resolve(p);
        }

        private void AddFiscal(PlayerState p, string kind, int principal, int outstanding, string status)
        {
            p.Fiscal.Add(new FiscalRecord
            {
                Id = p.Id + "-F" + (p.Fiscal.Count + 1), Kind = kind, SourceYear = State.YearIndex,
                Principal = principal, Outstanding = outstanding, Status = status
            });
        }

        /// <summary>Physical coin result: <paramref name="happy"/> = FELIZ. Handles evasion and audit coins.</summary>
        public V4Result RegisterCoin(string commandId, string decisionId, string playerId, bool happy)
        {
            return Run(commandId, () =>
            {
                var d = State.Pending;
                if (d == null || d.Id != decisionId || (d.Kind != DecisionKind.TaxCoin && d.Kind != DecisionKind.FiscalCoin))
                    return V4Result.Fail("Decisão inválida ou desatualizada.");
                if (d.PlayerId != playerId) return V4Result.Fail("Não é a vez deste jogador.");
                var p = Player(playerId);
                State.Rng.Add(new RngRecord { DecisionId = d.Id, Kind = "coin", Value = happy ? "FELIZ" : "TRISTE" });
                State.Pending = null;
                State.Phase = V4Phase.AwaitRoll;
                if (d.Kind == DecisionKind.TaxCoin) ResolveEvasionCoin(p, d, happy);
                else ResolveFiscalCoin(p, d, happy);
                return V4Result.Success();
            });
        }

        private void ResolveEvasionCoin(PlayerState p, PendingDecision d, bool happy)
        {
            var due = d.Amount;
            if (happy)
            {
                AddFiscal(p, d.Tag, due, due, FiscalStatus.Sonegado);
                Log(p, "tax", d.HouseId, null, p.Name + " sonegou " + due + " UMR de " + d.Tag + " (moeda FELIZ): pendência registrada.");
            }
            else
            {
                var penalty = due * Rules.TaxEvasionPenaltyMultiplier;
                if (Capacity(p) >= penalty)
                {
                    Pay(p, penalty, "fund", "multa_" + d.Tag, d.HouseId);
                    ApplyEffects(p, OneDelta(AttributeId.Cortisol, 1m));
                    Log(p, "tax", d.HouseId, null, p.Name + " pagou " + penalty + " UMR (dobro) de " + d.Tag + " (moeda TRISTE), +1 Cortisol.");
                }
                else
                {
                    AddFiscal(p, d.Tag, due, penalty, FiscalStatus.Autuado);
                    ApplyEffects(p, OneDelta(AttributeId.Dividas, Rules.UnpaidTaxDebtBase));
                    Log(p, "tax", d.HouseId, null, p.Name + " autuado(a) em " + penalty + " UMR sem recursos: +" + Rules.UnpaidTaxDebtBase + " Dívidas.");
                }
            }
            Resolve(p);
        }

        private void ResolveFiscalCoin(PlayerState p, PendingDecision d, bool happy)
        {
            var records = p.Fiscal.Where(f => f.Status != FiscalStatus.Regularizado).ToList();
            if (happy)
            {
                Log(p, "audit", d.HouseId, null, p.Name + ": fiscalização (FELIZ), pendências mantidas.");
            }
            else
            {
                var values = records.ToDictionary(f => f, f => f.Status == FiscalStatus.Autuado ? f.Outstanding : f.Principal * Rules.TaxEvasionPenaltyMultiplier);
                var total = values.Values.Sum();
                ApplyEffects(p, OneDelta(AttributeId.Cortisol, 1m));
                if (Capacity(p) >= total)
                {
                    Pay(p, total, "fund", "fiscalizacao", d.HouseId);
                    foreach (var f in records) { f.Status = FiscalStatus.Regularizado; f.Outstanding = 0; }
                    Log(p, "audit", d.HouseId, null, p.Name + " pagou " + total + " UMR na fiscalização (TRISTE), +1 Cortisol.");
                }
                else
                {
                    foreach (var f in records) { f.Outstanding = values[f]; f.Status = FiscalStatus.Autuado; }
                    ApplyEffects(p, OneDelta(AttributeId.Dividas, Rules.UnpaidTaxDebtBase));
                    Log(p, "audit", d.HouseId, null, p.Name + " autuado(a) em " + total + " UMR sem recursos (TRISTE): +1 Cortisol, +" + Rules.UnpaidTaxDebtBase + " Dívidas.");
                }
            }
            if (!string.IsNullOrEmpty(d.Tag) && d.Tag.StartsWith("after:", StringComparison.Ordinal))
                ContinueTaxHouse(p, Content.House(d.Tag.Substring("after:".Length)));
            else Resolve(p);
        }

        // ------------------------------------------------------------------ hospital

        private int PrivateCostFor(PlayerState p)
        {
            var cost = Difficulty.PrivateHospitalCost;
            return p.ProfessionId == AbilityMedica && !p.AbilityUsedThisYear ? (cost + 1) / 2 : cost;
        }

        private bool PublicAccessible() => State.FundCash >= Difficulty.PublicHospitalCost;
        private bool PrivateAccessible(PlayerState p) => Capacity(p) >= PrivateCostFor(p);
        private bool WouldEliminateInHospital(PlayerState p) => Difficulty.EliminateWithoutResources && !PublicAccessible() && !PrivateAccessible(p);

        private void StartHospital(PlayerState p)
        {
            p.Position = 9; // H10
            p.HospitalPending = false;
            var pub = PublicAccessible();
            var priv = PrivateAccessible(p);
            if (!pub && !priv) { Emergency(p); return; }
            var d = NewDecision(DecisionKind.HospitalModality, p, "H10");
            if (pub) d.OptionIds.Add("public");
            if (priv) d.OptionIds.Add("private");
            d.Amount = Difficulty.PublicHospitalCost;
            d.Amount2 = PrivateCostFor(p);
            State.Pending = d;
            State.Phase = V4Phase.AwaitDecision;
            Note(p.Name + " foi internado(a): escolha o tratamento.");
        }

        private void Emergency(PlayerState p)
        {
            if (Difficulty.EliminateWithoutResources)
            {
                Eliminate(p, "sem recursos para o tratamento");
                return;
            }
            p.HospitalTurnsRemaining = Difficulty.EmergencyTurns;
            if (Difficulty.EmergencyDebtBase > 0) ApplyEffects(p, OneDelta(AttributeId.Dividas, Difficulty.EmergencyDebtBase));
            Log(p, "hospital", "H10", null, p.Name + " em recuperação emergencial: " + Difficulty.EmergencyTurns + " turno(s) perdido(s).");
            Drain(p);
        }

        public V4Result ChooseHospital(string commandId, string decisionId, string playerId, string modality)
        {
            return Run(commandId, () =>
            {
                var d = State.Pending;
                if (d == null || d.Kind != DecisionKind.HospitalModality || d.Id != decisionId) return V4Result.Fail("Decisão inválida ou desatualizada.");
                if (d.PlayerId != playerId) return V4Result.Fail("Não é a vez deste jogador.");
                if (!d.OptionIds.Contains(modality)) return V4Result.Fail("Modalidade indisponível.");
                var p = Player(playerId);
                State.Pending = null;
                State.Phase = V4Phase.AwaitRoll;
                if (modality == "public")
                {
                    Transfer("fund", "bank", Difficulty.PublicHospitalCost, "hospital_publico", null);
                    p.HospitalTurnsRemaining = Difficulty.PublicHospitalTurns;
                }
                else
                {
                    var doctor = p.ProfessionId == AbilityMedica && !p.AbilityUsedThisYear;
                    var cost = PrivateCostFor(p);
                    Pay(p, cost, "bank", "hospital_particular", null);
                    p.HospitalTurnsRemaining = doctor ? Math.Max(1, Difficulty.PrivateHospitalTurns - 1) : Difficulty.PrivateHospitalTurns;
                    if (doctor) p.AbilityUsedThisYear = true;
                }
                Log(p, "hospital", "H10", modality, p.Name + " escolheu tratamento " + (modality == "public" ? "público" : "particular") + ": " + p.HospitalTurnsRemaining + " turno(s) perdido(s).");
                Resolve(p);
                return V4Result.Success();
            });
        }

        private void Eliminate(PlayerState p, string reason)
        {
            // Account closure, not a mysterious revenue: wallet returns to the bank; reserve/receivables are cancelled liabilities.
            Transfer("w:" + p.Id, "bank", p.Wallet, "encerramento_conta", p.Id);
            p.ReserveBalance = 0; p.ReservePrincipal = 0;
            p.Receivables.Clear();
            p.HasVehicle = false; p.Properties = 0; p.CheckpointRights = 0;
            p.HospitalTurnsRemaining = 0; p.HospitalPending = false;
            p.Eliminated = true;
            Log(p, "eliminated", null, null, p.Name + " foi eliminado(a): " + reason + ".");
            var alive = State.Players.Where(x => !x.Eliminated).ToList();
            if (alive.Count == 1) End(alive[0].Id, "survival");
            else if (alive.Count == 0) End(null, "all_eliminated");
        }
    }
}

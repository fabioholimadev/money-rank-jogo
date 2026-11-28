using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MoneyRank.Domain.V4
{
    public sealed partial class V4Engine
    {
        // ------------------------------------------------------------------ turn

        /// <summary>
        /// Registers the two dice (physical or virtual; each 1..6). Movement is the sum; only the destination house triggers.
        /// Doubles grant another roll to the same player, at most <c>doubles_extra_rolls_max</c> (3) extra rolls per turn,
        /// and never when the turn ends in hospital or elimination.
        /// </summary>
        public V4Result RegisterRoll(string commandId, string playerId, int die1, int die2)
        {
            return Run(commandId, () =>
            {
                if (State.Phase != V4Phase.AwaitRoll) return V4Result.Fail("Não é o momento de lançar os dados.");
                var p = CurrentPlayer;
                if (p.Id != playerId) return V4Result.Fail("Não é a vez deste jogador.");
                var two = Rules.DiceCount >= 2;
                if (die1 < 1 || die1 > Rules.DieFaces) return V4Result.Fail("Valor de dado inválido: " + die1 + ".");
                if (two ? (die2 < 1 || die2 > Rules.DieFaces) : die2 != 0) return V4Result.Fail("Valor de dado inválido: " + die2 + ".");
                var total = die1 + die2;
                State.Rng.Add(new RngRecord { DecisionId = "T" + State.History.Count, Kind = "dice", Value = die1.ToString(CultureInfo.InvariantCulture) + "+" + die2.ToString(CultureInfo.InvariantCulture) });
                State.LastDie1 = die1; State.LastDie2 = die2;
                State.LastDieValue = die1.ToString(CultureInfo.InvariantCulture) + "+" + die2.ToString(CultureInfo.InvariantCulture);
                var isDouble = two && die1 == die2;
                State.RollAgain = false;
                if (isDouble)
                {
                    if (State.ExtraRollsUsed < Rules.DoublesExtraRollsMax)
                    {
                        State.RollAgain = true;
                        State.ExtraRollsUsed += 1;
                        Note("Dados iguais! " + p.Name + " joga novamente após resolver a casa (extra " + State.ExtraRollsUsed + "/" + Rules.DoublesExtraRollsMax + ").");
                    }
                    else Note("Dados iguais, mas o limite de " + Rules.DoublesExtraRollsMax + " jogadas extras foi atingido: a vez passa.");
                }
                var from = p.Position;
                p.Position = (from + total) % Content.Houses.Count;
                var house = Content.HouseAt(p.Position);
                Log(p, "move", house.Id, null, p.Name + " tirou " + (two ? die1 + " + " + die2 + " = " + total : total.ToString(CultureInfo.InvariantCulture)) + (isDouble ? " (iguais)" : string.Empty) + " e chegou a " + house.Id + " — " + house.Title + ".");
                ResolveHouse(p, house);
                return V4Result.Success();
            });
        }

        /// <summary>Motorista: swap the roll for -2 base Cortisol or Toxina, once per year; salary preserved, no house triggered.</summary>
        public V4Result UseDriverFlex(string commandId, string playerId, AttributeId attribute)
        {
            return Run(commandId, () =>
            {
                if (State.Phase != V4Phase.AwaitRoll) return V4Result.Fail("Habilidade só antes de lançar o dado.");
                var p = CurrentPlayer;
                if (p.Id != playerId) return V4Result.Fail("Não é a vez deste jogador.");
                if (p.ProfessionId != AbilityMotorista) return V4Result.Fail("Habilidade exclusiva do Motorista.");
                if (p.AbilityUsedThisYear) return V4Result.Fail("Habilidade já usada neste ano.");
                if (attribute != AttributeId.Cortisol && attribute != AttributeId.Toxina) return V4Result.Fail("Escolha Cortisol ou Toxina.");
                p.AbilityUsedThisYear = true;
                V4Attributes.Apply(p, OneDelta(attribute, -2m), ProfessionOf(p), Difficulty, Rules);
                Log(p, "ability", null, null, p.Name + " usou Flexibilidade de Jornada: -2 " + AttributeIds.Keys[(int)attribute] + " (sem lançar o dado).");
                EndTurn();
                return V4Result.Success();
            });
        }

        /// <summary>Resolves queued concessions, then victory, then hospital, then the turn flow (HANDOFF §5/§7).</summary>
        private void Resolve(PlayerState p)
        {
            if (!Drain(p)) return;
            FinishPlayerAction(p);
        }

        private void FinishPlayerAction(PlayerState p)
        {
            if (State.Phase == V4Phase.GameEnd) return;
            if (V4Ludo.AllCompleted(p) && !p.Eliminated)
            {
                if (!(p.HospitalPending && WouldEliminateInHospital(p)))
                {
                    End(p.Id, "victory");
                    return;
                }
            }
            if (p.HospitalPending && p.HospitalTurnsRemaining == 0 && !p.Eliminated)
            {
                StartHospital(p);
                if (State.Pending != null || State.Phase == V4Phase.GameEnd) return;
            }
            Continue();
        }

        private void Continue()
        {
            if (State.Phase == V4Phase.GameEnd) return;
            if (!string.IsNullOrEmpty(State.CloseStep) && State.CloseStep != "done") ContinueYearClose();
            else EndTurn();
        }

        private void End(string winnerId, string reason)
        {
            State.Phase = V4Phase.GameEnd;
            State.WinnerId = winnerId;
            State.EndReason = reason;
            State.Pending = null;
            Note(winnerId == null ? "Partida encerrada sem vencedor." : "Vitória de " + Player(winnerId).Name + ".");
        }

        private void BeginTurn()
        {
            if (State.Phase == V4Phase.GameEnd) return;
            State.Pending = null;
            State.Phase = V4Phase.AwaitRoll;
            var p = CurrentPlayer;
            if (p.Eliminated) { EndTurn(); return; }
            if (p.HospitalTurnsRemaining > 0)
            {
                p.HospitalTurnsRemaining -= 1;
                if (p.HospitalTurnsRemaining == 0)
                {
                    V4Attributes.Discharge(p);
                    Log(p, "hospital", "H10", null, p.Name + " recebeu alta (Toxina e Cortisol zerados); joga a partir do próximo turno.");
                }
                else Log(p, "hospital", "H10", null, p.Name + " perdeu o turno internado (" + p.HospitalTurnsRemaining + " restante[s]).");
                EndTurn();
            }
        }

        private void EndTurn()
        {
            if (State.Phase == V4Phase.GameEnd) return;
            if (State.RollAgain)
            {
                State.RollAgain = false;
                var again = CurrentPlayer;
                if (!again.Eliminated && again.HospitalTurnsRemaining == 0 && !again.HospitalPending && ActiveCount() > 0)
                {
                    State.Pending = null;
                    State.Phase = V4Phase.AwaitRoll;
                    Log(again, "roll_again", null, null, again.Name + " tirou dados iguais e joga novamente (extra " + State.ExtraRollsUsed + "/" + Rules.DoublesExtraRollsMax + ").");
                    return;
                }
            }
            State.ExtraRollsUsed = 0;
            var n = State.TurnOrder.Count;
            if (ActiveCount() == 0) { End(null, "all_eliminated"); return; }
            var wrapped = false;
            var guard = 0;
            do
            {
                State.CurrentIndex += 1;
                if (State.CurrentIndex >= n) { State.CurrentIndex = 0; wrapped = true; }
                guard++;
            }
            while (CurrentPlayer.Eliminated && guard <= n + 1);

            if (wrapped)
            {
                State.CompletedRounds += 1;
                State.RoundIndex += 1;
                Note("Rodada " + State.CompletedRounds + " concluída.");
                if (State.CompletedRounds % Rules.RoundsPerYear == 0)
                {
                    State.Phase = V4Phase.YearClose;
                    State.CloseStep = "salary";
                    State.CloseCursor = 0;
                    ContinueYearClose();
                    return;
                }
            }
            BeginTurn();
        }

        // ------------------------------------------------------------------ annual close (cursor persisted)

        private void ContinueYearClose()
        {
            State.Phase = V4Phase.YearClose;
            var n = State.TurnOrder.Count;
            if (State.CloseStep == "salary")
            {
                for (; State.CloseCursor < n; State.CloseCursor++)
                {
                    var p = Player(State.TurnOrder[State.CloseCursor]);
                    if (p.Eliminated) continue;
                    Receive(p, p.Salary, "salario", "ano" + State.YearIndex);
                    var line = YearLine(p);
                    line.Salary = p.Salary;
                    line.IncomeTaxDue = CeilPct(p.Salary, Difficulty.IncomeTaxPct);
                    Note(p.Name + " recebeu salário de " + p.Salary + " CRIA Coin.");
                }
                State.CloseStep = "tax";
                State.CloseCursor = 0;
            }
            if (State.CloseStep == "tax")
            {
                while (State.CloseCursor < n)
                {
                    var p = Player(State.TurnOrder[State.CloseCursor]);
                    State.CloseCursor += 1;
                    if (p.Eliminated) continue;
                    var tax = CeilPct(p.Salary, Difficulty.IncomeTaxPct);
                    OpenTaxChoice(p, "IR", "SYS_IR", tax);
                    return;
                }
                State.CloseStep = "settle";
                State.CloseCursor = 0;
            }
            if (State.CloseStep == "settle")
            {
                SettleYear();
                State.CloseStep = "done";
                State.CloseCursor = 0;
                State.Phase = V4Phase.AwaitRoll;
                BeginTurn();
            }
        }

        /// <summary>Summary line of the player for the year being closed (created on first use).</summary>
        private YearSummaryLine YearLine(PlayerState p)
        {
            var line = State.YearSummaries.FirstOrDefault(l => l.Year == State.YearIndex && l.PlayerId == p.Id);
            if (line != null) return line;
            line = new YearSummaryLine { Year = State.YearIndex, PlayerId = p.Id };
            State.YearSummaries.Add(line);
            return line;
        }

        private void RecordIncomeTax(PlayerState p, string houseId, string outcome, int paid)
        {
            if (houseId != "SYS_IR") return;
            var line = YearLine(p);
            line.IncomeTaxOutcome = outcome;
            line.IncomeTaxPaid += paid;
        }

        private void SettleYear()
        {
            foreach (var id in State.TurnOrder)
            {
                var p = Player(id);
                if (p.Eliminated) continue;
                var line = YearLine(p);
                foreach (var rc in p.Receivables.Where(r => !r.Paid && r.DueYear <= State.YearIndex).ToList())
                {
                    Receive(p, rc.TotalReturn, "retorno_parceria", rc.Id);
                    rc.Paid = true;
                    line.PartnershipReturns += rc.TotalReturn;
                    Note(p.Name + " recebeu retorno de parceria de " + rc.TotalReturn + " CRIA Coin.");
                }
                if (p.ReserveBalance > 0)
                {
                    var raw = (int)Math.Ceiling(p.ReserveBalance * Rules.ReserveAnnualRate);
                    var room = Math.Max(0, p.Salary - p.ReserveBalance);
                    var interest = Math.Min(raw, room);
                    p.ReserveBalance += interest;
                    line.ReserveInterest += interest;
                    if (interest > 0) Note(p.Name + " recebeu " + interest + " CRIA Coin de rendimento na reserva (passivo do banco).");
                }
            }
            foreach (var p in State.Players) p.AbilityUsedThisYear = false;
            State.History.Add(new HistoryEntry
            {
                Seq = State.History.Count + 1, Round = State.RoundIndex, Year = State.YearIndex,
                Kind = "year_close", Detail = "Fechamento do ano " + State.YearIndex + " | banco " + State.BankCash + " | cofre " + State.FundCash
            });
            State.YearIndex += 1;
        }
    }
}

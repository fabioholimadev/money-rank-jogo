using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MoneyRank.Domain.V4
{
    public sealed class V4Result
    {
        public bool Ok;
        public string Error;
        public bool Duplicate;
        public bool Liquidity;
        public readonly List<string> Events = new List<string>();
        public static V4Result Success() => new V4Result { Ok = true };
        public static V4Result Fail(string error) => new V4Result { Error = error };
    }

    public sealed class V4PlayerSetup
    {
        public string Id;
        public string Name;
        public string ProfessionId;
        public string Color;
    }

    public sealed class LiquidityException : Exception
    {
        public LiquidityException(string message) : base(message) { }
    }

    /// <summary>
    /// Authoritative V4 rules engine. Pure C# (no engine types), command-based and all-or-nothing:
    /// every public command runs on a snapshot and is rolled back completely on any failure.
    /// Physical dice/coin results are passed in (local master or authoritative online RNG).
    /// </summary>
    public sealed partial class V4Engine
    {
        public const string AbilityProfessora = "P01";
        public const string AbilityMedica = "P02";
        public const string AbilityEconomista = "P03";
        public const string AbilityAtleta = "P04";
        public const string AbilityEmpreendedora = "P05";
        public const string AbilityMotorista = "P06";
        public static readonly string[] Colors = { "verde", "vermelho", "azul", "amarelo" };

        public V4ContentSet Content { get; }
        public V4State State { get; private set; }
        private V4Result _current;
        // In-memory undo stack (local master correction only). Not persisted: a reload clears the ability to undo.
        private readonly List<V4State> _snapshots = new List<V4State>();
        private const int MaxUndoSnapshots = 60;
        public bool CanUndo => _snapshots.Count > 0;

        public V4Engine(V4ContentSet content, V4State state = null)
        {
            Content = content ?? throw new ArgumentNullException(nameof(content));
            State = state ?? new V4State();
        }

        public RulesParameters Rules => Content.Rules;
        public DifficultyDefinition Difficulty => Content.Difficulty(State.Config.DifficultyId);
        public PlayerState Player(string id) => State.Players.First(p => p.Id == id);
        public PlayerState CurrentPlayer => State.TurnOrder.Count == 0 ? null : Player(State.TurnOrder[State.CurrentIndex]);
        public ProfessionDefinition ProfessionOf(PlayerState p) => Content.Profession(p.ProfessionId);
        public bool VehicleActive(PlayerState p) => p.HasVehicle && State.CompletedRounds < p.VehicleExpiresAfterRound;
        public int ActiveCount() => State.Players.Count(p => !p.Eliminated);

        /// <summary>Identity that must hold after every confirmation: bank + cofre + wallets = physical total.</summary>
        public int TotalCash() => State.BankCash + State.FundCash + State.Players.Sum(p => p.Wallet);

        public int Capacity(PlayerState p) => p.Wallet + Math.Min(p.ReserveBalance, State.BankCash);
        public static int CeilPct(int amount, int pct) => (amount * pct + 99) / 100;

        // ------------------------------------------------------------------ atomic command runner

        private V4Result Run(string commandId, Func<V4Result> body, bool allowWhileBlocked = false, bool recordUndo = true)
        {
            if (!string.IsNullOrEmpty(commandId) && State.ProcessedCommands.Contains(commandId))
                return new V4Result { Ok = true, Duplicate = true };
            if (State.LiquidityBlocked && !allowWhileBlocked)
                return V4Result.Fail("Partida pausada por falta de caixa/troco no banco: " + State.LiquidityNote);
            var backup = StateClone.Clone(State);
            V4Result result;
            try
            {
                _current = new V4Result { Ok = true };
                var r = body();
                result = r ?? _current;
                if (r != null && r != _current) result.Events.InsertRange(0, _current.Events);
            }
            catch (LiquidityException ex)
            {
                State = backup;
                State.LiquidityBlocked = true;
                State.LiquidityNote = ex.Message;
                return new V4Result { Ok = false, Liquidity = true, Error = ex.Message };
            }
            catch (Exception ex)
            {
                State = backup;
                return V4Result.Fail(ex.Message);
            }
            if (!result.Ok) { State = backup; return result; }
            if (!string.IsNullOrEmpty(commandId)) State.ProcessedCommands.Add(commandId);
            if (recordUndo)
            {
                _snapshots.Add(backup);
                if (_snapshots.Count > MaxUndoSnapshots) _snapshots.RemoveAt(0);
            }
            if (TotalCash() != Rules.PhysicalMoneyTotal)
            {
                var broken = TotalCash();
                State = backup;
                return V4Result.Fail("Invariante de caixa violada (" + broken + " != " + Rules.PhysicalMoneyTotal + "); comando revertido.");
            }
            return result;
        }

        private void Note(string text) => _current?.Events.Add(text);

        private void Log(PlayerState p, string kind, string houseId, string optionId, string detail)
        {
            State.History.Add(new HistoryEntry
            {
                Seq = State.History.Count + 1, Round = State.RoundIndex, Year = State.YearIndex,
                PlayerId = p?.Id, Kind = kind, HouseId = houseId, OptionId = optionId, Detail = detail
            });
            Note(detail ?? kind);
        }

        // ------------------------------------------------------------------ setup

        public V4Result Setup(IList<V4PlayerSetup> players, V4Config config, string sessionId = null)
        {
            if (State.Phase != V4Phase.Setup || State.Players.Count > 0) return V4Result.Fail("A partida já foi iniciada.");
            if (players == null) return V4Result.Fail("Jogadores ausentes.");
            var min = config != null && config.SoloDemo ? 1 : Rules.PlayersMin;
            if (players.Count < min || players.Count > Rules.PlayersMax)
                return V4Result.Fail("Número de jogadores inválido (" + min + "–" + Rules.PlayersMax + ").");
            if (players.Select(x => x.Id).Distinct().Count() != players.Count || players.Any(x => string.IsNullOrEmpty(x.Id)))
                return V4Result.Fail("IDs de jogador duplicados ou vazios.");
            if (players.Select(x => x.ProfessionId).Distinct().Count() != players.Count)
                return V4Result.Fail("Profissão repetida não é permitida.");
            if (players.Any(x => !Content.Professions.Any(pr => pr.Id == x.ProfessionId)))
                return V4Result.Fail("Profissão desconhecida.");
            if (players.Select(x => x.Color).Distinct().Count() != players.Count || players.Any(x => !Colors.Contains(x.Color)))
                return V4Result.Fail("Cores de peão inválidas ou repetidas.");
            var cfg = config ?? new V4Config();
            if (!Content.Difficulties.Any(d => d.Id == cfg.DifficultyId)) return V4Result.Fail("Dificuldade desconhecida.");

            var s = new V4State
            {
                RulesVersion = Rules.RulesVersion,
                ContentHash = Content.ContentHash,
                SessionId = sessionId ?? Guid.NewGuid().ToString("D"),
                Config = cfg
            };
            foreach (var ps in players)
            {
                var pr = Content.Profession(ps.ProfessionId);
                var p = new PlayerState
                {
                    Id = ps.Id, Name = string.IsNullOrEmpty(ps.Name) ? ps.Id : ps.Name, ProfessionId = ps.ProfessionId, Color = ps.Color,
                    Position = 0, Wallet = Rules.StartingWallet, Salary = pr.Salary, Level = 0
                };
                for (var i = 0; i < p.Pawns.Length; i++) p.Pawns[i] = new PawnState();
                for (var i = 0; i < p.Attributes.Length; i++) p.Attributes[i] = new AttributeState();
                p.ActivePawn = 0;
                s.Players.Add(p);
                s.TurnOrder.Add(p.Id);
            }
            s.FundCash = Rules.StartingPublicFund;
            s.BankCash = Rules.PhysicalMoneyTotal - players.Count * Rules.StartingWallet - s.FundCash;
            State = s;
            State.Phase = V4Phase.AwaitRoll;
            BeginTurn();
            return V4Result.Success();
        }

        // ------------------------------------------------------------------ money

        private int Balance(string account)
        {
            if (account == "bank") return State.BankCash;
            if (account == "fund") return State.FundCash;
            return Player(account.Substring(2)).Wallet;
        }

        private void SetBalance(string account, int value)
        {
            if (account == "bank") State.BankCash = value;
            else if (account == "fund") State.FundCash = value;
            else Player(account.Substring(2)).Wallet = value;
        }

        private void Transfer(string from, string to, int amount, string reason, string reference)
        {
            if (amount < 0) throw new InvalidOperationException("Valor negativo em transferência.");
            if (amount == 0) return;
            var fromBefore = Balance(from);
            if (fromBefore < amount)
            {
                if (from == "bank") throw new LiquidityException("Banco sem caixa/troco para pagar " + amount + " CRIA Coin (" + reason + ").");
                throw new InvalidOperationException("Saldo insuficiente em " + from + " para " + amount + " CRIA Coin (" + reason + ").");
            }
            var toBefore = Balance(to);
            SetBalance(from, fromBefore - amount);
            SetBalance(to, toBefore + amount);
            State.Ledger.Add(new LedgerEntry
            {
                Seq = State.NextLedgerNumber++, Key = "L" + State.Ledger.Count, Round = State.RoundIndex, Year = State.YearIndex,
                From = from, To = to, Amount = amount, Reason = reason, Ref = reference, Operator = "system",
                FromBefore = fromBefore, FromAfter = fromBefore - amount, ToBefore = toBefore, ToAfter = toBefore + amount
            });
        }

        private void Redeem(PlayerState p, int amount)
        {
            if (amount <= 0) return;
            if (amount > p.ReserveBalance) throw new InvalidOperationException("Resgate acima do saldo da reserva.");
            Transfer("bank", "w:" + p.Id, amount, "resgate_reserva", null);
            p.ReserveBalance -= amount;
            p.ReservePrincipal = Math.Max(0, p.ReservePrincipal - amount);
        }

        private void EnsureWallet(PlayerState p, int amount)
        {
            if (p.Wallet >= amount) return;
            Redeem(p, amount - p.Wallet);
        }

        private void Pay(PlayerState p, int amount, string destination, string reason, string reference)
        {
            if (amount <= 0) return;
            EnsureWallet(p, amount);
            Transfer("w:" + p.Id, destination, amount, reason, reference);
        }

        private void Receive(PlayerState p, int amount, string reason, string reference)
        {
            if (amount <= 0) return;
            Transfer("bank", "w:" + p.Id, amount, reason, reference);
        }

        /// <summary>Master command: redeem from reserve at any time during own turn, no time cost, no fee.</summary>
        public V4Result RedeemReserve(string commandId, string playerId, int amount)
        {
            return Run(commandId, () =>
            {
                var p = Player(playerId);
                if (p.Eliminated) return V4Result.Fail("Jogador eliminado.");
                if (amount <= 0 || amount > p.ReserveBalance) return V4Result.Fail("Valor de resgate inválido.");
                Redeem(p, amount);
                Log(p, "redeem", null, null, p.Name + " resgatou " + amount + " CRIA Coin da reserva.");
                return V4Result.Success();
            });
        }

        public V4Result ClearLiquidityBlock(string commandId, string note)
        {
            return Run(commandId, () =>
            {
                State.LiquidityBlocked = false;
                State.LiquidityNote = null;
                Log(null, "liquidity_cleared", null, null, "Ajuste físico de troco registrado: " + note);
                return V4Result.Success();
            }, allowWhileBlocked: true, recordUndo: false);
        }

        /// <summary>
        /// Local master correction: restores the snapshot taken before the last confirmed command. History and ledger are never
        /// erased: reverted entries stay, flagged Undone/Reversed, so analytics can ignore them. Forbidden for online players.
        /// The caller must resend the coffer balance after an undo (CofferDisplaySync.Resync).
        /// </summary>
        public V4Result Undo(string commandId, string reason)
        {
            return Run(commandId, () =>
            {
                if (State.Config.Mode == "online") return V4Result.Fail("Undo competitivo não é permitido no modo online.");
                if (_snapshots.Count == 0) return V4Result.Fail("Nada para desfazer.");
                var target = _snapshots[_snapshots.Count - 1];
                _snapshots.RemoveAt(_snapshots.Count - 1);
                var undoneLedger = State.Ledger.Skip(target.Ledger.Count).ToList();
                var undoneHistory = State.History.Skip(target.History.Count).ToList();
                var next = StateClone.Clone(target);
                foreach (var l in undoneLedger) { l.Reversed = true; next.Ledger.Add(l); }
                foreach (var h in undoneHistory) { h.Undone = true; next.History.Add(h); }
                next.ProcessedCommands = State.ProcessedCommands.ToList();
                next.Rng = State.Rng.ToList();
                next.NextLedgerNumber = State.NextLedgerNumber;
                next.NextDecisionNumber = State.NextDecisionNumber;
                State = next;
                Log(null, "undo", null, null, "Ação desfeita pelo mestre: " + (string.IsNullOrEmpty(reason) ? "sem justificativa" : reason) + ".");
                return V4Result.Success();
            }, allowWhileBlocked: true, recordUndo: false);
        }

        // ------------------------------------------------------------------ cycles -> pawn concessions

        /// <summary>Applies base deltas (money must already be settled) and queues pawn concessions.</summary>
        private AttributeOutcome ApplyEffects(PlayerState p, decimal[] deltas)
        {
            var outcome = V4Attributes.Apply(p, deltas, ProfessionOf(p), Difficulty, Rules);
            for (var k = 3; k <= 5; k++) for (var i = 0; i < outcome.Cycles[k]; i++) State.CycleQueue.Add("R");
            for (var k = 0; k <= 2; k++) for (var i = 0; i < outcome.Cycles[k]; i++) State.CycleQueue.Add("A:" + k);
            return outcome;
        }

        private static decimal[] DeltasOf(OptionDefinition o)
        {
            var d = new decimal[AttributeIds.Count];
            for (var i = 0; i < d.Length; i++) d[i] = o.Deltas[i];
            return d;
        }

        private static decimal[] OneDelta(AttributeId a, decimal value)
        {
            var d = new decimal[AttributeIds.Count];
            d[(int)a] = value;
            return d;
        }

        /// <summary>Drains queued concessions in order. Returns false if suspended on an ability decision.</summary>
        private bool Drain(PlayerState p)
        {
            while (State.CycleQueue.Count > 0)
            {
                var item = State.CycleQueue[0];
                if (item == "R")
                {
                    State.CycleQueue.RemoveAt(0);
                    var back = 0;
                    for (var i = 0; i < Math.Max(1, Rules.NegativeCycleSteps); i++)
                        if (V4Ludo.Retreat(p).Moved) back++;
                    if (back > 0) Note(p.Name + " recuou " + back + (back == 1 ? " casa interna." : " casas internas."));
                    continue;
                }
                var k = int.Parse(item.Substring(2), CultureInfo.InvariantCulture);
                var steps = NormalAdvanceSteps(p);
                var auto = false;
                if (!p.AbilityUsedThisYear)
                {
                    if (p.ProfessionId == AbilityProfessora && k == (int)AttributeId.Conhecimento) auto = true;
                    if (p.ProfessionId == AbilityEconomista && k == (int)AttributeId.Investimento) auto = true;
                    if (p.ProfessionId == AbilityAtleta)
                    {
                        if (!Rules.AthleteAbilityAutomatic)
                        {
                            OpenAthleteChoice(p);
                            return false;
                        }
                        auto = true;
                    }
                }
                if (auto)
                {
                    steps = Math.Max(steps, DoubledAdvanceSteps());
                    p.AbilityUsedThisYear = true;
                    var prof = ProfessionOf(p);
                    Log(p, "ability", null, null, prof.Ability + " (" + prof.Name + "): habilidade anual aplicada automaticamente — " + p.Name + " avança " + Math.Min(steps, Rules.MaxStepsPerPositiveCycle) + " casas nesta barra.");
                }
                steps = Math.Min(steps, Rules.MaxStepsPerPositiveCycle);
                State.CycleQueue.RemoveAt(0);
                DoAdvance(p, steps);
            }
            return true;
        }

        /// <summary>Cells advanced by one positive bar: base steps, multiplied while the vehicle is active.</summary>
        private int NormalAdvanceSteps(PlayerState p)
        {
            var steps = Math.Max(1, Rules.PositiveCycleSteps);
            return VehicleActive(p) ? steps * Math.Max(1, Rules.VehicleAdvanceMultiplier) : steps;
        }

        /// <summary>Doubling abilities ("avança o dobro"): twice the base steps; never stacks with the vehicle (capped by max).</summary>
        private int DoubledAdvanceSteps() => Math.Max(1, Rules.PositiveCycleSteps) * 2;

        private void DoAdvance(PlayerState p, int steps)
        {
            var r = V4Ludo.Advance(Content, p, State.Players, steps);
            if (!r.Moved) return;
            Note(p.Name + " avançou no Ludo até " + r.ToIndex + (r.PawnCompleted ? " (peão concluído)" : string.Empty) + ".");
            foreach (var id in r.CapturedPlayerIds) Note(Player(id).Name + " teve um peão capturado.");
        }

        private void OpenAthleteChoice(PlayerState p)
        {
            var d = NewDecision(DecisionKind.AbilityChoice, p, null);
            d.Tag = "athlete";
            d.OptionIds.Add("advance2");
            foreach (var a in new[] { AttributeId.Toxina, AttributeId.Cortisol, AttributeId.Dividas })
                if (p.Attributes[(int)a].Progress > 0m) d.OptionIds.Add("reduce:" + AttributeIds.Keys[(int)a]);
            d.OptionIds.Add("decline");
            State.Pending = d;
            State.Phase = V4Phase.AwaitDecision;
        }

        public V4Result ChooseAbility(string commandId, string decisionId, string choice)
        {
            return Run(commandId, () =>
            {
                var d = State.Pending;
                if (d == null || d.Kind != DecisionKind.AbilityChoice || d.Id != decisionId) return V4Result.Fail("Decisão inválida ou desatualizada.");
                if (!d.OptionIds.Contains(choice)) return V4Result.Fail("Opção indisponível.");
                var p = Player(d.PlayerId);
                State.Pending = null;
                if (choice == "decline") { State.CycleQueue.RemoveAt(0); DoAdvance(p, Math.Min(NormalAdvanceSteps(p), Rules.MaxStepsPerPositiveCycle)); }
                else if (choice == "advance2")
                {
                    State.CycleQueue.RemoveAt(0);
                    p.AbilityUsedThisYear = true;
                    var doubled = Math.Min(Math.Max(NormalAdvanceSteps(p), DoubledAdvanceSteps()), Rules.MaxStepsPerPositiveCycle);
                    DoAdvance(p, doubled);
                    Note("Fôlego Extra: avanço de " + doubled + " casas.");
                }
                else
                {
                    var key = choice.Substring("reduce:".Length);
                    var a = (AttributeId)Array.IndexOf(AttributeIds.Keys, key);
                    State.CycleQueue.RemoveAt(0);
                    p.AbilityUsedThisYear = true;
                    V4Attributes.Apply(p, OneDelta(a, -1m), ProfessionOf(p), Difficulty, Rules);
                    DoAdvance(p, Math.Min(NormalAdvanceSteps(p), Rules.MaxStepsPerPositiveCycle));
                    Note("Fôlego Extra: redução de 1 ponto-base de " + key + ".");
                }
                State.Phase = V4Phase.AwaitRoll;
                Resolve(p);
                return V4Result.Success();
            });
        }

        // ------------------------------------------------------------------ decisions

        private PendingDecision NewDecision(string kind, PlayerState p, string houseId)
        {
            return new PendingDecision { Id = "D" + State.NextDecisionNumber++, Kind = kind, PlayerId = p.Id, HouseId = houseId };
        }
    }
}

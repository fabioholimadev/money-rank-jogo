using System.Collections.Generic;
using System.Linq;

namespace MoneyRank.Domain.V4
{
    /// <summary>
    /// Players joining or leaving mid-match (playtest 27/11/2026, approved by the user):
    /// a new player starts from zero (starting wallet from the bank, H01, pawns at base, initial salary), plays last in the
    /// order and may only join at the start of a year (join_only_at_year_start); a leaving player closes the account
    /// (wallet back to the bank, reserve and partnerships cancelled, property/vehicle/checkpoints undone, pieces removed).
    /// </summary>
    public sealed partial class V4Engine
    {
        private static readonly HashSet<string> TurnActionKinds = new HashSet<string> { "move", "ability", "hospital", "roll_again" };

        private int MinimumActivePlayers => State.Config.SoloDemo ? 1 : Rules.PlayersMin;

        private int FirstActiveIndex()
        {
            for (var i = 0; i < State.TurnOrder.Count; i++)
                if (!Player(State.TurnOrder[i]).Eliminated) return i;
            return -1;
        }

        /// <summary>True when no one has played yet in the current year (first player of the year still to roll).</summary>
        public bool IsYearStart()
        {
            if (State.CompletedRounds % Rules.RoundsPerYear != 0) return false;
            if (State.CurrentIndex != FirstActiveIndex() || State.ExtraRollsUsed > 0) return false;
            return !State.History.Any(h => !h.Undone && h.Round == State.RoundIndex && TurnActionKinds.Contains(h.Kind));
        }

        /// <summary>Null when a player may join now; otherwise the reason shown to the master.</summary>
        public string JoinBlockReason()
        {
            if (State.Phase != V4Phase.AwaitRoll || State.Pending != null) return "Só é possível adicionar jogador entre turnos, sem decisão pendente.";
            if (ActiveCount() >= Rules.PlayersMax) return "A partida já tem o máximo de " + Rules.PlayersMax + " jogadores ativos.";
            if (Rules.JoinOnlyAtYearStart && !IsYearStart())
                return "Novos jogadores só entram no início de um ano (antes do primeiro lançamento do ano " + State.YearIndex + (State.CompletedRounds % Rules.RoundsPerYear == 0 ? string.Empty : "; próximo: ano " + (State.YearIndex + 1)) + ").";
            return null;
        }

        /// <summary>Professions and colors still free among the active players (for the join form).</summary>
        public IReadOnlyList<string> FreeProfessions() =>
            Content.Professions.Select(x => x.Id).Where(id => !State.Players.Any(p => !p.Eliminated && p.ProfessionId == id)).ToList();

        public IReadOnlyList<string> FreeColors() =>
            Colors.Where(c => !State.Players.Any(p => !p.Eliminated && p.Color == c)).ToList();

        public V4Result AddPlayer(string commandId, V4PlayerSetup setup)
        {
            return Run(commandId, () =>
            {
                var block = JoinBlockReason();
                if (block != null) return V4Result.Fail(block);
                if (setup == null || string.IsNullOrEmpty(setup.ProfessionId) || string.IsNullOrEmpty(setup.Color)) return V4Result.Fail("Informe profissão e cor.");
                if (!FreeProfessions().Contains(setup.ProfessionId)) return V4Result.Fail("Profissão indisponível (já em uso por um jogador ativo).");
                if (!FreeColors().Contains(setup.Color)) return V4Result.Fail("Cor indisponível (já em uso por um jogador ativo).");

                var id = setup.Id;
                if (string.IsNullOrEmpty(id) || State.Players.Any(x => x.Id == id))
                {
                    var n = State.Players.Count + 1;
                    while (State.Players.Any(x => x.Id == "p" + n)) n++;
                    id = "p" + n;
                }
                var pr = Content.Profession(setup.ProfessionId);
                var p = new PlayerState
                {
                    Id = id, Name = string.IsNullOrWhiteSpace(setup.Name) ? id : setup.Name.Trim(), ProfessionId = pr.Id, Color = setup.Color,
                    Position = 0, Wallet = 0, Salary = pr.Salary, Level = 0, ActivePawn = 0, JoinedRound = State.RoundIndex
                };
                for (var i = 0; i < p.Pawns.Length; i++) p.Pawns[i] = new PawnState();
                for (var i = 0; i < p.Attributes.Length; i++) p.Attributes[i] = new AttributeState();
                State.Players.Add(p);
                State.TurnOrder.Add(p.Id);
                Transfer("bank", "w:" + p.Id, Rules.StartingWallet, "entrada_jogador", p.Id);
                Log(p, "join", null, null, p.Name + " (" + pr.Name + ") entrou na partida no início do ano " + State.YearIndex + " com " + Rules.StartingWallet + " CRIA Coin; joga por último na ordem.");
                return V4Result.Success();
            });
        }

        /// <summary>Null when the player may leave now; otherwise the reason.</summary>
        public string LeaveBlockReason(string playerId)
        {
            var p = State.Players.FirstOrDefault(x => x.Id == playerId);
            if (p == null) return "Jogador desconhecido.";
            if (p.Eliminated) return p.Left ? "Este jogador já saiu da partida." : "Jogador eliminado.";
            if (State.Phase != V4Phase.AwaitRoll || State.Pending != null) return "Só é possível retirar jogador entre turnos, sem decisão pendente.";
            if (ActiveCount() - 1 < MinimumActivePlayers) return "A partida precisa de pelo menos " + MinimumActivePlayers + " jogadores ativos.";
            return null;
        }

        public V4Result RemovePlayer(string commandId, string playerId, string reason)
        {
            return Run(commandId, () =>
            {
                var block = LeaveBlockReason(playerId);
                if (block != null) return V4Result.Fail(block);
                var p = Player(playerId);
                var wasCurrent = CurrentPlayer != null && CurrentPlayer.Id == p.Id;
                var wallet = p.Wallet;
                // Account closure: coins go back to the bank; reserve and partnerships are cancelled bank liabilities.
                Transfer("w:" + p.Id, "bank", p.Wallet, "saida_jogador", p.Id);
                p.ReserveBalance = 0; p.ReservePrincipal = 0;
                p.Receivables.Clear();
                p.HasVehicle = false; p.VehicleExpiresAfterRound = 0; p.Properties = 0;
                p.CheckpointRights = 0; p.Checkpoints.Clear();
                foreach (var pawn in p.Pawns) { pawn.RouteIndex = -1; pawn.MaxIndex = -1; }
                p.HospitalTurnsRemaining = 0; p.HospitalPending = false;
                p.Left = true;
                p.Eliminated = true;
                Log(p, "leave", null, null, p.Name + " saiu da partida" + (string.IsNullOrWhiteSpace(reason) ? string.Empty : " (" + reason.Trim() + ")") +
                    ": " + wallet + " CRIA Coin devolvidos ao banco; reserva, parcerias, imóveis, carro e checkpoints encerrados; peças retiradas do tabuleiro.");
                if (wasCurrent)
                {
                    State.RollAgain = false;
                    EndTurn();
                }
                return V4Result.Success();
            });
        }
    }
}

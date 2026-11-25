using System;
using System.Collections.Generic;

namespace MoneyRank.Domain.V4
{
    // All state classes use ONLY public fields of: int, decimal, bool, string, enums, arrays/lists of these,
    // and nested state classes. This keeps deep-clone (atomic rollback) and JSON save/restore generic and exact.

    public enum V4Phase
    {
        Setup = 0,
        AwaitRoll = 10,        // player's turn: roll physical die (or use alternative ability)
        AwaitDecision = 20,    // a PendingDecision must be answered (choice, coin, hospital modality, ability)
        YearClose = 30,        // annual close in progress (cursor persisted)
        GameEnd = 90
    }

    public static class FiscalStatus
    {
        public const string Sonegado = "sonegado";
        public const string Inadimplente = "inadimplente";
        public const string Autuado = "autuado";
        public const string Regularizado = "regularizado";
    }

    public static class DecisionKind
    {
        public const string Choice = "choice";                 // pick one of OptionIds
        public const string TaxCoin = "tax_coin";              // evasion coin flip (FELIZ/TRISTE)
        public const string FiscalCoin = "fiscal_coin";        // audit coin flip
        public const string HospitalModality = "hospital";     // public / private
        public const string AbilityChoice = "ability";         // Atleta / Empreendedora style choices
        public const string LiquidityBlocked = "liquidity";    // master must fix physical change; nothing applied
    }

    public sealed class V4Config
    {
        public string DifficultyId = "B1_medio";
        public string Mode = "local";           // local | online
        public bool SoloDemo;
        public bool VoluntaryTaxCitizenship = true;
        public bool FiscalPrivateToMaster = true;   // FIS07: local = master only; online = public
        public bool EmpreendedoraSwapEnabled = true;
    }

    public sealed class AttributeState
    {
        public decimal Progress;        // current bar, 0 <= Progress < 3 (exact decimal, never float)
        public int LifetimeCycles;
        public int HealthCyclesSinceDischarge;
    }

    public sealed class PawnState
    {
        public int RouteIndex = -1;     // -1 base, 0..goal; goal = completed
        public int MaxIndex = -1;       // furthest index ever reached (checkpoint eligibility)
        public bool Completed;
    }

    public sealed class FiscalRecord
    {
        public string Id;
        public string Kind;             // IR | IPTU | IPVA | ISS | ICMS | FINE
        public int SourceYear;
        public int Principal;
        public int Outstanding;         // amount due if audited (autuado keeps the computed value)
        public string Status;
    }

    public sealed class Receivable
    {
        public string Id;
        public int Investment;
        public int TotalReturn;
        public int DueYear;
        public bool Paid;
    }

    public sealed class PlayerState
    {
        public string Id;
        public string Name;
        public string ProfessionId;
        public string Color;                    // verde | vermelho | azul | amarelo (pawn color)
        public int Position;                    // external board 0..35
        public PawnState[] Pawns = new PawnState[4];
        public int ActivePawn;                  // index of the pawn currently in play (first not completed)
        public int CheckpointRights;
        public List<int> Checkpoints = new List<int>();
        public int Wallet;
        public int ReserveBalance;              // bank liability, not physical cash
        public int ReservePrincipal;            // net principal deposited
        public int ReserveRecord;               // historical max net principal
        public int Salary;                      // current annual salary
        public int Level;                       // 0..4
        public bool HasVehicle;
        public int VehicleExpiresAfterRound;    // vehicle active while completedRounds < this value
        public int Properties;
        public AttributeState[] Attributes = new AttributeState[AttributeIds.Count];
        public bool AbilityUsedThisYear;
        public int HospitalTurnsRemaining;
        public bool HospitalPending;            // internment pending after current resolution
        public bool Eliminated;
        public List<FiscalRecord> Fiscal = new List<FiscalRecord>();
        public List<Receivable> Receivables = new List<Receivable>();
        public string Nick;
    }

    public sealed class LedgerEntry
    {
        public int Seq;
        public string Key;              // idempotency key
        public int Round;
        public int Year;
        public string From;             // bank | fund | w:<playerId>
        public string To;
        public int Amount;
        public string Reason;
        public string Ref;              // option / tax / event reference
        public string Operator;         // master | system | online
        public int FromBefore, FromAfter, ToBefore, ToAfter;
        public bool Reversed;
    }

    public sealed class PendingDecision
    {
        public string Id;               // unique per decision (idempotency / stale-click guard)
        public string Kind;
        public string PlayerId;
        public string HouseId;
        public List<string> OptionIds = new List<string>();
        public List<string> BlockedOptionIds = new List<string>();
        public List<string> BlockedReasons = new List<string>();
        public bool Delegated;          // H34 sub-flow
        public bool Mandatory;          // cannot dismiss (essential expenses)
        public string Context;          // free key=value payload for sub-steps (typed by Kind)
        public int Amount;
        public int Amount2;
        public int Amount3;
        public string Tag;
    }

    public sealed class RngRecord
    {
        public string DecisionId;
        public string Kind;             // die | coin
        public string Value;
    }

    public sealed class HistoryEntry
    {
        public int Seq;
        public int Round;
        public int Year;
        public string PlayerId;
        public string Kind;
        public string HouseId;
        public string OptionId;
        public string Detail;
        public bool Undone;
    }

    public sealed class V4State
    {
        public int SchemaVersion = 1;
        public string RulesVersion;
        public string ContentHash;
        public string SessionId;
        public V4Config Config = new V4Config();
        public V4Phase Phase = V4Phase.Setup;
        public int RoundIndex = 1;              // 1-based
        public int CompletedRounds;
        public int YearIndex = 1;               // 1-based
        public int CurrentIndex;                // index into TurnOrder
        public List<string> TurnOrder = new List<string>();
        public List<PlayerState> Players = new List<PlayerState>();
        public int BankCash;
        public int FundCash;                    // cofre publico
        public List<LedgerEntry> Ledger = new List<LedgerEntry>();
        public PendingDecision Pending;         // null when none
        public List<RngRecord> Rng = new List<RngRecord>();
        public List<HistoryEntry> History = new List<HistoryEntry>();
        public List<string> ProcessedCommands = new List<string>();
        public int[] CoinInventory = new int[6];  // optional physical inventory (1,5,10,50,100,500)
        public string WinnerId;
        public string EndReason;
        // year close cursor (persisted so a reload never pays twice)
        public string CloseStep;                // salary | tax | settle | done
        public int CloseCursor;
        public int NextDecisionNumber = 1;
        public int NextLedgerNumber = 1;
        public bool LiquidityBlocked;
        public string LiquidityNote;
        /// <summary>Pending pawn concessions of the action being resolved ("R" retreat, "A:k" advance by attribute k).</summary>
        public List<string> CycleQueue = new List<string>();
        public string LastDieValue;
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MoneyRank.Domain.V4
{
    /// <summary>The six attributes, in the stable order used by every V4 CSV column group.</summary>
    public enum AttributeId
    {
        Conhecimento = 0,
        Investimento = 1,
        Cidadania = 2,
        Toxina = 3,
        Cortisol = 4,
        Dividas = 5
    }

    public static class AttributeIds
    {
        public const int Count = 6;
        public static readonly string[] Keys = { "conhecimento", "investimento", "cidadania", "toxina", "cortisol", "dividas" };
        public static bool IsPositive(AttributeId id) => (int)id <= 2;
        public static bool IsHealth(AttributeId id) => id == AttributeId.Toxina || id == AttributeId.Cortisol;
    }

    public sealed class HouseDefinition
    {
        public string Id; public int Position; public int UnityIndex; public string Sector; public string Title;
        public string Category; public string ColorHex; public string Handler; public string Icon; public string Text; public string Status;
    }

    public sealed class OptionDefinition
    {
        public string Id; public string HouseId; public int Order; public string Title; public string Text;
        public int Cost; public int RewardFixed; public int RewardPctSalary;
        /// <summary>Signed base points in <see cref="AttributeId"/> order.</summary>
        public int[] Deltas = new int[AttributeIds.Count];
        public string Action; public string Condition;
        public Dictionary<string, object> Params = new Dictionary<string, object>(StringComparer.Ordinal);
        public string Status; public string Art;

        public int ParamInt(string key, int fallback = 0)
        {
            object v;
            if (Params.TryGetValue(key, out v) && v is decimal d) return (int)d;
            return fallback;
        }

        public string ParamString(string key, string fallback = null)
        {
            object v;
            if (Params.TryGetValue(key, out v) && v != null) return v is decimal d ? d.ToString(CultureInfo.InvariantCulture) : v.ToString();
            return fallback;
        }
    }

    public sealed class ProfessionDefinition
    {
        public string Id; public string Name; public int Salary;
        /// <summary>Multipliers in <see cref="AttributeId"/> order.</summary>
        public decimal[] Multipliers = new decimal[AttributeIds.Count];
        public string Ability; public string AbilityText; public string Frequency; public string Status;
    }

    public sealed class ProgressionCard
    {
        public string Id; public string ProfessionId; public string Profession; public int Step; public string Title;
        public int CostPctOfInitialSalary; public int Cost; public int RaisePctOfInitialSalary; public int SalaryAfter;
        public int Conhecimento; public int Investimento; public int Cidadania; public string Text; public string Art; public string Status;
        public int PositiveTotal => Conhecimento + Investimento + Cidadania;
    }

    public sealed class DifficultyDefinition
    {
        public string Id; public int Battery; public string Difficulty;
        public decimal PositiveMultiplier; public decimal NegativeMultiplier; public int IncomeTaxPct;
        public int PublicHospitalCost; public int PublicHospitalTurns; public int PrivateHospitalCost; public int PrivateHospitalTurns;
        public int EmergencyTurns; public int EmergencyDebtBase; public bool EliminateWithoutResources; public string Status;

        public decimal MultiplierFor(AttributeId a) => AttributeIds.IsPositive(a) ? PositiveMultiplier : NegativeMultiplier;
    }

    public sealed class AttributeDefinition
    {
        public string Id; public bool Positive; public int Threshold; public int BarMin; public string CycleEffect;
        public int HospitalAfterCycles; public bool ResetOnDischarge;
    }

    public sealed class CoinDefinition { public int Denomination; public int Quantity; public int Total; }

    public sealed class SystemEventDefinition
    {
        public string Id; public string Title; public string Text; public string Action;
        public Dictionary<string, object> Params; public string Art;
    }

    public sealed class RouteCell
    {
        public string Color; public int Index; public int Row; public int Col; public string GlobalCell; public string Kind; public bool Protected;
        public bool IsShared => Kind == "comum";
        public bool IsGoal => Kind == "objetivo";
    }

    /// <summary>Typed accessor over parametros.json (flat key/value, complemento values are configurable).</summary>
    public sealed class RulesParameters
    {
        private readonly Dictionary<string, object> _raw;
        public RulesParameters(Dictionary<string, object> raw) { _raw = raw ?? new Dictionary<string, object>(StringComparer.Ordinal); }
        public IReadOnlyDictionary<string, object> Raw => _raw;

        public int Int(string key, int fallback = 0) { object v; return _raw.TryGetValue(key, out v) && v is decimal d ? (int)d : fallback; }
        public decimal Decimal(string key, decimal fallback = 0m) { object v; return _raw.TryGetValue(key, out v) && v is decimal d ? d : fallback; }
        public bool Bool(string key, bool fallback = false) { object v; return _raw.TryGetValue(key, out v) && v is bool b ? b : fallback; }
        public string String(string key, string fallback = null) { object v; return _raw.TryGetValue(key, out v) && v is string s ? s : fallback; }

        public string RulesVersion => String("rules_version", "unknown");
        public int StartingWallet => Int("starting_wallet", 500);
        public int StartingPublicFund => Int("starting_public_fund", 0);
        public int PhysicalMoneyTotal => Int("physical_money_total", 28260);
        public int PlayersMin => Int("players_min", 2);
        public int PlayersMax => Int("players_max", 4);
        public int RoundsPerYear => Int("rounds_per_year", 3);
        public int DieFaces => Int("die_faces", 6);
        /// <summary>Dice rolled for movement (2 since the user-approved dice rule); movement is their sum.</summary>
        public int DiceCount => Int("dice_count", 1);
        /// <summary>Extra rolls granted by doubles in one turn (user rule: at most 3, then the turn passes).</summary>
        public int DoublesExtraRollsMax => Int("doubles_extra_rolls_max", 0);
        public int AttributeThreshold => Int("attribute_threshold", 3);
        public int HealthCyclesToHospital => Int("health_cycles_to_hospital", 3);
        public bool MultiplyReductions => Bool("multiply_reductions", false);
        public int MaxStepsPerPositiveCycle => Int("max_steps_per_positive_cycle", 2);
        /// <summary>Internal Ludo cells advanced per completed positive bar (playtest 27/11/2026: 3).</summary>
        public int PositiveCycleSteps => Int("positive_cycle_steps", 1);
        /// <summary>Internal Ludo cells retreated per completed negative bar.</summary>
        public int NegativeCycleSteps => Int("negative_cycle_steps", 1);
        /// <summary>Advance multiplier while the vehicle is active (does not stack with the doubling abilities).</summary>
        public int VehicleAdvanceMultiplier => Int("vehicle_advance_multiplier", 2);
        /// <summary>When true, the hospital always offers "stay without paying" (emergency recovery), except where it eliminates.</summary>
        public bool HospitalStayWithoutPayingOption => Bool("hospital_stay_without_paying_option", false);
        /// <summary>When true, the Atleta's Fôlego Extra is applied automatically (no choice, no decline).</summary>
        public bool AthleteAbilityAutomatic => Bool("athlete_ability_automatic", false);
        /// <summary>Players may join only at the start of a year (before the first roll of the year).</summary>
        public bool JoinOnlyAtYearStart => Bool("join_only_at_year_start", true);
        public int VehicleCost => Int("vehicle_cost", 400);
        public int VehicleDurationRounds => Int("vehicle_duration_rounds", 9);
        public int VehicleMaintenanceCost => Int("vehicle_maintenance_cost", 150);
        public int VehicleMaintenanceExtensionRounds => Int("vehicle_maintenance_extension_rounds", 3);
        public int PropertyCost => Int("property_cost", 500);
        public int TaxEvasionPenaltyMultiplier => Int("tax_evasion_penalty_multiplier", 2);
        public int VoluntaryTaxCitizenshipBase => Int("voluntary_tax_citizenship_base", 1);
        public int UnpaidTaxDebtBase => Int("unpaid_tax_debt_base", 3);
        public int UnpaidEssentialDebtBase => Int("unpaid_essential_debt_base", 3);
        public decimal ReserveAnnualRate => Decimal("reserve_annual_rate", 0.1m);
        public int PublicFundVoluntaryContribution => Int("public_fund_voluntary_contribution", 50);
        public int PawnCount => Int("pawn_count", 4);
    }

    /// <summary>The complete, validated V4 content set (single source for all rules data).</summary>
    public sealed class V4ContentSet
    {
        public IReadOnlyList<HouseDefinition> Houses;
        public IReadOnlyList<OptionDefinition> Options;
        public IReadOnlyList<ProfessionDefinition> Professions;
        public IReadOnlyList<ProgressionCard> Progression;
        public IReadOnlyList<DifficultyDefinition> Difficulties;
        public IReadOnlyList<AttributeDefinition> Attributes;
        public IReadOnlyList<CoinDefinition> Coins;
        public IReadOnlyList<SystemEventDefinition> SystemEvents;
        public IReadOnlyList<RouteCell> Routes;
        public RulesParameters Rules;
        /// <summary>Stable hash of the raw source texts, stored in saves to detect content drift.</summary>
        public string ContentHash;

        private Dictionary<string, HouseDefinition> _housesById;
        private Dictionary<string, List<OptionDefinition>> _optionsByHouse;
        private Dictionary<string, ProfessionDefinition> _professionsById;
        private Dictionary<string, List<RouteCell>> _routesByColor;

        public void BuildIndexes()
        {
            _housesById = Houses.ToDictionary(h => h.Id, StringComparer.Ordinal);
            _optionsByHouse = Options.GroupBy(o => o.HouseId, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.OrderBy(o => o.Order).ToList(), StringComparer.Ordinal);
            _professionsById = Professions.ToDictionary(p => p.Id, StringComparer.Ordinal);
            _routesByColor = Routes.GroupBy(r => r.Color, StringComparer.Ordinal)
                .ToDictionary(g => g.Key, g => g.OrderBy(r => r.Index).ToList(), StringComparer.Ordinal);
        }

        public HouseDefinition House(string id) => _housesById[id];
        public HouseDefinition HouseAt(int unityIndex) => Houses.First(h => h.UnityIndex == unityIndex);
        public IReadOnlyList<OptionDefinition> OptionsOf(string houseId) => _optionsByHouse.TryGetValue(houseId, out var l) ? l : (IReadOnlyList<OptionDefinition>)Array.Empty<OptionDefinition>();
        public ProfessionDefinition Profession(string id) => _professionsById[id];
        public IReadOnlyList<RouteCell> RouteOf(string color) => _routesByColor[color];
        public IEnumerable<string> RouteColors => _routesByColor.Keys;
        public DifficultyDefinition Difficulty(string id) => Difficulties.First(d => d.Id == id);
        public IReadOnlyList<ProgressionCard> ProgressionOf(string professionId) =>
            Progression.Where(p => p.ProfessionId == professionId).OrderBy(p => p.Step).ToList();
        public SystemEventDefinition SystemEvent(string id) => SystemEvents.First(e => e.Id == id);
        public AttributeDefinition Attribute(AttributeId id) => Attributes.First(a => a.Id == AttributeIds.Keys[(int)id]);
    }

    /// <summary>Builds <see cref="V4ContentSet"/> from the raw CSV/JSON texts. CSV is the authoring source.</summary>
    public static class V4ContentLoader
    {
        public sealed class RawSources
        {
            public string Casas, Opcoes, Personagens, Progressao, Dificuldades, Atributos, Moedas, EventosSistema, RotasLudo, ParametrosJson;
        }

        public static V4ContentSet Load(RawSources s)
        {
            var set = new V4ContentSet
            {
                Houses = CsvReader.ReadRecords(s.Casas).Select(r => new HouseDefinition
                {
                    Id = r["id"], Position = I(r["posicao"]), UnityIndex = I(r["indice_unity"]), Sector = r["setor"], Title = r["titulo"],
                    Category = r["categoria"], ColorHex = r["cor"], Handler = r["handler"], Icon = r["icone"], Text = r["texto"], Status = r["status"]
                }).ToList(),
                Options = CsvReader.ReadRecords(s.Opcoes).Select(r => new OptionDefinition
                {
                    Id = r["id"], HouseId = r["casa_id"], Order = I(r["ordem"]), Title = r["titulo"], Text = r["texto"],
                    Cost = I(r["custo_umr"]), RewardFixed = I(r["recompensa_imediata_umr"]), RewardPctSalary = I(r["recompensa_pct_salario_atual"]),
                    Deltas = AttributeIds.Keys.Select(k => I(r[k])).ToArray(),
                    Action = r["acao"], Condition = r["condicao"], Params = MiniJson.ParseObject(r["parametros_json"]),
                    Status = r["status"], Art = r["arte"]
                }).ToList(),
                Professions = CsvReader.ReadRecords(s.Personagens).Select(r => new ProfessionDefinition
                {
                    Id = r["id"], Name = r["profissao"], Salary = I(r["salario_inicial"]),
                    Multipliers = new[] { D(r["mult_conhecimento"]), D(r["mult_investimento"]), D(r["mult_cidadania"]), D(r["mult_toxina"]), D(r["mult_cortisol"]), D(r["mult_dividas"]) },
                    Ability = r["habilidade"], AbilityText = r["descricao_habilidade"], Frequency = r["frequencia"], Status = r["status"]
                }).ToList(),
                Progression = CsvReader.ReadRecords(s.Progressao).Select(r => new ProgressionCard
                {
                    Id = r["id"], ProfessionId = r["personagem_id"], Profession = r["profissao"], Step = I(r["etapa"]), Title = r["titulo"],
                    CostPctOfInitialSalary = I(r["custo_pct_salario_inicial"]), Cost = I(r["custo_umr"]),
                    RaisePctOfInitialSalary = I(r["aumento_pct_salario_inicial"]), SalaryAfter = I(r["salario_apos_umr"]),
                    Conhecimento = I(r["conhecimento"]), Investimento = I(r["investimento"]), Cidadania = I(r["cidadania"]),
                    Text = r["texto"], Art = r["arte"], Status = r["status"]
                }).ToList(),
                Difficulties = CsvReader.ReadRecords(s.Dificuldades).Select(r => new DifficultyDefinition
                {
                    Id = r["id"], Battery = I(r["bateria"]), Difficulty = r["dificuldade"],
                    PositiveMultiplier = D(r["mult_positivos"]), NegativeMultiplier = D(r["mult_negativos"]), IncomeTaxPct = I(r["ir_pct"]),
                    PublicHospitalCost = I(r["hospital_publico_custo"]), PublicHospitalTurns = I(r["hospital_publico_turnos"]),
                    PrivateHospitalCost = I(r["hospital_particular_custo"]), PrivateHospitalTurns = I(r["hospital_particular_turnos"]),
                    EmergencyTurns = I(r["emergencia_turnos"]), EmergencyDebtBase = I(r["emergencia_dividas_base"]),
                    EliminateWithoutResources = B(r["eliminar_sem_recursos"]), Status = r["status"]
                }).ToList(),
                Attributes = CsvReader.ReadRecords(s.Atributos).Select(r => new AttributeDefinition
                {
                    Id = r["id"], Positive = r["tipo"] == "positivo", Threshold = I(r["limiar"]), BarMin = I(r["barra_min"]),
                    CycleEffect = r["efeito_ciclo"], HospitalAfterCycles = string.IsNullOrEmpty(r["internar_apos_ciclos"]) ? 0 : I(r["internar_apos_ciclos"]),
                    ResetOnDischarge = B(r["reset_alta"])
                }).ToList(),
                Coins = CsvReader.ReadRecords(s.Moedas).Select(r => new CoinDefinition
                {
                    Denomination = I(r["denominacao"]), Quantity = I(r["quantidade"]), Total = I(r["total"])
                }).ToList(),
                SystemEvents = CsvReader.ReadRecords(s.EventosSistema).Select(r => new SystemEventDefinition
                {
                    Id = r["id"], Title = r["titulo"], Text = r["texto"], Action = r["acao"],
                    Params = MiniJson.ParseObject(r["parametros_json"]), Art = r["arte"]
                }).ToList(),
                Routes = CsvReader.ReadRecords(s.RotasLudo).Select(r => new RouteCell
                {
                    Color = r["cor"], Index = I(r["indice"]), Row = I(r["linha"]), Col = I(r["coluna"]), GlobalCell = r["celula_global"],
                    Kind = r["tipo"], Protected = B(r["protegida"])
                }).ToList(),
                Rules = new RulesParameters(MiniJson.ParseObject(s.ParametrosJson)),
                ContentHash = Hash(s)
            };
            set.BuildIndexes();
            return set;
        }

        /// <summary>FNV-1a 64-bit over all raw sources (dependency-free, deterministic across platforms).</summary>
        public static string Hash(RawSources s)
        {
            ulong h = 14695981039346656037UL;
            foreach (var part in new[] { s.Casas, s.Opcoes, s.Personagens, s.Progressao, s.Dificuldades, s.Atributos, s.Moedas, s.EventosSistema, s.RotasLudo, s.ParametrosJson })
            {
                var text = (part ?? string.Empty).Replace("\r\n", "\n");
                foreach (var ch in text) { h ^= ch; h *= 1099511628211UL; }
                h ^= 0xFF; h *= 1099511628211UL;
            }
            return h.ToString("x16", CultureInfo.InvariantCulture);
        }

        private static int I(string v) => int.Parse(v.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture);
        private static decimal D(string v) => decimal.Parse(v.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture);
        private static bool B(string v) => string.Equals(v.Trim(), "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Integrity validation required by HANDOFF §3. Returns human-readable diagnostics; empty means valid.</summary>
    public static class V4ContentValidator
    {
        private static readonly string[] Categories = { "especial", "nao_essencial", "oportunidade", "essencial", "investimento", "lazer", "imposto", "progressao", "divida" };
        private static readonly string[] KnownActions =
        {
            "neutro","normal","recusar","inadimplencia","pagar_imposto","sonegar_imposto","imposto_sem_recursos","progressao","depositar_reserva",
            "parceria","delegar","isento","automatico","comprar_veiculo","manter_veiculo","internar","comprar_imovel","consulta_cofre","doar_cofre"
        };
        private static readonly string[] KnownConditions =
        {
            "sempre","nenhuma_opcao_paga_acessivel","tributo_devido_e_sem_recursos","etapa_menor_que_4","etapa_igual_4","saldo_e_teto","veiculo_ativo",
            "barra_dividas_maior_zero","imoveis_maior_que_zero","sem_patrimonio_tributavel","sem_veiculo_ativo","barra_dividas_zero"
        };

        public static List<string> Validate(V4ContentSet c, Func<string, bool> artExists = null)
        {
            var e = new List<string>();
            if (c.Houses.Count != 36) e.Add("Expected 36 houses, found " + c.Houses.Count);
            if (c.Houses.Select(h => h.Id).Distinct().Count() != c.Houses.Count) e.Add("Duplicate house IDs");
            // House IDs are stable content IDs and need not match the board position (since the 27/11/2026 playtest H19 sits at
            // position 28 and H28 at 19, as on the physical board). Every ID H01..H36 must exist, every position 1..36 must hold
            // exactly one house, and the Unity index is always position - 1.
            for (var i = 0; i < c.Houses.Count; i++)
            {
                var id = "H" + (i + 1).ToString("00", CultureInfo.InvariantCulture);
                if (c.Houses.All(x => x.Id != id)) e.Add("Missing house " + id);
                var atPosition = c.Houses.Count(x => x.Position == i + 1);
                if (atPosition != 1) e.Add("Position " + (i + 1) + " holds " + atPosition + " houses");
            }
            foreach (var h in c.Houses)
                if (h.UnityIndex != h.Position - 1) e.Add(h.Id + " has inconsistent position/unity index");
            foreach (var cat in Categories)
                if (c.Houses.Count(h => h.Category == cat) != 4) e.Add("Category " + cat + " must have 4 houses");
            if (c.Houses.Any(h => !Categories.Contains(h.Category))) e.Add("Unknown house category present");

            if (c.Options.Select(o => o.Id).Distinct().Count() != c.Options.Count) e.Add("Duplicate option IDs");
            foreach (var o in c.Options)
            {
                if (!c.Houses.Any(h => h.Id == o.HouseId)) e.Add(o.Id + " references unknown house " + o.HouseId);
                if (!KnownActions.Contains(o.Action)) e.Add(o.Id + " has unknown action '" + o.Action + "'");
                if (!KnownConditions.Contains(o.Condition)) e.Add(o.Id + " has unknown condition '" + o.Condition + "'");
            }
            foreach (var h in c.Houses)
                if (h.Handler != "inicio" && h.Handler != "visita_hospital" && c.OptionsOf(h.Id).Count == 0)
                    e.Add(h.Id + " has no options");

            if (c.Professions.Count != 6) e.Add("Expected 6 professions, found " + c.Professions.Count);
            foreach (var p in c.Professions)
            {
                var cards = c.ProgressionOf(p.Id);
                if (cards.Count != 4 || cards.Select(x => x.Step).Distinct().Count() != 4) e.Add(p.Name + " must have 4 progression steps");
                var pos = new[] { 2, 3, 4, 5 };
                for (var i = 0; i < cards.Count && i < 4; i++)
                {
                    if (cards[i].PositiveTotal != pos[i]) e.Add(cards[i].Id + " positive total " + cards[i].PositiveTotal + " != " + pos[i]);
                    if (new[] { cards[i].Conhecimento, cards[i].Investimento, cards[i].Cidadania }.Any(v => v > 3)) e.Add(cards[i].Id + " exceeds 3 points on one attribute");
                    var expectedCost = CeilDiv(p.Salary * new[] { 50, 75, 100, 125 }[i], 100);
                    if (cards[i].Cost != expectedCost) e.Add(cards[i].Id + " cost " + cards[i].Cost + " != ceil(" + p.Salary + "*pct) " + expectedCost);
                    var expectedSalary = CeilDiv(p.Salary * (100 + 20 * (i + 1)), 100);
                    if (cards[i].SalaryAfter != expectedSalary) e.Add(cards[i].Id + " salary " + cards[i].SalaryAfter + " != " + expectedSalary);
                }
            }

            if (c.Difficulties.Count != 6) e.Add("Expected 6 difficulty configurations");
            if (c.Attributes.Count != 6) e.Add("Expected 6 attributes");

            var coinTotal = c.Coins.Sum(x => x.Denomination * x.Quantity);
            if (c.Coins.Sum(x => x.Quantity) != 360) e.Add("Coin count must be 360");
            if (coinTotal != c.Rules.PhysicalMoneyTotal || coinTotal != 28260) e.Add("Coin total must be 28260, found " + coinTotal);
            if (c.Coins.Any(x => x.Denomination * x.Quantity != x.Total)) e.Add("Coin row total mismatch");

            foreach (var color in new[] { "verde", "vermelho", "azul", "amarelo" })
            {
                if (!c.RouteColors.Contains(color)) { e.Add("Missing route color " + color); continue; }
                var r = c.RouteOf(color);
                if (r.Count != 57) e.Add("Route " + color + " must have 57 positions, found " + r.Count);
                for (var i = 0; i < r.Count; i++) if (r[i].Index != i) { e.Add("Route " + color + " has non-contiguous index at " + i); break; }
                if (r.Count(x => x.IsShared) != 51) e.Add("Route " + color + " must have 51 shared cells");
                if (r.Count(x => x.Kind == "privado") != 5) e.Add("Route " + color + " must have 5 private cells");
                if (r.Count(x => x.IsGoal) != 1 || !r[r.Count - 1].IsGoal) e.Add("Route " + color + " must end with exactly one goal");
            }
            var ring = c.Routes.Where(x => x.IsShared).Select(x => x.GlobalCell).Distinct().Count();
            if (ring != 52) e.Add("Shared ring must have 52 unique cells, found " + ring);

            if (artExists != null)
            {
                foreach (var h in c.Houses) if (!artExists(h.Icon)) e.Add("Missing icon " + h.Icon);
                foreach (var o in c.Options) if (!artExists(o.Art)) e.Add("Missing card art " + o.Art);
                foreach (var p in c.Progression) if (!artExists(p.Art)) e.Add("Missing card art " + p.Art);
                foreach (var s in c.SystemEvents) if (!artExists(s.Art)) e.Add("Missing card art " + s.Art);
            }
            return e;
        }

        private static int CeilDiv(int a, int b) => (a + b - 1) / b;
    }
}

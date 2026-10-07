using System;
using System.Collections.Generic;
using System.Globalization;
using MoneyRank.Domain.Economy;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "EconomyDefinition", menuName = "Money Rank/Economy Definition")]
    public sealed class EconomyDefinitionAsset : ScriptableObject
    {
        [SerializeField] private List<EconomyAccountData> accounts = new List<EconomyAccountData>();
        [SerializeField] private List<EconomyBalanceData> initialBalances = new List<EconomyBalanceData>();

        public IReadOnlyList<EconomyAccountData> Accounts => accounts;
        public IReadOnlyList<EconomyBalanceData> InitialBalances => initialBalances;

        public EconomyDefinition ToDomain()
        {
            var accountDefinitions = new List<EconomyAccountDefinition>(accounts.Count);
            foreach (var account in accounts)
            {
                if (account == null)
                    throw new InvalidOperationException("Economy definition contains a missing account.");
                accountDefinitions.Add(account.ToDomain());
            }

            var balances = new List<EconomyBalanceDefinition>(initialBalances.Count);
            foreach (var balance in initialBalances)
            {
                if (balance == null)
                    throw new InvalidOperationException("Economy definition contains a missing initial balance.");
                balances.Add(balance.ToDomain());
            }

            return new EconomyDefinition(accountDefinitions, balances);
        }

        public EconomyState CreateState() => ToDomain().CreateState();
    }

    [Serializable]
    public sealed class EconomyAccountData
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string ownerId;
        [SerializeField] private string resourceTypeId;
        [SerializeField] private string unitId;
        [SerializeField] private bool hasMinimumBalance;
        [SerializeField] private string minimumBalance;
        [SerializeField] private bool hasMaximumBalance;
        [SerializeField] private string maximumBalance;

        public EconomyAccountData(
            string id,
            string displayName,
            string ownerId,
            string resourceTypeId,
            string minimumBalance = null,
            string maximumBalance = null,
            string unitId = null)
        {
            this.id = id;
            this.displayName = displayName;
            this.ownerId = ownerId;
            this.resourceTypeId = resourceTypeId;
            this.unitId = unitId;
            hasMinimumBalance = minimumBalance != null;
            this.minimumBalance = minimumBalance;
            hasMaximumBalance = maximumBalance != null;
            this.maximumBalance = maximumBalance;
        }

        public EconomyAccountDefinition ToDomain() =>
            new EconomyAccountDefinition(
                id,
                displayName,
                ownerId,
                resourceTypeId,
                ParseOptional(hasMinimumBalance, minimumBalance, "minimum balance"),
                ParseOptional(hasMaximumBalance, maximumBalance, "maximum balance"),
                unitId);

        private static decimal? ParseOptional(bool enabled, string value, string label)
        {
            if (!enabled) return null;
            if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                throw new FormatException($"Economy account {label} '{value}' is not a valid invariant decimal.");
            return parsed;
        }
    }

    [Serializable]
    public sealed class EconomyBalanceData
    {
        [SerializeField] private string accountId;
        [SerializeField] private string balance;

        public EconomyBalanceData(string accountId, string balance)
        {
            this.accountId = accountId;
            this.balance = balance;
        }

        public EconomyBalanceDefinition ToDomain()
        {
            if (!decimal.TryParse(balance, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
                throw new FormatException($"Economy balance '{balance}' is not a valid invariant decimal.");
            return new EconomyBalanceDefinition(accountId, parsed);
        }
    }
}

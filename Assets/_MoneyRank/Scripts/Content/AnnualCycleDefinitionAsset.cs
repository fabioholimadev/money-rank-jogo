using System;
using MoneyRank.Domain.Economy;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "AnnualCycleDefinition", menuName = "Money Rank/Annual Cycle Definition")]
    public sealed class AnnualCycleDefinitionAsset : ScriptableObject
    {
        [SerializeField] private AnnualCycleDefinitionData definition = new AnnualCycleDefinitionData();

        public AnnualCycleDefinition ToDomain() => definition.ToDomain();
    }

    [Serializable]
    public sealed class AnnualCycleDefinitionData
    {
        [SerializeField] private string id;
        [SerializeField] private int completedLapsPerCycle = 1;
        [SerializeField] private string policyId;

        public AnnualCycleDefinitionData()
        {
        }

        public AnnualCycleDefinitionData(string id, int completedLapsPerCycle, string policyId)
        {
            this.id = id;
            this.completedLapsPerCycle = completedLapsPerCycle;
            this.policyId = policyId;
        }

        public AnnualCycleDefinition ToDomain() =>
            new AnnualCycleDefinition(id, completedLapsPerCycle, policyId);
    }
}

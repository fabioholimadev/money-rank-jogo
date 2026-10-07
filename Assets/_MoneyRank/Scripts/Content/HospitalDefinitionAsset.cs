using System;
using MoneyRank.Domain.Hospital;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "HospitalDefinition", menuName = "Money Rank/Hospital Definition")]
    public sealed class HospitalDefinitionAsset : ScriptableObject
    {
        [SerializeField] private HospitalDefinitionData definition = new HospitalDefinitionData();

        public HospitalDefinition ToDomain() => definition.ToDomain();
    }

    [Serializable]
    public sealed class HospitalDefinitionData
    {
        [SerializeField] private string id;
        [SerializeField] private string resolutionPolicyId;

        public HospitalDefinitionData()
        {
        }

        public HospitalDefinitionData(string id, string resolutionPolicyId)
        {
            this.id = id;
            this.resolutionPolicyId = resolutionPolicyId;
        }

        public HospitalDefinition ToDomain() => new HospitalDefinition(id, resolutionPolicyId);
    }
}

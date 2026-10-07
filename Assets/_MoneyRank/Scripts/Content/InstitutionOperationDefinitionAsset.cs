using System;
using MoneyRank.Domain.Economy;
using UnityEngine;

namespace MoneyRank.Content
{
    [CreateAssetMenu(fileName = "InstitutionOperation", menuName = "Money Rank/Institution Operation")]
    public sealed class InstitutionOperationDefinitionAsset : ScriptableObject
    {
        [SerializeField] private InstitutionOperationDefinitionData definition =
            new InstitutionOperationDefinitionData();

        public InstitutionOperationDefinitionData Definition => definition;
        public InstitutionOperationDefinition ToDomain() => definition.ToDomain();
    }

    [Serializable]
    public sealed class InstitutionOperationDefinitionData
    {
        [SerializeField] private string id;
        [SerializeField] private string displayName;
        [SerializeField] private string categoryId;
        [SerializeField] private string institutionOwnerId;
        [SerializeField] private string policyId;

        public InstitutionOperationDefinitionData()
        {
        }

        public InstitutionOperationDefinitionData(
            string id,
            string displayName,
            string categoryId,
            string institutionOwnerId,
            string policyId)
        {
            this.id = id;
            this.displayName = displayName;
            this.categoryId = categoryId;
            this.institutionOwnerId = institutionOwnerId;
            this.policyId = policyId;
        }

        public InstitutionOperationDefinition ToDomain() =>
            new InstitutionOperationDefinition(
                id,
                displayName,
                categoryId,
                institutionOwnerId,
                policyId);
    }
}

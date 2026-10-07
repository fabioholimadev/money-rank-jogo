using System;

namespace MoneyRank.Domain.Hospital
{
    public sealed class HospitalDefinition
    {
        public HospitalDefinition(string id, string resolutionPolicyId)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Hospital ID cannot be empty.", nameof(id));
            if (string.IsNullOrWhiteSpace(resolutionPolicyId))
                throw new ArgumentException("Hospital resolution policy ID cannot be empty.", nameof(resolutionPolicyId));

            Id = id;
            ResolutionPolicyId = resolutionPolicyId;
        }

        public string Id { get; }
        public string ResolutionPolicyId { get; }
    }
}

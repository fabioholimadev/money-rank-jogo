using System;
using System.Collections.Generic;

namespace MoneyRank.Domain.Hospital
{
    public sealed class HospitalService
    {
        private readonly Dictionary<string, IHospitalResolutionPolicy> _policies =
            new Dictionary<string, IHospitalResolutionPolicy>(StringComparer.Ordinal);

        public HospitalService(IEnumerable<IHospitalResolutionPolicy> policies)
        {
            if (policies == null)
                throw new ArgumentNullException(nameof(policies));

            foreach (var policy in policies)
            {
                if (policy == null)
                    throw new ArgumentException("Hospital policies cannot contain null entries.", nameof(policies));
                if (string.IsNullOrWhiteSpace(policy.PolicyId))
                    throw new ArgumentException("Hospital policy ID cannot be empty.", nameof(policies));
                if (_policies.ContainsKey(policy.PolicyId))
                    throw new ArgumentException($"Hospital policy '{policy.PolicyId}' is registered more than once.", nameof(policies));

                _policies.Add(policy.PolicyId, policy);
            }
        }

        public OperationResult Admit(HospitalState state, string reasonId, int pendingMovementSkips)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (state.IsHospitalized)
                return OperationResult.Failure("PLAYER_ALREADY_HOSPITALIZED", "The player is already hospitalized.");
            if (string.IsNullOrWhiteSpace(reasonId))
                return OperationResult.Failure("HOSPITAL_REASON_REQUIRED", "Hospital admission requires a reason ID.");
            if (pendingMovementSkips < 0)
                return OperationResult.Failure("HOSPITAL_SKIP_COUNT_INVALID", "Hospital movement skips cannot be negative.");

            state.Admit(reasonId, pendingMovementSkips);
            return OperationResult.Success();
        }

        public OperationResult<bool> TryConsumeMovementSkip(HospitalState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (!state.IsHospitalized || state.PendingMovementSkips == 0)
                return OperationResult<bool>.Success(false);

            state.ConsumeMovementSkip();
            return OperationResult<bool>.Success(true);
        }

        public OperationResult<HospitalResolutionResult> Resolve(
            HospitalDefinition definition,
            HospitalState state,
            HospitalResolutionCommand command)
        {
            if (definition == null)
                throw new ArgumentNullException(nameof(definition));
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (!state.IsHospitalized)
            {
                return OperationResult<HospitalResolutionResult>.Failure(
                    "PLAYER_NOT_HOSPITALIZED",
                    "Hospital resolution requires a hospitalized player.");
            }
            if (!_policies.TryGetValue(definition.ResolutionPolicyId, out var policy))
            {
                return OperationResult<HospitalResolutionResult>.Failure(
                    "HOSPITAL_POLICY_NOT_FOUND",
                    $"No hospital policy is registered for '{definition.ResolutionPolicyId}'.");
            }

            var calculation = policy.Calculate(definition, state, command);
            if (!calculation.Succeeded)
            {
                return OperationResult<HospitalResolutionResult>.Failure(
                    calculation.ErrorCode,
                    calculation.Message);
            }
            if (calculation.Value == null)
            {
                return OperationResult<HospitalResolutionResult>.Failure(
                    "HOSPITAL_CALCULATION_MISSING",
                    "The hospital policy did not produce a calculation.");
            }

            var wasHospitalized = state.IsHospitalized;
            state.Apply(calculation.Value);
            return OperationResult<HospitalResolutionResult>.Success(
                new HospitalResolutionResult(
                    state.PlayerId,
                    wasHospitalized,
                    state.IsHospitalized,
                    state.PendingMovementSkips,
                    calculation.Value.Effects));
        }
    }
}

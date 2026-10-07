using System;
using MoneyRank.Content;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;
using MoneyRank.Domain.Hospital;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class HospitalTests
    {
        [Test]
        public void AdmissionRecordsReasonAndConfigurableMovementSkips()
        {
            var state = new HospitalState(new PlayerId("p1"));
            var service = new HospitalService(Array.Empty<IHospitalResolutionPolicy>());

            var admission = service.Admit(state, "configured-health-crisis", 2);
            var firstSkip = service.TryConsumeMovementSkip(state);

            Assert.That(admission.Succeeded, Is.True);
            Assert.That(firstSkip.Succeeded, Is.True);
            Assert.That(firstSkip.Value, Is.True);
            Assert.That(state.IsHospitalized, Is.True);
            Assert.That(state.ReasonId, Is.EqualTo("configured-health-crisis"));
            Assert.That(state.PendingMovementSkips, Is.EqualTo(1));
        }

        [Test]
        public void ConfiguredPolicyCanRecoverAndPlanConsequences()
        {
            var state = new HospitalState(new PlayerId("p1"));
            var service = new HospitalService(
                new IHospitalResolutionPolicy[] { new RecoveryPolicy() });
            service.Admit(state, "health-crisis", 1);

            var result = service.Resolve(
                new HospitalDefinition("hospital", "test-recovery"),
                state,
                new HospitalResolutionCommand("configured-option", Array.Empty<DefinitionParameter>()));

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.WasHospitalized, Is.True);
            Assert.That(result.Value.IsHospitalized, Is.False);
            Assert.That(result.Value.Effects, Has.Count.EqualTo(1));
            Assert.That(result.Value.Effects[0].OperationId, Is.EqualTo("configured-operation"));
            Assert.That(state.IsHospitalized, Is.False);
            Assert.That(state.PendingMovementSkips, Is.Zero);
            Assert.That(state.ReasonId, Is.Empty);
        }

        [Test]
        public void RejectedResolutionLeavesHospitalStateUnchanged()
        {
            var state = new HospitalState(new PlayerId("p1"));
            var service = new HospitalService(
                new IHospitalResolutionPolicy[] { new RejectingPolicy() });
            service.Admit(state, "health-crisis", 3);

            var result = service.Resolve(
                new HospitalDefinition("hospital", "reject"),
                state,
                new HospitalResolutionCommand("attempt", Array.Empty<DefinitionParameter>()));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("TEST_RECOVERY_REJECTED"));
            Assert.That(state.IsHospitalized, Is.True);
            Assert.That(state.PendingMovementSkips, Is.EqualTo(3));
            Assert.That(state.ReasonId, Is.EqualTo("health-crisis"));
        }

        [Test]
        public void MissingPolicyDoesNotResolveHospital()
        {
            var state = new HospitalState(new PlayerId("p1"));
            var service = new HospitalService(Array.Empty<IHospitalResolutionPolicy>());
            service.Admit(state, "health-crisis", 0);

            var result = service.Resolve(
                new HospitalDefinition("hospital", "missing"),
                state,
                new HospitalResolutionCommand("attempt", Array.Empty<DefinitionParameter>()));

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("HOSPITAL_POLICY_NOT_FOUND"));
            Assert.That(state.IsHospitalized, Is.True);
        }

        [Test]
        public void SerializableDefinitionKeepsResolutionPolicyAsData()
        {
            var definition = new HospitalDefinitionData("configured-hospital", "configured-policy").ToDomain();

            Assert.That(definition.Id, Is.EqualTo("configured-hospital"));
            Assert.That(definition.ResolutionPolicyId, Is.EqualTo("configured-policy"));
        }

        private sealed class RecoveryPolicy : IHospitalResolutionPolicy
        {
            public string PolicyId => "test-recovery";

            public OperationResult<HospitalResolutionCalculation> Calculate(
                HospitalDefinition definition,
                HospitalState state,
                HospitalResolutionCommand command) =>
                OperationResult<HospitalResolutionCalculation>.Success(
                    new HospitalResolutionCalculation(
                        false,
                        0,
                        new[]
                        {
                            new GameEffectDefinition(
                                "configured-target",
                                "configured-operation",
                                "configured-value",
                                null,
                                definition.Id)
                        }));
        }

        private sealed class RejectingPolicy : IHospitalResolutionPolicy
        {
            public string PolicyId => "reject";

            public OperationResult<HospitalResolutionCalculation> Calculate(
                HospitalDefinition definition,
                HospitalState state,
                HospitalResolutionCommand command) =>
                OperationResult<HospitalResolutionCalculation>.Failure(
                    "TEST_RECOVERY_REJECTED",
                    "Rejected by the test policy.");
        }
    }
}

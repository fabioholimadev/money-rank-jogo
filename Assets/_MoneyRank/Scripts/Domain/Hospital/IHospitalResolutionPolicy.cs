namespace MoneyRank.Domain.Hospital
{
    public interface IHospitalResolutionPolicy
    {
        string PolicyId { get; }

        OperationResult<HospitalResolutionCalculation> Calculate(
            HospitalDefinition definition,
            HospitalState state,
            HospitalResolutionCommand command);
    }
}

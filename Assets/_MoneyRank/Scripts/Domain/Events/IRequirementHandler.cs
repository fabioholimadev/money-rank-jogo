namespace MoneyRank.Domain.Events
{
    public interface IRequirementHandler
    {
        string TypeId { get; }

        RequirementCheckResult Evaluate(
            RequirementDefinition requirement,
            RequirementEvaluationContext context);
    }
}

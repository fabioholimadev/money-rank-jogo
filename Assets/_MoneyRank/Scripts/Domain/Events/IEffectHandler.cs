namespace MoneyRank.Domain.Events
{
    public interface IEffectHandler
    {
        string OperationId { get; }

        EffectApplicationResult Apply(GameEffectDefinition effect, EventState state);
    }
}

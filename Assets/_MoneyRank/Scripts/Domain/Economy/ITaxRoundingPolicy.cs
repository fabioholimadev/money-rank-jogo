namespace MoneyRank.Domain.Economy
{
    public interface ITaxRoundingPolicy
    {
        string PolicyId { get; }
        decimal Round(decimal value);
    }
}

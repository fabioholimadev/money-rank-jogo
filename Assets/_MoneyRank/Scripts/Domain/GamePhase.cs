namespace MoneyRank.Domain
{
    public enum GamePhase
    {
        Setup = 0,
        StartTurn = 10,
        AwaitMove = 20,
        ResolveSpace = 30,
        AwaitChoice = 40,
        ApplyEffects = 50,
        CheckProgress = 60,
        EndTurn = 70,
        GameEnd = 80,
        Results = 90
    }
}


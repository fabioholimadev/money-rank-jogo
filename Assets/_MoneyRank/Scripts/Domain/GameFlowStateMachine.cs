using System.Collections.Generic;

namespace MoneyRank.Domain
{
    public sealed class GameFlowStateMachine
    {
        private static readonly HashSet<Transition> AllowedTransitions = new HashSet<Transition>
        {
            new Transition(GamePhase.Setup, GamePhase.StartTurn),
            new Transition(GamePhase.StartTurn, GamePhase.AwaitMove),
            new Transition(GamePhase.AwaitMove, GamePhase.ResolveSpace),
            new Transition(GamePhase.ResolveSpace, GamePhase.AwaitChoice),
            new Transition(GamePhase.ResolveSpace, GamePhase.ApplyEffects),
            new Transition(GamePhase.AwaitChoice, GamePhase.ApplyEffects),
            new Transition(GamePhase.ApplyEffects, GamePhase.CheckProgress),
            new Transition(GamePhase.CheckProgress, GamePhase.EndTurn),
            new Transition(GamePhase.CheckProgress, GamePhase.GameEnd),
            new Transition(GamePhase.EndTurn, GamePhase.StartTurn),
            new Transition(GamePhase.GameEnd, GamePhase.Results)
        };

        public bool CanTransition(GamePhase current, GamePhase next) =>
            AllowedTransitions.Contains(new Transition(current, next));

        private readonly struct Transition
        {
            public Transition(GamePhase from, GamePhase to)
            {
                From = from;
                To = to;
            }

            private GamePhase From { get; }
            private GamePhase To { get; }

            public override bool Equals(object obj) =>
                obj is Transition other && From == other.From && To == other.To;

            public override int GetHashCode() => ((int)From * 397) ^ (int)To;
        }
    }
}


using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Domain.Events;

namespace MoneyRank.Application
{
    public sealed class EventResolutionPreview
    {
        private readonly ReadOnlyCollection<RequirementEvaluation> _conditionResults;
        private readonly ReadOnlyCollection<RequirementEvaluation> _requirementResults;
        private readonly ReadOnlyCollection<GameEffectDefinition> _plannedEffects;

        public EventResolutionPreview(
            EventDefinition eventDefinition,
            EventOptionDefinition option,
            SessionId sessionId,
            PlayerId playerId,
            EventStateSnapshot evaluatedState,
            IEnumerable<RequirementEvaluation> conditionResults,
            IEnumerable<RequirementEvaluation> requirementResults,
            EventStateSnapshot projectedState = null,
            string effectErrorCode = "",
            string effectErrorMessage = "")
        {
            EventDefinition = eventDefinition ?? throw new ArgumentNullException(nameof(eventDefinition));
            Option = option ?? throw new ArgumentNullException(nameof(option));
            if (conditionResults == null)
                throw new ArgumentNullException(nameof(conditionResults));
            if (requirementResults == null)
                throw new ArgumentNullException(nameof(requirementResults));

            if (string.IsNullOrWhiteSpace(sessionId.Value))
                throw new ArgumentException("Preview session ID cannot be empty.", nameof(sessionId));
            if (string.IsNullOrWhiteSpace(playerId.Value))
                throw new ArgumentException("Preview player ID cannot be empty.", nameof(playerId));

            SessionId = sessionId;
            PlayerId = playerId;
            EvaluatedState = evaluatedState ?? throw new ArgumentNullException(nameof(evaluatedState));
            if (string.IsNullOrWhiteSpace(effectErrorCode) != string.IsNullOrWhiteSpace(effectErrorMessage))
            {
                throw new ArgumentException(
                    "Effect preview error code and message must either both be set or both be empty.");
            }

            ProjectedState = projectedState;
            EffectErrorCode = effectErrorCode ?? string.Empty;
            EffectErrorMessage = effectErrorMessage ?? string.Empty;
            _conditionResults = conditionResults.ToList().AsReadOnly();
            _requirementResults = requirementResults.ToList().AsReadOnly();
            _plannedEffects = option.Effects.ToList().AsReadOnly();
        }

        public EventDefinition EventDefinition { get; }
        public EventOptionDefinition Option { get; }
        public SessionId SessionId { get; }
        public PlayerId PlayerId { get; }
        public EventStateSnapshot EvaluatedState { get; }
        public EventStateSnapshot ProjectedState { get; }
        public string EffectErrorCode { get; }
        public string EffectErrorMessage { get; }
        public bool HasProjectedState => ProjectedState != null;
        public IReadOnlyList<RequirementEvaluation> ConditionResults => _conditionResults;
        public IReadOnlyList<RequirementEvaluation> RequirementResults => _requirementResults;
        public IReadOnlyList<GameEffectDefinition> PlannedEffects => _plannedEffects;
        public bool CanConfirm =>
            _conditionResults.All(result => result.IsSatisfied) &&
            _requirementResults.All(result => result.IsSatisfied) &&
            string.IsNullOrEmpty(EffectErrorCode);
    }
}

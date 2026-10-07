using System;
using System.Collections.Generic;
using MoneyRank.Domain;
using MoneyRank.Domain.Economy;

namespace MoneyRank.Application
{
    public sealed class GameInstitutionOperationService
    {
        private readonly IGameSessionRepository _repository;
        private readonly Dictionary<string, InstitutionOperationDefinition> _definitions =
            new Dictionary<string, InstitutionOperationDefinition>(StringComparer.Ordinal);
        private readonly IReadOnlyList<IInstitutionOperationPolicy> _policies;

        public GameInstitutionOperationService(
            IGameSessionRepository repository,
            IEnumerable<InstitutionOperationDefinition> definitions,
            IEnumerable<IInstitutionOperationPolicy> policies)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (policies == null) throw new ArgumentNullException(nameof(policies));

            foreach (var definition in definitions)
            {
                if (definition == null) throw new ArgumentException("Institution operation definitions cannot contain null entries.", nameof(definitions));
                if (_definitions.ContainsKey(definition.Id)) throw new ArgumentException($"Institution operation definition '{definition.Id}' is registered more than once.", nameof(definitions));
                _definitions.Add(definition.Id, definition);
            }
            _policies = new List<IInstitutionOperationPolicy>(policies).AsReadOnly();
        }

        public OperationResult<InstitutionOperationResult> Execute(
            SessionId sessionId,
            string definitionId,
            InstitutionOperationCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var session = _repository.Get(sessionId) ??
                throw new InvalidOperationException($"Session '{sessionId}' was not found.");
            if (!_definitions.TryGetValue(definitionId, out var definition))
            {
                return OperationResult<InstitutionOperationResult>.Failure(
                    "INSTITUTION_OPERATION_NOT_FOUND",
                    $"Institution operation definition '{definitionId}' was not found.");
            }
            if (session.Players == null || !ContainsPlayer(session, command.PlayerId))
            {
                return OperationResult<InstitutionOperationResult>.Failure(
                    "INSTITUTION_PLAYER_NOT_FOUND",
                    $"Player '{command.PlayerId}' was not found in this session.");
            }
            if (session.Economy == null || session.EconomyState == null)
            {
                return OperationResult<InstitutionOperationResult>.Failure(
                    "SESSION_ECONOMY_NOT_CONFIGURED",
                    "This session has no economy definition.");
            }

            var service = new InstitutionOperationService(session.Economy, _policies);
            var result = service.Execute(
                definition,
                session.EconomyState,
                session.EconomyHistory,
                command);
            if (result.Succeeded)
                _repository.Save(session);
            return result;
        }

        private static bool ContainsPlayer(GameSession session, PlayerId playerId)
        {
            foreach (var player in session.Players)
            {
                if (player.Id == playerId)
                    return true;
            }
            return false;
        }
    }
}

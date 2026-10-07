using System;
using System.Collections.Generic;
using System.Linq;
using MoneyRank.Domain;
using MoneyRank.Domain.Economy;

namespace MoneyRank.Application
{
    public sealed class GameEconomicResourceService
    {
        private readonly IGameSessionRepository _repository;
        private readonly Dictionary<string, EconomicResourceDefinition> _definitions;
        private readonly IReadOnlyList<IEconomicResourceTransactionPolicy> _policies;

        public GameEconomicResourceService(
            IGameSessionRepository repository,
            IEnumerable<EconomicResourceDefinition> definitions,
            IEnumerable<IEconomicResourceTransactionPolicy> policies)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));
            if (policies == null) throw new ArgumentNullException(nameof(policies));
            var definitionList = definitions.ToList();
            if (definitionList.Any(definition => definition == null))
                throw new ArgumentException("Resource definitions cannot contain null entries.", nameof(definitions));
            if (definitionList.Select(definition => definition.Id).Distinct(StringComparer.Ordinal).Count() != definitionList.Count)
                throw new ArgumentException("Resource definition IDs must be unique.", nameof(definitions));
            _definitions = definitionList.ToDictionary(definition => definition.Id, StringComparer.Ordinal);
            _policies = policies.ToList().AsReadOnly();
        }

        public OperationResult<EconomicResourceTransactionResult> Execute(
            SessionId sessionId,
            string resourceId,
            EconomicResourceCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            var session = _repository.Get(sessionId) ??
                throw new InvalidOperationException($"Session '{sessionId}' was not found.");
            if (session.Economy == null)
                return OperationResult<EconomicResourceTransactionResult>.Failure("ECONOMY_NOT_CONFIGURED", "The session does not have an economy definition.");
            if (!_definitions.TryGetValue(resourceId, out var definition))
                return OperationResult<EconomicResourceTransactionResult>.Failure("RESOURCE_NOT_FOUND", $"Resource '{resourceId}' was not found.");
            if (session.Players.All(player => player.Id != command.PlayerId))
                return OperationResult<EconomicResourceTransactionResult>.Failure("RESOURCE_PLAYER_NOT_FOUND", "The resource player was not found in the session.");

            var service = new EconomicResourceService(session.Economy, _policies);
            var result = service.Execute(
                definition,
                session.EconomyState,
                session.EconomyHistory,
                command);
            if (result.Succeeded)
                _repository.Save(session);
            return result;
        }
    }
}

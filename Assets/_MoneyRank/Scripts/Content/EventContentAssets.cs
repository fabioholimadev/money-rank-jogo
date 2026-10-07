using System;
using System.Collections.Generic;
using MoneyRank.Domain.Events;
using UnityEngine;

namespace MoneyRank.Content
{
    [Serializable]
    public sealed class EventDefinitionData
    {
        [SerializeField] private string id;
        [SerializeField, Min(1)] private int version = 1;
        [SerializeField] private string categoryId;
        [SerializeField] private string title;
        [SerializeField, TextArea] private string educationalText;
        [SerializeField] private List<string> tags = new List<string>();
        [SerializeField] private List<RequirementDefinitionData> conditions = new List<RequirementDefinitionData>();
        [SerializeField] private List<EventOptionDefinitionData> options = new List<EventOptionDefinitionData>();
        [SerializeField] private string durationId;
        [SerializeField] private string presentationId;

        public EventDefinitionData()
        {
        }

        public EventDefinitionData(
            string id,
            int version,
            string categoryId,
            string title,
            string educationalText,
            IEnumerable<string> tags,
            IEnumerable<RequirementDefinitionData> conditions,
            IEnumerable<EventOptionDefinitionData> options,
            string durationId = null,
            string presentationId = null)
        {
            this.id = id;
            this.version = version;
            this.categoryId = categoryId;
            this.title = title;
            this.educationalText = educationalText;
            this.tags = new List<string>(tags ?? throw new ArgumentNullException(nameof(tags)));
            this.conditions = new List<RequirementDefinitionData>(conditions ?? throw new ArgumentNullException(nameof(conditions)));
            this.options = new List<EventOptionDefinitionData>(options ?? throw new ArgumentNullException(nameof(options)));
            this.durationId = durationId;
            this.presentationId = presentationId;
        }

        public EventDefinition ToDomain()
        {
            var domainConditions = new List<RequirementDefinition>(conditions.Count);
            foreach (var condition in conditions)
            {
                if (condition == null)
                    throw new InvalidOperationException("Event definition contains a missing condition.");
                domainConditions.Add(condition.ToDomain());
            }

            var domainOptions = new List<EventOptionDefinition>(options.Count);
            foreach (var option in options)
            {
                if (option == null)
                    throw new InvalidOperationException("Event definition contains a missing option.");
                domainOptions.Add(option.ToDomain());
            }

            return new EventDefinition(
                id,
                version,
                categoryId,
                title,
                educationalText,
                tags,
                domainConditions,
                domainOptions,
                durationId,
                presentationId);
        }
    }

    [Serializable]
    public sealed class EventOptionDefinitionData
    {
        [SerializeField] private string id;
        [SerializeField] private string label;
        [SerializeField, TextArea] private string description;
        [SerializeField] private List<RequirementDefinitionData> requirements = new List<RequirementDefinitionData>();
        [SerializeField] private List<GameEffectDefinitionData> effects = new List<GameEffectDefinitionData>();
        [SerializeField] private string visibilityId = "always";

        public EventOptionDefinitionData()
        {
        }

        public EventOptionDefinitionData(
            string id,
            string label,
            string description,
            IEnumerable<RequirementDefinitionData> requirements,
            IEnumerable<GameEffectDefinitionData> effects,
            string visibilityId)
        {
            this.id = id;
            this.label = label;
            this.description = description;
            this.requirements = new List<RequirementDefinitionData>(requirements ?? throw new ArgumentNullException(nameof(requirements)));
            this.effects = new List<GameEffectDefinitionData>(effects ?? throw new ArgumentNullException(nameof(effects)));
            this.visibilityId = visibilityId;
        }

        public EventOptionDefinition ToDomain()
        {
            var domainRequirements = new List<RequirementDefinition>(requirements.Count);
            foreach (var requirement in requirements)
            {
                if (requirement == null)
                    throw new InvalidOperationException("Event option contains a missing requirement.");
                domainRequirements.Add(requirement.ToDomain());
            }

            var domainEffects = new List<GameEffectDefinition>(effects.Count);
            foreach (var effect in effects)
            {
                if (effect == null)
                    throw new InvalidOperationException("Event option contains a missing effect.");
                domainEffects.Add(effect.ToDomain());
            }

            return new EventOptionDefinition(id, label, description, domainRequirements, domainEffects, visibilityId);
        }
    }

    [Serializable]
    public sealed class RequirementDefinitionData
    {
        [SerializeField] private string typeId;
        [SerializeField] private List<DefinitionParameterData> parameters = new List<DefinitionParameterData>();

        public RequirementDefinitionData()
        {
        }

        public RequirementDefinitionData(string typeId, IEnumerable<DefinitionParameterData> parameters)
        {
            this.typeId = typeId;
            this.parameters = new List<DefinitionParameterData>(parameters ?? throw new ArgumentNullException(nameof(parameters)));
        }

        public RequirementDefinition ToDomain()
        {
            var domainParameters = new List<DefinitionParameter>(parameters.Count);
            foreach (var parameter in parameters)
            {
                if (parameter == null)
                    throw new InvalidOperationException("Requirement contains a missing parameter.");
                domainParameters.Add(parameter.ToDomain());
            }

            return new RequirementDefinition(typeId, domainParameters);
        }
    }

    [Serializable]
    public sealed class DefinitionParameterData
    {
        [SerializeField] private string key;
        [SerializeField] private string value;

        public DefinitionParameterData()
        {
        }

        public DefinitionParameterData(string key, string value)
        {
            this.key = key;
            this.value = value;
        }

        public DefinitionParameter ToDomain() => new DefinitionParameter(key, value);
    }

    [Serializable]
    public sealed class GameEffectDefinitionData
    {
        [SerializeField] private string targetId;
        [SerializeField] private string operationId;
        [SerializeField] private string value;
        [SerializeField] private string durationId;
        [SerializeField] private string sourceId;

        public GameEffectDefinitionData()
        {
        }

        public GameEffectDefinitionData(
            string targetId,
            string operationId,
            string value,
            string durationId,
            string sourceId)
        {
            this.targetId = targetId;
            this.operationId = operationId;
            this.value = value;
            this.durationId = durationId;
            this.sourceId = sourceId;
        }

        public GameEffectDefinition ToDomain() =>
            new GameEffectDefinition(targetId, operationId, value, durationId, sourceId);
    }

    [Serializable]
    public sealed class EventTableDefinitionData
    {
        [SerializeField] private string id;
        [SerializeField] private List<EventReferenceData> events = new List<EventReferenceData>();

        public EventTableDefinitionData()
        {
        }

        public EventTableDefinitionData(string id, IEnumerable<EventReferenceData> events)
        {
            this.id = id;
            this.events = new List<EventReferenceData>(events ?? throw new ArgumentNullException(nameof(events)));
        }

        public EventTableDefinition ToDomain()
        {
            var eventKeys = new List<EventDefinitionKey>(events.Count);
            foreach (var eventReference in events)
            {
                if (eventReference == null)
                    throw new InvalidOperationException("Event table contains a missing event reference.");
                eventKeys.Add(eventReference.ToDomain());
            }

            return new EventTableDefinition(id, eventKeys);
        }
    }

    [Serializable]
    public sealed class EventReferenceData
    {
        [SerializeField] private string eventId;
        [SerializeField, Min(1)] private int version = 1;

        public EventReferenceData()
        {
        }

        public EventReferenceData(string eventId, int version)
        {
            this.eventId = eventId;
            this.version = version;
        }

        public EventDefinitionKey ToDomain() => new EventDefinitionKey(eventId, version);
    }
}

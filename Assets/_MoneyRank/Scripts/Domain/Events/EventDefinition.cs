using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace MoneyRank.Domain.Events
{
    public sealed class EventDefinition
    {
        private readonly ReadOnlyCollection<string> _tags;
        private readonly ReadOnlyCollection<RequirementDefinition> _conditions;
        private readonly ReadOnlyCollection<EventOptionDefinition> _options;

        public EventDefinition(
            string id,
            int version,
            string categoryId,
            string title,
            string educationalText,
            IEnumerable<string> tags,
            IEnumerable<RequirementDefinition> conditions,
            IEnumerable<EventOptionDefinition> options,
            string durationId = null,
            string presentationId = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Event ID cannot be empty.", nameof(id));
            if (version <= 0)
                throw new ArgumentOutOfRangeException(nameof(version), "Event version must be positive.");
            if (string.IsNullOrWhiteSpace(categoryId))
                throw new ArgumentException("Event category ID cannot be empty.", nameof(categoryId));
            if (string.IsNullOrWhiteSpace(title))
                throw new ArgumentException("Event title cannot be empty.", nameof(title));
            if (string.IsNullOrWhiteSpace(educationalText))
                throw new ArgumentException("Event educational text cannot be empty.", nameof(educationalText));
            if (tags == null)
                throw new ArgumentNullException(nameof(tags));
            if (conditions == null)
                throw new ArgumentNullException(nameof(conditions));
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var tagList = tags.ToList();
            var conditionList = conditions.ToList();
            var optionList = options.ToList();
            if (tagList.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Event tags cannot contain empty entries.", nameof(tags));
            if (tagList.Distinct(StringComparer.Ordinal).Count() != tagList.Count)
                throw new ArgumentException("Event tags must be unique.", nameof(tags));
            if (conditionList.Any(condition => condition == null))
                throw new ArgumentException("Event conditions cannot contain null entries.", nameof(conditions));
            if (optionList.Count == 0)
                throw new ArgumentException("An event requires at least one option.", nameof(options));
            if (optionList.Any(option => option == null))
                throw new ArgumentException("Event options cannot contain null entries.", nameof(options));
            if (optionList.Select(option => option.Id).Distinct(StringComparer.Ordinal).Count() != optionList.Count)
                throw new ArgumentException("Event option IDs must be unique within an event.", nameof(options));

            Id = id;
            Version = version;
            CategoryId = categoryId;
            Title = title;
            EducationalText = educationalText;
            DurationId = durationId;
            PresentationId = presentationId;
            _tags = tagList.AsReadOnly();
            _conditions = conditionList.AsReadOnly();
            _options = optionList.AsReadOnly();
        }

        public string Id { get; }
        public int Version { get; }
        public string CategoryId { get; }
        public string Title { get; }
        public string EducationalText { get; }
        public string DurationId { get; }
        public string PresentationId { get; }
        public IReadOnlyList<string> Tags => _tags;
        public IReadOnlyList<RequirementDefinition> Conditions => _conditions;
        public IReadOnlyList<EventOptionDefinition> Options => _options;

        public EventOptionDefinition GetOption(string optionId)
        {
            if (string.IsNullOrWhiteSpace(optionId))
                throw new ArgumentException("Event option ID cannot be empty.", nameof(optionId));

            return _options.FirstOrDefault(option => string.Equals(option.Id, optionId, StringComparison.Ordinal));
        }
    }
}

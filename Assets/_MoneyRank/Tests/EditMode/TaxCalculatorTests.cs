using System;
using MoneyRank.Content;
using MoneyRank.Domain.Economy;
using MoneyRank.Domain.Events;
using NUnit.Framework;

namespace MoneyRank.Tests.EditMode
{
    public sealed class TaxCalculatorTests
    {
        [Test]
        public void ConfiguredRateRoundingAndDestinationsProduceBalancedEntries()
        {
            var rule = new TaxRuleDefinition(
                "test-tax",
                "player-cash",
                0.175m,
                "whole-away-from-zero",
                new[]
                {
                    new TaxDestinationDefinition("public-treasury", 0.6m),
                    new TaxDestinationDefinition("service-fund", 0.4m)
                },
                Array.Empty<RequirementDefinition>());
            var calculator = new TaxCalculator(
                new ITaxRoundingPolicy[] { new WholeAwayFromZeroPolicy() });

            var result = calculator.Calculate(rule, 23m);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Value.TaxAmount, Is.EqualTo(4m));
            Assert.That(result.Value.Entries, Has.Count.EqualTo(3));
            Assert.That(result.Value.Entries[0].Amount, Is.EqualTo(-4m));
            Assert.That(result.Value.Entries[1].Amount, Is.EqualTo(2m));
            Assert.That(result.Value.Entries[2].Amount, Is.EqualTo(2m));
        }

        [Test]
        public void MissingRoundingPolicyDoesNotCalculateTax()
        {
            var calculator = new TaxCalculator(Array.Empty<ITaxRoundingPolicy>());

            var result = calculator.Calculate(CreateRule(), 10m);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("TAX_ROUNDING_POLICY_NOT_FOUND"));
        }

        [Test]
        public void PathologicalRoundingCannotProduceNegativeDestinationRemainder()
        {
            var rule = new TaxRuleDefinition(
                "test-tax",
                "player-cash",
                1m,
                "over-allocate",
                new[]
                {
                    new TaxDestinationDefinition("public-treasury", 0.6m),
                    new TaxDestinationDefinition("service-fund", 0.4m)
                },
                Array.Empty<RequirementDefinition>());
            var calculator = new TaxCalculator(
                new ITaxRoundingPolicy[] { new OverAllocatingPolicy() });

            var result = calculator.Calculate(rule, 10m);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorCode, Is.EqualTo("TAX_ALLOCATION_INVALID"));
        }

        [Test]
        public void DestinationSharesMustSumToOne()
        {
            Assert.Throws<ArgumentException>(() =>
                new TaxRuleDefinition(
                    "test-tax",
                    "player-cash",
                    0.2m,
                    "round",
                    new[] { new TaxDestinationDefinition("public-treasury", 0.8m) },
                    Array.Empty<RequirementDefinition>()));
        }

        [Test]
        public void SerializableTaxRulePreservesUnapprovedValuesAsData()
        {
            var data = new TaxRuleDefinitionData(
                "configurable-tax",
                "payer-account",
                "0.1375",
                "custom-rounding",
                new[] { new TaxDestinationData("configured-destination", "1") },
                Array.Empty<RequirementDefinitionData>());

            var rule = data.ToDomain();

            Assert.That(rule.Rate, Is.EqualTo(0.1375m));
            Assert.That(rule.RoundingPolicyId, Is.EqualTo("custom-rounding"));
            Assert.That(rule.Destinations[0].AccountId, Is.EqualTo("configured-destination"));
        }

        private static TaxRuleDefinition CreateRule() =>
            new TaxRuleDefinition(
                "test-tax",
                "player-cash",
                0.2m,
                "missing",
                new[] { new TaxDestinationDefinition("public-treasury", 1m) },
                Array.Empty<RequirementDefinition>());

        private sealed class WholeAwayFromZeroPolicy : ITaxRoundingPolicy
        {
            public string PolicyId => "whole-away-from-zero";

            public decimal Round(decimal value) =>
                Math.Round(value, 0, MidpointRounding.AwayFromZero);
        }

        private sealed class OverAllocatingPolicy : ITaxRoundingPolicy
        {
            private int _calls;
            public string PolicyId => "over-allocate";

            public decimal Round(decimal value)
            {
                _calls++;
                return _calls == 1 ? value : value + 5m;
            }
        }
    }
}

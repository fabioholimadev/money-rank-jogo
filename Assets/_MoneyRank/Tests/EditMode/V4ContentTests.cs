using System.IO;
using System.Linq;
using MoneyRank.Domain.V4;
using NUnit.Framework;
using UnityEngine;

namespace MoneyRank.Tests.EditMode
{
    /// <summary>Shared loader so every V4 test uses the real shipped content, never invented values.</summary>
    public static class V4TestContent
    {
        private static V4ContentSet _cached;

        public static string DataDir => Path.Combine(UnityEngine.Application.dataPath, "_MoneyRank", "Resources", "V4", "Data");
        public static string PackageRoot => Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, "..", "Docs", "V4"));

        public static V4ContentLoader.RawSources ReadSources() => new V4ContentLoader.RawSources
        {
            Casas = File.ReadAllText(Path.Combine(DataDir, "casas.csv")),
            Opcoes = File.ReadAllText(Path.Combine(DataDir, "cartas_opcoes.csv")),
            Personagens = File.ReadAllText(Path.Combine(DataDir, "personagens.csv")),
            Progressao = File.ReadAllText(Path.Combine(DataDir, "progressao.csv")),
            Dificuldades = File.ReadAllText(Path.Combine(DataDir, "dificuldades.csv")),
            Atributos = File.ReadAllText(Path.Combine(DataDir, "atributos.csv")),
            Moedas = File.ReadAllText(Path.Combine(DataDir, "moedas.csv")),
            EventosSistema = File.ReadAllText(Path.Combine(DataDir, "eventos_sistema.csv")),
            RotasLudo = File.ReadAllText(Path.Combine(DataDir, "rotas_ludo.csv")),
            ParametrosJson = File.ReadAllText(Path.Combine(DataDir, "parametros.json"))
        };

        public static V4ContentSet Content => _cached ?? (_cached = V4ContentLoader.Load(ReadSources()));
    }

    public sealed class V4ContentTests
    {
        [Test]
        public void ShippedContentPassesIntegrityValidation()
        {
            var errors = V4ContentValidator.Validate(V4TestContent.Content);
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void ArtReferencesExistInThePackage()
        {
            var root = V4TestContent.PackageRoot;
            var errors = V4ContentValidator.Validate(V4TestContent.Content, rel => File.Exists(Path.Combine(root, rel)));
            Assert.That(errors, Is.Empty, string.Join("\n", errors));
        }

        [Test]
        public void CountsMatchThePackageContract()
        {
            var c = V4TestContent.Content;
            Assert.That(c.Houses.Count, Is.EqualTo(36));
            Assert.That(c.Options.Count, Is.EqualTo(115));
            Assert.That(c.Progression.Count, Is.EqualTo(24));
            Assert.That(c.SystemEvents.Count, Is.EqualTo(9));
            Assert.That(c.Routes.Count, Is.EqualTo(228));
            Assert.That(c.Professions.Select(p => p.Salary), Is.EquivalentTo(new[] { 250, 700, 500, 300, 350, 400 }));
            Assert.That(c.Rules.RulesVersion, Is.EqualTo("4.1-pretest"));
        }

        [Test]
        public void MultipliersAreReadWithExactDecimals()
        {
            var economist = V4TestContent.Content.Professions.First(p => p.Name == "Economista");
            Assert.That(economist.Multipliers[(int)AttributeId.Investimento], Is.EqualTo(1.5m));
            Assert.That(economist.Multipliers[(int)AttributeId.Dividas], Is.EqualTo(0.75m));
            var hard = V4TestContent.Content.Difficulty("B1_dificil");
            Assert.That(hard.PositiveMultiplier, Is.EqualTo(0.75m));
            Assert.That(hard.NegativeMultiplier, Is.EqualTo(1.5m));
            Assert.That(hard.EliminateWithoutResources, Is.True);
        }

        [Test]
        public void HashIsStableAndSensitiveToContent()
        {
            var a = V4ContentLoader.Hash(V4TestContent.ReadSources());
            var b = V4ContentLoader.Hash(V4TestContent.ReadSources());
            Assert.That(a, Is.EqualTo(b));
            var changed = V4TestContent.ReadSources();
            changed.Casas += " ";
            Assert.That(V4ContentLoader.Hash(changed), Is.Not.EqualTo(a));
        }

        [Test]
        public void CsvReaderHandlesQuotesBomAndEmbeddedNewlines()
        {
            var rows = CsvReader.ReadRecords("﻿a,b\r\n\"x,1\",\"he said \"\"hi\"\"\"\r\n\"line1\nline2\",z\r\n");
            Assert.That(rows.Count, Is.EqualTo(2));
            Assert.That(rows[0]["a"], Is.EqualTo("x,1"));
            Assert.That(rows[0]["b"], Is.EqualTo("he said \"hi\""));
            Assert.That(rows[1]["a"], Is.EqualTo("line1\nline2"));
        }

        [Test]
        public void MiniJsonParsesTypedParameterObjects()
        {
            var o = MiniJson.ParseObject("{\"base_umr\":50,\"destino\":\"cofre\",\"x\":[1,2,{\"k\":true}],\"n\":null}");
            Assert.That(o["base_umr"], Is.EqualTo(50m));
            Assert.That(o["destino"], Is.EqualTo("cofre"));
            Assert.That(MiniJson.ParseObject("{}").Count, Is.EqualTo(0));
            Assert.Throws<System.FormatException>(() => MiniJson.Parse("{\"a\":}"));
        }

        [Test]
        public void ValidatorReportsBrokenContent()
        {
            var s = V4TestContent.ReadSources();
            s.Moedas = s.Moedas.Replace("60,60", "59,59");
            var broken = V4ContentLoader.Load(s);
            Assert.That(V4ContentValidator.Validate(broken), Is.Not.Empty);
        }
    }
}

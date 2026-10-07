using System;
using System.IO;
using System.Linq;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Infrastructure.Export;
using NUnit.Framework;
using UnityEngine;

namespace MoneyRank.Tests.EditMode
{
    public sealed class PlaytestExportTests
    {
        private string _temporaryDirectory;

        [SetUp]
        public void SetUp()
        {
            _temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                "money-rank-export-tests",
                Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_temporaryDirectory))
                Directory.Delete(_temporaryDirectory, true);
        }

        [Test]
        public void SummaryUsesAvailableSessionAndAuditDataWithoutInventingMetrics()
        {
            var session = CreateSession();
            var request = new PlaytestExportRequest(
                session,
                new SessionUndoJournal(),
                new DateTimeOffset(2026, 10, 4, 16, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 10, 4, 14, 30, 0, TimeSpan.Zero),
                new DateTimeOffset(2026, 10, 4, 15, 45, 0, TimeSpan.Zero));

            var summary = request.CreateSummary();

            Assert.That(summary.PlayerCount, Is.EqualTo(2));
            Assert.That(summary.EventResolutionCount, Is.Zero);
            Assert.That(summary.EconomyTransactionCount, Is.Zero);
            Assert.That(summary.UndoCount, Is.Zero);
            Assert.That(summary.Duration, Is.EqualTo(TimeSpan.FromMinutes(75)));
        }

        [Test]
        public void ExportCreatesCompleteJsonAndSingleRowCsv()
        {
            var request = new PlaytestExportRequest(
                CreateSession(),
                new SessionUndoJournal(),
                new DateTimeOffset(2026, 10, 4, 16, 0, 0, TimeSpan.Zero));
            var exporter = new FilePlaytestExporter();

            var result = exporter.Export(request, _temporaryDirectory);

            Assert.That(result.Succeeded, Is.True);
            var json = File.ReadAllText(result.Value.JsonPath);
            var probe = JsonUtility.FromJson<ExportProbe>(json);
            Assert.That(probe.exportSchemaVersion, Is.EqualTo(1));
            Assert.That(probe.participants, Has.Length.EqualTo(2));
            Assert.That(json, Does.Contain("\"session\""));
            Assert.That(json, Does.Contain($"\"schemaVersion\": {GameSession.CurrentSaveSchemaVersion}"));
            var csvLines = File.ReadAllLines(result.Value.CsvPath);
            Assert.That(csvLines, Has.Length.EqualTo(2));
            Assert.That(csvLines[0], Does.StartWith("sessionId,phase,playerCount"));
            Assert.That(csvLines[1], Does.Contain("export-session,Setup,2"));
        }

        [Test]
        public void ReExportPreservesPreviousFilesAsBackups()
        {
            var exporter = new FilePlaytestExporter();
            var request = new PlaytestExportRequest(
                CreateSession(),
                new SessionUndoJournal(),
                DateTimeOffset.UtcNow);

            var first = exporter.Export(request, _temporaryDirectory);
            var second = exporter.Export(request, _temporaryDirectory);

            Assert.That(first.Succeeded, Is.True);
            Assert.That(second.Succeeded, Is.True);
            Assert.That(File.Exists(second.Value.JsonPath + ".bak"), Is.True);
            Assert.That(File.Exists(second.Value.CsvPath + ".bak"), Is.True);
        }

        private static GameSession CreateSession()
        {
            var board = new BoardDefinition(
                "board",
                Enumerable.Range(0, 4)
                    .Select(index => new BoardSpaceDefinition($"space-{index}", index, "event")),
                0);
            var session = new GameSession(
                new SessionId("export-session"),
                new SessionRules(2, 2),
                new GameFlowStateMachine(),
                board);
            session.AddPlayer(new Player(new PlayerId("p1"), "Player, One", 0));
            session.AddPlayer(new Player(new PlayerId("p2"), "Player Two", 1));
            return session;
        }

        [Serializable]
        private sealed class ExportProbe
        {
            public int exportSchemaVersion;
            public ParticipantProbe[] participants;
        }

        [Serializable]
        private sealed class ParticipantProbe
        {
            public string playerId;
            public string displayName;
            public int turnOrder;
        }
    }
}

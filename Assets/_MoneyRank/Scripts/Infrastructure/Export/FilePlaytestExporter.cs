using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using MoneyRank.Application;
using MoneyRank.Domain;
using MoneyRank.Infrastructure.Persistence;
using UnityEngine;

namespace MoneyRank.Infrastructure.Export
{
    public sealed class FilePlaytestExporter
    {
        private readonly JsonGameSessionMementoSerializer _sessionSerializer;

        public FilePlaytestExporter(JsonGameSessionMementoSerializer sessionSerializer = null)
        {
            _sessionSerializer = sessionSerializer ?? new JsonGameSessionMementoSerializer();
        }

        public OperationResult<PlaytestExportResult> Export(
            PlaytestExportRequest request,
            string outputDirectory)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Export directory cannot be empty.", nameof(outputDirectory));

            var root = Path.GetFullPath(outputDirectory);
            var stem = $"playtest-{Hash(request.Session.SessionId.Value)}";
            var jsonPath = Path.Combine(root, stem + ".json");
            var csvPath = Path.Combine(root, stem + ".csv");
            var jsonTemporary = jsonPath + ".tmp";
            var csvTemporary = csvPath + ".tmp";

            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(jsonTemporary, BuildJson(request), new UTF8Encoding(false));
                File.WriteAllText(csvTemporary, BuildCsv(request.CreateSummary()), new UTF8Encoding(false));
                Replace(jsonTemporary, jsonPath);
                Replace(csvTemporary, csvPath);
                return OperationResult<PlaytestExportResult>.Success(
                    new PlaytestExportResult(jsonPath, csvPath));
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is ArgumentException ||
                exception is NotSupportedException)
            {
                TryDelete(jsonTemporary);
                TryDelete(csvTemporary);
                return OperationResult<PlaytestExportResult>.Failure(
                    "PLAYTEST_EXPORT_FAILED",
                    exception.Message);
            }
        }

        private string BuildJson(PlaytestExportRequest request)
        {
            var summary = request.CreateSummary();
            var metadata = new ExportMetadataDto
            {
                exportSchemaVersion = 1,
                exportedAt = request.ExportedAt.ToString("O", CultureInfo.InvariantCulture),
                startedAt = request.StartedAt?.ToString("O", CultureInfo.InvariantCulture),
                endedAt = request.EndedAt?.ToString("O", CultureInfo.InvariantCulture),
                durationSeconds = summary.Duration?.TotalSeconds.ToString(CultureInfo.InvariantCulture),
                participants = request.Participants.Select(participant => new ParticipantDto
                {
                    playerId = participant.PlayerId.Value,
                    displayName = participant.DisplayName,
                    turnOrder = participant.TurnOrder,
                    characterId = participant.CharacterId
                }).ToArray(),
                undoRecords = request.UndoRecords.Select(record => new UndoDto
                {
                    id = record.Id,
                    checkpointId = record.CheckpointId,
                    createdAt = record.CreatedAt.ToString("O", CultureInfo.InvariantCulture),
                    operatorId = record.OperatorId
                }).ToArray()
            };
            var metadataJson = JsonUtility.ToJson(metadata, true);
            var finalBrace = metadataJson.LastIndexOf('}');
            return metadataJson.Substring(0, finalBrace) +
                   ",\n  \"session\": " +
                   Indent(_sessionSerializer.Serialize(request.Session), 2) +
                   "\n}";
        }

        private static string BuildCsv(PlaytestExportSummary summary)
        {
            var header =
                "sessionId,phase,playerCount,actionCount,eventResolutionCount,economyTransactionCount," +
                "processedAnnualCycles,hospitalizedPlayerCount,undoCount,durationSeconds,exportedAt";
            var values = new[]
            {
                summary.SessionId.Value,
                summary.Phase.ToString(),
                summary.PlayerCount.ToString(CultureInfo.InvariantCulture),
                summary.ActionCount.ToString(CultureInfo.InvariantCulture),
                summary.EventResolutionCount.ToString(CultureInfo.InvariantCulture),
                summary.EconomyTransactionCount.ToString(CultureInfo.InvariantCulture),
                summary.ProcessedAnnualCycles.ToString(CultureInfo.InvariantCulture),
                summary.HospitalizedPlayerCount.ToString(CultureInfo.InvariantCulture),
                summary.UndoCount.ToString(CultureInfo.InvariantCulture),
                summary.Duration?.TotalSeconds.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                summary.ExportedAt.ToString("O", CultureInfo.InvariantCulture)
            };
            return header + Environment.NewLine + string.Join(",", values.Select(EscapeCsv)) + Environment.NewLine;
        }

        private static string EscapeCsv(string value)
        {
            value = value ?? string.Empty;
            return value.IndexOfAny(new[] { ',', '"', '\r', '\n' }) < 0
                ? value
                : "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static string Indent(string value, int spaces)
        {
            var prefix = new string(' ', spaces);
            return value.Replace("\r\n", "\n").Replace("\n", "\n" + prefix);
        }

        private static void Replace(string temporaryPath, string finalPath)
        {
            if (File.Exists(finalPath))
                File.Replace(temporaryPath, finalPath, finalPath + ".bak", true);
            else
                File.Move(temporaryPath, finalPath);
        }

        private static string Hash(string value)
        {
            using (var algorithm = SHA256.Create())
            {
                var bytes = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));
                var builder = new StringBuilder(bytes.Length * 2);
                foreach (var item in bytes)
                    builder.Append(item.ToString("x2"));
                return builder.ToString();
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        [Serializable]
        private sealed class ExportMetadataDto
        {
            public int exportSchemaVersion;
            public string exportedAt;
            public string startedAt;
            public string endedAt;
            public string durationSeconds;
            public ParticipantDto[] participants;
            public UndoDto[] undoRecords;
        }

        [Serializable]
        private sealed class ParticipantDto
        {
            public string playerId;
            public string displayName;
            public int turnOrder;
            public string characterId;
        }

        [Serializable]
        private sealed class UndoDto
        {
            public string id;
            public string checkpointId;
            public string createdAt;
            public string operatorId;
        }
    }
}

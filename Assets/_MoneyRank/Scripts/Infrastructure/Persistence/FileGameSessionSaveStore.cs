using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MoneyRank.Application;
using MoneyRank.Domain;

namespace MoneyRank.Infrastructure.Persistence
{
    public sealed class FileGameSessionSaveStore : IGameSessionSaveStore
    {
        private readonly string _rootDirectory;
        private readonly JsonGameSessionMementoSerializer _serializer;

        public FileGameSessionSaveStore(
            string rootDirectory,
            JsonGameSessionMementoSerializer serializer = null)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new ArgumentException("Save root directory cannot be empty.", nameof(rootDirectory));

            _rootDirectory = Path.GetFullPath(rootDirectory);
            _serializer = serializer ?? new JsonGameSessionMementoSerializer();
        }

        public OperationResult Save(GameSessionMemento memento)
        {
            if (memento == null)
                throw new ArgumentNullException(nameof(memento));

            var path = GetSavePath(memento.SessionId);
            var temporaryPath = path + ".tmp";
            var backupPath = path + ".bak";
            try
            {
                Directory.CreateDirectory(_rootDirectory);
                File.WriteAllText(temporaryPath, _serializer.Serialize(memento), new UTF8Encoding(false));
                if (File.Exists(path))
                    File.Replace(temporaryPath, path, backupPath, true);
                else
                    File.Move(temporaryPath, path);

                return OperationResult.Success();
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is ArgumentException ||
                exception is NotSupportedException)
            {
                TryDeleteTemporaryFile(temporaryPath);
                return OperationResult.Failure("SESSION_SAVE_FAILED", exception.Message);
            }
        }

        public OperationResult<GameSessionMemento> Load(SessionId sessionId)
        {
            var path = GetSavePath(sessionId);
            if (!File.Exists(path))
            {
                return OperationResult<GameSessionMemento>.Failure(
                    "SESSION_SAVE_NOT_FOUND",
                    $"No local save exists for session '{sessionId}'.");
            }

            try
            {
                return OperationResult<GameSessionMemento>.Success(
                    _serializer.Deserialize(File.ReadAllText(path, Encoding.UTF8)));
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is ArgumentException ||
                exception is FormatException ||
                exception is OverflowException)
            {
                return OperationResult<GameSessionMemento>.Failure(
                    "SESSION_LOAD_FAILED",
                    exception.Message);
            }
        }

        public string GetSavePath(SessionId sessionId) =>
            Path.Combine(_rootDirectory, $"session-{Hash(sessionId.Value)}.json");

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

        private static void TryDeleteTemporaryFile(string path)
        {
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}

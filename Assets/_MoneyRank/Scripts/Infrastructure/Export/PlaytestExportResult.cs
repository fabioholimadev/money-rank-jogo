namespace MoneyRank.Infrastructure.Export
{
    public sealed class PlaytestExportResult
    {
        public PlaytestExportResult(string jsonPath, string csvPath)
        {
            JsonPath = jsonPath;
            CsvPath = csvPath;
        }

        public string JsonPath { get; }
        public string CsvPath { get; }
    }
}

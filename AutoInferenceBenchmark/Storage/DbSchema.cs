namespace AutoInferenceBenchmark.Storage;

internal static class DbSchema
{
    public const int CurrentVersion = 1;

    public const string CreateTables = """
        CREATE TABLE IF NOT EXISTS SchemaVersion (
            Version INTEGER NOT NULL
        );

        CREATE TABLE IF NOT EXISTS BenchmarkRuns (
            Id          INTEGER PRIMARY KEY AUTOINCREMENT,
            ModelPath   TEXT    NOT NULL,
            ModelName   TEXT    NOT NULL,
            SweepMode   TEXT    NOT NULL,
            StartedAt   TEXT    NOT NULL,
            FinishedAt  TEXT,
            TotalConfigs INTEGER NOT NULL DEFAULT 0,
            TotalTests  INTEGER NOT NULL DEFAULT 0,
            BestScore   REAL,
            BestConfigJson TEXT
        );

        CREATE TABLE IF NOT EXISTS BenchmarkResults (
            Id              INTEGER PRIMARY KEY AUTOINCREMENT,
            RunId           INTEGER NOT NULL,
            TestCaseId      TEXT    NOT NULL,
            TestCaseName    TEXT    NOT NULL,
            Temperature     REAL    NOT NULL,
            TopP            REAL    NOT NULL,
            TopK            INTEGER NOT NULL,
            MinP            REAL    NOT NULL,
            RepeatPenalty   REAL    NOT NULL,
            FrequencyPenalty REAL   NOT NULL,
            PresencePenalty REAL    NOT NULL,
            MaxTokens       INTEGER NOT NULL,
            Seed            INTEGER NOT NULL,
            ResponseText    TEXT    NOT NULL,
            MatchPercentage REAL    NOT NULL,
            IsPass          INTEGER NOT NULL,
            TokensPerSecond REAL    NOT NULL,
            TimeToFirstToken REAL   NOT NULL,
            TotalLatency    REAL    NOT NULL,
            TokenCount      INTEGER NOT NULL,
            Timestamp       TEXT    NOT NULL,
            FOREIGN KEY (RunId) REFERENCES BenchmarkRuns(Id)
        );

        CREATE INDEX IF NOT EXISTS IX_BenchmarkResults_RunId ON BenchmarkResults(RunId);
        CREATE INDEX IF NOT EXISTS IX_BenchmarkResults_Score ON BenchmarkResults(MatchPercentage DESC);
        """;
}

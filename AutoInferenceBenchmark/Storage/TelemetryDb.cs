using System.Text.Json;
using AutoInferenceBenchmark.Core;
using Microsoft.Data.Sqlite;

namespace AutoInferenceBenchmark.Storage;

/// <summary>
/// SQLite-backed persistent storage for benchmark runs and results.
/// Thread-safe: all public methods synchronize on the connection.
/// </summary>
public sealed class TelemetryDb : IDisposable
{
    private readonly SqliteConnection _conn;
    private readonly object _lock = new();

    public TelemetryDb(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        EnsureSchema();
    }

    /// <summary>Default database path in %AppData%.</summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ApexUIBridge", "telemetry.db");

    private void EnsureSchema()
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = DbSchema.CreateTables;
        cmd.ExecuteNonQuery();

        // Check/set schema version
        cmd.CommandText = "SELECT COUNT(*) FROM SchemaVersion";
        var count = (long)cmd.ExecuteScalar()!;
        if (count == 0)
        {
            cmd.CommandText = $"INSERT INTO SchemaVersion (Version) VALUES ({DbSchema.CurrentVersion})";
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>Creates a new benchmark run record and returns its ID.</summary>
    public long CreateRun(string modelPath, string modelName, SweepMode sweepMode, int totalConfigs, int totalTests)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO BenchmarkRuns (ModelPath, ModelName, SweepMode, StartedAt, TotalConfigs, TotalTests)
                VALUES (@path, @name, @mode, @started, @configs, @tests);
                SELECT last_insert_rowid();
                """;
            cmd.Parameters.AddWithValue("@path", modelPath);
            cmd.Parameters.AddWithValue("@name", modelName);
            cmd.Parameters.AddWithValue("@mode", sweepMode.ToString());
            cmd.Parameters.AddWithValue("@started", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("@configs", totalConfigs);
            cmd.Parameters.AddWithValue("@tests", totalTests);
            return (long)cmd.ExecuteScalar()!;
        }
    }

    /// <summary>Saves a single benchmark result (one test case × one config).</summary>
    public void SaveResult(BenchmarkResult result)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO BenchmarkResults
                (RunId, TestCaseId, TestCaseName, Temperature, TopP, TopK, MinP,
                 RepeatPenalty, FrequencyPenalty, PresencePenalty, MaxTokens, Seed,
                 ResponseText, MatchPercentage, IsPass, TokensPerSecond,
                 TimeToFirstToken, TotalLatency, TokenCount, Timestamp)
                VALUES
                (@runId, @tcId, @tcName, @temp, @topP, @topK, @minP,
                 @repPen, @freqPen, @presPen, @maxTok, @seed,
                 @resp, @match, @pass, @tps,
                 @ttft, @latency, @tokens, @ts)
                """;
            cmd.Parameters.AddWithValue("@runId", result.RunId);
            cmd.Parameters.AddWithValue("@tcId", result.TestCaseId.ToString());
            cmd.Parameters.AddWithValue("@tcName", result.TestCaseName);
            cmd.Parameters.AddWithValue("@temp", result.Config.Temperature);
            cmd.Parameters.AddWithValue("@topP", result.Config.TopP);
            cmd.Parameters.AddWithValue("@topK", result.Config.TopK);
            cmd.Parameters.AddWithValue("@minP", result.Config.MinP);
            cmd.Parameters.AddWithValue("@repPen", result.Config.RepeatPenalty);
            cmd.Parameters.AddWithValue("@freqPen", result.Config.FrequencyPenalty);
            cmd.Parameters.AddWithValue("@presPen", result.Config.PresencePenalty);
            cmd.Parameters.AddWithValue("@maxTok", result.Config.MaxTokens);
            cmd.Parameters.AddWithValue("@seed", (long)result.Config.Seed);
            cmd.Parameters.AddWithValue("@resp", result.ResponseText);
            cmd.Parameters.AddWithValue("@match", result.MatchPercentage);
            cmd.Parameters.AddWithValue("@pass", result.IsPass ? 1 : 0);
            cmd.Parameters.AddWithValue("@tps", result.TokensPerSecond);
            cmd.Parameters.AddWithValue("@ttft", result.TimeToFirstTokenSeconds);
            cmd.Parameters.AddWithValue("@latency", result.TotalLatencySeconds);
            cmd.Parameters.AddWithValue("@tokens", result.TokenCount);
            cmd.Parameters.AddWithValue("@ts", result.Timestamp.ToString("O"));
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>Marks a run as finished and records the best result.</summary>
    public void FinishRun(long runId, float bestScore, InferenceConfig? bestConfig)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                UPDATE BenchmarkRuns
                SET FinishedAt = @finished, BestScore = @score, BestConfigJson = @config
                WHERE Id = @id
                """;
            cmd.Parameters.AddWithValue("@id", runId);
            cmd.Parameters.AddWithValue("@finished", DateTime.UtcNow.ToString("O"));
            cmd.Parameters.AddWithValue("@score", bestScore);
            cmd.Parameters.AddWithValue("@config", bestConfig != null
                ? JsonSerializer.Serialize(bestConfig) : DBNull.Value);
            cmd.ExecuteNonQuery();
        }
    }

    /// <summary>Returns all results for a given run, ordered by match percentage descending.</summary>
    public List<BenchmarkResult> GetResultsForRun(long runId)
    {
        lock (_lock)
        {
            var results = new List<BenchmarkResult>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT * FROM BenchmarkResults WHERE RunId = @runId ORDER BY MatchPercentage DESC
                """;
            cmd.Parameters.AddWithValue("@runId", runId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                results.Add(ReadResult(reader));
            return results;
        }
    }

    /// <summary>Returns all runs, most recent first.</summary>
    public List<(long Id, string ModelName, string SweepMode, DateTime StartedAt, float? BestScore)> GetAllRuns()
    {
        lock (_lock)
        {
            var runs = new List<(long, string, string, DateTime, float?)>();
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = "SELECT Id, ModelName, SweepMode, StartedAt, BestScore FROM BenchmarkRuns ORDER BY Id DESC";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                runs.Add((
                    reader.GetInt64(0),
                    reader.GetString(1),
                    reader.GetString(2),
                    DateTime.Parse(reader.GetString(3)),
                    reader.IsDBNull(4) ? null : reader.GetFloat(4)
                ));
            }
            return runs;
        }
    }

    /// <summary>Returns the best config (highest average score) for a given model path.</summary>
    public InferenceConfig? GetBestConfigForModel(string modelPath)
    {
        lock (_lock)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = """
                SELECT BestConfigJson FROM BenchmarkRuns
                WHERE ModelPath = @path AND BestConfigJson IS NOT NULL
                ORDER BY BestScore DESC LIMIT 1
                """;
            cmd.Parameters.AddWithValue("@path", modelPath);
            var json = cmd.ExecuteScalar() as string;
            return json != null ? JsonSerializer.Deserialize<InferenceConfig>(json) : null;
        }
    }

    private static BenchmarkResult ReadResult(SqliteDataReader r) => new()
    {
        Id = r.GetInt64(r.GetOrdinal("Id")),
        RunId = r.GetInt64(r.GetOrdinal("RunId")),
        TestCaseId = Guid.Parse(r.GetString(r.GetOrdinal("TestCaseId"))),
        TestCaseName = r.GetString(r.GetOrdinal("TestCaseName")),
        Config = new InferenceConfig
        {
            Temperature = r.GetFloat(r.GetOrdinal("Temperature")),
            TopP = r.GetFloat(r.GetOrdinal("TopP")),
            TopK = r.GetInt32(r.GetOrdinal("TopK")),
            MinP = r.GetFloat(r.GetOrdinal("MinP")),
            RepeatPenalty = r.GetFloat(r.GetOrdinal("RepeatPenalty")),
            FrequencyPenalty = r.GetFloat(r.GetOrdinal("FrequencyPenalty")),
            PresencePenalty = r.GetFloat(r.GetOrdinal("PresencePenalty")),
            MaxTokens = r.GetInt32(r.GetOrdinal("MaxTokens")),
            Seed = (uint)r.GetInt64(r.GetOrdinal("Seed"))
        },
        ResponseText = r.GetString(r.GetOrdinal("ResponseText")),
        MatchPercentage = r.GetFloat(r.GetOrdinal("MatchPercentage")),
        IsPass = r.GetInt32(r.GetOrdinal("IsPass")) != 0,
        TokensPerSecond = r.GetFloat(r.GetOrdinal("TokensPerSecond")),
        TimeToFirstTokenSeconds = r.GetFloat(r.GetOrdinal("TimeToFirstToken")),
        TotalLatencySeconds = r.GetDouble(r.GetOrdinal("TotalLatency")),
        TokenCount = r.GetInt32(r.GetOrdinal("TokenCount")),
        Timestamp = DateTime.Parse(r.GetString(r.GetOrdinal("Timestamp")))
    };

    public void Dispose() => _conn.Dispose();
}

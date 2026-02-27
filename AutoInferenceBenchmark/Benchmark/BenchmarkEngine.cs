using AutoInferenceBenchmark.Core;
using AutoInferenceBenchmark.Scoring;
using AutoInferenceBenchmark.Storage;

namespace AutoInferenceBenchmark.Benchmark;

/// <summary>
/// Core orchestrator: runs test datasets across parameter combinations,
/// scores each response, and persists results to SQLite incrementally.
/// </summary>
public sealed class BenchmarkEngine
{
    private readonly TelemetryDb _db;
    private readonly IResponseScorer _scorer;

    public BenchmarkEngine(TelemetryDb db, IResponseScorer? scorer = null)
    {
        _db = db;
        _scorer = scorer ?? new SimilarityScorer();
    }

    /// <summary>
    /// Runs the full benchmark: for each parameter config × each test case,
    /// runs inference, scores the result, and saves to the database.
    /// </summary>
    public async Task<BenchmarkRunSummary> RunBenchmarkAsync(
        IInferenceClient client,
        string modelPath,
        string systemPrompt,
        TestDataset dataset,
        ParameterSweepConfig sweep,
        IProgress<BenchmarkProgress>? progress = null,
        CancellationToken ct = default)
    {
        var configs = ParameterSweepGenerator.Generate(sweep);
        var testCases = dataset.TestCases;
        int totalRuns = configs.Count * testCases.Count;

        var modelName = Path.GetFileNameWithoutExtension(modelPath);
        long runId = _db.CreateRun(modelPath, modelName, sweep.Mode, configs.Count, testCases.Count);

        var allResults = new List<BenchmarkResult>();
        var startTime = DateTime.UtcNow;
        int completed = 0;
        float bestAvgScore = 0f;
        InferenceConfig bestConfig = configs[0];

        // Track per-config average scores
        var configScores = new Dictionary<InferenceConfig, List<float>>();

        try
        {
            foreach (var config in configs)
            {
                ct.ThrowIfCancellationRequested();

                if (!configScores.ContainsKey(config))
                    configScores[config] = new List<float>();

                foreach (var testCase in testCases)
                {
                    ct.ThrowIfCancellationRequested();

                    // Reset conversation for isolation
                    client.ResetConversation(systemPrompt);

                    // Run inference
                    InferenceResult inferenceResult;
                    try
                    {
                        inferenceResult = await client.RunInferenceAsync(testCase.Prompt, config, ct);
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        // Record failed inference as 0% match
                        inferenceResult = new InferenceResult
                        {
                            ResponseText = $"[ERROR] {ex.Message}",
                            TokenCount = 0,
                            TokensPerSecond = 0,
                            TimeToFirstTokenSeconds = 0,
                            TotalLatencySeconds = 0,
                            Config = config
                        };
                    }

                    // Score the response
                    var score = _scorer.Score(
                        testCase.ExpectedResponse,
                        inferenceResult.ResponseText,
                        testCase.MatchMode,
                        testCase.SimilarityThreshold);

                    var benchResult = new BenchmarkResult
                    {
                        RunId = runId,
                        TestCaseId = testCase.Id,
                        TestCaseName = testCase.Name,
                        Config = config,
                        ResponseText = inferenceResult.ResponseText,
                        MatchPercentage = score.MatchPercentage,
                        IsPass = score.IsPass,
                        TokensPerSecond = inferenceResult.TokensPerSecond,
                        TimeToFirstTokenSeconds = inferenceResult.TimeToFirstTokenSeconds,
                        TotalLatencySeconds = inferenceResult.TotalLatencySeconds,
                        TokenCount = inferenceResult.TokenCount,
                        Timestamp = DateTime.UtcNow
                    };

                    // Save incrementally (crash-safe)
                    _db.SaveResult(benchResult);
                    allResults.Add(benchResult);
                    configScores[config].Add(score.MatchPercentage);

                    completed++;

                    // Update best config tracking
                    float currentAvg = configScores[config].Average();
                    if (configScores[config].Count == testCases.Count && currentAvg > bestAvgScore)
                    {
                        bestAvgScore = currentAvg;
                        bestConfig = config;
                    }

                    // Report progress
                    var elapsed = DateTime.UtcNow - startTime;
                    TimeSpan? eta = completed > 0
                        ? TimeSpan.FromSeconds(elapsed.TotalSeconds / completed * (totalRuns - completed))
                        : null;

                    progress?.Report(new BenchmarkProgress
                    {
                        CurrentConfig = config,
                        CurrentTestCaseName = testCase.Name,
                        CompletedCount = completed,
                        TotalCount = totalRuns,
                        BestScoreSoFar = bestAvgScore,
                        CurrentScore = score.MatchPercentage,
                        ElapsedTime = elapsed,
                        EstimatedTimeRemaining = eta
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Partial results already saved — finalize the run as cancelled
            _db.FinishRun(runId, bestAvgScore, bestConfig);
            return new BenchmarkRunSummary
            {
                RunId = runId,
                ModelPath = modelPath,
                ModelName = modelName,
                TotalConfigs = configs.Count,
                TotalTests = testCases.Count,
                TotalRuns = completed,
                BestConfig = bestConfig,
                BestAverageScore = bestAvgScore,
                Duration = DateTime.UtcNow - startTime,
                WasCancelled = true,
                AllResults = allResults
            };
        }

        _db.FinishRun(runId, bestAvgScore, bestConfig);

        return new BenchmarkRunSummary
        {
            RunId = runId,
            ModelPath = modelPath,
            ModelName = modelName,
            TotalConfigs = configs.Count,
            TotalTests = testCases.Count,
            TotalRuns = completed,
            BestConfig = bestConfig,
            BestAverageScore = bestAvgScore,
            Duration = DateTime.UtcNow - startTime,
            WasCancelled = false,
            AllResults = allResults
        };
    }
}

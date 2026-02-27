using AutoInferenceBenchmark.Core;

namespace AutoInferenceBenchmark.Scoring;

public interface IResponseScorer
{
    ScoringResult Score(string expected, string actual, MatchMode mode, float similarityThreshold);
}

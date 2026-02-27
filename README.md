# AutoInferenceBenchmark

AutoInferenceBenchmark is a Windows desktop application for evaluating and tuning local GGUF LLMs (via LLamaSharp/llama.cpp) with repeatable benchmark datasets.

It is designed as a practical control layer around inference, with emphasis on:

- **Configuration exploration** (temperature-only or full parameter sweeps)
- **Prompt-template-aware model execution** (auto-detects chat template format)
- **Response scoring** (exact or similarity-based)
- **Persistent telemetry** (SQLite run/result history)
- **Apply-best workflow** (move best benchmark config back to chat settings)

> Current implementation focus: local inference with LLamaSharp adapters and benchmark automation. Some advanced optimization components (e.g., drift detection / hill-climbing) are included as scaffolds for future integration.

---

<img width="991" height="657" alt="image" src="https://github.com/user-attachments/assets/e76f5658-bf37-4f57-829c-4476e9875cfa" />


---

## Table of Contents

- [Architecture at a Glance](#architecture-at-a-glance)
- [How the System Works](#how-the-system-works)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Build and Run](#build-and-run)
- [Using the App](#using-the-app)
- [Dataset Format](#dataset-format)
- [Scoring Model](#scoring-model)
- [Telemetry and Persistence](#telemetry-and-persistence)
- [Prompt Canonicalization, Drift, and Refinement](#prompt-canonicalization-drift-and-refinement)
- [Troubleshooting](#troubleshooting)
- [Roadmap Ideas](#roadmap-ideas)

---

## Architecture at a Glance

The `AutoInferenceBenchmark` project is a WinForms app (`net10.0-windows`) that sits on top of LLamaSharp and provides benchmarking + comparison workflows.

### Main runtime flow

1. User selects a local GGUF model and system prompt in the Chat tab.
2. Benchmark tab defines a test dataset + sweep ranges.
3. App generates parameter configurations.
4. App runs each test case against each config.
5. Responses are scored and persisted to SQLite incrementally.
6. UI displays live progress, best-so-far, and full results.
7. User can apply the best config back to active chat settings.

---

## How the System Works

## 1) Inference client selection

When a benchmark starts, the app loads the model and inspects GGUF metadata (`tokenizer.chat_template`) to detect template style and choose chat vs instruct execution path automatically.

- Chat-oriented templates: InteractiveExecutor-backed adapter
- Instruct-oriented templates: InstructExecutor-backed adapter

## 2) Parameter sweep generation

The benchmark supports two sweep modes:

- **TemperatureOnly**: vary temperature only, keep all other params at defaults
- **AllCombinations**: cartesian product of temperature, top-p, top-k, min-p, repeat penalty ranges

The app also computes estimated total run count (`configs × test cases`) before execution.

## 3) Execution and scoring

For each `(config, test case)` pair:

- conversation state is reset for isolation
- inference runs with the selected sampling settings
- scoring compares output to expected response (exact or similarity mode)
- row is persisted immediately to SQLite (crash-safe incremental logging)

## 4) Results and feedback loop

- best average-scoring config is tracked across the run
- run summary includes duration and cancellation status
- user can export CSV and apply the best config directly to Form1 settings

---

## Project Structure

```text
AutoInferenceBenchmark/
  Benchmark/           # BenchmarkEngine, progress DTOs, sweep generator
  Canonicalization/    # PromptCanonicalizer (normalization map + transforms)
  Clients/             # LLamaSharp chat/instruct adapters + factory
  Core/                # TestCase, TestDataset, InferenceConfig, results models
  Forms/               # BenchmarkPanel + test case editor
  LlamaSharp/          # Settings and local inference support classes
  Optimization/        # DriftDetector, HillClimbOptimizer (scaffolds)
  Scoring/             # Similarity and normalization logic
  Storage/             # SQLite schema + TelemetryDb
  Templates/           # Chat template detection and prompt formatters
  Form1.cs             # Main app UI + chat/inference interaction
  Program.cs           # App entrypoint + native llama init
```

---

## Prerequisites

- **Windows** (WinForms target)
- **.NET SDK supporting `net10.0-windows`**
- A **local GGUF model file**
- Native runtime support via LLamaSharp package dependencies

NuGet dependencies include:

- `LLamaSharp.Backend.Cpu` (0.26.0)
- `Microsoft.Data.Sqlite` (9.0.3)

---

## Build and Run

From solution root:

```bash
dotnet restore
dotnet build AutoInferenceBenchmark.sln
dotnet run --project AutoInferenceBenchmark/AutoInferenceBenchmark.csproj
```

If you use Visual Studio, open `AutoInferenceBenchmark.sln` and run the `AutoInferenceBenchmark` startup project.

---

## Using the App

## 1) Configure model and chat settings

- Select provider (`LlamaSharp (Local)` or `LlamaSharp Instruct (Local)`)
- Browse/select GGUF model path
- Set system prompt and advanced settings (threads/context/max tokens/etc.)

Settings are persisted under `%AppData%\ApexUIBridge\ai-settings.json`.

## 2) Prepare benchmark dataset

In the Benchmark tab:

- add/edit/remove test cases
- import/export dataset JSON
- each case includes prompt, expected response, difficulty, match mode, threshold

A default starter dataset is created automatically on panel initialization.

## 3) Configure sweep

Choose either:

- **Temperature Only**
- **All Combinations**

Tune min/max/step for:

- Temperature
- Top-P
- Top-K
- Min-P
- Repeat Penalty

Optionally enforce deterministic seed (`42`) for reproducibility.

## 4) Run benchmark

Click **Start Benchmark**:

- model is loaded (auto adapter selection)
- run progresses with live ETA and best score display
- stop/cancel is supported

## 5) Analyze and apply

After completion:

- review sorted results grid with score + latency metrics
- export CSV
- click **Apply Best Config** to push the winning temperature into main settings workflow

---

## Dataset Format

Datasets are JSON-serializable `TestDataset` objects with a `TestCases` array.

Each test case includes:

- `Id` (GUID)
- `Name`
- `Difficulty` (`Easy|Medium|Complex`)
- `Prompt`
- `ExpectedResponse`
- `MatchMode` (`Exact|Similarity`)
- `SimilarityThreshold` (0-100)

Minimal example:

```json
{
  "name": "Quick smoke",
  "testCases": [
    {
      "id": "9a9236ac-17c1-4f95-a5cb-39d74770f1cf",
      "name": "Simple Arithmetic",
      "difficulty": "Easy",
      "prompt": "What is 2 + 2? Answer with just the number.",
      "expectedResponse": "4",
      "matchMode": "Similarity",
      "similarityThreshold": 80
    }
  ]
}
```

---

## Scoring Model

`SimilarityScorer` supports:

- **Exact mode**: normalized case-insensitive equality
- **Similarity mode**: composite of
  - Levenshtein similarity
  - Jaccard token overlap
  - LCS similarity

Composite rule:

- `composite = 0.5 * max(metric) + 0.5 * average(metrics)`
- pass/fail determined by `SimilarityThreshold`

This helps avoid over-penalizing formatting/style variance while still enforcing quality.

---

## Telemetry and Persistence

Benchmark telemetry is stored in SQLite at:

`%AppData%\ApexUIBridge\telemetry.db`

Tables:

- `SchemaVersion`
- `BenchmarkRuns`
- `BenchmarkResults`

Design notes:

- each result row is persisted immediately during execution
- run completion stores best score + serialized best config JSON
- run and score indexes support retrieval and sorting

---

## Prompt Canonicalization, Drift, and Refinement

The repository already includes core classes aligned with an adaptive orchestration direction:

- **PromptCanonicalizer**: configurable phrase/number normalization map for clustering/consistency workflows
- **DriftDetector**: rolling-window statistical drift monitor scaffold
- **HillClimbOptimizer**: neighbor-generation and improvement checks for incremental refinement

At present these are foundational components and not yet fully wired into the active benchmark execution loop.

---

## Troubleshooting

- **“Please load a model first”**
  - Ensure model path points to an existing `.gguf` file.
- **Model loads but output quality is poor**
  - Reduce sweep space first (temperature-only), then expand.
  - Use deterministic mode while comparing configs.
- **Run count explodes unexpectedly**
  - Check step sizes in all-combinations mode; total runs are multiplicative.
- **Template mismatch behavior**
  - Confirm model metadata includes a valid `tokenizer.chat_template`.
  - Unknown formats fall back to generic handling.

---

## Roadmap Ideas

- Integrate canonicalization into active benchmark pre-processing path
- Add cluster-aware dataset bucketing and per-cluster best configs
- Wire `DriftDetector` to trigger automatic re-exploration
- Integrate `HillClimbOptimizer` as post-sweep local search
- Add structured response validators and task-specific scoring plugins

---

## License

This repository includes license files at root (`LICENSE`, `LICENSE.txt`).

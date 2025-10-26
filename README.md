# TestTaskEye

High-performance file generation and sorting solution with parallel processing.

## Projects

### 1. Generator
Generates large text files with format `Number. Text` using parallel processing.

**Features:**
- Multi-core parallel generation
- Configurable file size (GB)
- Customizable number range
- UTF-8 with emoji support
- ~5s for 1GB file on modern hardware

**Usage:**
```bash
cd Generator
dotnet run -- -s 1.0 -m 1000 -o output.txt
```

**Options:**
- `-s, --size` - File size in GB (default: 2.0)
- `-m, --max-number` - Max random number (default: 1000)
- `-o, --output` - Output path (default: output.txt)

---

### 2. FileSorter
Sorts large files using Parallel External Merge Sort algorithm.

**Features:**
- Handles files larger than RAM (external sorting)
- Parallel chunk sorting with Array.Sort
- K-way merge with PriorityQueue
- Memory-efficient (configurable chunk size)
- String interning for reduced memory allocations
- Optimized span-based parsing
- Sorts by text alphabetically, then by number
- **~14.7 min for 50GB file** (4.9 min sorting + 7.8 min merge)

**Usage:**
```bash
cd FileSorter
dotnet run -- -i input.txt -o sorted.txt -c 100
```

**Options:**
- `-i, --input` - Input file path
- `-o, --output` - Output file path
- `-c, --chunk-size` - Chunk size in MB (default: 100)
- `-t, --temp-dir` - Temp directory (default: ./temp)

**Algorithm:**
1. **Split** - Divide input into raw chunks (~100MB each)
2. **Sort** - Process chunks in parallel using Array.Sort
3. **Merge** - K-way merge sorted chunks using PriorityQueue

---

### 3. Benchmarks
Performance benchmarking using BenchmarkDotNet.

**Tests:**
- ChunkedFileGenerator performance (100MB, 500MB, 1000MB)
- ParallelExternalMergeSorter performance (100MB, 500MB, 1000MB)

**Usage:**
```bash
cd Benchmarks
dotnet run -c Release
```

**⚠️ Important:** Always run in Release mode for accurate results.

**Output:**
- Execution time statistics (mean, median, std dev)
- Memory allocations (Gen0/1/2, total allocated)
- Results saved to `BenchmarkDotNet.Artifacts/results/`

---

### 4. Tests
Unit tests using MSTest framework.

**Coverage:**
- **ChunkedFileGeneratorTests** (4 tests)
  - File creation and format validation
  - Size target accuracy

- **ParallelExternalMergeSorterTests** (5 tests)
  - Sort correctness
  - Large dataset handling (10,000 lines)
  - Data preservation and cleanup

**Usage:**
```bash
cd Tests
dotnet test
```

**Results:** 9/9 tests passing ✅

---

## Solution Structure

```
TestTaskEye/
├── Generator/           # File generation
├── FileSorter/          # Parallel external merge sort
├── Benchmarks/          # Performance testing
├── Tests/               # Unit tests
└── TestTaskEye.sln      # Solution file
```

## Build All Projects

```bash
cd C:\proj\TestTaskEye
dotnet build
```

## Quick Start

**Generate a 1GB file:**
```bash
cd Generator
dotnet run -- -s 1.0
```

**Sort the file:**
```bash
cd ../FileSorter
dotnet run -- -i ../Generator/bin/Debug/net9.0/output.txt -o sorted.txt
```

**Run tests:**
```bash
cd ../Tests
dotnet test
```

**Run benchmarks:**
```bash
cd ../Benchmarks
dotnet run -c Release
```

## Performance

**Hardware:** Modern system with 32 cores

### Actual Performance (2 GB file)

| Operation | File Size | Time | Breakdown |
|-----------|-----------|------|-----------|
| Generate | 2 GB | ~2.5s | 1.4s generation + 1.0s merge + 0.1s cleanup |
| Sort | 2 GB | **31.6s** | 10.0s sorting + 16.8s merge + 4.8s overhead |

**Sorting Details:**
- **Chunk size:** 100 MB
- **Total chunks:** 21
- **Merge strategy:** Single-pass K-way merge
- **Throughput:** ~63 MB/s

### Actual Performance (50 GB file)

| Operation | File Size | Time | Breakdown |
|-----------|-----------|------|-----------|
| Generate | 50 GB | ~65s | 22.5s generation + 41.1s merge + 1.8s cleanup |
| Sort | 50 GB | **884.8s (14.7 min)** | 293.7s sorting + 468.2s merge + 2.1s cleanup |

**Sorting Details:**
- **Chunk size:** 50 MB
- **Total chunks:** 1024
- **Merge strategy:** Single-pass K-way merge (all 1024 files merged directly)
- **Throughput:** ~56.5 MB/s

### Projected Performance (100 GB file)

| Phase | Estimated Time | Notes |
|-------|---------------|-------|
| Sorting | ~9.8 min | Scales linearly with data size (293.7s × 2) |
| Merging | ~17.2 min | Scales with data size × 1.1 priority queue overhead factor |
| **Total** | **~27 min** | With 50MB chunks, creates ~2048 intermediate files |

**Notes on 100 GB Estimation:**
- Based on actual 50 GB benchmark results
- Assumes same hardware and chunk size (50 MB)
- Priority queue overhead: log(2048)/log(1024) = 11/10 = 1.1× for 2048 chunks
- Single-pass K-way merge handles 2048 files efficiently with 2GB RAM usage
- Actual time may vary based on disk speed (SSD vs HDD significantly impacts merge performance)

## Key Optimizations

### Generator
- Parallel.For for multi-core generation
- 1MB write buffers for line-by-line streaming
- 10MB buffers for file merging
- Direct file merging (no intermediate processing)

### FileSorter
- **String interning** - Reuses identical text strings via ConcurrentDictionary (70-90% memory reduction)
- **Span-based parsing** - Zero-allocation line parsing using ReadOnlySpan<char>
- **UTF-8 encoding reuse** - Single static encoding instance (eliminates repeated allocations)
- **Optimized validation** - Minimal checks in hot paths for performance
- **Parallel chunk sorting** - Array.Sort on multiple chunks simultaneously
- **K-way merge with PriorityQueue** - Efficient merging of sorted chunks
- **Large I/O buffers:**
  - 4MB standard buffer for chunk reading/writing
  - 16MB buffer per chunk reader during merge (1024 chunks × 16MB = ~16GB RAM for single-pass)
  - 128MB output writer buffer for final file
- **50MB chunk size** - Optimal for 50-100GB files (creates 1000-2000 chunks, single-pass K-way merge)

## Requirements

- .NET 9.0
- Windows/Linux/macOS
- Sufficient disk space (2-3x input file size for temp files)

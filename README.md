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
- Sorts by text alphabetically, then by number
- ~41s for 1GB file (16.8s sorting + 19.4s merging)

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

| Operation | File Size | Time | Breakdown                                   |
|-----------|-----------|------|---------------------------------------------|
| Generate | 2 GB | 2.5s | 1.4s generation + 1.0s merge + 0.1s cleanup |
| Sort | 2 GB | ~40s | 4.5s split + 17.2s sort + 18.4s merge       |

## Key Optimizations

### Generator
- Parallel.For for multi-core generation
- 16MB buffers for I/O
- Direct file merging (no intermediate processing)

### FileSorter
- Span-based parsing (reduced allocations)
- Array.Sort (faster than LINQ OrderBy)
- Pre-allocated arrays
- 16MB buffers for I/O
- 100MB chunk size for optimal memory/performance balance

## Requirements

- .NET 9.0
- Windows/Linux/macOS
- Sufficient disk space (2-3x input file size for temp files)

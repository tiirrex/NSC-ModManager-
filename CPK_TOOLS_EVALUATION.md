# CPK-Tools Evaluation Plan

**Objective**: Determine viability of using cpk-tools as CpkMaker.dll replacement  
**Priority**: CRITICAL (blocks entire cross-platform migration)  
**Timeline**: 2-3 days for full evaluation  
**Risk**: If cpk-tools unsuitable, must pursue alternative path (P/Invoke wrapper, custom implementation)

---

## EVALUATION CRITERIA

### 1. API Compatibility (Must Match YaCpkTool Usage)

**Current CpkMaker API** (Properties/Program.cs):

```csharp
// Initialization
CpkMaker cpkMaker = new CpkMaker();
CAsyncFile cManager = new CAsyncFile();

// Extraction
cpkMaker.AnalyzeCpkFile(extractWhat);                    // ← Parse CPK structure
CFileData cpkFileData = cpkMaker.FileData;               // ← Get file list
cpkMaker.StartToExtract(outFileName);                    // ← Begin extraction
CriCpkMaker.Status status = cpkMaker.Execute();          // ← Non-blocking execute

// Repacking
cpkMaker.CpkFileMode = CpkMaker.EnumCpkFileMode.ModeFilename;
cpkMaker.CompressCodec = compressCodec;                  // CodecDpk enum
cpkMaker.DataAlign = dataAlign;                          // Alignment bytes
cpkMaker.AddFile(file, localPath, fileIndex, compress);  // Add file to CPK
cpkMaker.StartToBuild(save_directory);                   // Begin build
while (status > CriCpkMaker.Status.Stop && percent < 100) {
    percent = (int)Math.Floor(cpkMaker.GetProgress());
    status = cpkMaker.Execute();
}
cpkMaker.WaitForComplete();

// Utilities
CpkCrypt_141342730(a1, bytes, length);                   // Custom XOR decryption
```

**Required API Surface for cpk-tools**:

| Function | Signature | Purpose | Criticality |
|----------|-----------|---------|------------|
| `AnalyzeCpkFile()` | CPK path → CFileData | Parse CPK structure | 🔴 CRITICAL |
| `ExtractCpk()` | CPK path, output dir → void | Extract all files | 🔴 CRITICAL |
| `PackageCpk()` | Input dir, output path → void | Repack CPK | 🔴 CRITICAL |
| `GetProgress()` | → 0-100 float | Progress tracking | 🟡 IMPORTANT |
| `SupportCompression()` | Enum | Codec support (DPK) | 🟡 IMPORTANT |
| `SetDataAlign()` | uint | Alignment control | 🟡 IMPORTANT |
| `Encrypt/Decrypt()` | bytes → bytes | CPK encryption | 🟡 IMPORTANT |

**Evaluation Question**: Does cpk-tools expose equivalent functionality?

---

### 2. Performance Characteristics

**Current Performance Baseline** (from YACpkTool usage):

- **Extraction**: Seconds to minutes (varies by CPK size)
- **Repacking**: Minutes (depends on file count and compression)
- **Progress tracking**: Real-time feedback every 1-5%
- **No blocking**: Async state machine allows cancellation

**Performance Requirements**:
- ✓ Extract: < 2x slower than CpkMaker
- ✓ Repack: < 2x slower than CpkMaker
- ✓ Memory usage: < 500 MB for large CPKs
- ✓ Cancellation: Must support mid-operation abort

**Evaluation Methods**:
1. Test with game CPK files (sizes: 50MB, 500MB, 1GB+)
2. Measure extraction time
3. Measure repacking time
4. Monitor memory usage with profiler
5. Compare against baseline expectations

---

### 3. Code Quality & Maintenance

| Factor | Assessment | Importance |
|--------|-----------|-----------|
| **Language** | C# (same as NSC) | HIGH |
| **License** | MIT/Apache/GPL (must verify) | CRITICAL |
| **Active maintenance** | Commit history recent? | MEDIUM |
| **Issue/PR response** | Community engagement? | MEDIUM |
| **Documentation** | API docs, examples? | LOW-MEDIUM |
| **Test coverage** | Unit tests present? | MEDIUM |
| **Dependencies** | Minimal, no Windows deps? | HIGH |

**Repository**: https://github.com/ConnorKrammer/cpk-tools

---

### 4. Licensing Compatibility

**NSC Mod Manager License**: Check AI_INSTRUCTIONS.md (not specified in audit)  
**Required Compatibility**: MIT, Apache 2.0, or GPL (with additional notes if copyleft)

**Evaluation Steps**:
1. Check cpk-tools LICENSE file
2. Identify all transitive dependencies and their licenses
3. Verify no conflicts with NSC license
4. Document license in NSC.Core project

---

## EVALUATION PROCEDURE

### Step 1: Clone & Inspect Repository (1 hour)

```bash
cd /tmp
git clone https://github.com/ConnorKrammer/cpk-tools.git
cd cpk-tools
```

**Inspect**:
- [ ] Read README.md (API usage, examples)
- [ ] Check LICENSE file
- [ ] Review source code structure
- [ ] Identify main classes/entry points
- [ ] List dependencies

**Output**: Feature matrix checklist

---

### Step 2: Build & Test on Windows (2-3 hours)

```bash
# Restore dependencies
dotnet restore

# Build project
dotnet build -c Release

# Locate compiled binaries
ls bin/Release/net*/
```

**Verify**:
- [ ] Builds without errors
- [ ] Targets net6.0+ (or net4.8+)
- [ ] Can be referenced as NuGet or project reference
- [ ] No Windows-specific P/Invoke calls

---

### Step 3: Create Integration Proof-of-Concept (4-6 hours)

**Create test project**: NSC.Core.CpkToolsIntegration.Tests

```csharp
using CpkTools;  // Assuming namespace

[TestClass]
public class CpkToolsIntegrationTests
{
    private string _testCpkPath = "path/to/test.cpk";
    private string _outputDir = "path/to/output";
    
    [TestMethod]
    public void CanAnalyzeCpk()
    {
        var cpk = new CpkFile(_testCpkPath);
        Assert.IsNotNull(cpk.Files);
        Assert.IsTrue(cpk.Files.Count > 0);
    }
    
    [TestMethod]
    public async Task CanExtractCpk()
    {
        var cpk = new CpkFile(_testCpkPath);
        await cpk.ExtractAsync(_outputDir, CancellationToken.None);
        
        var filesExtracted = Directory.GetFiles(_outputDir, "*", SearchOption.AllDirectories);
        Assert.IsTrue(filesExtracted.Length > 0);
    }
    
    [TestMethod]
    public async Task CanRepackCpk()
    {
        var cpk = new CpkFile();
        await cpk.PackAsync(_outputDir, "output.cpk", CancellationToken.None);
        
        Assert.IsTrue(File.Exists("output.cpk"));
        Assert.IsTrue(new FileInfo("output.cpk").Length > 0);
    }
    
    [TestMethod]
    public async Task CanTrackProgress()
    {
        var cpk = new CpkFile(_testCpkPath);
        var progressReports = new List<int>();
        var progress = new Progress<int>(p => progressReports.Add(p));
        
        await cpk.ExtractAsync(_outputDir, progress, CancellationToken.None);
        
        Assert.IsTrue(progressReports.Count > 0);
        Assert.IsTrue(progressReports.Last() >= 99);
    }
}
```

**Test with Real Game Files**:
- [ ] Small CPK (< 100 MB)
- [ ] Medium CPK (100-500 MB)
- [ ] Large CPK (> 500 MB)

**Results**:
- ✓ Success: API matches requirements
- ⚠️ Partial: Some features missing, need adapter
- ❌ Failure: Unsuitable, pursue alternative

---

### Step 4: Performance Benchmarking (3-4 hours)

```csharp
[Benchmark]
public async Task ExtractCpkBenchmark()
{
    var cpk = new CpkFile("test_50mb.cpk");
    await cpk.ExtractAsync("output_bench", CancellationToken.None);
}

[Benchmark]
public async Task RepackCpkBenchmark()
{
    var cpk = new CpkFile();
    await cpk.PackAsync("input_dir", "output_bench.cpk", CancellationToken.None);
}
```

**Run with BenchmarkDotNet**:
```bash
dotnet add package BenchmarkDotNet
dotnet run -c Release -- --filter '*Benchmark*'
```

**Compare Against Baseline**:
| Operation | CpkMaker | cpk-tools | Ratio | Status |
|-----------|----------|-----------|-------|--------|
| Extract 50MB | ~2s | ? | < 2x | ? |
| Repack 50MB | ~5s | ? | < 2x | ? |
| Memory peak | ? | ? | < 500MB | ? |

---

### Step 5: Cross-Platform Validation (2-3 hours)

**On Linux** (Ubuntu, Debian, or WSL):

```bash
git clone https://github.com/ConnorKrammer/cpk-tools.git
cd cpk-tools
dotnet build -c Release
dotnet test
```

**Verify**:
- [ ] Builds without errors on Linux
- [ ] No P/Invoke to kernel32 or Win32 APIs
- [ ] Uses System.IO (cross-platform)
- [ ] Uses System.Text (cross-platform)
- [ ] No registry access
- [ ] No Process.Start with UseShellExecute

**Output**: Cross-platform validation report

---

### Step 6: Dependency Analysis (1-2 hours)

**List all NuGet dependencies**:
```bash
cd cpk-tools
dotnet list package --transitive
```

**For each dependency**:
- [ ] Check if cross-platform (https://platform.uno/)
- [ ] Check for Windows-only APIs
- [ ] Verify licensing

**Red Flags**:
- 🚫 Windows.ApplicationModel
- 🚫 Microsoft.Win32
- 🚫 System.Windows
- 🚫 NAPI (Node.js native)
- 🚫 GPL without exemption

---

### Step 7: License Verification (30 minutes)

```bash
# Generate license report
cd cpk-tools
dotnet list license  # If available, or manual inspection

# Check for GPL
grep -r "GNU GENERAL PUBLIC" .
grep -r "GPL-3" .
```

**For GPL**: Determine if compatible with NSC license

---

## SUCCESS CRITERIA

### Go (Adopt cpk-tools)
- ✅ API covers all required functions
- ✅ Performance within 2x of CpkMaker
- ✅ Builds on Windows and Linux without P/Invoke
- ✅ License compatible with NSC
- ✅ Can be integrated within 2 weeks

### Conditional Go (With Adapter)
- ⚠️ API 80%+ compatible
- ⚠️ Missing features can be worked around
- ⚠️ Performance acceptable
- ⚠️ Requires light adapter layer
- ⚠️ Integration timeline: 3-4 weeks

### No-Go (Pursue Alternative)
- ❌ API fundamentally incompatible
- ❌ Performance unacceptable (> 3x slowdown)
- ❌ Windows-only dependencies
- ❌ License conflict
- ❌ Not maintained or broken tests

---

## FALLBACK STRATEGIES (If cpk-tools Unsuitable)

### Option A: P/Invoke + Wine Wrapper

**Concept**: Keep CpkMaker.dll, abstract behind IProcessRunner  
**Feasibility**: MEDIUM  
**Timeline**: 1-2 weeks  
**Platforms**: Windows, Wine, Proton (not native Linux)

```csharp
public interface ICpkService {
    Task ExtractAsync(string cpkPath, string outputDir, IProgress<int> progress, CancellationToken ct);
    Task PackAsync(string inputDir, string outputPath, IProgress<int> progress, CancellationToken ct);
}

// Windows implementation - uses COM
public class WindowsCpkService : ICpkService
{
    // Use CpkMaker.dll directly
}

// Wine fallback - invokes YACpkTool.exe via Wine
public class WineCpkService : ICpkService
{
    // Run YACpkTool.exe under Wine using IProcessRunner
}
```

---

### Option B: Custom CPK Implementation

**Concept**: Implement CPK format from scratch based on reverse-engineering  
**Feasibility**: LOW  
**Timeline**: 8-12 weeks  
**Risk**: CRITICAL (file format may have undocumented features)

**Approach**:
1. Reverse-engineer CPK binary format
2. Implement file parsing
3. Implement compression/encryption
4. Create extraction and packing functions
5. Extensive testing with game files

**Not Recommended** for MVP, but document as future enhancement.

---

## DECISION TREE

```
cpk-tools Evaluation
│
├─ API Compatible?
│  ├─ YES
│  │  ├─ License OK?
│  │  │  ├─ YES → Cross-platform OK?
│  │  │  │         ├─ YES → Performance OK?
│  │  │  │         │       ├─ YES → ✅ ADOPT (Start integration immediately)
│  │  │  │         │       └─ NO  → ⚠️ Investigate optimization, proceed with caution
│  │  │  │         └─ NO  → ⚠️ CONDITIONAL GO (Option A: Wine wrapper fallback)
│  │  │  └─ NO  → ❌ REJECT (Option B: Custom implementation)
│  │  
│  └─ NO (API incompatible)
│     └─ Can adapter layer work?
│        ├─ YES → ⚠️ CONDITIONAL GO (requires adapter, extends timeline)
│        └─ NO  → ❌ REJECT (Option A: Wine wrapper fallback)
```

---

## REPORTING TEMPLATE

**After evaluation, produce:**

### Executive Summary
- [ ] Decision: ADOPT / CONDITIONAL / REJECT
- [ ] Confidence: HIGH / MEDIUM / LOW
- [ ] Recommendation: Proceed / Hold for further investigation / Pursue alternative

### Detailed Findings
- [ ] API compatibility matrix (✓/✗ for each required function)
- [ ] Performance metrics (extraction, repacking, memory)
- [ ] License compatibility statement
- [ ] Cross-platform validation results
- [ ] Integration complexity estimate

### Next Steps
- [ ] If ADOPT: Create CpkService wrapper, begin integration
- [ ] If CONDITIONAL: Design adapter layer, update timeline
- [ ] If REJECT: Activate fallback strategy (Option A or B)

---

**Evaluation Owner**: [To be assigned]  
**Start Date**: [TBD]  
**Target Completion**: [Start Date + 2-3 days]  
**Blocking Migration Until**: Resolved

---

**Related Documents**:
- ARCHITECTURE_AUDIT.md - Audit findings
- MIGRATION_STRATEGY.md - Overall migration plan
- Risk Matrix - CpkMaker.dll replacement rated CRITICAL

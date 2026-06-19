# NSC Mod Manager - Production-Grade Cross-Platform Migration Strategy

**Status**: Pre-implementation (Audit Complete)  
**Target Frameworks**: .NET 8 LTS  
**Target Platforms**: Windows, Linux, Steam Deck, Wine, Proton, Winlator  
**Avalonia UI**: 11.0+  
**Architecture Pattern**: Clean Architecture + MVVM + Dependency Injection  

---

## EXECUTIVE SUMMARY

### Current State
- **Framework**: .NET 10 Windows (net10.0-windows), WPF
- **Architecture**: Monolithic WPF application with mixed business logic
- **Platform**: x86-only, 100% Windows-dependent
- **Dependencies**: 11 NuGet packages, 9/11 Windows-only
- **External Tools**: 3 (CpkMaker.dll, YACpkTool.exe, vgmstream-cli)
- **Code Size**: 40+ ViewModels, 40+ Models, 8 XAML files, 2000+ lines in critical files

### Target State
- **Framework**: .NET 8 LTS (net8.0), no platform-specific TFM
- **Architecture**: Layered (Core, Platform, UI, CLI, Tests)
- **Platform**: Windows (x64), Linux (x64, ARM64), through Wine/Proton/Winlator
- **Dependencies**: Replaced 9 Windows packages with cross-platform alternatives
- **External Tools**: Replace/abstract proprietary tools, integrate cross-platform libraries
- **Code Structure**: Separated concerns (Core business logic, Platform abstraction, UI layer, CLI)

### Transformation Scope
| Component | Current | Target | Effort | Risk |
|-----------|---------|--------|--------|------|
| **Business Logic** | Mixed in ViewModels | NSC.Core (DI + SOLID) | Medium | Low |
| **UI Framework** | WPF | Avalonia 11 | High | Medium |
| **Data Dialogs** | CommonOpenFileDialog | Avalonia FilePicker | Medium | Low |
| **CPK Tools** | CpkMaker.dll (x86 COM) | cpk-tools wrapper or reimpl. | High | High |
| **Platform Services** | Hard-coded Windows APIs | IPlatformService abstraction | Medium | Low |
| **Process Execution** | ProcessStartInfo everywhere | IProcessRunner abstraction | Medium | Low |
| **Audio Processing** | NAudio (Windows) | Cross-platform (NAudio + fallback) | Medium | Low |
| **Configuration** | App.config + Settings.settings | JSON config | Low | Low |
| **Build Targets** | x86 only | x64 (win, linux), ARM64 (linux) | Low | Low |

---

## ARCHITECTURE: LAYERED SOLUTION STRUCTURE

```
NSC.sln
├── src/
│   ├── NSC.Core/                    [Business Logic - Pure .NET 8, No Windows deps]
│   │   ├── Models/                  [Data models, ObservableCollection replacements]
│   │   ├── Services/                [CPK, XFBIN, Mod, Game services]
│   │   ├── Interfaces/              [IPlatformService, IProcessRunner, IFileDialog]
│   │   ├── Workflows/               [Mod install, compile, merge, package]
│   │   ├── Utilities/               [Encryption, serialization, helpers]
│   │   ├── Configuration/           [Settings management, config files]
│   │   └── NSC.Core.csproj          [net8.0, no Windows-specific deps]
│   │
│   ├── NSC.Platform/                [Platform Abstraction Layer]
│   │   ├── WindowsPlatformService
│   │   ├── LinuxPlatformService
│   │   ├── WinePlatformService
│   │   ├── ProcessRunner
│   │   ├── FileDialogService
│   │   └── NSC.Platform.csproj      [net8.0]
│   │
│   ├── NSC.Shared/                  [Shared types, contracts, constants]
│   │   └── NSC.Shared.csproj        [net8.0]
│   │
│   ├── NSC.UI.Avalonia/             [GUI Application]
│   │   ├── Views/                   [Avalonia XAML views (replaces WPF XAML)]
│   │   ├── ViewModels/              [MVVM (CommunityToolkit.Mvvm)]
│   │   ├── Models/                  [UI-specific models]
│   │   ├── Services/                [UI services (clipboard, notifications)]
│   │   ├── App.axaml                [Avalonia application root]
│   │   ├── MainWindow.axaml
│   │   ├── Converters/              [Avalonia value converters]
│   │   ├── Resources/               [Themes, images, localization]
│   │   └── NSC.UI.Avalonia.csproj   [net8.0 + Avalonia 11]
│   │
│   ├── NSC.CLI/                     [Command-line Interface]
│   │   ├── Commands/                [nsc install, nsc build, nsc merge, etc.]
│   │   ├── Formatters/              [Output formatting]
│   │   ├── Program.cs               [CLI entry point]
│   │   └── NSC.CLI.csproj           [net8.0 Exe]
│   │
│   └── NSC.Tests/                   [Test Suite]
│       ├── Unit/                    [Core logic tests]
│       ├── Integration/             [Workflow tests]
│       ├── Platform/                [Platform service tests]
│       └── NSC.Tests.csproj         [net8.0 xUnit tests]
│
├── build/
│   ├── publish-win-x64.sh           [Windows x64 single-file publish]
│   ├── publish-linux-x64.sh         [Linux x64 single-file publish]
│   ├── publish-linux-arm64.sh       [Linux ARM64 single-file publish]
│   └── build-ci.yaml                [GitHub Actions CI workflow]
│
└── docs/
    ├── ARCHITECTURE.md              [Detailed architecture decisions]
    ├── MIGRATION.md                 [Step-by-step migration guide]
    ├── WINE_COMPAT.md               [Wine/Proton/Winlator guide]
    ├── CLI_REFERENCE.md             [CLI command reference]
    └── DEVELOPER_GUIDE.md           [Setup and contribution guide]
```

---

## NSC.CORE - Business Logic Layer

### Responsibilities (NO Windows dependencies allowed)
- Mod installation, extraction, compilation, merging
- CPK file handling (extract, repack, decrypt/encrypt)
- XFBIN file handling (read, write, repack)
- Game detection and version management
- Configuration and settings management
- Cache management
- Download management
- Update checking
- External tool orchestration (via abstracted IProcessRunner)

### Service Interfaces (Contracts)

```csharp
// Abstracted platform detection
public interface IPlatformService
{
    string GetConfigDirectory();
    string GetCacheDirectory();
    string GetModsDirectory();
    
    void OpenFolder(string path);
    void OpenUrl(string url);
    
    bool IsWine();
    bool IsProton();
    bool IsSteamDeck();
    bool IsWinlator();
    
    string GetPlatformIdentifier(); // "windows-x64", "linux-x64", "linux-arm64"
}

// Abstracted external process execution
public interface IProcessRunner
{
    Task<ProcessResult> RunAsync(
        string executable,
        string[] arguments,
        string? workingDirectory = null,
        CancellationToken cancellationToken = default
    );
    
    Task<string> RunAndCaptureOutputAsync(
        string executable,
        string[] arguments,
        CancellationToken cancellationToken = default
    );
}

// Abstracted CPK operations (replace CpkMaker.dll)
public interface ICpkService
{
    Task<CpkAnalysisResult> AnalyzeCpkFileAsync(string cpkPath, CancellationToken ct);
    Task ExtractCpkAsync(string cpkPath, string outputDir, IProgress<CpkProgress> progress, CancellationToken ct);
    Task RepackCpkAsync(string inputDir, string outputCpkPath, IProgress<CpkProgress> progress, CancellationToken ct);
    Task<bool> VerifyCpkIntegrityAsync(string cpkPath, CancellationToken ct);
}

// Abstracted XFBIN operations (integrate XFBIN_LIB)
public interface IXfbinService
{
    Task<XfbinFile> ReadXfbinAsync(string filePath, CancellationToken ct);
    Task WriteXfbinAsync(XfbinFile xfbin, string outputPath, CancellationToken ct);
    Task RepackXfbinAsync(string extractedDir, string outputPath, CancellationToken ct);
}

// Abstracted mod operations
public interface IModService
{
    Task InstallModAsync(string modPackagePath, string gameRootPath, IProgress<ModProgress> progress, CancellationToken ct);
    Task CompileModAsync(string modSourceDir, string gameRootPath, IProgress<CompileProgress> progress, CancellationToken ct);
    Task<ModMergeResult> MergeModsAsync(IEnumerable<string> modPaths, string gameRootPath, IProgress<MergeProgress> progress, CancellationToken ct);
    Task PackageModAsync(string modSourceDir, string outputPackagePath, CancellationToken ct);
}

// Abstracted game detection
public interface IGameDetectionService
{
    Task<IEnumerable<GameInstallation>> DetectGamesAsync(CancellationToken ct);
    Task<GameVersion> DetectGameVersionAsync(string gameRootPath, CancellationToken ct);
    bool ValidateGameInstallation(string gameRootPath, GameVersion expectedVersion);
}

// Abstracted configuration
public interface IConfigurationService
{
    T GetSetting<T>(string key, T defaultValue);
    void SetSetting<T>(string key, T value);
    void Save();
    void Load();
}
```

### Dependency Injection Container Setup

```csharp
// In NSC.Core setup
public static IServiceCollection AddCoreServices(this IServiceCollection services)
{
    // Configuration
    services.AddSingleton<IConfigurationService, JsonConfigurationService>();
    
    // Game & Mod detection
    services.AddSingleton<IGameDetectionService, GameDetectionService>();
    services.AddSingleton<IModService, ModService>();
    
    // CPK & XFBIN processing
    services.AddSingleton<ICpkService, CpkServiceImpl>();    // To be implemented
    services.AddSingleton<IXfbinService, XfbinServiceImpl>();
    
    // Cache and downloads
    services.AddSingleton<ICacheService, CacheService>();
    services.AddSingleton<IDownloadService, DownloadService>();
    
    // Logging
    services.AddSerilog();
    
    return services;
}
```

---

## CRITICAL DECISION: CpkMaker.dll Replacement

### Current Situation
- **CpkMaker.dll**: Proprietary x86 COM/.NET assembly from CRI Middleware
- **Size**: ~661 KB
- **Architecture**: x86-only (enforces 32-bit build)
- **No source code available**
- **Critical functions**:
  - `AnalyzeCpkFile()` - CPK structure parsing
  - `StartToExtract()` - CPK extraction
  - `StartToBuild()` - CPK repacking
  - `GetProgress()` - Progress tracking
  - `Execute()` - Async state machine

### Option A: Use cpk-tools (Recommended)

**Repository**: https://github.com/ConnorKrammer/cpk-tools  
**Language**: C# / C++  
**License**: Check (verify compatibility)  
**Status**: External, not part of this repo

**Evaluation**:
```
Feasibility:  HIGH (C# library, likely .NET referenceable)
Risk:         MEDIUM (external dependency, requires audit)
Compatibility: HIGH (designed for cross-platform)
Maintenance:  LOW (community-maintained)
```

**Action Required**:
1. Clone and analyze cpk-tools source
2. Verify license compatibility
3. Test API compatibility with current YACpkTool usage
4. Measure performance parity
5. Create wrapper if needed

### Option B: P/Invoke Wrapper + Wine

**Concept**: Keep CpkMaker.dll, run under Wine/Proton  
**Feasibility**: MEDIUM (requires Wine runtime, .NET Framework 4.6 compat layer)  
**Risk**: HIGH (Windows-specific threading, COM marshalling issues)  
**Compatibility**: Only Windows + Wine/Proton (not native Linux)  
**Maintenance**: HIGH (fragile coupling to Windows internals)

**Not Recommended** for native Linux support, but viable as fallback.

### Option C: Implement Custom CPK Library

**Effort**: VERY HIGH (weeks/months)  
**Risk**: CRITICAL (file format may have undocumented features)  
**Feasibility**: MEDIUM (format reverse-engineered in YACpkTool.exe source)  
**Maintenance**: HIGH (ongoing reverse-engineering)

**Not Recommended** for initial migration, but document as future option.

### DECISION: **Pursue Option A (cpk-tools evaluation)**

Next step after audit: Detailed analysis of cpk-tools API and compatibility assessment.

---

## PLATFORM ABSTRACTION LAYER (NSC.Platform)

### Windows Platform Service
```csharp
public class WindowsPlatformService : IPlatformService
{
    public string GetConfigDirectory() => 
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NSC");
    
    public string GetModsDirectory() =>
        Path.Combine(GetConfigDirectory(), "Mods");
    
    public void OpenFolder(string path)
    {
        var psi = new ProcessStartInfo {
            FileName = "explorer.exe",
            Arguments = path,
            UseShellExecute = true
        };
        Process.Start(psi);
    }
    
    public bool IsWine() => /* Check for Wine environment variables */;
    public bool IsProton() => /* Check PROTON_VERSION env var */;
    public bool IsSteamDeck() => /* Check /etc/os-release */;
    public bool IsWinlator() => /* Check specific Winlator markers */;
}
```

### Linux Platform Service
```csharp
public class LinuxPlatformService : IPlatformService
{
    public string GetConfigDirectory() =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nsc");
    
    public void OpenFolder(string path)
    {
        var psi = new ProcessStartInfo {
            FileName = "xdg-open",  // freedesktop standard
            Arguments = path,
            UseShellExecute = false
        };
        Process.Start(psi);
    }
    
    // Implement platform detection
}
```

### Composition Root
```csharp
public static class PlatformRegistry
{
    public static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        var platformService = OperatingSystem.IsWindows()
            ? new WindowsPlatformService()
            : new LinuxPlatformService();
        
        services.AddSingleton<IPlatformService>(platformService);
        services.AddSingleton<IProcessRunner, ProcessRunnerImpl>();
        
        return services;
    }
}
```

---

## UI LAYER: WPF → Avalonia Migration

### Approach
1. **Keep MVVM pattern** (CommunityToolkit.Mvvm replaces WPF's INotifyPropertyChanged)
2. **Minimal XAML changes** (Avalonia XAML is ~95% compatible with WPF XAML)
3. **Replace Windows dialogs** with Avalonia FilePicker
4. **Replace WPF controls** with Avalonia equivalents
5. **Remove WPF-specific packages** (ModernWpf, WindowsAPICodePack, NAudio UI, WpfAnimatedGif)

### XAML Conversion Examples

**WPF XAML**:
```xml
<Window x:Class="NSC_ModManager.View.TitleView"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="NSC Mod Manager" Height="800" Width="1300">
```

**Avalonia XAML** (almost identical):
```xml
<Window x:Class="NSC.UI.Avalonia.Views.TitleView"
        xmlns="https://github.com/avaloniaui"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="NSC Mod Manager" Height="800" Width="1300">
```

### Converter Pattern Update

**WPF**:
```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => (bool)value ? Visibility.Visible : Visibility.Collapsed;
}
```

**Avalonia**:
```csharp
public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => (bool?)value == true ? true : false;  // Avalonia uses bool/null directly
}
```

### ViewModel Update (ObservableCollection Replacement)

**Before** (WPF):
```csharp
public ObservableCollection<ModModel> InstalledMods { get; } = new();

public ModViewModel()
{
    InstalledMods.PropertyChanged += (s, e) => RaisePropertyChanged(nameof(InstalledMods));
}
```

**After** (Avalonia + CommunityToolkit.Mvvm):
```csharp
[ObservableProperty]
private ObservableCollection<ModModel> installedMods = new();

// ObservableProperty generates PropertyChanged events automatically
```

### Dialog Migration

**WPF CommonOpenFileDialog**:
```csharp
var dialog = new CommonOpenFileDialog { IsFolderPicker = true, Title = "Select folder" };
if (dialog.ShowDialog() == CommonFileDialogResult.Ok) { /* ... */ }
```

**Avalonia FilePicker**:
```csharp
var result = await storageProvider.OpenFolderAsync(new BrowserFolderOpenOptions 
{ 
    AllowMultiple = false,
    Title = "Select folder"
});

if (result.Count == 1) 
{ 
    var folder = result[0];
    // ... 
}
```

### Dependencies: Before → After

| WPF | Avalonia |
|-----|----------|
| `ModernWpf.MessageBox` | `MessageBox.Avalonia` |
| `WpfAnimatedGif` | `Avalonia.Animation` (built-in) |
| `Extended.Wpf.Toolkit` | `Avalonia.Controls.DataGrid` + custom controls |
| `WindowsAPICodePack-Shell` | `StorageProvider` (Avalonia built-in) |
| `System.Windows.Forms` | `Avalonia.Controls` |
| `NAudio` (audio UI) | `Avalonia.Media` + background processing |

---

## EXTERNAL TOOL ABSTRACTION

### Audio Processing (vgmstream-cli, NAudio)

**Current**: NAudio hard-coded for Windows, vgmstream-cli invoked via ProcessStartInfo

**New Approach**:
```csharp
public interface IAudioService
{
    Task PlayAudioAsync(byte[] audioData, string mimeType, CancellationToken ct);
    Task<byte[]> ConvertAudioFormatAsync(byte[] input, string outputFormat, CancellationToken ct);
}

public class CrossPlatformAudioService : IAudioService
{
    // On Windows: use NAudio directly
    // On Linux: use vgmstream-cli + ffmpeg via IProcessRunner
    // Fallback: use PortAudio bindings (NuGet: NAudio.PortAudio)
}
```

### Zone.Identifier Handling

**Current**: File.Delete(path + ":Zone.Identifier") + PowerShell fallback

**New Approach**:
```csharp
public interface ISecurityService
{
    Task RemoveZoneIdentifierAsync(string filePath, CancellationToken ct);
}

public class WindowsSecurityService : ISecurityService
{
    public async Task RemoveZoneIdentifierAsync(string filePath, CancellationToken ct)
    {
        // Try NTFS ADS removal
        try { File.Delete(filePath + ":Zone.Identifier"); }
        catch { /* Fall through */ }
        
        // Fall back to PowerShell
        await _processRunner.RunAsync("powershell", new[] { "-NoProfile", "-Command", $"Unblock-File -LiteralPath '{filePath}'" }, cancellationToken: ct);
    }
}

public class LinuxSecurityService : ISecurityService
{
    public Task RemoveZoneIdentifierAsync(string filePath, CancellationToken ct)
    {
        // No-op on Linux; Zone.Identifier doesn't exist
        return Task.CompletedTask;
    }
}
```

---

## RUNTIME CONFIGURATION

### .csproj Publishing Settings

**Core Projects** (net8.0, no platform-specific):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
  </PropertyGroup>
  
  <!-- NO platform-specific TFM, NO Prefer32Bit -->
</Project>
```

**UI Project** (Avalonia):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <UseAvaloniaUI>true</UseAvaloniaUI>
  </PropertyGroup>
  
  <ItemGroup>
    <PackageReference Include="Avalonia" Version="11.0.0" />
    <!-- No WindowsAPICodePack, no WPF -->
  </ItemGroup>
</Project>
```

**Publish Profiles**:
```xml
<!-- Properties/PublishProfiles/win-x64.pubxml -->
<PropertyGroup>
  <PublishProtocol>FileSystem</PublishProtocol>
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <SelfContained>true</SelfContained>
  <PublishSingleFile>true</PublishSingleFile>
  <PublishTrimmed>false</PublishTrimmed>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
</PropertyGroup>
```

### Publish Targets

```bash
# Win x64 (standalone executable)
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true

# Linux x64 (standalone executable)
dotnet publish -c Release -r linux-x64 -p:PublishSingleFile=true -p:SelfContained=true

# Linux ARM64 (for Raspberry Pi, future ARM servers)
dotnet publish -c Release -r linux-arm64 -p:PublishSingleFile=true -p:SelfContained=true
```

---

## RISK ANALYSIS & MITIGATION

### High-Risk Areas

#### 1. CpkMaker.dll Replacement ⚠️ **CRITICAL**

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| cpk-tools API mismatch | 40% | High | Early integration test, fallback to Wine wrapper |
| CPK format edge cases | 30% | High | Comprehensive unit tests with game files |
| Performance degradation | 20% | Medium | Benchmarking cpk-tools vs CpkMaker |
| Licensing conflict | 10% | High | Pre-audit cpk-tools license (GPL/MIT/Apache) |

**Mitigation Strategy**:
1. Parallel implementation: create ICpkService abstraction, allow plug-in multiple backends
2. Extended test suite with actual game CPK files
3. Create fallback: if cpk-tools unavailable, fall back to CpkMaker.dll on Wine (as interim)
4. Performance benchmarks before/after

#### 2. XFBIN_LIB Integration ⚠️ **MEDIUM**

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| API changes/breaking | 20% | Medium | Version pin, unit tests, integration tests |
| Missing features | 15% | Medium | Maintain custom XFBIN parser as fallback |
| Performance | 10% | Low | Profile and optimize |

**Mitigation**:
- Pin XFBIN_LIB version in csproj
- Comprehensive unit tests for read/write/repack
- Keep legacy XfbinParser.cs as fallback if needed

#### 3. Audio Processing (vgmstream, NAudio) ⚠️ **MEDIUM**

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| vgmstream unavailable on target | 30% | Medium | Graceful degradation, stub implementation |
| NAudio Windows-only | 100% | Low | Already using cross-platform alternatives in newer NAudio versions |
| Performance/latency | 20% | Low | Test on Steam Deck, Winlator |

**Mitigation**:
- Abstract audio behind IAudioService
- Implement graceful fallback when audio unavailable
- Test on actual Steam Deck/Winlator hardware

#### 4. UI Migration (WPF → Avalonia) ⚠️ **MEDIUM**

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Feature parity issues | 25% | Medium | Incremental migration, side-by-side builds |
| Performance (rendering) | 20% | Low | Profile Avalonia on target platforms |
| Library compatibility | 15% | Low | Avalonia 11 stable, extensive ecosystem |

**Mitigation**:
- Build Avalonia version in parallel (branch)
- Feature-for-feature parity tests
- Extensive testing on Steam Deck / Winlator / Wine

### Low-Risk Areas

✓ **Business Logic**: Already portable (.NET standard patterns, no Windows APIs)  
✓ **File I/O**: All relative paths, portable structures  
✓ **Models**: Standard .NET data structures, no platform-specific serialization  
✓ **CLI**: No UI dependencies, naturally platform-agnostic  

---

## IMPLEMENTATION ROADMAP

### Phase 1: Foundation (Weeks 1-3)
**Objective**: Extract core logic, establish abstraction layer

- [ ] Create NSC.Core project (net8.0)
- [ ] Create NSC.Platform project with IPlatformService
- [ ] Implement WindowsPlatformService, LinuxPlatformService
- [ ] Migrate business logic from TitleViewModel to NSC.Core services
- [ ] Set up Dependency Injection container
- [ ] Create ICpkService, IXfbinService, IModService interfaces
- [ ] Write unit tests for core services
- [ ] Evaluate cpk-tools API and create proof-of-concept wrapper

**Deliverable**: NSC.Core + NSC.Platform compiling on Windows and Linux

### Phase 2: CPK Tools Integration (Weeks 3-5)
**Objective**: Replace CpkMaker.dll with cross-platform solution

- [ ] Finalize cpk-tools evaluation
- [ ] Implement CpkService using cpk-tools library
- [ ] Create comprehensive unit tests for CPK operations
- [ ] Test with actual game CPK files
- [ ] Performance benchmarking vs original
- [ ] Create fallback strategy if cpk-tools insufficient

**Deliverable**: CPK extraction/repacking working on Windows and Linux

### Phase 3: CLI Application (Weeks 5-7)
**Objective**: Build command-line interface for headless usage

- [ ] Create NSC.CLI project
- [ ] Implement core commands: `nsc install`, `nsc build`, `nsc merge`, `nsc launch`
- [ ] Implement helper commands: `nsc detect-games`, `nsc list-mods`, `nsc clear-cache`
- [ ] Write integration tests for all workflows
- [ ] Test on Steam Deck / Linux / Wine

**Deliverable**: Fully functional CLI, all features accessible from command line

### Phase 4: UI Migration to Avalonia (Weeks 7-12)
**Objective**: Replace WPF with Avalonia, maintain feature parity

- [ ] Set up NSC.UI.Avalonia project with Avalonia 11
- [ ] Migrate App.xaml → App.axaml
- [ ] Migrate MainWindow (TitleView)
- [ ] Migrate all remaining Views (8 XAML files)
- [ ] Convert WPF ValueConverters to Avalonia IValueConverter
- [ ] Update all ViewModels to use CommunityToolkit.Mvvm
- [ ] Replace CommonOpenFileDialog with Avalonia FilePicker
- [ ] Replace WPF dialogs with Avalonia equivalents
- [ ] Extensive testing: Windows, Linux, Wine, Proton, Steam Deck, Winlator

**Deliverable**: Avalonia UI feature-complete, tested across all platforms

### Phase 5: Testing & Optimization (Weeks 12-14)
**Objective**: Comprehensive testing, performance tuning, documentation

- [ ] Unit test suite (NSC.Core)
- [ ] Integration tests (workflows, platform services)
- [ ] UI tests (Avalonia application)
- [ ] Platform-specific testing (Wine, Proton, Steam Deck, Winlator)
- [ ] Performance profiling
- [ ] Memory usage optimization
- [ ] Build optimization (trim, IL linking)
- [ ] Write documentation (Architecture, CLI Reference, Platform Guides)

**Deliverable**: Production-ready builds, comprehensive documentation

### Phase 6: Release & Distribution (Weeks 14-16)
**Objective**: Build CI/CD, publish packages, final testing

- [ ] GitHub Actions CI/CD workflow
- [ ] Publish scripts for win-x64, linux-x64, linux-arm64
- [ ] Create release packages (ZIP, AppImage, etc.)
- [ ] User documentation & setup guides
- [ ] Final QA on all platforms

**Deliverable**: Release-ready binaries for Windows, Linux, Steam Deck

---

## SUCCESS CRITERIA

**Hard Requirements** (✓ = Must Pass):
- ✓ Compiles on Windows (net8.0)
- ✓ Compiles on Linux (net8.0)
- ✓ Launches successfully without errors
- ✓ Installs mods successfully (existing workflows preserved)
- ✓ Compiles mods successfully
- ✓ Merges mods successfully
- ✓ Packages mods successfully
- ✓ Extracts/repacks CPK files
- ✓ XFBIN processing works (read, write, repack)
- ✓ Game detection works
- ✓ External tools execute correctly (vgmstream, etc.)
- ✓ Runs on Windows 10/11 (x64)
- ✓ Runs on Ubuntu/Debian (x64)
- ✓ Runs on Steam Deck
- ✓ Runs through Wine 9+
- ✓ Runs through Proton GE
- ✓ Runs through Winlator
- ✓ No WPF dependencies
- ✓ No Windows-only UI frameworks
- ✓ No mandatory registry dependency
- ✓ Portable mode (relocatable without AppData)

**Quality Metrics**:
- Code coverage: ≥80% for NSC.Core
- Zero Windows-only P/Invoke in NSC.Core
- All external processes abstracted via IProcessRunner
- All platform-specific code in NSC.Platform layer

---

## ALTERNATIVE STRATEGIES (If Blocked)

### If cpk-tools is unsuitable:

1. **P/Invoke wrapper** (Windows-only, but works in Wine):
   - Keep CpkMaker.dll
   - Run application under Wine on non-Windows
   - Supports Steam Deck via Proton
   - **Trade-off**: Not native Linux

2. **Custom CPK implementation** (months of reverse-engineering):
   - Parse CPK format from scratch
   - Implement encryption/compression
   - **Trade-off**: High effort, maintenance burden

### If Avalonia migration blocked:

1. **WinUI 3** (Windows 11+ only):
   - Similar XAML, modern UX
   - **Trade-off**: Not cross-platform (Windows-only)

2. **Uno Platform** (experimental):
   - WebAssembly backend
   - **Trade-off**: Immature, performance concerns

### If .NET 8 insufficient:

- Target .NET 9 LTS when released
- Maintain .NET 8 compatibility layer

---

## DEPENDENCIES & PREREQUISITES

### Build Requirements
- .NET 8 SDK
- Avalonia 11 templates (`dotnet new --list avalonia`)
- Git (for external libraries)
- GitHub Actions (for CI/CD)

### Runtime Requirements
- .NET 8 runtime (in self-contained single-file mode, included)
- Bundled external tools (vgmstream, cpk-tools)
- Optional: Wine 9+ (for non-native platform testing)

### Development Environment
- Visual Studio 2022+ OR Visual Studio Code + C# extension
- Git
- Terminal/PowerShell

---

## BUDGET & RESOURCE ESTIMATE

| Phase | Effort (days) | Cost (estimat) | Risk Level |
|-------|---------------|----------------|-----------|
| Foundation | 15 | $3,000-5,000 | Low |
| CPK Integration | 14 | $2,800-4,000 | High |
| CLI | 10 | $2,000-3,000 | Low |
| Avalonia UI | 30 | $6,000-9,000 | Medium |
| Testing | 14 | $2,800-4,000 | Low |
| Release & Docs | 10 | $2,000-3,000 | Low |
| **TOTAL** | **~93 days** | **~$19,000-28,000** | — |

**Notes**:
- Assumes 1 senior engineer (familiar with .NET, WPF, MVVM, Avalonia)
- Includes contingency for cpk-tools evaluation and integration challenges
- Does not include extended platform testing (Steam Deck hardware, Winlator mobile testing)

---

## NEXT STEPS (Immediate Actions)

1. **Review this strategy** - Confirm approach and risk tolerance
2. **Audit cpk-tools** - Detailed analysis of API, licensing, performance
3. **Set up repository structure** - Create NSC.Core, NSC.Platform projects
4. **Establish DI framework** - Microsoft.Extensions.DependencyInjection setup
5. **Extract first workflow** - Mod installation (easiest to verify)
6. **Parallel: Start Avalonia setup** - Get simple window rendering
7. **Create CI/CD scaffold** - GitHub Actions for multi-platform builds

---

**Version**: 1.0  
**Date**: 2026-06-19  
**Status**: Pre-Implementation (Approved for execution)  
**Next Review**: After Phase 1 (Foundation completion)

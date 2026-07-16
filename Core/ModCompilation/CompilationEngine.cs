using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace NSC_ModManager.Core.ModCompilation
{
    /// <summary>
    /// Compilation phases according to MVP specification
    /// Each phase is critical and builds on the previous one
    /// </summary>
    public enum CompilationPhase
    {
        PreflightCheck = 1,  // Validate everything before starting
        Clean = 2,           // Remove old mod files
        Merge = 3,           // Merge multiple mods into single dataset
        CPKPhase = 4,        // Process CPK archives if needed
        Deploy = 5,          // Copy compiled mods to game folder
        Launch = 6           // Start the game
    }

    /// <summary>
    /// Main compilation engine implementing 6-phase system
    /// PHASE 1: Pre-flight Check
    /// PHASE 2: Clean Phase
    /// PHASE 3: Merge Phase
    /// PHASE 4: CPK Phase (Conditional)
    /// PHASE 5: Deploy Phase
    /// PHASE 6: Launch Phase
    /// </summary>
    public class CompilationEngine
    {
        private string _gameRootPath;
        private string _modStoragePath;
        private List<ModItem> _activeMods;
        private string _ultimateStormAPIPath;

        public event Action<CompilationPhase, string>? OnPhaseProgress;
        public event Action<string>? OnError;

        public CompilationEngine(string gameRoot, string modStorage)
        {
            _gameRootPath = gameRoot;
            _modStoragePath = modStorage;
            _ultimateStormAPIPath = Path.Combine(gameRoot, "UltimateStormAPI");
        }

        /// <summary>
        /// PHASE 1: Pre-flight Check
        /// Validates:
        /// - Root folder exists
        /// - data_win32 folder is NOT present (game must be clean)
        /// - Mods are selected
        /// - Dependencies are valid
        /// </summary>
        private bool Phase1_PreflightCheck()
        {
            OnPhaseProgress?.Invoke(CompilationPhase.PreflightCheck, "Checking game installation...");

            try
            {
                // Check root folder exists
                if (!Directory.Exists(_gameRootPath))
                {
                    OnError?.Invoke($"❌ Game root folder not found: {_gameRootPath}");
                    return false;
                }

                // CRITICAL: Check if data_win32 folder exists (must be clean)
                string dataWin32Path = Path.Combine(_gameRootPath, "data_win32");
                if (Directory.Exists(dataWin32Path))
                {
                    OnError?.Invoke(
                        "❌ ERROR: Found data_win32 folder in game root.\n\n" +
                        "NSC Mod Manager requires a CLEAN game installation.\n" +
                        "Please delete or backup the data_win32 folder and try again.");
                    return false;
                }

                // Validate mod files are selected
                if (_activeMods == null || _activeMods.Count == 0)
                {
                    OnError?.Invoke("❌ No mods selected for compilation");
                    return false;
                }

                // Validate each mod file exists
                foreach (var mod in _activeMods)
                {
                    if (!File.Exists(mod.FilePath))
                    {
                        OnError?.Invoke($"❌ Mod file not found: {mod.FilePath}");
                        return false;
                    }
                }

                // Check for dependency conflicts
                if (!ValidateDependencies())
                {
                    OnError?.Invoke("❌ Mod dependency validation failed");
                    return false;
                }

                OnPhaseProgress?.Invoke(CompilationPhase.PreflightCheck, "✓ Preflight check passed");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"❌ Preflight check error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PHASE 2: Clean Phase
        /// Removes all old mod files and creates fresh folder structure
        /// CRITICAL: This clears ALL previous mods
        /// </summary>
        private bool Phase2_Clean()
        {
            OnPhaseProgress?.Invoke(CompilationPhase.Clean, "Cleaning old mod files...");

            try
            {
                // Delete UltimateStormAPI folder if exists
                if (Directory.Exists(_ultimateStormAPIPath))
                {
                    Directory.Delete(_ultimateStormAPIPath, true);
                    System.Threading.Thread.Sleep(200); // Wait for file deletion
                }

                // Create fresh folder structure
                Directory.CreateDirectory(_ultimateStormAPIPath);
                Directory.CreateDirectory(Path.Combine(_ultimateStormAPIPath, "param", "NSC"));
                Directory.CreateDirectory(Path.Combine(_ultimateStormAPIPath, "param", "NS4"));
                Directory.CreateDirectory(Path.Combine(_ultimateStormAPIPath, "lua"));
                Directory.CreateDirectory(Path.Combine(_ultimateStormAPIPath, "resources"));

                OnPhaseProgress?.Invoke(CompilationPhase.Clean, "✓ Clean phase completed");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"❌ Clean phase error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PHASE 3: Merge Phase
        /// Parses and merges multiple mod files
        /// Handles conflict resolution
        /// </summary>
        private bool Phase3_Merge()
        {
            OnPhaseProgress?.Invoke(CompilationPhase.Merge, "Merging mod files...");

            try
            {
                ModMerger merger = new ModMerger();

                foreach (var mod in _activeMods.Where(m => m.IsEnabled))
                {
                    OnPhaseProgress?.Invoke(CompilationPhase.Merge, $"Processing: {mod.Name} v{mod.Version}");

                    // Parse mod file based on format
                    if (!merger.AddMod(mod))
                    {
                        OnError?.Invoke($"❌ Failed to parse mod: {mod.Name}");
                        return false;
                    }
                }

                // Perform the actual merge
                var mergedData = merger.MergeAll();

                OnPhaseProgress?.Invoke(CompilationPhase.Merge, "✓ Merge phase completed");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"❌ Merge phase error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PHASE 4: CPK Processing (Conditional)
        /// Only runs if mods require CPK repacking
        /// Extracts, modifies, and repacks CPK archives
        /// </summary>
        private bool Phase4_CPKPhase()
        {
            OnPhaseProgress?.Invoke(CompilationPhase.CPKPhase, "Checking for CPK modifications...");

            try
            {
                // Check if any mods need CPK repacking
                bool needsCPKRepack = _activeMods
                    .Where(m => m.IsEnabled)
                    .Any(m => m.RequiresCPKRepacking);

                if (!needsCPKRepack)
                {
                    OnPhaseProgress?.Invoke(CompilationPhase.CPKPhase, "✓ No CPK modifications needed");
                    return true;
                }

                CPKHandler cpkHandler = new CPKHandler(_gameRootPath);

                OnPhaseProgress?.Invoke(CompilationPhase.CPKPhase, "Extracting CPK archives...");
                if (!cpkHandler.ExtractRequiredCPKs())
                {
                    OnError?.Invoke("❌ Failed to extract CPK files");
                    return false;
                }

                OnPhaseProgress?.Invoke(CompilationPhase.CPKPhase, "Repacking CPK files...");
                if (!cpkHandler.RepackCPKs())
                {
                    OnError?.Invoke("❌ Failed to repack CPK files");
                    return false;
                }

                OnPhaseProgress?.Invoke(CompilationPhase.CPKPhase, "✓ CPK phase completed");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"❌ CPK phase error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PHASE 5: Deploy Phase
        /// Copies compiled mods to UltimateStormAPI folder structure
        /// </summary>
        private bool Phase5_Deploy()
        {
            OnPhaseProgress?.Invoke(CompilationPhase.Deploy, "Deploying compiled mods...");

            try
            {
                // Copy param files to appropriate game folder (NSC or NS4)
                string gameType = GetGameType();
                string paramPath = Path.Combine(_ultimateStormAPIPath, "param", gameType);

                OnPhaseProgress?.Invoke(CompilationPhase.Deploy, $"Deploying to {gameType} structure...");

                // Deploy files to UltimateStormAPI structure
                if (!DeployFiles(paramPath))
                {
                    OnError?.Invoke("❌ Failed to deploy files");
                    return false;
                }

                OnPhaseProgress?.Invoke(CompilationPhase.Deploy, "✓ Deploy phase completed");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"❌ Deploy phase error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// PHASE 6: Launch Phase
        /// Starts the game executable
        /// </summary>
        private bool Phase6_Launch()
        {
            OnPhaseProgress?.Invoke(CompilationPhase.Launch, "Launching game...");

            try
            {
                string? gamePath = Directory.GetFiles(_gameRootPath, "*.exe")
                    .FirstOrDefault(f => f.EndsWith("NSC.exe") || f.EndsWith("NS4.exe"));

                if (gamePath == null)
                {
                    OnError?.Invoke("❌ Game executable not found");
                    return false;
                }

                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = gamePath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(gamePath)
                };

                System.Diagnostics.Process.Start(psi);
                OnPhaseProgress?.Invoke(CompilationPhase.Launch, "✓ Game launched successfully");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"❌ Launch error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Execute full compilation pipeline (all 6 phases)
        /// </summary>
        public async Task<bool> CompileAsync(List<ModItem> mods)
        {
            _activeMods = mods;

            // Phase 1: Preflight
            if (!Phase1_PreflightCheck())
                return false;

            // Phase 2: Clean
            if (!Phase2_Clean())
                return false;

            // Phase 3: Merge
            if (!Phase3_Merge())
                return false;

            // Phase 4: CPK
            if (!Phase4_CPKPhase())
                return false;

            // Phase 5: Deploy
            if (!Phase5_Deploy())
                return false;

            // Phase 6: Launch
            if (!Phase6_Launch())
                return false;

            return true;
        }

        private bool ValidateDependencies()
        {
            // TODO: Implement dependency validation
            // Check if required mods are present for dependent mods
            return true;
        }

        private string GetGameType()
        {
            if (File.Exists(Path.Combine(_gameRootPath, "NSC.exe")))
                return "NSC";
            if (File.Exists(Path.Combine(_gameRootPath, "NS4.exe")))
                return "NS4";
            return "NSC"; // Default
        }

        private bool DeployFiles(string paramPath)
        {
            // TODO: Implement file deployment logic
            // Copy parsed mod files to UltimateStormAPI structure
            return true;
        }
    }

    /// <summary>
    /// Represents a single mod item
    /// </summary>
    public class ModItem
    {
        public string Name { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Character, Stage, Costume, etc.
        public string FilePath { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public bool RequiresCPKRepacking { get; set; } = false;
        public List<string> Dependencies { get; set; } = new();
        public string? Thumbnail { get; set; }
    }
}

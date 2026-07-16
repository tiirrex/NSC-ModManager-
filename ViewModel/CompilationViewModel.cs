using NSC_ModManager.Core.ModCompilation;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NSC_ModManager.ViewModel
{
    /// <summary>
    /// Main ViewModel for title/mod manager view
    /// Integrates CompilationEngine with MVVM pattern
    /// </summary>
    public class CompilationViewModel : INotifyPropertyChanged
    {
        private readonly CompilationEngine _compilationEngine;
        private string _gameRootPath = string.Empty;
        private string _modStoragePath = string.Empty;
        private CompilationPhase _currentPhase = CompilationPhase.PreflightCheck;
        private string _phaseStatus = "Ready";
        private bool _isCompiling = false;
        private int _phaseProgress = 0; // 0-100 for progress bar

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<ModItem> AvailableMods { get; } = new();
        public ObservableCollection<string> CompilationLog { get; } = new();

        public string GameRootPath
        {
            get => _gameRootPath;
            set { if (_gameRootPath != value) { _gameRootPath = value; OnPropertyChanged(); } }
        }

        public string ModStoragePath
        {
            get => _modStoragePath;
            set { if (_modStoragePath != value) { _modStoragePath = value; OnPropertyChanged(); } }
        }

        public CompilationPhase CurrentPhase
        {
            get => _currentPhase;
            set { if (_currentPhase != value) { _currentPhase = value; OnPropertyChanged(); } }
        }

        public string PhaseStatus
        {
            get => _phaseStatus;
            set { if (_phaseStatus != value) { _phaseStatus = value; OnPropertyChanged(); } }
        }

        public bool IsCompiling
        {
            get => _isCompiling;
            set { if (_isCompiling != value) { _isCompiling = value; OnPropertyChanged(); } }
        }

        public int PhaseProgress
        {
            get => _phaseProgress;
            set { if (_phaseProgress != value) { _phaseProgress = value; OnPropertyChanged(); } }
        }

        public ICommand CompileAndLaunchCommand { get; }
        public ICommand ClearGameCommand { get; }
        public ICommand AddModCommand { get; }
        public ICommand RemoveModCommand { get; }
        public ICommand ToggleModCommand { get; }

        public CompilationViewModel()
        {
            _compilationEngine = new CompilationEngine(_gameRootPath, _modStoragePath);
            _compilationEngine.OnPhaseProgress += HandlePhaseProgress;
            _compilationEngine.OnError += HandleError;

            CompileAndLaunchCommand = new RelayCommand(_ => CompileAndLaunch(), CanCompile);
            ClearGameCommand = new RelayCommand(_ => ClearGame(), CanClearGame);
            AddModCommand = new RelayCommand(_ => AddMod());
            RemoveModCommand = new RelayCommand(RemoveMod, CanRemoveMod);
            ToggleModCommand = new RelayCommand(ToggleMod);
        }

        #region Command Implementations

        private async void CompileAndLaunch()
        {
            try
            {
                IsCompiling = true;
                CompilationLog.Clear();
                PhaseProgress = 0;

                AddLog("🔄 Starting compilation...\n");

                // Get active mods
                var activeMods = AvailableMods.Where(m => m.IsEnabled).ToList();
                AddLog($"📦 {activeMods.Count} mods selected\n");

                // Run compilation
                bool success = await _compilationEngine.CompileAsync(activeMods);

                if (success)
                {
                    AddLog("\n✅ SUCCESS! Game launched with mods.\n");
                    PhaseProgress = 100;
                }
                else
                {
                    AddLog("\n❌ Compilation failed. Check logs above.\n");
                    PhaseProgress = 0;
                }
            }
            catch (Exception ex)
            {
                AddLog($"\n❌ FATAL ERROR: {ex.Message}\n");
                PhaseProgress = 0;
            }
            finally
            {
                IsCompiling = false;
            }
        }

        private void ClearGame()
        {
            try
            {
                if (!System.IO.Directory.Exists(GameRootPath))
                {
                    AddLog("❌ Game root path not set\n");
                    return;
                }

                AddLog("🧹 Clearing game mods...\n");

                // Remove proxy DLL
                if (DLLInjector.RemoveProxyDLL(GameRootPath))
                {
                    AddLog("✓ Removed DLL injection\n");
                }

                // Delete UltimateStormAPI folder
                string apiPath = System.IO.Path.Combine(GameRootPath, "UltimateStormAPI");
                if (System.IO.Directory.Exists(apiPath))
                {
                    System.IO.Directory.Delete(apiPath, true);
                    AddLog("✓ Removed UltimateStormAPI folder\n");
                }

                AddLog("\n✅ Game cleaned successfully!\n");
            }
            catch (Exception ex)
            {
                AddLog($"\n❌ Clear game error: {ex.Message}\n");
            }
        }

        private void AddMod()
        {
            // TODO: File dialog to select mod
            AddLog("➕ Add mod functionality (TODO)\n");
        }

        private void RemoveMod(object? obj)
        {
            if (obj is ModItem mod)
            {
                AvailableMods.Remove(mod);
                AddLog($"❌ Removed: {mod.Name}\n");
            }
        }

        private void ToggleMod(object? obj)
        {
            if (obj is ModItem mod)
            {
                mod.IsEnabled = !mod.IsEnabled;
                string status = mod.IsEnabled ? "enabled" : "disabled";
                AddLog($"✓ {mod.Name} {status}\n");
            }
        }

        #endregion

        #region Event Handlers

        private void HandlePhaseProgress(CompilationPhase phase, string message)
        {
            CurrentPhase = phase;
            PhaseStatus = message;
            PhaseProgress = (int)phase * 16; // Roughly 16-17% per phase

            AddLog($"[Phase {(int)phase}] {message}\n");
        }

        private void HandleError(string error)
        {
            AddLog($"❌ {error}\n");
            PhaseProgress = 0;
        }

        #endregion

        #region Helper Methods

        private void AddLog(string message)
        {
            CompilationLog.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        }

        private bool CanCompile()
        {
            return !IsCompiling && AvailableMods.Any(m => m.IsEnabled);
        }

        private bool CanClearGame()
        {
            return !IsCompiling && !string.IsNullOrEmpty(GameRootPath);
        }

        private bool CanRemoveMod(object? obj)
        {
            return !IsCompiling && obj is ModItem;
        }

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}

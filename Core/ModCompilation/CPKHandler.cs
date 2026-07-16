using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace NSC_ModManager.Core.ModCompilation
{
    /// <summary>
    /// Handles CPK archive extraction and repacking
    /// 
    /// CPK Structure (CyberConnect2 proprietary):
    /// - data_win32.cpk - Main game files (models, textures, params)
    /// - data_sound.cpk - Audio/music files
    /// - data_movie.cpk - Video/cutscene files
    /// - data_update.cpk - DLC/update files
    /// </summary>
    public class CPKHandler
    {
        private string _gameRootPath;
        private string _cpkToolPath;
        private string _tempExtractPath;

        public event Action<string>? OnProgress;
        public event Action<string>? OnError;

        public CPKHandler(string gameRootPath)
        {
            _gameRootPath = gameRootPath;
            _cpkToolPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "YACpkTool.exe");
            _tempExtractPath = Path.Combine(Path.GetTempPath(), "NSCModManager_CPK_Extract");
        }

        /// <summary>
        /// Extract required CPK files
        /// Only extracts CPKs that contain modified files
        /// </summary>
        public bool ExtractRequiredCPKs()
        {
            try
            {
                if (!File.Exists(_cpkToolPath))
                {
                    OnError?.Invoke($"CPK tool not found: {_cpkToolPath}");
                    return false;
                }

                // Clean temp directory
                if (Directory.Exists(_tempExtractPath))
                    Directory.Delete(_tempExtractPath, true);
                Directory.CreateDirectory(_tempExtractPath);

                // List of CPKs to check
                string[] cpkFiles = new[]
                {
                    "data_win32.cpk",
                    "data_sound.cpk",
                    "data_movie.cpk",
                    "data_update.cpk"
                };

                foreach (string cpkFile in cpkFiles)
                {
                    string cpkPath = Path.Combine(_gameRootPath, cpkFile);
                    if (!File.Exists(cpkPath))
                        continue;

                    OnProgress?.Invoke($"Extracting {cpkFile}...");

                    // Extract CPK using YACpkTool
                    if (!ExtractCPK(cpkPath))
                    {
                        OnError?.Invoke($"Failed to extract {cpkFile}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"CPK extraction error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Extract a single CPK file
        /// </summary>
        private bool ExtractCPK(string cpkPath)
        {
            try
            {
                string cpkName = Path.GetFileNameWithoutExtension(cpkPath);
                string extractPath = Path.Combine(_tempExtractPath, cpkName);
                Directory.CreateDirectory(extractPath);

                // Run YACpkTool.exe -extract cpk_path output_path
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = _cpkToolPath,
                    Arguments = $"-extract \"{cpkPath}\" \"{extractPath}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi)!)
                {
                    process.WaitForExit(300000); // 5 minute timeout

                    if (process.ExitCode != 0)
                    {
                        OnError?.Invoke($"YACpkTool failed with exit code {process.ExitCode}");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"CPK extraction failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Repack modified CPK files
        /// Uses CpkMaker.dll to create new CPK archives
        /// </summary>
        public bool RepackCPKs()
        {
            try
            {
                OnProgress?.Invoke("Repacking CPK files...");

                // Find extracted directories
                if (!Directory.Exists(_tempExtractPath))
                {
                    OnError?.Invoke("Extracted CPK directory not found");
                    return false;
                }

                string[] extractedCPKs = Directory.GetDirectories(_tempExtractPath);

                foreach (string extractedPath in extractedCPKs)
                {
                    string cpkName = Path.GetFileName(extractedPath);
                    string originalCPKPath = Path.Combine(_gameRootPath, cpkName + ".cpk");

                    if (!File.Exists(originalCPKPath))
                        continue;

                    OnProgress?.Invoke($"Repacking {cpkName}.cpk...");

                    // Create backup of original
                    string backupPath = originalCPKPath + ".bak";
                    if (!File.Exists(backupPath))
                    {
                        File.Copy(originalCPKPath, backupPath, true);
                    }

                    // Repack using CpkMaker
                    if (!RepackSingleCPK(extractedPath, originalCPKPath))
                    {
                        OnError?.Invoke($"Failed to repack {cpkName}.cpk");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"CPK repacking error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Repack a single CPK file using CpkMaker.dll
        /// </summary>
        private bool RepackSingleCPK(string extractedPath, string outputCPKPath)
        {
            try
            {
                // TODO: Implement CpkMaker.dll interop for repacking
                // CpkMaker is a .NET DLL so we can call it directly
                
                // For MVP: placeholder
                OnProgress?.Invoke($"Would repack: {extractedPath}");
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Repack single CPK failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Restore CPK files from backup
        /// </summary>
        public bool RestoreCPKBackups()
        {
            try
            {
                string[] backups = Directory.GetFiles(_gameRootPath, "*.cpk.bak");

                foreach (string backupPath in backups)
                {
                    string originalPath = backupPath.Replace(".bak", "");
                    File.Copy(backupPath, originalPath, true);
                    OnProgress?.Invoke($"Restored: {Path.GetFileName(originalPath)}");
                }

                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"CPK restore error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Clean up temporary extraction files
        /// </summary>
        public bool CleanupTempFiles()
        {
            try
            {
                if (Directory.Exists(_tempExtractPath))
                {
                    Directory.Delete(_tempExtractPath, true);
                }
                return true;
            }
            catch (Exception ex)
            {
                OnError?.Invoke($"Cleanup failed: {ex.Message}");
                return false;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NSC_ModManager.Core.ModCompilation
{
    /// <summary>
    /// Merges multiple mod files into a single unified dataset
    /// Handles conflict resolution when multiple mods modify the same file
    /// </summary>
    public class ModMerger
    {
        private List<ModData> _modDataList = new();
        private Dictionary<string, byte[]> _mergedFiles = new();

        /// <summary>
        /// Add a mod to the merge queue
        /// </summary>
        public bool AddMod(ModItem mod)
        {
            try
            {
                // Parse mod file based on extension
                string extension = Path.GetExtension(mod.FilePath).ToLower();

                ModData modData = extension switch
                {
                    ".nsc" => ParseNSCMod(mod.FilePath),
                    ".ensc" => ParseENCryptedMod(mod.FilePath),
                    ".uns" => ParseUNSMod(mod.FilePath),
                    ".unse" => ParseUNSEncryptedMod(mod.FilePath),
                    ".nus4" => ParseLegacyNUS4Mod(mod.FilePath),
                    _ => throw new Exception($"Unsupported mod format: {extension}")
                };

                if (modData == null)
                    return false;

                _modDataList.Add(modData);
                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to add mod: {ex.Message}");
            }
        }

        /// <summary>
        /// Merge all added mods into single dataset
        /// Handles conflicts using priority system
        /// </summary>
        public Dictionary<string, byte[]> MergeAll()
        {
            _mergedFiles.Clear();

            // Sort by load priority (earlier = lower priority, gets overwritten)
            var sortedMods = _modDataList.OrderBy(m => m.Priority).ToList();

            foreach (var modData in sortedMods)
            {
                MergeMod(modData);
            }

            return _mergedFiles;
        }

        /// <summary>
        /// Merge a single mod's files into the master dataset
        /// </summary>
        private void MergeMod(ModData modData)
        {
            foreach (var file in modData.Files)
            {
                string filePath = file.Key;

                if (_mergedFiles.ContainsKey(filePath))
                {
                    // File already exists - check for conflicts
                    if (IsMergeableFile(filePath))
                    {
                        // Try to merge file contents
                        _mergedFiles[filePath] = MergeFileContents(
                            _mergedFiles[filePath],
                            file.Value,
                            filePath);
                    }
                    else
                    {
                        // Non-mergeable - use priority (higher priority wins)
                        if (modData.Priority > GetModPriority(filePath))
                        {
                            _mergedFiles[filePath] = file.Value;
                        }
                    }
                }
                else
                {
                    // New file - just add it
                    _mergedFiles[filePath] = file.Value;
                }
            }
        }

        /// <summary>
        /// Check if a file can be intelligently merged (not just replaced)
        /// </summary>
        private bool IsMergeableFile(string filePath)
        {
            // XFBIN files can be merged (multiple entries can coexist)
            if (filePath.EndsWith(".xfbin"))
                return true;

            // Binary param files can be merged
            if (filePath.Contains("param") && filePath.EndsWith(".bin"))
                return true;

            return false;
        }

        /// <summary>
        /// Merge file contents intelligently
        /// For XFBIN: merge entries
        /// For params: merge entries
        /// </summary>
        private byte[] MergeFileContents(byte[] existing, byte[] incoming, string filePath)
        {
            try
            {
                // TODO: Implement XFBIN and binary param merging
                // For MVP, just use incoming (higher priority wins)
                return incoming;
            }
            catch
            {
                // Merge failed - use incoming
                return incoming;
            }
        }

        /// <summary>
        /// Get the priority/order of a file in the merge queue
        /// </summary>
        private int GetModPriority(string filePath)
        {
            var mod = _modDataList.FirstOrDefault(m => m.Files.ContainsKey(filePath));
            return mod?.Priority ?? 0;
        }

        #region Mod Format Parsers

        private ModData? ParseNSCMod(string filePath)
        {
            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    // NSC format header (4 bytes magic)
                    string magic = System.Text.Encoding.ASCII.GetString(br.ReadBytes(4));
                    if (magic != "NSC\0" && magic != "NSC1")
                        throw new Exception("Invalid NSC magic number");

                    ModData modData = new ModData { Priority = 1 };

                    // Parse files from NSC archive
                    while (fs.Position < fs.Length)
                    {
                        // Read file entry
                        int nameLength = br.ReadInt32();
                        string fileName = System.Text.Encoding.UTF8.GetString(br.ReadBytes(nameLength));
                        int fileSize = br.ReadInt32();
                        byte[] fileContent = br.ReadBytes(fileSize);

                        modData.Files[fileName] = fileContent;
                    }

                    return modData;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse NSC mod: {ex.Message}");
            }
        }

        private ModData? ParseENCryptedMod(string filePath)
        {
            try
            {
                // ENSC files use custom AES encryption
                // TODO: Implement ENSC decryption
                // For MVP: placeholder
                throw new NotImplementedException("ENSC decryption not yet implemented");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse ENSC mod: {ex.Message}");
            }
        }

        private ModData? ParseUNSMod(string filePath)
        {
            try
            {
                // UNS format (older format from Ultimate Ninja Storm)
                // Similar to NSC but different header
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    string magic = System.Text.Encoding.ASCII.GetString(br.ReadBytes(4));
                    if (magic != "UNS\0" && magic != "UNS1")
                        throw new Exception("Invalid UNS magic number");

                    ModData modData = new ModData { Priority = 2 }; // Lower priority than NSC

                    while (fs.Position < fs.Length)
                    {
                        int nameLength = br.ReadInt32();
                        string fileName = System.Text.Encoding.UTF8.GetString(br.ReadBytes(nameLength));
                        int fileSize = br.ReadInt32();
                        byte[] fileContent = br.ReadBytes(fileSize);

                        modData.Files[fileName] = fileContent;
                    }

                    return modData;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse UNS mod: {ex.Message}");
            }
        }

        private ModData? ParseUNSEncryptedMod(string filePath)
        {
            try
            {
                // UNSE = Encrypted UNS
                throw new NotImplementedException("UNSE decryption not yet implemented");
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse UNSE mod: {ex.Message}");
            }
        }

        private ModData? ParseLegacyNUS4Mod(string filePath)
        {
            try
            {
                // NUS4 format - legacy from Naruto Storm 4
                // May have compatibility issues with NSC
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (BinaryReader br = new BinaryReader(fs))
                {
                    ModData modData = new ModData { Priority = 0 }; // Lowest priority - legacy

                    // Attempt to parse as archive
                    // TODO: Implement NUS4 format parser
                    
                    return modData;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse NUS4 mod (legacy): {ex.Message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents parsed mod data
    /// </summary>
    internal class ModData
    {
        public Dictionary<string, byte[]> Files { get; set; } = new();
        public int Priority { get; set; } = 1; // Higher = loads later (overwrites earlier)
    }
}

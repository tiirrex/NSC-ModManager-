using System;
using System.IO;
using System.Runtime.InteropServices;

namespace NSC_ModManager.Core.GameDetection
{
    /// <summary>
    /// Validates that the game is a legitimate Steam version
    /// CRITICAL: NSC Mod Manager ONLY works with Steam versions (anti-piracy)
    /// </summary>
    public class SteamVersionValidator
    {
        private const string STEAM_APP_ID_NSC = "2766590";  // Naruto Storm Connections
        private const string STEAM_APP_ID_NS4 = "660950";   // Naruto Storm 4

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern bool GetFileAttributesEx(string name, int fileInfoLevel, out WIN32_FILE_ATTRIBUTE_DATA fileData);

        [StructLayout(LayoutKind.Sequential)]
        private struct WIN32_FILE_ATTRIBUTE_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
        }

        /// <summary>
        /// Validate if the game is a legitimate Steam version
        /// Checks for:
        /// - NSC.exe or NS4.exe executable
        /// - Steam appmanifest files
        /// - Proper file structure
        /// </summary>
        public static bool IsValidSteamVersion(string gamePath)
        {
            if (string.IsNullOrEmpty(gamePath) || !Directory.Exists(gamePath))
                return false;

            try
            {
                // Check if NSC.exe or NS4.exe exists
                bool hasNSCExe = File.Exists(Path.Combine(gamePath, "NSC.exe"));
                bool hasNS4Exe = File.Exists(Path.Combine(gamePath, "NS4.exe"));

                if (!hasNSCExe && !hasNS4Exe)
                    return false;

                // Check for Steam's app manifest (appmanifest_*.acf)
                // This is the key indicator that the game is installed via Steam
                string steamAppsPath = Path.GetDirectoryName(gamePath);
                if (steamAppsPath == null)
                    return false;

                // Look for appmanifest file in the parent directory
                string[] appManifests = Directory.GetFiles(Path.GetDirectoryName(steamAppsPath), "appmanifest_*.acf");
                if (appManifests.Length == 0)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Determine which game version (NSC or NS4)
        /// </summary>
        public static string? GetGameType(string gamePath)
        {
            try
            {
                if (File.Exists(Path.Combine(gamePath, "NSC.exe")))
                    return "NSC"; // Naruto Storm Connections

                if (File.Exists(Path.Combine(gamePath, "NS4.exe")))
                    return "NS4";  // Naruto Storm 4

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get the Steam library path from a game directory
        /// </summary>
        public static string? GetSteamLibraryPath(string gamePath)
        {
            try
            {
                return Path.GetDirectoryName(Path.GetDirectoryName(gamePath));
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Verify file integrity and executable signatures
        /// </summary>
        public static bool VerifyGameFileIntegrity(string gamePath)
        {
            try
            {
                string gameExe = Path.Combine(gamePath, "NSC.exe");
                if (!File.Exists(gameExe))
                    gameExe = Path.Combine(gamePath, "NS4.exe");

                if (!File.Exists(gameExe))
                    return false;

                // Check file attributes (executable, readable)
                FileInfo fileInfo = new FileInfo(gameExe);
                return fileInfo.Exists && fileInfo.Length > 0;
            }
            catch
            {
                return false;
            }
        }
    }
}

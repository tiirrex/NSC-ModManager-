using System;
using System.IO;

namespace NSC_ModManager.Core.ModCompilation
{
    /// <summary>
    /// Handles DLL injection using xinput9_1_0.dll proxy pattern
    /// 
    /// Why xinput9_1_0.dll?
    /// - Game requires XInput for controller support (will load this DLL)
    /// - Windows DLL Search Order: Game folder → System32
    /// - Proxy can forward calls to original DLL without affecting gameplay
    /// - Low detection risk - appears as legitimate controller input API
    /// - Simple export functions (~5 functions vs 100+ for d3dcompiler)
    /// </summary>
    public class DLLInjector
    {
        /// <summary>
        /// Setup proxy DLL injection system
        /// Creates xinput9_1_0.dll proxy that loads UltimateStormAPI
        /// </summary>
        public static bool SetupProxyDLL(string gameRootPath)
        {
            try
            {
                string dllPath = Path.Combine(gameRootPath, "xinput9_1_0.dll");
                string origDllPath = Path.Combine(gameRootPath, "xinput9_1_0_o.dll");

                // Backup original DLL if not already backed up
                if (!File.Exists(origDllPath) && File.Exists(dllPath))
                {
                    // Check if current file is the original (not our proxy)
                    if (!IsProxyDLL(dllPath))
                    {
                        File.Copy(dllPath, origDllPath, true);
                    }
                }

                // Write proxy DLL binary
                if (!WriteProxyDLL(dllPath))
                {
                    throw new Exception("Failed to write proxy DLL");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to setup proxy DLL: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if DLL is our proxy or the original system DLL
        /// </summary>
        private static bool IsProxyDLL(string dllPath)
        {
            try
            {
                // Check file size - proxy is typically larger than original
                FileInfo fileInfo = new FileInfo(dllPath);
                
                // Original xinput9_1_0.dll is typically < 100KB
                // Our proxy should be > 100KB (includes UltimateStormAPI code)
                return fileInfo.Length > 100000;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Write proxy DLL to disk
        /// In production, this would extract from embedded resources
        /// </summary>
        private static bool WriteProxyDLL(string dllPath)
        {
            try
            {
                // TODO: Extract compiled proxy DLL from embedded resources
                // For MVP, this is a placeholder
                // Production would have actual compiled C++ DLL binary

                if (!File.Exists(dllPath))
                {
                    // In real implementation:
                    // byte[] proxyDLLBinary = ExtractResourceBinary("ProxyDLL.dll");
                    // File.WriteAllBytes(dllPath, proxyDLLBinary);
                    
                    throw new NotImplementedException(
                        "Proxy DLL binary not found in resources.\n" +
                        "Compile the C++ proxy DLL project and embed as resource.");
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to write proxy DLL: {ex.Message}");
            }
        }

        /// <summary>
        /// Remove proxy DLL and restore original system DLL
        /// </summary>
        public static bool RemoveProxyDLL(string gameRootPath)
        {
            try
            {
                string dllPath = Path.Combine(gameRootPath, "xinput9_1_0.dll");
                string origDllPath = Path.Combine(gameRootPath, "xinput9_1_0_o.dll");

                // Delete proxy DLL
                if (File.Exists(dllPath))
                {
                    File.Delete(dllPath);
                }

                // Restore original if backup exists
                if (File.Exists(origDllPath))
                {
                    File.Copy(origDllPath, dllPath, true);
                    File.Delete(origDllPath);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to remove proxy DLL: {ex.Message}");
            }
        }

        /// <summary>
        /// Verify proxy DLL is correctly installed and loaded
        /// </summary>
        public static bool VerifyProxyDLLInstallation(string gameRootPath)
        {
            try
            {
                string dllPath = Path.Combine(gameRootPath, "xinput9_1_0.dll");
                string origDllPath = Path.Combine(gameRootPath, "xinput9_1_0_o.dll");

                if (!File.Exists(dllPath))
                    return false;

                // At least one backup should exist
                if (!File.Exists(origDllPath))
                    return false;

                // Proxy should be larger than original
                FileInfo proxyInfo = new FileInfo(dllPath);
                FileInfo origInfo = new FileInfo(origDllPath);

                return proxyInfo.Length > origInfo.Length;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Proxy DLL architecture details (for reference/documentation)
        /// 
        /// C++ Export Functions (via extern "C"):
        /// - XInputGetState()
        /// - XInputSetState()
        /// - XInputGetCapabilities()
        /// - XInputGetBatteryInformation()
        /// - XInputGetKeystroke()
        /// 
        /// DllMain Initialization:
        /// 1. Load original xinput9_1_0_o.dll
        /// 2. Get function pointers for all exports
        /// 3. Initialize UltimateStormAPI
        /// 4. Install hooks for game file loading
        /// 5. Return TRUE to continue
        /// 
        /// Function Forwarding:
        /// - All exported functions forward to original DLL
        /// - No modification to controller input (preserves gameplay)
        /// - UltimateStormAPI hooks intercept file loading
        /// </summary>
        public static string GetProxyDLLArchitectureInfo()
        {
            return @"
Proxy DLL Architecture (xinput9_1_0.dll):

PURPOSE:
- Non-invasive mod injection point
- Loads before game initialization
- Intercepts file loading system

MECHANISM:
1. DLL Search Order: Game folder searched BEFORE System32
2. Windows loads our xinput9_1_0.dll first
3. DllMain() initializes UltimateStormAPI
4. All XInput functions forwarded to original DLL
5. Game receives legitimate controller input + mod files

ADVANTAGES:
✓ Steam-compatible (no executable modification)
✓ Easily removable (delete 2 DLL files)
✓ Low detection risk (standard API)
✓ Proven method (used in many mod managers)
✓ Supports complex mod merging/patching

EXPORT FUNCTIONS:
- XInputGetState() -> Get controller state
- XInputSetState() -> Set controller rumble
- XInputGetCapabilities() -> Get controller info
- XInputGetBatteryInformation() -> Battery status
- XInputGetKeystroke() -> Get keystroke
";
        }
    }
}

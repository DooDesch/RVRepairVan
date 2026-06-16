using System;
using System.IO;

namespace RVRepairVan.Persistence
{
    /// <summary>
    /// Per-save persistence of the "RV was repaired" flag. The flag file lives inside
    /// the current save folder (derived from SaveManager.PlayersSavePath), so repair
    /// state stays scoped to a single save game and survives reloads.
    /// </summary>
    internal static class RepairStateStore
    {
        private const string FileName = "rv_repairvan.json";

        private static string StateFilePath()
        {
            try
            {
                SaveManager sm = SaveManager.Instance;
                if (sm == null)
                {
                    return null;
                }

                string playersPath = sm.PlayersSavePath; // <save>/Players
                if (string.IsNullOrEmpty(playersPath))
                {
                    return null;
                }

                string saveDir = Path.GetDirectoryName(playersPath);
                if (string.IsNullOrEmpty(saveDir))
                {
                    return null;
                }

                return Path.Combine(saveDir, FileName);
            }
            catch
            {
                return null;
            }
        }

        internal static void SetRepaired(bool repaired)
        {
            string path = StateFilePath();
            if (path == null)
            {
                return;
            }

            try
            {
                File.WriteAllText(path, "{\"repaired\":" + (repaired ? "true" : "false") + "}");
            }
            catch (Exception e)
            {
                Core.Log.Warning("[State] save failed: " + e.Message);
            }
        }

        internal static bool GetRepaired()
        {
            string path = StateFilePath();
            if (path == null || !File.Exists(path))
            {
                return false;
            }

            try
            {
                return File.ReadAllText(path).Contains("\"repaired\":true");
            }
            catch
            {
                return false;
            }
        }
    }
}

using MelonLoader;
using UnityEngine;

namespace RVRepairVan.Config
{
    /// <summary>
    /// MelonPreferences wrapper. The category identifier is prefixed with the mod name
    /// ("RVRepairVan_...") so it is auto-detected by the "Mod Manager &amp; Phone App"
    /// settings UI (Prowiler). See Phase 4 integration.
    /// </summary>
    internal static class RVRepairVanPreferences
    {
        private const string CategoryId = "RVRepairVan_01_Main";

        private static MelonPreferences_Category _category;
        private static MelonPreferences_Entry<bool> _enabled;
        private static MelonPreferences_Entry<int> _repairPrice;
        private static MelonPreferences_Entry<bool> _destroyRvDebug;
        private static MelonPreferences_Entry<bool> _addCashDebug;

        internal static void Initialize()
        {
            if (_category != null)
            {
                return;
            }

            _category = MelonPreferences.CreateCategory(CategoryId, "RV Repair Van");
            _enabled = CreateEntry("Enabled", true, "Enabled", "Enable the RV repair feature.");
            _repairPrice = CreateEntry("RepairPrice", 1500, "Repair Price", "Cash required to repair the RV.");
            _destroyRvDebug = CreateEntry("DestroyRvDebug", false, "Destroy RV (debug, one-shot)",
                "Toggle ON to manually wreck the RV so you can test the repair. Auto-resets to OFF.");
            _addCashDebug = CreateEntry("AddCashDebug", false, "Add $10,000 (debug, one-shot)",
                "Toggle ON to give yourself $10,000 cash for testing. Auto-resets to OFF.");
        }

        internal const float DebugCashAmount = 10000f;

        private static MelonPreferences_Entry<T> CreateEntry<T>(string identifier, T defaultValue, string displayName, string description = null)
        {
            return _category.CreateEntry(identifier, defaultValue, displayName, description);
        }

        internal static bool Enabled => _enabled?.Value ?? true;
        internal static int RepairPrice => Mathf.Max(0, _repairPrice?.Value ?? 1500);

        /// <summary>
        /// Returns true once if the debug "Destroy RV" toggle is on, resetting it back to off
        /// (in-memory, to avoid a save -> OnPreferencesSaved recursion). One-shot.
        /// </summary>
        internal static bool ConsumeDestroyRequest()
        {
            if (_destroyRvDebug != null && _destroyRvDebug.Value)
            {
                _destroyRvDebug.Value = false;
                return true;
            }
            return false;
        }

        /// <summary>One-shot: true once if the debug "Add cash" toggle is on, then resets it.</summary>
        internal static bool ConsumeAddCashRequest()
        {
            if (_addCashDebug != null && _addCashDebug.Value)
            {
                _addCashDebug.Value = false;
                return true;
            }
            return false;
        }
    }
}

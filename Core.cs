using System;
using System.Collections;
using MelonLoader;
using RVRepairVan.Config;
using RVRepairVan.Dialogue;
using RVRepairVan.Managers;
using RVRepairVan.Persistence;

[assembly: MelonInfo(typeof(RVRepairVan.Core), "RVRepairVan", "2.0.0", "DooDesch", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace RVRepairVan
{
    /// <summary>
    /// MelonLoader entry point. Mirrors the Mimesis-InventoryExpansion template:
    /// initialize preferences, patch Harmony, log status. Per-scene setup runs the
    /// Marco dialogue injection and the post-load repair-state restore.
    /// </summary>
    public sealed class Core : MelonMod
    {
        public static Core Instance { get; private set; }
        public static MelonLogger.Instance Log { get; private set; }

        public override void OnInitializeMelon()
        {
            Instance = this;
            Log = LoggerInstance;

            RVRepairVanPreferences.Initialize();
            HarmonyInstance.PatchAll();

            Log.Msg($"RVRepairVan initialized. Enabled={RVRepairVanPreferences.Enabled}, RepairPrice={RVRepairVanPreferences.RepairPrice}");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName != "Main")
            {
                return;
            }

            RVManager.Reset();
            MarcoRepairDialogue.Reset();
            MingQuestDialogue.Reset();

            MelonCoroutines.Start(MarcoRepairDialogue.SetupCoroutine());
            MelonCoroutines.Start(MingQuestDialogue.SetupCoroutine());
            MelonCoroutines.Start(RestoreRepairCoroutine());
        }

        /// <summary>
        /// Fired when preferences are saved (incl. via the Mod Manager &amp; Phone App UI).
        /// Handles the one-shot debug "Destroy RV" toggle. Full live-settings integration
        /// follows in the Mod Manager phase.
        /// </summary>
        public override void OnPreferencesSaved()
        {
            try
            {
                // Live-update Marco's repair choice label to the current price (no restart).
                MarcoRepairDialogue.RefreshPrice();

                if (RVRepairVanPreferences.ConsumeDestroyRequest())
                {
                    Log.Msg("[Debug] Destroy RV toggle on - wrecking the RV now.");
                    if (RVManager.Destroy())
                    {
                        RepairStateStore.SetRepaired(false);
                    }
                }

                if (RVRepairVanPreferences.ConsumeAddCashRequest())
                {
                    S1API.Money.Money.ChangeCashBalance(RVRepairVanPreferences.DebugCashAmount, true, true);
                    Log.Msg("[Debug] Added " + MoneyManager.FormatAmount(RVRepairVanPreferences.DebugCashAmount) + " cash.");
                }
            }
            catch (Exception e)
            {
                Log.Warning("[Prefs] OnPreferencesSaved failed: " + e.Message);
            }
        }

        /// <summary>
        /// After a save loads, re-apply a previously paid-for repair (no charge) if the
        /// RV spawned back in its destroyed state.
        /// </summary>
        private static IEnumerator RestoreRepairCoroutine()
        {
            yield return new WaitForSeconds(4f);

            bool restore = false;
            try
            {
                restore = RVManager.TryLocate()
                          && RepairStateStore.GetRepaired()
                          && RVManager.IsDestroyed();
            }
            catch (Exception e)
            {
                Log.Warning("[Restore] check failed: " + e.Message);
            }

            if (restore)
            {
                Log.Msg("[Restore] RV was previously repaired - restoring without charge.");
                RVManager.Repair();
            }
        }
    }
}

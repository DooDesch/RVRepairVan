using System;
using RVRepairVan.Managers;
using S1API.Quests;

namespace RVRepairVan.Quests
{
    /// <summary>
    /// The "Repair the RV" quest, built on S1API's Quest wrapper (handles the game's
    /// quest object, journal + HUD, and per-save persistence automatically).
    ///
    /// Lifecycle (driven by S1API): constructor builds the underlying quest object ->
    /// the game's Quest.Start fires -> S1API calls CreateInternal -> OnCreated (entries
    /// added here) -> InitializeQuest -> Begin (if AutoBegin) -> shows in journal/HUD.
    /// </summary>
    public class RepairRVQuest : Quest
    {
        protected override string Title => RepairQuest.Title;

        protected override string Description =>
            "Your RV got wrecked. Marco, the mechanic down at the docks, can patch it up - for a price.";

        protected override bool AutoBegin => true;

        protected override void OnCreated()
        {
            base.OnCreated();
            try
            {
                QuestEntry entry = AddEntry("Get your RV repaired by Marco");

                // The game's QuestEntry.Start() registers a compass element from PoILocation.
                // S1API leaves PoILocation null, and on this game build (0.4.5f2) CompassManager
                // then dereferences null every frame (the "0m flicker"). S1API's NPC system is
                // also broken here (DanSamwell), so SetPOIToNPC<MarcoBaron> can't be used.
                // Fix: give the entry a VALID fixed POI at Marco's actual world position
                // (resolved via direct interop), falling back to the RV's position.
                entry.POIPosition = ResolveMarkerPosition();
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Quest] OnCreated failed: " + e.Message);
            }
        }

        /// <summary>Marco's world position (preferred), else the RV's, else origin - always valid.</summary>
        private static Vector3 ResolveMarkerPosition()
        {
            try
            {
                var marcos = UnityEngine.Object.FindObjectsOfType<Marco>();
                if (marcos != null && marcos.Length > 0 && marcos[0] != null)
                {
                    return marcos[0].transform.position;
                }
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Quest] locating Marco for POI failed: " + e.Message);
            }

            if (RVManager.TryGetPosition(out Vector3 rvPos))
            {
                return rvPos;
            }

            return Vector3.zero;
        }
    }
}

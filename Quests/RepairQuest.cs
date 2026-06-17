using System;
using S1API.Quests;

namespace RVRepairVan.Quests
{
    /// <summary>
    /// Drives the "Back on the Road" quest lifecycle. Start is triggered by the explosion-beat
    /// auto-start in Questline.ProximityCoroutine; completion is triggered by reaching the
    /// repaired RV. S1API persists the active quest per save, so we look it up by name rather
    /// than holding a stale reference across reloads.
    /// </summary>
    internal static class RepairQuest
    {
        internal const string Title = "Back on the Road";

        internal static bool IsActive()
        {
            try
            {
                return QuestManager.GetQuestByName(Title) != null;
            }
            catch
            {
                return false;
            }
        }

        internal static void StartIfNeeded()
        {
            try
            {
                if (IsActive())
                {
                    Core.Log.Msg("[Quest] '" + Title + "' already active.");
                    return;
                }

                QuestManager.CreateQuest<RepairRVQuest>();
                Core.Log.Msg("[Quest] '" + Title + "' started.");
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Quest] start failed: " + e.Message);
            }
        }

        /// <summary>Retitle + re-point the (single) quest entry - used by the questline per stage.</summary>
        internal static void UpdateEntry(string title, Vector3 poi)
        {
            try
            {
                Quest quest = QuestManager.GetQuestByName(Title);
                if (quest?.QuestEntries != null && quest.QuestEntries.Count > 0)
                {
                    QuestEntry entry = quest.QuestEntries[0];
                    entry.Title = title;
                    entry.POIPosition = poi;
                }
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Quest] update entry failed: " + e.Message);
            }
        }

        internal static void CompleteIfActive()
        {
            try
            {
                Quest quest = QuestManager.GetQuestByName(Title);
                if (quest == null)
                {
                    return;
                }

                if (quest.QuestEntries != null)
                {
                    foreach (QuestEntry entry in quest.QuestEntries)
                    {
                        try { entry.Complete(); }
                        catch { /* keep completing the rest */ }
                    }
                }

                Core.Log.Msg("[Quest] '" + Title + "' completed.");
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Quest] complete failed: " + e.Message);
            }
        }
    }
}

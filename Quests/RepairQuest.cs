using System;
using S1API.Quests;

namespace RVRepairVan.Quests
{
    /// <summary>
    /// Drives the "Repair the RV" quest lifecycle. Start is triggered by Ming
    /// (quest-giver); completion is triggered when Marco finishes the repair.
    /// S1API persists the active quest per save, so we look it up by name rather
    /// than holding a stale reference across reloads.
    /// </summary>
    internal static class RepairQuest
    {
        internal const string Title = "Repair the RV";

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
                    Core.Log.Msg("[Quest] 'Repair the RV' already active.");
                    return;
                }

                QuestManager.CreateQuest<RepairRVQuest>();
                Core.Log.Msg("[Quest] 'Repair the RV' started.");
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Quest] start failed: " + e.Message);
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

                Core.Log.Msg("[Quest] 'Repair the RV' completed.");
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Quest] complete failed: " + e.Message);
            }
        }
    }
}

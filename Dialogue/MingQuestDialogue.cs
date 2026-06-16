using System;
using System.Collections;
using Il2CppInterop.Runtime;
using RVRepairVan.Managers;
using RVRepairVan.Persistence;
using RVRepairVan.Quests;
using UnityEngine.Events;

namespace RVRepairVan.Dialogue
{
    /// <summary>
    /// Ming is the quest-giver: while the RV is wrecked (and not yet repaired or
    /// already quested), a dialogue choice with Ming starts the "Repair the RV" quest,
    /// which directs the player to Marco.
    /// </summary>
    internal static class MingQuestDialogue
    {
        private static bool _injected;

        internal static void Reset()
        {
            _injected = false;
        }

        internal static IEnumerator SetupCoroutine()
        {
            for (int attempt = 0; attempt < 20 && !_injected; attempt++)
            {
                yield return new WaitForSeconds(2f);
                TryInject();
            }

            if (!_injected)
            {
                Core.Log.Warning("[Ming] Could not inject RV quest dialogue (Ming not found within ~40s).");
            }
        }

        private static void TryInject()
        {
            try
            {
                var mings = UnityEngine.Object.FindObjectsOfType<Ming>();
                if (mings == null || mings.Length == 0)
                {
                    return;
                }
                // Inject into the NPC's actual dialogue controller (via DialogueHandler),
                // the same proven path used for Marco - not the standalone DialogueController_Ming.
                DialogueController dc = FindController(mings[0]);
                if (dc == null)
                {
                    return;
                }

                DialogueController.DialogueChoice choice = new DialogueController.DialogueChoice
                {
                    Enabled = true,
                    ChoiceText = "My RV got wrecked - what should I do?",
                    Conversation = null,
                    Priority = 90
                };

                // Only offer the quest while the RV is wrecked, unrepaired, and not already quested.
                Func<bool, bool> shouldShow = (enabled) =>
                    enabled
                    && RVManager.IsDestroyed()
                    && !RepairStateStore.GetRepaired()
                    && !RepairQuest.IsActive();
                choice.shouldShowCheck =
                    DelegateSupport.ConvertDelegate<DialogueController.DialogueChoice.ShouldShowCheck>(shouldShow);

                choice.onChoosen = new UnityEvent();
                choice.onChoosen.AddListener((Action)OnAskedMing);

                dc.AddDialogueChoice(choice, 90);

                _injected = true;
                Core.Log.Msg("[Ming] RV quest dialogue injected.");
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Ming] inject attempt failed: " + e.Message);
            }
        }

        private static DialogueController FindController(Ming ming)
        {
            try
            {
                DialogueHandler dh = ((NPC)ming).DialogueHandler;
                if (dh != null)
                {
                    DialogueController dc = ((Component)dh).GetComponentInChildren<DialogueController>();
                    if (dc != null)
                    {
                        return dc;
                    }
                }
            }
            catch { /* fall through */ }

            try
            {
                return ((Component)ming).GetComponentInChildren<DialogueController>();
            }
            catch
            {
                return null;
            }
        }

        private static void OnAskedMing()
        {
            try
            {
                RepairQuest.StartIfNeeded();
            }
            catch (Exception e)
            {
                Core.Log.Error("[Ming] starting quest failed: " + e);
            }
        }
    }
}

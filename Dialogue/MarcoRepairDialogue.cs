using System;
using System.Collections;
using Il2CppInterop.Runtime;
using RVRepairVan.Config;
using RVRepairVan.Managers;
using RVRepairVan.Persistence;
using RVRepairVan.Quests;
using UnityEngine.Events;

namespace RVRepairVan.Dialogue
{
    /// <summary>
    /// Injects a "Repair my RV (&lt;price&gt;)" choice into the mechanic Marco's dialogue.
    /// Marco (Il2CppScheduleOne.NPCs.CharacterClasses.Marco) is found at runtime;
    /// the choice is only shown while the RV is destroyed, and charges the player
    /// the configured price before repairing.
    /// </summary>
    internal static class MarcoRepairDialogue
    {
        private static bool _injected;
        private static DialogueController.DialogueChoice _repairChoice;

        internal static void Reset()
        {
            _injected = false;
            _repairChoice = null;
        }

        /// <summary>Update the visible choice label to the current price (live, no restart).</summary>
        internal static void RefreshPrice()
        {
            try
            {
                if (_repairChoice != null)
                {
                    _repairChoice.ChoiceText = BuildChoiceText();
                }
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Marco] price refresh failed: " + e.Message);
            }
        }

        /// <summary>
        /// Retry-injects the choice until Marco has spawned (NPCs load late). The choice is
        /// hidden until the RV is destroyed, so injecting early on a fresh game is harmless;
        /// the generous window ensures it is in place before the RV can ever be wrecked.
        /// </summary>
        internal static IEnumerator SetupCoroutine()
        {
            for (int attempt = 0; attempt < 20 && !_injected; attempt++)
            {
                yield return new WaitForSeconds(2f);
                TryInject();
            }

            if (!_injected)
            {
                Core.Log.Warning("[Marco] Could not inject RV repair dialogue (Marco not found within ~40s).");
            }
        }

        private static void TryInject()
        {
            try
            {
                Marco marco = FindMarco();
                if (marco == null)
                {
                    return;
                }

                DialogueController dc = FindController(marco);
                if (dc == null)
                {
                    return;
                }

                DialogueController.DialogueChoice choice = new DialogueController.DialogueChoice
                {
                    Enabled = true,
                    ChoiceText = BuildChoiceText(),
                    Conversation = null,
                    Priority = 100
                };

                // Only show the choice while the RV is actually destroyed.
                Func<bool, bool> shouldShow = (enabled) => enabled && RVManager.IsDestroyed();
                choice.shouldShowCheck =
                    DelegateSupport.ConvertDelegate<DialogueController.DialogueChoice.ShouldShowCheck>(shouldShow);

                choice.onChoosen = new UnityEvent();
                choice.onChoosen.AddListener((Action)OnRepairChosen);

                dc.AddDialogueChoice(choice, 100);

                _repairChoice = choice;
                _injected = true;
                Core.Log.Msg("[Marco] RV repair dialogue injected.");
            }
            catch (Exception e)
            {
                Core.Log.Warning("[Marco] inject attempt failed: " + e.Message);
            }
        }

        private static Marco FindMarco()
        {
            var marcos = UnityEngine.Object.FindObjectsOfType<Marco>();
            if (marcos == null || marcos.Length == 0)
            {
                return null;
            }
            return marcos[0];
        }

        private static DialogueController FindController(Marco marco)
        {
            try
            {
                DialogueHandler dh = ((NPC)marco).DialogueHandler;
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
                return ((Component)marco).GetComponentInChildren<DialogueController>();
            }
            catch
            {
                return null;
            }
        }

        private static string BuildChoiceText()
        {
            return "Repair my RV (" + MoneyManager.FormatAmount(RVRepairVanPreferences.RepairPrice) + ")";
        }

        // Floating worldspace bubble at Marco (his completion line after the repair cinematic).
        private static void WorldSay(NPC npc, string line)
        {
            try
            {
                if (npc != null) npc.SendWorldSpaceDialogue(line, 5f);
            }
            catch { /* worldspace lines are nice-to-have */ }
        }

        private static void OnRepairChosen()
        {
            try
            {
                if (!RVRepairVanPreferences.Enabled)
                {
                    return;
                }

                if (!RVManager.IsDestroyed())
                {
                    Core.Log.Msg("[Marco] RV is not destroyed - nothing to repair.");
                    return;
                }

                float price = RVRepairVanPreferences.RepairPrice;

                // Money via S1API (backend-agnostic wrapper) instead of direct interop.
                if (S1API.Money.Money.GetCashBalance() < price)
                {
                    Core.Log.Msg("[Marco] Not enough cash for repair (" + MoneyManager.FormatAmount(price) + ").");
                    return;
                }

                // Commit payment synchronously (so an interrupted cinematic can never double-charge), then play the
                // repair cinematic - parity with the questline path (Quests/Questline.cs OnMarcoRepair).
                S1API.Money.Money.ChangeCashBalance(-price, true, true);
                float paid = price;
                Marco marco = FindMarco();
                RVRepairVan.Effects.RepairCinematic.Play(
                    () =>   // at the darkest point, with the swap hidden
                    {
                        if (RVManager.Repair())
                        {
                            RepairStateStore.SetRepaired(true);
                            RepairQuest.CompleteIfActive();
                            Core.Log.Msg("[Marco] RV repaired for " + MoneyManager.FormatAmount(paid) + ".");
                        }
                    },
                    () => WorldSay(marco, "There she is - back from the dead. Try not to total her again."),
                    () => Questline.GruntNpc(marco));   // mid-repair: Marco hurts himself
            }
            catch (Exception e)
            {
                Core.Log.Error("[Marco] repair failed: " + e);
            }
        }
    }
}

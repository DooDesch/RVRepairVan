using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
#if IL2CPP
using GameQuestManager = Il2CppScheduleOne.Quests.QuestManager;
#else
using GameQuestManager = ScheduleOne.Quests.QuestManager;
#endif

namespace RVRepairVan.Net.Patches
{
    /// <summary>
    /// Intercepts the game's quest RPC logic to carry RVRepairVan bus messages (guids prefixed "RVRV:"):
    /// - RpcLogic___SendQuestState runs on the HOST when a client's SendQuestState ServerRpc arrives -> our intent.
    /// - RpcLogic___ReceiveQuestState runs on each CLIENT when the host's ReceiveQuestState ObserversRpc arrives -> our state.
    /// For our guids the prefix dispatches to the NetworkBus and SKIPS the vanilla body (so it never chokes on a
    /// guid that is not a real quest). Real quest guids fall through untouched. Patched manually (not via PatchAll)
    /// so a failure to resolve a method just disables networking gracefully instead of breaking other patches.
    /// </summary>
    internal static class QuestManagerNetPatches
    {
        internal static void Apply(HarmonyLib.Harmony h)
        {
            TryPatch(h, "RpcLogic___SendQuestState", nameof(SendStatePrefix));
            TryPatch(h, "RpcLogic___ReceiveQuestState", nameof(ReceiveStatePrefix));
        }

        private static void TryPatch(HarmonyLib.Harmony h, string namePrefix, string prefixMethod)
        {
            try
            {
                MethodInfo target = typeof(GameQuestManager)
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name.StartsWith(namePrefix, StringComparison.Ordinal));
                if (target == null) { Core.Log.Warning("[Net] could not find " + namePrefix + " - co-op sync disabled."); return; }

                MethodInfo pre = typeof(QuestManagerNetPatches).GetMethod(prefixMethod, BindingFlags.NonPublic | BindingFlags.Static);
                h.Patch(target, prefix: new HarmonyMethod(pre));
                Core.LogDebug("[Net] patched " + target.Name);
            }
            catch (Exception e) { Core.Log.Warning("[Net] patch " + namePrefix + " failed: " + e.Message); }
        }

        // Host side. RpcLogic___SendQuestState(string guid, EQuestState state) -> guid is arg 0.
        private static bool SendStatePrefix(string __0)
        {
            if (RvMsg.TryDecode(__0, out RvMsg m)) { NetworkBus.DispatchServerIntent(m); return false; }
            return true;
        }

        // Client side. RpcLogic___ReceiveQuestState(NetworkConnection conn, string guid, EQuestState state) -> guid is arg 1.
        private static bool ReceiveStatePrefix(string __1)
        {
            if (RvMsg.TryDecode(__1, out RvMsg m)) { NetworkBus.DispatchClientState(m); return false; }
            return true;
        }
    }
}

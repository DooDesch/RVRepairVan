#if DEBUG
using System;
using HarmonyLib;
using Il2CppScheduleOne.Product.Packaging;            // PackagingDefinition

namespace RVRepairVan.Patches
{
    /// <summary>
    /// DEBUG-only test helper. Typing <c>rvtest</c> in the dev console drops a set of pre-packaged products at every
    /// quality into the inventory, so the Marco sample / quality-multiplier flow can be tested without manually
    /// equipping + setquality + packageproduct each one (packageproduct only works on the equipped-in-hand item).
    /// Usage: <c>rvtest</c> (jar OG Kush) or <c>rvtest &lt;packaging&gt; &lt;productId&gt;</c>. Compiled out of Release.
    /// </summary>
    [HarmonyPatch(typeof(Il2CppScheduleOne.Console), nameof(Il2CppScheduleOne.Console.SubmitCommand), new Type[] { typeof(Il2CppSystem.Collections.Generic.List<string>) })]
    internal static class DebugConsolePatch
    {
        private static readonly EQuality[] Qualities =
            { EQuality.Trash, EQuality.Poor, EQuality.Standard, EQuality.Premium, EQuality.Heavenly };

        private static bool Prefix(Il2CppSystem.Collections.Generic.List<string> args)
        {
            try
            {
                if (args == null || args.Count == 0) return true;   // not ours - let the game handle it
                switch (args[0].ToLower())
                {
                    case "rvtest":
                        GiveTestProducts(args.Count > 1 ? args[1] : "jar", args.Count > 2 ? args[2] : "ogkush");
                        return false;
                    case "rvstage":   // skip errands while testing - run ON THE HOST (it broadcasts to clients)
                        if (args.Count > 1 && int.TryParse(args[1], out int st)) RVRepairVan.Quests.Questline.DebugSetStage(st);
                        else Core.Log.Warning("[Debug] rvstage <n>  (1=Start 5=Referred 6=MarcoMet 7=ReadyToPay 8=Trusted 9=Paid). Run on the HOST.");
                        return false;
                    case "rvdrops":   // dump every dead drop: name, position, empty? - to diagnose the pickup drop
                        DumpDrops();
                        return false;
                    case "rvclear":   // wipe accumulated test crates/packages from ALL drops + re-reserve a fresh one
                        ClearErrandItems();
                        RVRepairVan.Quests.Questline.DebugResetErrandDrop();
                        return false;
                    case "rvgiveclient":   // host: tell every CLIENT to spawn packaged test products (jar OG Kush)
                        RVRepairVan.Net.NetworkBus.BroadcastToAll(RVRepairVan.Net.RvOp.DebugGiveItems);
                        Core.Log.Msg("[Debug] rvgiveclient: broadcast DebugGiveItems to clients (run this on the HOST).");
                        return false;
                    default:
                        return true;   // not one of ours - let the game handle it
                }
            }
            catch (Exception e) { Core.Log.Warning("[Debug] console cmd failed: " + e.Message); return false; }
        }

        private static void ClearErrandItems()
        {
            try
            {
                var all = S1API.DeadDrops.DeadDropManager.All;
                if (all == null) { Core.Log.Msg("[Debug] rvclear: no drops."); return; }
                int n = 0;
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] == null) continue;
                    n += all[i].Storage.RemoveAllOfDefinition("rv_ming_crate");
                    n += all[i].Storage.RemoveAllOfDefinition("rv_marco_package");
                }
                Core.Log.Msg("[Debug] rvclear: removed " + n + " quest item(s) from all drops.");
            }
            catch (Exception e) { Core.Log.Warning("[Debug] rvclear failed: " + e.Message); }
        }

        private static void DumpDrops()
        {
            try
            {
                var all = S1API.DeadDrops.DeadDropManager.All;
                if (all == null) { Core.Log.Msg("[Debug] rvdrops: DeadDropManager.All is null."); return; }
                Core.Log.Msg("[Debug] rvdrops: " + all.Length + " dead drops (empty=no items):");
                for (int i = 0; i < all.Length; i++)
                {
                    var d = all[i];
                    if (d == null) continue;
                    Core.Log.Msg("  [" + i + "] '" + d.Name + "' pos=(" + Mathf.RoundToInt(d.Position.x)
                        + "," + Mathf.RoundToInt(d.Position.z) + ") empty=" + d.IsEmpty);
                }
            }
            catch (Exception e) { Core.Log.Warning("[Debug] rvdrops failed: " + e.Message); }
        }

        internal static void GiveTestProducts(string packagingId, string productId)
        {
            PlayerInventory inv = PlayerSingleton<PlayerInventory>.Instance;
            if (inv == null) { Core.Log.Warning("[Debug] rvtest: no PlayerInventory."); return; }

            ProductDefinition def = Il2CppScheduleOne.Registry.GetItem(productId.ToLower())?.TryCast<ProductDefinition>();
            PackagingDefinition pkg = Il2CppScheduleOne.Registry.GetItem(packagingId.ToLower())?.TryCast<PackagingDefinition>();
            if (def == null) { Core.Log.Warning("[Debug] rvtest: unknown product '" + productId + "'."); return; }
            if (pkg == null) { Core.Log.Warning("[Debug] rvtest: unknown packaging '" + packagingId + "'."); return; }

            int given = 0;
            foreach (EQuality q in Qualities)
            {
                ProductItemInstance inst = def.GetDefaultInstance(1)?.TryCast<ProductItemInstance>();
                if (inst == null) continue;
                inst.Quality = q;
                inst.SetPackaging(pkg);   // the exact call the 'packageproduct' console command uses
                if (!inv.CanItemFitInInventory(inst)) { Core.Log.Warning("[Debug] rvtest: inventory full at " + q + "."); break; }
                inv.AddItemToInventory(inst);
                given++;
            }
            Core.Log.Msg("[Debug] rvtest: gave " + given + " packaged " + def.Name + " (" + pkg.Name + ") - one per quality.");
        }
    }
}
#endif

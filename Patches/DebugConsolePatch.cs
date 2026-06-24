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
                if (!string.Equals(args[0], "rvtest", StringComparison.OrdinalIgnoreCase)) return true;

                string packaging = args.Count > 1 ? args[1] : "jar";
                string product = args.Count > 2 ? args[2] : "ogkush";
                GiveTestProducts(packaging, product);
                return false;   // handled - skip the game's dispatcher (avoids "command not found")
            }
            catch (Exception e) { Core.Log.Warning("[Debug] rvtest failed: " + e.Message); return false; }
        }

        private static void GiveTestProducts(string packagingId, string productId)
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

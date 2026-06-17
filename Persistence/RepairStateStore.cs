namespace RVRepairVan.Persistence
{
    /// <summary>
    /// Accessor for the questline's persisted state. Backs onto <see cref="RepairSave"/>, which S1API serialises
    /// into the game save (written on game-save, read on load) - so reads/writes here are in-memory during play
    /// and only hit disk when the player actually saves. This is what keeps our state in sync with the game's own
    /// RV/quest state instead of an immediately-written json that could desync. If the Saveable instance does not
    /// exist yet (very early, before the first load), reads return defaults and writes no-op.
    /// </summary>
    internal static class RepairStateStore
    {
        private static RepairSave S => RepairSave.Instance;

        internal static bool GetRepaired() => S != null && S.Repaired;
        internal static void SetRepaired(bool repaired) { if (S != null) S.Repaired = repaired; }

        internal static int GetStage() => S != null ? S.Stage : 0;
        internal static void SetStage(int stage) { if (S != null) S.Stage = stage; }

        internal static int GetSamples() => S != null ? S.Samples : 0;
        internal static void SetSamples(int samples) { if (S != null) S.Samples = samples; }

        internal static int GetDiscountTotal() => S != null ? S.Discount : 0;
        internal static void SetDiscountTotal(int discount) { if (S != null) S.Discount = discount; }
    }
}

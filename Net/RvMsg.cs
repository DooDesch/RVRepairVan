using System;

namespace RVRepairVan.Net
{
    /// <summary>
    /// Opcodes for the RVRepairVan network bus. Intents flow client -> host; state flows host -> all clients.
    /// They travel as a private guid string on the game's own quest RPC channel (see NetworkBus), so no custom
    /// FishNet serializer is needed - the payload is a plain string the game already knows how to replicate.
    /// </summary>
    internal enum RvOp
    {
        // client -> host intents (the host validates + applies the shared effect)
        AcceptErrand = 0,
        DeliverCrate = 1,
        MingPayLoss = 2,
        MarcoGreet = 3,
        MarcoReferral = 4,
        MarcoFavour = 5,
        GotPackage = 6,
        MarcoPayLoss = 7,
        GiveSample = 8,   // A = discount the client computed from its consumed sample
        PayRepair = 9,
        RequestSnapshot = 10,
        AskDonna = 11,
        CheckedRv = 12,   // client reached the post-repair "check on the RV" spot

        // host -> all clients state (absolute values, idempotent on re-receive)
        StageSync = 100,      // A = stage, B = discount total
        RepairApplied = 101,
        TransientSync = 102,  // A = flags (1=pickupActive, 2=hasPackage), B = active dead-drop index (-1 = none)
        Ping = 200,   // Phase 0 spike only
    }

    /// <summary>A decoded bus message. Carried as the guid string "RVRV:&lt;op&gt;:&lt;a&gt;:&lt;b&gt;".</summary>
    internal struct RvMsg
    {
        internal const string Prefix = "RVRV:";

        internal RvOp Op;
        internal int A;   // primary arg (stage, discount, ping value...)
        internal int B;   // secondary arg

        internal static string Encode(RvOp op, int a = 0, int b = 0) => Prefix + (int)op + ":" + a + ":" + b;

        internal static bool TryDecode(string guid, out RvMsg msg)
        {
            msg = default;
            if (string.IsNullOrEmpty(guid) || !guid.StartsWith(Prefix, StringComparison.Ordinal)) return false;
            try
            {
                string[] p = guid.Substring(Prefix.Length).Split(':');
                if (p.Length < 1 || !int.TryParse(p[0], out int op)) return false;
                msg.Op = (RvOp)op;
                if (p.Length > 1) int.TryParse(p[1], out msg.A);
                if (p.Length > 2) int.TryParse(p[2], out msg.B);
                return true;
            }
            catch { return false; }
        }

        public override string ToString() => Op + "(" + A + "," + B + ")";
    }
}

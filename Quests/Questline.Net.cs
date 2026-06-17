using System;
using System.Collections;
using MelonLoader;
using RVRepairVan.Config;
using RVRepairVan.Managers;
using RVRepairVan.Net;
using RVRepairVan.Persistence;

namespace RVRepairVan.Quests
{
    /// <summary>
    /// Co-op networking glue for the questline (host-authoritative, "1:1" with base-game quests).
    ///
    /// Model: the HOST owns the canonical state (Stage / DiscountTotal / RV repair). A client never mutates
    /// shared state directly - it sends an INTENT (RvOp, client -> host via NetworkBus.SendToHost) and the host
    /// validates it against its own Stage, applies the effect (advance Stage, charge the shared money pool, repair
    /// the RV), then broadcasts the resulting STATE (StageSync / RepairApplied, host -> all via BroadcastToAll).
    /// Clients render that state (objective text/POI, price, RV visual) but are otherwise dumb terminals.
    ///
    /// Offline (NetworkBus.Online == false) NONE of this runs - every handler takes its original local branch,
    /// so single-player is byte-for-byte unchanged.
    /// </summary>
    internal static partial class Questline
    {
        private static bool _netWired;

        /// <summary>Wire the bus dispatch once (idempotent). Called from Start().</summary>
        internal static void InitNet()
        {
            if (_netWired) return;
            _netWired = true;
            NetworkBus.OnServerIntent = OnServerIntent;   // host: a client (or host-loopback) intent arrived
            NetworkBus.OnClientState = OnClientState;      // any client: host state arrived
        }

        /// <summary>
        /// Intent router used at the top of every shared-state handler. On a co-op CLIENT it forwards the action
        /// to the host as an intent and returns true (the caller must return - the host will apply + replicate).
        /// Offline or on the host it returns false, so the caller runs the body locally (and the host's Stage/
        /// DiscountTotal writes auto-broadcast). This is the single branch that separates "ask the host" from
        /// "I am the authority" - keeping every handler's offline path byte-for-byte unchanged.
        /// </summary>
        private static bool RouteIntent(RvOp op, int a = 0, int b = 0)
        {
            if (NetworkBus.Online && !NetworkBus.IsServer) { NetworkBus.SendToHost(op, a, b); return true; }
            return false;
        }

        // On join, a client cannot trust its local save - ask the host for the current snapshot once the
        // network session is up. The host answers with StageSync (+ RepairApplied if already repaired).
        private static IEnumerator NetJoinCoroutine()
        {
            // Wait until we're actually in a networked session and know our role (up to ~30s).
            for (int i = 0; i < 60; i++)
            {
                yield return new WaitForSeconds(0.5f);
                if (NetworkBus.Online) break;
            }
            if (!NetworkBus.Online || NetworkBus.IsServer) yield break;   // offline or host: nothing to request

            // Ask twice, a beat apart, in case the first rides in before the host's QuestManager is ready.
            NetworkBus.SendToHost(RvOp.RequestSnapshot);
            yield return new WaitForSeconds(2f);
            NetworkBus.SendToHost(RvOp.RequestSnapshot);
        }

        // ---- HOST: an intent arrived (from a client, or from the host itself via RPC loopback) ----
        private static void OnServerIntent(RvMsg m)
        {
            if (!NetworkBus.IsServer) return;   // only the host is authoritative
            try
            {
                // Each case runs the SAME handler body the host would run for its own action: on the host RouteIntent
                // returns false, so the body executes and its Stage/DiscountTotal writes auto-broadcast to clients.
                // Handlers with a per-player inventory effect dispatch to their Host* variant (no local inventory).
                switch (m.Op)
                {
                    case RvOp.AskDonna: OnAskDonna(); break;
                    case RvOp.AcceptErrand: OnAcceptErrand(); break;
                    case RvOp.DeliverCrate: HostDeliverCrate(); break;
                    case RvOp.MingPayLoss: OnMingPayLoss(); break;
                    case RvOp.MarcoGreet: OnMarcoGreet(); break;
                    case RvOp.MarcoReferral: OnMarcoReferral(); break;
                    case RvOp.MarcoFavour: OnMarcoFavour(); break;
                    case RvOp.GotPackage: HostGotPackage(); break;
                    case RvOp.MarcoPayLoss: OnMarcoPayLoss(); break;
                    case RvOp.GiveSample: HostGiveSample(m.A); break;   // A = discount the client computed
                    case RvOp.PayRepair: HostPayRepair(false); break;  // no cinematic on the host for a client's repair
                    case RvOp.CheckedRv: HostCheckedRv(); break;       // a client reached the post-repair RV
                    case RvOp.RequestSnapshot: HostSendSnapshot(); break;
#if DEBUG
                    case RvOp.Ping: Core.Log.Msg("[Net] host <- Ping " + m.A); break;
#endif
                }
            }
            catch (Exception e) { Core.Log.Warning("[Net] server intent " + m.Op + " failed: " + e.Message); }
        }

        // ---- CLIENT: host state arrived ----
        private static void OnClientState(RvMsg m)
        {
            try
            {
                switch (m.Op)
                {
                    case RvOp.StageSync: ApplyStageSync(m.A, m.B); break;
                    case RvOp.TransientSync: ApplyTransient(m.A, m.B); break;
                    case RvOp.RepairApplied: ApplyRepair(); break;
#if DEBUG
                    case RvOp.Ping: Core.Log.Msg("[Net] client <- Ping " + m.A); break;
#endif
                }
            }
            catch (Exception e) { Core.Log.Warning("[Net] client state " + m.Op + " failed: " + e.Message); }
        }

        /// <summary>Host: replay the full current state to all clients (answer to RequestSnapshot / on change).</summary>
        private static void HostSendSnapshot()
        {
            NetworkBus.BroadcastToAll(RvOp.StageSync, Stage, DiscountTotal);
            HostBroadcastTransient();
            if (RepairStateStore.GetRepaired()) NetworkBus.BroadcastToAll(RvOp.RepairApplied);
            Core.LogDebug("[Net] host snapshot sent: Stage=" + Stage + " Discount=" + DiscountTotal + " Repaired=" + RepairStateStore.GetRepaired());
        }

        /// <summary>Client: adopt the host's authoritative stage + discount and re-render the journal/price.</summary>
        private static void ApplyStageSync(int stage, int discount)
        {
            if (NetworkBus.IsServer) return;   // host is the source of truth; never echo back onto itself
            RepairStateStore.SetStage(stage);
            RepairStateStore.SetDiscountTotal(discount);
            if (stage >= Done) { RepairQuest.CompleteIfActive(); }   // closing beat reached host-side
            else if (Active && stage >= Started) EnsureQuest();      // make sure the client has the journal
            SyncEntry();
            RefreshRepairChoice();
            Core.LogDebug("[Net] client applied StageSync: Stage=" + stage + " Discount=" + discount);
        }

        // --- transient pickup/drop state (host -> clients) -------------------------------------------------
        // The errand pickups use non-persisted transient state (_pickupActive/_hasPackage + which dead drop).
        // The host owns it and replicates it as a single compact message so a client's SyncEntry renders the exact
        // same objective text + dead-drop marker. Dead-drop items themselves already replicate (StorageEntity),
        // so the client just needs to know WHICH drop is the current target (index into DeadDropManager.All).

        /// <summary>Host: broadcast the current transient pickup state + active dead-drop index to all clients.</summary>
        private static void HostBroadcastTransient()
        {
            int flags = (_pickupActive ? 1 : 0) | (_hasPackage ? 2 : 0);
            int idx = -1;
            if (_pickupActive) idx = IndexOfDrop(_drop);
            else if (Stage == MingErrand) idx = IndexOfDrop(_crateDrop);
            NetworkBus.BroadcastToAll(RvOp.TransientSync, flags, idx);
        }

        /// <summary>Client: adopt the host's transient pickup state + resolve the active dead drop, then re-render.</summary>
        private static void ApplyTransient(int flags, int idx)
        {
            if (NetworkBus.IsServer) return;
            _pickupActive = (flags & 1) != 0;
            _hasPackage = (flags & 2) != 0;
            var drop = ResolveDrop(idx);
            Vector3 pos = drop != null ? drop.Position : Vector3.zero;
            if (_pickupActive)
            {
                _drop = drop; _pkgPlaced = drop != null; _dropPoint = pos;
            }
            else
            {
                _crateDrop = drop; _cratePlaced = drop != null; _cratePoint = pos;
            }
            SyncEntry();
            Core.LogDebug("[Net] client applied TransientSync: pickup=" + _pickupActive + " hasPkg=" + _hasPackage + " dropIdx=" + idx);
        }

        // DeadDropManager.All is assumed to be identically ordered on host + client (same scene/build => deterministic
        // registration). If that ever fails, only the client's drop MARKER is wrong - the host still drives the
        // pickup detection + stage advancement, so the quest still completes. Index, not reference: S1API may hand
        // back fresh wrapper instances per call, so we match by position to find the index.
        private static int IndexOfDrop(S1API.DeadDrops.DeadDropInstance d)
        {
            try
            {
                if (d == null) return -1;
                var all = S1API.DeadDrops.DeadDropManager.All;
                if (all == null) return -1;
                for (int i = 0; i < all.Length; i++)
                    if (all[i] != null && all[i].Position == d.Position) return i;
            }
            catch { }
            return -1;
        }

        private static S1API.DeadDrops.DeadDropInstance ResolveDrop(int idx)
        {
            try
            {
                if (idx < 0) return null;
                var all = S1API.DeadDrops.DeadDropManager.All;
                if (all == null || idx >= all.Length) return null;
                return all[idx];
            }
            catch { return null; }
        }

        /// <summary>
        /// Host-only (or offline): the closing "check on the RV" beat - advance Paid -> Done + complete the journal.
        /// Called by the host's own proximity loop AND by a client's CheckedRv intent, so either player finishing it
        /// completes the quest for everyone (Stage = Done auto-broadcasts StageSync, which completes it on clients).
        /// </summary>
        private static void HostCheckedRv()
        {
            if (Stage != Paid) return;
            Stage = Done;
            RepairQuest.CompleteIfActive();
            WorldSay(_marcoT, "There she is. Standing again. Interior's your problem. Try not to piss off whoever torched it the first time.");
            Core.Log.Msg("[Questline] quest complete (RV checked).");
        }

        /// <summary>Client: the host repaired the shared RV - swap the visual + mirror the repaired flag.</summary>
        private static void ApplyRepair()
        {
            if (NetworkBus.IsServer) return;   // the host already did the real repair
            RVManager.RepairVisualOnly();
            RepairStateStore.SetRepaired(true);
            RepairQuest.CompleteIfActive();
            Core.LogDebug("[Net] client applied RepairApplied (visual swap).");
        }

        /// <summary>
        /// Host-authoritative repair (used by BOTH quest modes). Validate funds against the shared pool, charge it,
        /// repair the RV (state + visual + persistence), and broadcast RepairApplied to all clients. In Questline
        /// mode it also advances to Paid (which auto-broadcasts StageSync); in Simple mode it just completes.
        /// <paramref name="withCinematic"/> = the host player is the one acting (play the local fade); false when
        /// it's a client's PayRepair intent (commit immediately, no black on the host's screen).
        /// </summary>
        internal static void HostPayRepair(bool withCinematic)
        {
            try
            {
                if (!RVManager.IsDestroyed()) { Core.LogDebug("[Net] host PayRepair ignored - RV not destroyed."); return; }
                int price = RVRepairVanPreferences.QuestlineEnabled ? CurrentPrice() : RVRepairVanPreferences.RepairPrice;
                if (S1API.Money.Money.GetCashBalance() < price)
                {
                    Core.Log.Msg("[Net] host PayRepair rejected - shared pool short (" + MoneyManager.FormatAmount(price) + ").");
                    return;
                }
                // Charge the shared pool synchronously (host = authoritative for money), then repair.
                S1API.Money.Money.ChangeCashBalance(-price, true, true);
                int paid = price;

                Action commit = () =>
                {
                    if (RVManager.Repair())   // field write (persists + correct for joiners) + visual + FX
                    {
                        RepairStateStore.SetRepaired(true);
                        NetworkBus.BroadcastToAll(RvOp.RepairApplied);
                        if (RVRepairVanPreferences.QuestlineEnabled) { Stage = Paid; SyncEntry(); }   // setter broadcasts StageSync
                        else RepairQuest.CompleteIfActive();
                        Core.Log.Msg("[Net] host repaired the RV for " + MoneyManager.FormatAmount(paid) + " (broadcast to clients).");
                    }
                };

                if (withCinematic)
                    RVRepairVan.Effects.RepairCinematic.Play(
                        () => commit(),
                        () => WorldSay(_marcoT, "There she is - back from the dead. Go take a look, and try not to total her again."),
                        () => GruntNpc(FindNpc(MarcoId)));
                else
                    commit();   // a client acted; do it now and let our broadcast swap their RV during their cinematic
            }
            catch (Exception e) { Core.Log.Error("[Net] host repair failed: " + e); }
        }
    }
}

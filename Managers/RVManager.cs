using System;

namespace RVRepairVan.Managers
{
    /// <summary>
    /// Locates the RV in the scene and performs the actual repair (state + visual swap).
    /// Static, backend-agnostic logic - no MonoBehaviour injection required.
    ///
    /// Scene layout (verified against decompiled v0.4.5f2):
    ///   @Properties/RV            -> root, carries the RV (Property) component
    ///     RV                      -> intact model
    ///     Destroyed RV            -> wreck model (active while destroyed)
    ///       CartelNote            -> note left on the wreck
    /// </summary>
    internal static class RVManager
    {
        private static Transform _root;        // @Properties/RV
        private static RV _rv;                  // RV component on the root
        private static Transform _model;        // child "RV"
        private static Transform _destroyed;    // child "Destroyed RV"
        private static Transform _cartelNote;   // Destroyed RV/CartelNote

        internal static bool IsReady => _root != null && _rv != null;

        /// <summary>World position of the RV (used as a quest-marker fallback). Never throws.</summary>
        internal static bool TryGetPosition(out Vector3 position)
        {
            position = Vector3.zero;
            try
            {
                if (!TryLocate())
                {
                    return false;
                }
                position = _root.position;
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static void Reset()
        {
            _root = null;
            _rv = null;
            _model = null;
            _destroyed = null;
            _cartelNote = null;
        }

        /// <summary>Find and cache the RV. Returns true once located. Never throws.</summary>
        internal static bool TryLocate()
        {
            if (IsReady)
            {
                return true;
            }

            try
            {
                GameObject props = GameObject.Find("@Properties");
                if (props == null)
                {
                    return false;
                }

                Transform root = props.transform.Find("RV");
                if (root == null)
                {
                    return false;
                }

                RV rv = root.GetComponent<RV>();
                if (rv == null)
                {
                    return false;
                }

                _root = root;
                _rv = rv;
                _model = root.Find("RV");
                _destroyed = root.Find("Destroyed RV");
                _cartelNote = _destroyed != null ? _destroyed.Find("CartelNote") : null;
                return true;
            }
            catch (Exception e)
            {
                Core.Log.Warning("[RVManager] locate failed: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// True if the RV is currently in its destroyed/wrecked state.
        /// Never throws - it is called from the dialogue choice predicate, which must
        /// stay exception-free so a fresh game (RV not yet spawned/destroyed) is unaffected.
        /// </summary>
        internal static bool IsDestroyed()
        {
            try
            {
                if (!TryLocate())
                {
                    return false;
                }

                if (_destroyed != null && _destroyed.gameObject.activeSelf)
                {
                    return true;
                }

                return _rv.IsDestroyed;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Repair the RV: clear the destroyed/exploded state and swap wreck visuals
        /// back to the intact model. Does NOT charge the player (caller handles payment).
        /// </summary>
        internal static bool Repair()
        {
            if (!TryLocate())
            {
                Core.Log.Warning("[RVManager] RV not found - cannot repair.");
                return false;
            }

            // State flags (interop exposes these private game fields as settable properties).
            try { _rv.IsDestroyed = false; }
            catch (Exception e) { Core.Log.Warning("[RVManager] set IsDestroyed failed: " + e.Message); }
            try { _rv._exploded = false; }
            catch { /* field may not exist on every build */ }

            // Visual swap: intact model on, wreck + note off.
            try
            {
                if (_model != null && !_model.gameObject.activeSelf)
                {
                    _model.gameObject.SetActive(true);
                }
                if (_destroyed != null && _destroyed.gameObject.activeSelf)
                {
                    _destroyed.gameObject.SetActive(false);
                }
                if (_cartelNote != null && _cartelNote.gameObject.activeSelf)
                {
                    _cartelNote.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                Core.Log.Warning("[RVManager] visual swap failed: " + e.Message);
            }

            // Re-enable the FX container if the explosion disabled it.
            try
            {
                if (_rv.FXContainer != null && !_rv.FXContainer.gameObject.activeSelf)
                {
                    _rv.FXContainer.gameObject.SetActive(true);
                }
            }
            catch { /* FXContainer optional */ }

            Core.Log.Msg("[RVManager] RV repaired.");
            return true;
        }

        /// <summary>
        /// Debug helper: wreck the RV so the repair flow can be tested without progressing
        /// to the story destruction event. Sets the destroyed state and swaps to the wreck visual.
        /// </summary>
        internal static bool Destroy()
        {
            if (!TryLocate())
            {
                Core.Log.Warning("[RVManager] RV not found - cannot destroy.");
                return false;
            }

            try { _rv.IsDestroyed = true; }
            catch (Exception e) { Core.Log.Warning("[RVManager] set IsDestroyed(true) failed: " + e.Message); }
            try { _rv._exploded = true; }
            catch { /* field may not exist on every build */ }

            try
            {
                if (_destroyed != null && !_destroyed.gameObject.activeSelf)
                {
                    _destroyed.gameObject.SetActive(true);
                }
                if (_model != null && _model.gameObject.activeSelf)
                {
                    _model.gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                Core.Log.Warning("[RVManager] destroy visual swap failed: " + e.Message);
            }

            Core.Log.Msg("[RVManager] RV wrecked (debug).");
            return true;
        }
    }
}

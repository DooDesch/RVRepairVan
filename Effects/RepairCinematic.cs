using System;
using System.Collections;
using System.Reflection;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppScheduleOne.Audio;
using Il2CppScheduleOne.UI;
using MelonLoader;

namespace RVRepairVan.Effects
{
    /// <summary>
    /// Short "repair" cinematic for the Marco RV repair: fade the screen to black (the game's own BlackOverlay),
    /// hold ~2.5s while a bundled repair sound plays AND the RV is swapped (hidden behind the black), fade back,
    /// then run a completion callback (Marco's line). Input is locked during the black so the player can't walk or
    /// look while blind. All game access is guarded - if a system is missing the step is simply skipped.
    /// </summary>
    internal static class RepairCinematic
    {
        private const float FadeTime = 0.6f;
        private const float HoldTime = 2.5f;
        private const float MidHoldDelay = 1.25f;  // Marco grunts here - lands in the WAV's built-in quiet gap (~1.05-1.70s)
        private const float SoundVolume = 0.2f;    // repair clanks sit UNDER Marco's voice (tune to taste)

        private static UnityEngine.AudioClip _clip;
        private static bool _clipTried;

        /// <summary>
        /// Run the cinematic. <paramref name="onMidHold"/> fires partway through the black hold (e.g. Marco
        /// grunting), <paramref name="doWhileBlack"/> at the end of the hold, <paramref name="onDone"/> after fade-back.
        /// </summary>
        internal static void Play(Action doWhileBlack, Action onDone, Action onMidHold = null)
        {
            MelonCoroutines.Start(Run(doWhileBlack, onDone, onMidHold));
        }

        /// <summary>Defensive un-black + input restore (call on scene load so an interrupted cinematic can't strand the player).</summary>
        internal static void ForceReset()
        {
            Fade(false);
            LockInput(false);
        }

        private static IEnumerator Run(Action doWhileBlack, Action onDone, Action onMidHold)
        {
            try
            {
                LockInput(true);
                Fade(true);
                yield return new UnityEngine.WaitForSeconds(FadeTime);   // fully black

                PlaySound();                                            // repair clanks start
                yield return new UnityEngine.WaitForSeconds(MidHoldDelay);
                Safe(onMidHold);                                        // Marco grunts / hurts himself mid-repair
                yield return new UnityEngine.WaitForSeconds(HoldTime - MidHoldDelay);

                Safe(doWhileBlack);                                      // swap the RV while it's hidden

                Fade(false);
                yield return new UnityEngine.WaitForSeconds(FadeTime);   // back to normal
                Safe(onDone);                                            // Marco's completion line
            }
            finally
            {
                // Never leave the player frozen or the screen stuck black, even on an exception mid-fade.
                Fade(false);
                LockInput(false);
            }
        }

        // --- screen fade (game BlackOverlay singleton, HUD overlay as fallback) ---
        private static void Fade(bool toBlack)
        {
            try
            {
                if (Singleton<BlackOverlay>.InstanceExists)
                {
                    BlackOverlay bo = Singleton<BlackOverlay>.Instance;
                    if (toBlack) bo.Open(FadeTime); else bo.Close(FadeTime);
                    return;
                }
                if (Singleton<HUD>.InstanceExists)
                    Singleton<HUD>.Instance.SetBlackOverlayVisible(toBlack, FadeTime);
            }
            catch (Exception e) { Core.Log.Warning("[Cinematic] fade failed: " + e.Message); }
        }

        // --- input lock (the overlay is purely visual and does NOT freeze the player) ---
        private static void LockInput(bool locked)
        {
            try { var pm = PlayerSingleton<PlayerMovement>.Instance; if (pm != null) pm.CanMove = !locked; } catch { }
            try { var pc = PlayerSingleton<PlayerCamera>.Instance; if (pc != null) pc.SetCanLook(!locked); } catch { }
        }

        // --- bundled repair sound ---
        private static void PlaySound()
        {
            try
            {
                UnityEngine.AudioClip clip = EnsureClip();
                if (clip == null) return;

                // Own 2D AudioSource routed through the game's MainGameMixer, so the clip RESPECTS the player's
                // in-game volume settings (PlayClipAtPoint does NOT - it plays raw/full and ignores the sliders)
                // and sits at a sane level under Marco's voice. 2D (spatialBlend 0) keeps it consistent + audible.
                var go = new UnityEngine.GameObject("RVRepairSound");
                go.transform.position = PlayerPos();
                var src = go.AddComponent<UnityEngine.AudioSource>();
                src.clip = clip;
                src.volume = SoundVolume;
                src.spatialBlend = 0f;
                try
                {
                    var am = AudioManager.Instance;
                    if (am != null && am.MainGameMixer != null) src.outputAudioMixerGroup = am.MainGameMixer;
                }
                catch { }
                src.Play();
                UnityEngine.Object.Destroy(go, clip.length + 0.3f);
            }
            catch (Exception e) { Core.Log.Warning("[Cinematic] sound failed: " + e.Message); }
        }

        private static UnityEngine.Vector3 PlayerPos()
        {
            try { var pm = PlayerSingleton<PlayerMovement>.Instance; if (pm != null) return pm.transform.position; } catch { }
            return UnityEngine.Vector3.zero;
        }

        private static UnityEngine.AudioClip EnsureClip()
        {
            if (_clipTried) return _clip;
            _clipTried = true;
            try
            {
                using (var s = Assembly.GetExecutingAssembly().GetManifestResourceStream("RVRepairVan.repair.wav"))
                {
                    if (s == null) { Core.Log.Warning("[Cinematic] repair.wav resource missing"); return null; }
                    byte[] bytes = new byte[s.Length];
                    int read = 0;
                    while (read < bytes.Length)
                    {
                        int n = s.Read(bytes, read, bytes.Length - read);
                        if (n <= 0) break;
                        read += n;
                    }
                    _clip = WavToClip(bytes);
                }
            }
            catch (Exception e) { Core.Log.Warning("[Cinematic] clip load failed: " + e.Message); }
            return _clip;
        }

        // Minimal RIFF/WAVE parser for 16-bit PCM (what we bundle). Walks the chunks for "fmt " + "data".
        private static UnityEngine.AudioClip WavToClip(byte[] wav)
        {
            if (wav == null || wav.Length < 44) return null;
            int channels = 1, sampleRate = 22050, bits = 16, dataOffset = -1, dataLen = 0;
            int i = 12; // skip "RIFF"<size>"WAVE"
            while (i + 8 <= wav.Length)
            {
                string id = System.Text.Encoding.ASCII.GetString(wav, i, 4);
                int sz = BitConverter.ToInt32(wav, i + 4);
                int body = i + 8;
                if (id == "fmt " && body + 16 <= wav.Length)
                {
                    channels = BitConverter.ToInt16(wav, body + 2);
                    sampleRate = BitConverter.ToInt32(wav, body + 4);
                    bits = BitConverter.ToInt16(wav, body + 14);
                }
                else if (id == "data") { dataOffset = body; dataLen = sz; }
                i = body + sz + (sz & 1);   // chunks are word-aligned
            }
            if (dataOffset < 0 || bits != 16 || channels < 1) { Core.Log.Warning("[Cinematic] unsupported WAV (need 16-bit PCM)"); return null; }
            if (dataOffset + dataLen > wav.Length) dataLen = wav.Length - dataOffset;

            int total = dataLen / 2;   // 16-bit samples
            float[] samples = new float[total];
            for (int s = 0; s < total; s++)
                samples[s] = BitConverter.ToInt16(wav, dataOffset + s * 2) / 32768f;

            UnityEngine.AudioClip clip = UnityEngine.AudioClip.Create("rv_repair", total / channels, channels, sampleRate, false);
            clip.SetData((Il2CppStructArray<float>)samples, 0);
            Core.Log.Msg("[Cinematic] repair clip loaded (" + (total / channels) + " frames, " + channels + "ch, " + sampleRate + "Hz).");
            return clip;
        }

        private static void Safe(Action a)
        {
            if (a == null) return;
            try { a(); } catch (Exception e) { Core.Log.Warning("[Cinematic] callback failed: " + e.Message); }
        }
    }
}

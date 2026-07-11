#if UNITY_EDITOR
using System.IO;
using DeadManZone.Data;
using UnityEditor;
using UnityEngine;

namespace DeadManZone.Presentation.Editor
{
    /// <summary>
    /// PLACEHOLDER combat audio for the Combat3D demo. The project ships no combat SFX
    /// (t:AudioClip finds only menu clicks + AI voice samples), so this generates tiny
    /// deterministic filtered-noise blips as .wav files under Combat3D/Audio and binds
    /// them into a demo-only CombatArenaAudioSetSO. Replace the .wav files (keep the
    /// file names) or swap the set asset when real SFX land. The shared 2D set
    /// (Resources/DeadManZone/CombatArenaAudioSet) is deliberately untouched.
    /// Existing .wav files are never regenerated (identical bytes anyway — fixed seed).
    /// </summary>
    public static class Combat3DPlaceholderAudioBuilder
    {
        public const string AudioFolder = "Assets/_Project/Combat3D/Audio";
        public const string AudioSetPath = AudioFolder + "/Combat3DDemoAudioSet.asset";

        private const int SampleRate = 44100;
        private const int Seed = 20260711;

        private delegate float[] Generator(System.Random rng);

        /// <summary>Generate-or-load the placeholder clips and the demo audio set.
        /// Returns the set plus the looping ambience bed (not part of the SO's schema —
        /// the bootstrap wires it onto a plain looping AudioSource).</summary>
        public static (CombatArenaAudioSetSO set, AudioClip ambienceLoop) EnsureAudioSet()
        {
            EnsureFolder(AudioFolder);

            var rifle = EnsureClip("placeholder_rifle_shot.wav", RifleShot);
            var cannon = EnsureClip("placeholder_cannon_shot.wav", CannonShot);
            var impact = EnsureClip("placeholder_bullet_impact.wav", BulletImpact);
            var explosion = EnsureClip("placeholder_explosion.wav", Explosion);
            var death = EnsureClip("placeholder_unit_death.wav", UnitDeath);
            var ambience = EnsureClip("placeholder_ambience_wind_loop.wav", AmbienceWindLoop);

            var set = AssetDatabase.LoadAssetAtPath<CombatArenaAudioSetSO>(AudioSetPath);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<CombatArenaAudioSetSO>();
                AssetDatabase.CreateAsset(set, AudioSetPath);
            }

            set.rifleShot = rifle;
            set.cannonShot = cannon;
            set.bulletImpact = impact;
            set.explosion = explosion;
            set.unitDeath = death;
            EditorUtility.SetDirty(set);
            return (set, ambience);
        }

        private static AudioClip EnsureClip(string fileName, Generator generate)
        {
            string assetPath = AudioFolder + "/" + fileName;
            string diskPath = Path.Combine(Application.dataPath, assetPath.Substring("Assets/".Length));
            if (!File.Exists(diskPath))
            {
                File.WriteAllBytes(diskPath, ToWav(generate(new System.Random(Seed))));
                AssetDatabase.ImportAsset(assetPath);
            }

            // Self-check: a silent or failed import here means a silent demo scene.
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (clip == null || clip.samples == 0)
                Debug.LogError($"[Combat3D] Placeholder clip failed to import: {assetPath}");
            return clip;
        }

        // --- Synthesis (all deterministic; peaks kept modest — grimdark, not arcade) ---

        /// <summary>0.14 s dry crack: noise through a fast-closing lowpass, sharp decay.</summary>
        private static float[] RifleShot(System.Random rng)
        {
            int n = Samples(0.14f);
            var s = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float cutoff = Mathf.Lerp(0.8f, 0.10f, Mathf.Clamp01(t / 0.06f));
                lp += cutoff * (Noise(rng) - lp);
                s[i] = lp * Mathf.Exp(-t / 0.03f);
            }

            return Normalize(s, 0.85f);
        }

        /// <summary>0.5 s mortar thump: descending sub sine + heavily lowpassed noise.</summary>
        private static float[] CannonShot(System.Random rng)
        {
            int n = Samples(0.5f);
            var s = new float[n];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float freq = Mathf.Lerp(110f, 42f, Mathf.Clamp01(t / 0.25f));
                phase += 2f * Mathf.PI * freq / SampleRate;
                lp += 0.08f * (Noise(rng) - lp);
                s[i] = (Mathf.Sin(phase) * 0.7f + lp * 0.6f) * Mathf.Exp(-t / 0.14f);
            }

            return Normalize(s, 0.9f);
        }

        /// <summary>0.09 s dirt-thud: short midband noise tick, quieter than the shot.</summary>
        private static float[] BulletImpact(System.Random rng)
        {
            int n = Samples(0.09f);
            var s = new float[n];
            float lp = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                lp += 0.25f * (Noise(rng) - lp);
                s[i] = lp * Mathf.Exp(-t / 0.018f);
            }

            return Normalize(s, 0.55f);
        }

        /// <summary>0.8 s shell burst: deep noise rumble + descending sub sine, long tail.</summary>
        private static float[] Explosion(System.Random rng)
        {
            int n = Samples(0.8f);
            var s = new float[n];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float freq = Mathf.Lerp(60f, 28f, Mathf.Clamp01(t / 0.4f));
                phase += 2f * Mathf.PI * freq / SampleRate;
                lp += 0.05f * (Noise(rng) - lp);
                s[i] = lp * Mathf.Exp(-t / 0.22f) * 1.2f
                     + Mathf.Sin(phase) * Mathf.Exp(-t / 0.3f) * 0.5f;
            }

            return Normalize(s, 0.9f);
        }

        /// <summary>0.45 s body-fall thud: low descending tone with a soft noise tail.</summary>
        private static float[] UnitDeath(System.Random rng)
        {
            int n = Samples(0.45f);
            var s = new float[n];
            float lp = 0f, phase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / SampleRate;
                float freq = Mathf.Lerp(95f, 48f, Mathf.Clamp01(t / 0.2f));
                phase += 2f * Mathf.PI * freq / SampleRate;
                lp += 0.12f * (Noise(rng) - lp);
                s[i] = Mathf.Sin(phase) * Mathf.Exp(-t / 0.1f)
                     + lp * Mathf.Exp(-t / 0.06f) * 0.4f;
            }

            return Normalize(s, 0.6f);
        }

        /// <summary>6 s seamless wind/rumble bed: double-pole lowpassed noise with
        /// loop-periodic gust swells, tail crossfaded into the head.</summary>
        private static float[] AmbienceWindLoop(System.Random rng)
        {
            int n = Samples(6f);
            int fade = Samples(0.5f);
            var raw = new float[n + fade];
            float lp = 0f, lp2 = 0f;
            for (int i = 0; i < raw.Length; i++)
            {
                lp += 0.03f * (Noise(rng) - lp);
                lp2 += 0.008f * (lp - lp2);
                raw[i] = lp2;
            }

            // Whole sine cycles over the loop length keep the swell itself loop-periodic.
            float Swell(int i) =>
                0.65f + 0.25f * Mathf.Sin(2f * Mathf.PI * 3f * i / n)
                      + 0.10f * Mathf.Sin(2f * Mathf.PI * 7f * i / n + 1.7f);

            var s = new float[n];
            for (int i = 0; i < n; i++)
                s[i] = raw[i] * Swell(i);
            for (int i = 0; i < fade; i++)
            {
                float w = (float)i / fade; // 0 → continuation of the tail, 1 → the head
                s[i] = Mathf.Lerp(raw[n + i] * Swell(i), s[i], w);
            }

            return Normalize(s, 0.5f);
        }

        // --- Plumbing ---

        private static int Samples(float seconds) => (int)(seconds * SampleRate);

        private static float Noise(System.Random rng) => (float)(rng.NextDouble() * 2.0 - 1.0);

        private static float[] Normalize(float[] samples, float peak)
        {
            float max = 0f;
            foreach (float s in samples)
                max = Mathf.Max(max, Mathf.Abs(s));
            if (max < 1e-6f)
                return samples;

            float gain = peak / max;
            for (int i = 0; i < samples.Length; i++)
                samples[i] *= gain;
            return samples;
        }

        /// <summary>Mono 16-bit PCM WAV bytes (44-byte canonical header).</summary>
        private static byte[] ToWav(float[] samples)
        {
            int byteCount = samples.Length * 2;
            using var stream = new MemoryStream(44 + byteCount);
            using var writer = new BinaryWriter(stream);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + byteCount);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);            // PCM
            writer.Write((short)1);            // mono
            writer.Write(SampleRate);
            writer.Write(SampleRate * 2);      // byte rate
            writer.Write((short)2);            // block align
            writer.Write((short)16);           // bits per sample
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(byteCount);
            foreach (float s in samples)
                writer.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
            writer.Flush();
            return stream.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            string leaf = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, leaf);
        }
    }
}
#endif

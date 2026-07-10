using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatUnit2DStripPlayerTests
    {
        [Test]
        public void SliceUnitStrip_GridSheet_SlicesAllFramesWithinTextureBounds()
        {
            // combatvisualv2 sheets: square grid, row-major from top-left (7×7 @ 49 frames).
            // Unity 6 rejects sprite rects larger than the texture, so the downscaled-GPU
            // case can only occur via the importer; slicing from texture.width/height keeps
            // both paths in bounds. This guards the in-bounds contract.
            var texture = new Texture2D(896, 896, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[896 * 896]);
            texture.Apply();

            var sheet = Sprite.Create(texture, new Rect(0f, 0f, 896f, 896f), new Vector2(0.5f, 0.05f), 256f);
            var strip = new CombatUnit2DStrip
            {
                sheet = sheet,
                frameCount = 49,
                columns = 7,
                framesPerSecond = 12f,
                loop = true
            };

            var frames = CombatUnit2DStripPlayer.SliceUnitStrip(strip);
            Assert.AreEqual(49, frames.Length);
            for (int i = 0; i < frames.Length; i++)
            {
                Assert.NotNull(frames[i], $"frame {i} should slice without exceeding texture bounds");
                Assert.Greater(frames[i].rect.width, 0f);
                Assert.Greater(frames[i].rect.height, 0f);
                Assert.LessOrEqual(frames[i].rect.width, 128f);
                Assert.LessOrEqual(frames[i].rect.height, 128f);
            }

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void SliceUnitStrip_GridSheet_CropsSharedContentAwayFromCellEdges()
        {
            var texture = new Texture2D(512, 256, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[512 * 256]);

            PaintRect(texture, 40, 60, 150, 130, Color.white);
            PaintRect(texture, 296, 60, 150, 130, Color.white);
            PaintRect(texture, 254, 80, 2, 60, Color.white);
            texture.Apply();

            var sheet = Sprite.Create(texture, new Rect(0f, 0f, 512f, 256f), new Vector2(0.5f, 0.05f), 256f);
            var strip = new CombatUnit2DStrip
            {
                sheet = sheet,
                frameCount = 2,
                columns = 2,
                framesPerSecond = 12f,
                loop = true
            };

            var frames = CombatUnit2DStripPlayer.SliceUnitStrip(strip);
            Assert.AreEqual(2, frames.Length);
            Assert.NotNull(frames[0]);
            Assert.NotNull(frames[1]);
            Assert.Less(frames[0].rect.xMax, 250f, "cell-edge bleed should be outside the shared crop");
            Assert.Greater(frames[1].rect.x, 256f, "second frame should use the same local crop inside its own cell");

            Object.DestroyImmediate(texture);
        }

        [Test]
        public void ResolveSprite_SameSetAcrossPlayers_ReusesSharedFrameSprites()
        {
            var texture = new Texture2D(256, 128, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[256 * 128]);
            PaintRect(texture, 32, 24, 64, 80, Color.white);
            PaintRect(texture, 160, 24, 64, 80, Color.white);
            texture.Apply();

            var sheet = Sprite.Create(texture, new Rect(0f, 0f, 256f, 128f), new Vector2(0.5f, 0.05f), 256f);
            var set = ScriptableObject.CreateInstance<CombatUnit2DAnimationSetSO>();
            set.idle = new CombatUnit2DStrip
            {
                sheet = sheet,
                frameCount = 2,
                columns = 2,
                framesPerSecond = 12f,
                loop = true
            };

            var first = new CombatUnit2DStripPlayer();
            var second = new CombatUnit2DStripPlayer();
            first.Bind(set);
            second.Bind(set);
            first.Play(CombatUnit2DAnimState.Idle);
            second.Play(CombatUnit2DAnimState.Idle);

            Assert.AreSame(first.ResolveSprite(null), second.ResolveSprite(null));

            Object.DestroyImmediate(set);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Play_WithTargetDuration_CompressesPlaybackToWindow()
        {
            // Shoot strips are authored long (e.g. 1.5s) but must finish inside the
            // attack presentation window so locomotion lock and animation end together.
            var (set, texture) = CreateSetWithShoot(frameCount: 2, framesPerSecond: 2f); // natural 1s
            var player = new CombatUnit2DStripPlayer();
            player.Bind(set);

            player.Play(CombatUnit2DAnimState.Shoot, restart: true, targetDurationSeconds: 0.5f);
            Assert.IsTrue(player.IsLocked, "shoot should lock locomotion while playing");

            player.Tick(0.5f);
            Assert.IsFalse(player.IsLocked, "compressed shoot should finish at the target window");
            Assert.AreEqual(CombatUnit2DAnimState.Idle, player.State, "one-shot should return to idle");

            Object.DestroyImmediate(set);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Play_WithTargetDuration_AdvancesTimeAtScaledRate()
        {
            var (set, texture) = CreateSetWithShoot(frameCount: 2, framesPerSecond: 2f); // natural 1s
            var player = new CombatUnit2DStripPlayer();
            player.Bind(set);

            player.Play(CombatUnit2DAnimState.Shoot, restart: true, targetDurationSeconds: 0.5f);
            player.Tick(0.25f); // half the window -> half the natural strip
            Assert.AreEqual(0.5f, player.CurrentTimeSeconds, 0.0001f);

            Object.DestroyImmediate(set);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Play_WithoutTargetDuration_UsesNaturalRate()
        {
            var (set, texture) = CreateSetWithShoot(frameCount: 2, framesPerSecond: 2f);
            var player = new CombatUnit2DStripPlayer();
            player.Bind(set);

            player.Play(CombatUnit2DAnimState.Shoot);
            player.Tick(0.25f);
            Assert.AreEqual(0.25f, player.CurrentTimeSeconds, 0.0001f);

            Object.DestroyImmediate(set);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void CurrentDurationSeconds_ReportsScaledDuration()
        {
            // AnimatedDeathRoutine waits on this; a scaled die strip must report the
            // compressed duration, not the authored one.
            var (set, texture) = CreateSetWithShoot(frameCount: 2, framesPerSecond: 2f); // natural 1s
            var player = new CombatUnit2DStripPlayer();
            player.Bind(set);

            player.Play(CombatUnit2DAnimState.Shoot, restart: true, targetDurationSeconds: 0.5f);
            Assert.AreEqual(0.5f, player.CurrentDurationSeconds, 0.0001f);

            Object.DestroyImmediate(set);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Play_FinishedOneShot_ResetsRateForNextState()
        {
            var (set, texture) = CreateSetWithShoot(frameCount: 2, framesPerSecond: 2f);
            var player = new CombatUnit2DStripPlayer();
            player.Bind(set);

            player.Play(CombatUnit2DAnimState.Shoot, restart: true, targetDurationSeconds: 0.25f);
            player.Tick(0.25f); // finishes, falls back to idle
            Assert.AreEqual(CombatUnit2DAnimState.Idle, player.State);

            player.Tick(0.1f);
            Assert.AreEqual(0.1f, player.CurrentTimeSeconds, 0.0001f, "idle should play at natural rate");

            Object.DestroyImmediate(set);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Play_ScaledDie_StaysLockedOnLastFrameAfterCompletion()
        {
            // The corpse must hold its final frame (still locked, still Die) so the
            // death presentation can linger before the actor is pooled.
            var (set, texture) = CreateSetWithShoot(frameCount: 2, framesPerSecond: 2f);
            set.die = set.shoot; // reuse the non-looping strip as the die strip
            var player = new CombatUnit2DStripPlayer();
            player.Bind(set);

            player.Play(CombatUnit2DAnimState.Die, restart: true, targetDurationSeconds: 0.5f);
            player.Tick(0.6f);

            Assert.IsTrue(player.IsLocked, "die should stay locked after completion");
            Assert.AreEqual(CombatUnit2DAnimState.Die, player.State, "die should not fall back to idle");

            Object.DestroyImmediate(set);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void Play_LocomotionSwap_PreservesCyclePhaseAcrossDifferentDurations()
        {
            // The sim's bursty anchor toggles Walk<->Idle many times a second. The
            // cycle must neither restart (frozen-legs bug) nor carry raw seconds
            // (idle ~4s vs walk ~1s -> unrelated pose); it carries the phase fraction.
            var (set, texture) = CreateSetWithShoot(frameCount: 2, framesPerSecond: 2f);
            set.idle = new CombatUnit2DStrip
            {
                sheet = set.idle.sheet, frameCount = 2, columns = 2,
                framesPerSecond = 0.5f, loop = true // 4s cycle
            };
            set.walk = new CombatUnit2DStrip
            {
                sheet = set.idle.sheet, frameCount = 2, columns = 2,
                framesPerSecond = 2f, loop = true // 1s cycle
            };
            var player = new CombatUnit2DStripPlayer();
            player.Bind(set);

            player.Play(CombatUnit2DAnimState.Idle);
            player.Tick(2f); // half of the 4s idle cycle
            player.Play(CombatUnit2DAnimState.Walk, restart: false);
            Assert.AreEqual(0.5f, player.CurrentTimeSeconds, 0.0001f,
                "half-phase idle should land at half-phase walk (0.5s of 1s)");

            player.Play(CombatUnit2DAnimState.Idle, restart: false);
            Assert.AreEqual(2f, player.CurrentTimeSeconds, 0.0001f,
                "swapping back should return to half-phase idle, not reset");

            Object.DestroyImmediate(set);
            Object.DestroyImmediate(texture);
        }

        private static (CombatUnit2DAnimationSetSO set, Texture2D texture) CreateSetWithShoot(
            int frameCount,
            float framesPerSecond)
        {
            var texture = new Texture2D(256, 128, TextureFormat.RGBA32, false);
            texture.SetPixels(new Color[256 * 128]);
            PaintRect(texture, 32, 24, 64, 80, Color.white);
            PaintRect(texture, 160, 24, 64, 80, Color.white);
            texture.Apply();

            var sheet = Sprite.Create(texture, new Rect(0f, 0f, 256f, 128f), new Vector2(0.5f, 0.05f), 256f);
            var set = ScriptableObject.CreateInstance<CombatUnit2DAnimationSetSO>();
            var strip = new CombatUnit2DStrip
            {
                sheet = sheet,
                frameCount = frameCount,
                columns = frameCount,
                framesPerSecond = framesPerSecond,
                loop = false
            };
            set.shoot = strip;
            set.idle = new CombatUnit2DStrip
            {
                sheet = sheet,
                frameCount = frameCount,
                columns = frameCount,
                framesPerSecond = framesPerSecond,
                loop = true
            };
            return (set, texture);
        }

        private static void PaintRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height; py++)
            {
                for (int px = x; px < x + width; px++)
                    texture.SetPixel(px, py, color);
            }
        }
    }
}

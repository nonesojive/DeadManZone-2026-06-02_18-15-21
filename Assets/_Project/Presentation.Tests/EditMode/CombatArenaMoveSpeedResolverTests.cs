using DeadManZone.Core.Board;
using DeadManZone.Core.Combat;
using DeadManZone.Data;
using DeadManZone.Presentation.Combat.Arena;
using NUnit.Framework;
using UnityEngine;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class CombatArenaMoveSpeedResolverTests
    {
        [Test]
        public void MediumTier_MatchesSimSecondsPerCell()
        {
            float speed = CombatArenaMoveSpeedResolver.ResolveWorldSpeed(
                MovementSpeedTier.Medium,
                cellWidth: 1.8f,
                presentationScale: 1f);

            float expectedSeconds = CombatMovementSpeed.NormalStepChargeCost
                / (float)CombatMovementSpeed.GetChargePerTick(MovementSpeedTier.Medium)
                / CombatPacingConfig.TicksPerSecond;

            Assert.That(expectedSeconds, Is.EqualTo(2f).Within(0.001f));
            Assert.That(speed, Is.EqualTo(1.8f / expectedSeconds).Within(0.001f));
        }

        [Test]
        public void HighTier_FasterThanLowTier()
        {
            float high = CombatArenaMoveSpeedResolver.ResolveWorldSpeed(
                MovementSpeedTier.High,
                cellWidth: 1.8f,
                presentationScale: 1f);
            float low = CombatArenaMoveSpeedResolver.ResolveWorldSpeed(
                MovementSpeedTier.Low,
                cellWidth: 1.8f,
                presentationScale: 1f);

            Assert.Greater(high, low);
        }

        [Test]
        public void ResolveFromPiece_UsesPieceMovementTier()
        {
            var config = ScriptableObject.CreateInstance<CombatArenaConfigSO>();
            config.cellWidth = 2f;
            config.moveSpeedPresentationScale = 1f;

            var piece = ScriptableObject.CreateInstance<PieceDefinitionSO>();
            piece.movementSpeed = MovementSpeedTier.High;

            try
            {
                float speed = CombatArenaMoveSpeedResolver.ResolveWorldSpeed(piece, config);
                float expected = CombatArenaMoveSpeedResolver.ResolveWorldSpeed(
                    MovementSpeedTier.High,
                    config.cellWidth,
                    config.moveSpeedPresentationScale);

                Assert.That(speed, Is.EqualTo(expected).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(piece);
                Object.DestroyImmediate(config);
            }
        }
    }
}

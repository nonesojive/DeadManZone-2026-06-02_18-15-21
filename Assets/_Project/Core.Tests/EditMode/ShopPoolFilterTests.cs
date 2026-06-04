using DeadManZone.Core.Board;
using DeadManZone.Core.Common;
using DeadManZone.Core.Shop;
using NUnit.Framework;

namespace DeadManZone.Core.Tests.EditMode
{
    public sealed class ShopPoolFilterTests
    {
        [Test]
        public void Fight1_HeavilyFavorsNeutral()
        {
            var shape = new PieceShape(new[] { new GridCoord(0, 0) });
            var pool = new[]
            {
                new PieceDefinition
                {
                    Id = "neutral_a",
                    DisplayName = "Neutral",
                    Category = PieceCategory.Unit,
                    Shape = shape,
                    FactionId = "neutral"
                },
                new PieceDefinition
                {
                    Id = "iv_a",
                    DisplayName = "IV",
                    Category = PieceCategory.Unit,
                    Shape = shape,
                    FactionId = "iron_vanguard"
                }
            };

            var rng = new Rng(123);
            int neutral = 0;
            for (int i = 0; i < 100; i++)
            {
                var piece = ShopPoolFilter.PickWeighted(pool, fightIndex: 1, rng);
                if (piece.FactionId == "neutral")
                    neutral++;
            }

            Assert.Greater(neutral, 70);
        }
    }
}

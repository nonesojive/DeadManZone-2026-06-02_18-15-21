using DeadManZone.Core.Board;

namespace DeadManZone.Core.Board
{
    public sealed class FightStartBoardContext
    {
        public BoardState CombatBoard { get; init; }
        public BoardState HqBoard { get; init; }

        public static FightStartBoardContext From(BuildBoardSet boards) =>
            new FightStartBoardContext
            {
                CombatBoard = boards?.Combat,
                HqBoard = boards?.Hq
            };
    }
}

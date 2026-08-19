using System.Collections.Generic;
using HMS_Chess.Engine.Models;
using HMS_Chess.Engine.Modes;

namespace HMS_Chess.Engine.Pieces
{
    public class Rook : Piece
    {
        public bool HasMoved { get; private set; } = false;

        public Rook(PieceColor color, Position position) : base(color, position) { }

        public void MarkAsMoved() => HasMoved = true;

        public override IEnumerable<Position> GetLegalMoves(Board board)
        {
            var moves = new List<Position>();
            int[] rowDirs = { 1, -1, 0, 0 };
            int[] colDirs = { 0, 0, 1, -1 };

            for (int i = 0; i < 4; i++)
            {
                moves.AddRange(SlidingPieceHelper.GetSlidingMoves(Position, board, Color, rowDirs[i], colDirs[i]));
            }
            return moves;
        }
    }
}

using System.Collections.Generic;
using HMS_Chess.Engine.Models;
using HMS_Chess.Engine.Modes;

namespace HMS_Chess.Engine.Pieces
{
    public class Queen : Piece
    {
        public Queen(PieceColor color, Position position) : base(color, position) { }

        public override IEnumerable<Position> GetLegalMoves(Board board)
        {
            var moves = new List<Position>();
            int[] rowDirs = { 1, -1, 0, 0, 1, 1, -1, -1 };
            int[] colDirs = { 0, 0, 1, -1, 1, -1, 1, -1 };

            for (int i = 0; i < 8; i++)
            {
                moves.AddRange(SlidingPieceHelper.GetSlidingMoves(Position, board, Color, rowDirs[i], colDirs[i]));
            }
            return moves;
        }
    }
}

using System.Collections.Generic;
using HMS_Chess.Engine.Models;
using HMS_Chess.Engine.Modes;

namespace HMS_Chess.Engine.Pieces
{
    public class Bishop : Piece
    {
        public Bishop(PieceColor color, Position position) : base(color, position) { }

        public override IEnumerable<Position> GetLegalMoves(Board board)
        {
            var moves = new List<Position>();
            int[] rowDirs = { 1, 1, -1, -1 };
            int[] colDirs = { 1, -1, 1, -1 };

            for (int i = 0; i < 4; i++)
            {
                moves.AddRange(SlidingPieceHelper.GetSlidingMoves(Position, board, Color, rowDirs[i], colDirs[i]));
            }
            return moves;
        }
    }
}

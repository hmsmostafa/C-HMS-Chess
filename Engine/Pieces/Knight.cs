using System.Collections.Generic;
using HMS_Chess.Engine.Models;
using HMS_Chess.Engine.Modes;

namespace HMS_Chess.Engine.Pieces
{
    public class Knight : Piece
    {
        public Knight(PieceColor color, Position position) : base(color, position) { }

        public override IEnumerable<Position> GetLegalMoves(Board board)
        {
            var moves = new List<Position>();
            int[] rowOffsets = { -2, -2, -1, -1, 1, 1, 2, 2 };
            int[] colOffsets = { -1, 1, -2, 2, -2, 2, -1, 1 };

            for (int i = 0; i < 8; i++)
            {
                int targetRow = Position.Row + rowOffsets[i];
                int targetCol = Position.Column + colOffsets[i];

                if (Board.IsPositionOnBoard(targetRow, targetCol))
                {
                    var targetPosition = new Position(targetRow, targetCol);
                    var targetPiece = board.GetPieceAt(targetPosition);

                    if (targetPiece == null || targetPiece.Color != this.Color)
                    {
                        moves.Add(targetPosition);
                    }
                }
            }
            return moves;
        }
    }
}

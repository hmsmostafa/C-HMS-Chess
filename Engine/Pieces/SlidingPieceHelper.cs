using System.Collections.Generic;
using HMS_Chess.Engine.Models;
using HMS_Chess.Engine.Modes;

namespace HMS_Chess.Engine.Pieces
{
    public static class SlidingPieceHelper
    {
        public static IEnumerable<Position> GetSlidingMoves(Position current, Board board, PieceColor color, int rowDirection, int colDirection)
        {
            var moves = new List<Position>();
            int stepRow = current.Row;
            int stepCol = current.Column;

            while (true)
            {
                stepRow += rowDirection;
                stepCol += colDirection;

                if (!Board.IsPositionOnBoard(stepRow, stepCol))
                    break;

                var targetPosition = new Position(stepRow, stepCol);
                var targetPiece = board.GetPieceAt(targetPosition);

                if (targetPiece == null)
                {
                    moves.Add(targetPosition);
                }
                else
                {
                    if (targetPiece.Color != color)
                    {
                        moves.Add(targetPosition);
                    }
                    break; // Path is blocked by this piece
                }
            }
            return moves;
        }
    }
}

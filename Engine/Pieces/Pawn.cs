using System;
using System.Collections.Generic;
using HMS_Chess.Engine.Models;
using HMS_Chess.Engine.Modes;

namespace HMS_Chess.Engine.Pieces
{
    public class Pawn : Piece
    {
        public Pawn(PieceColor color, Position position) : base(color, position) { }

        public override IEnumerable<Position> GetLegalMoves(Board board)
        {
            var moves = new List<Position>();
            int direction = (Color == PieceColor.White) ? 1 : -1;
            int startRow = (Color == PieceColor.White) ? 1 : 6;
            int enPassantRow = (Color == PieceColor.White) ? 4 : 3;

            // 1. Forward 1 Square
            int nextRow = Position.Row + direction;
            if (Board.IsPositionOnBoard(nextRow, Position.Column) && board.GetPieceAt(new Position(nextRow, Position.Column)) == null)
            {
                moves.Add(new Position(nextRow, Position.Column));

                // 2. Initial Forward 2 Squares
                int doubleRow = Position.Row + (2 * direction);
                if (Position.Row == startRow && Board.IsPositionOnBoard(doubleRow, Position.Column) && board.GetPieceAt(new Position(doubleRow, Position.Column)) == null)
                {
                    moves.Add(new Position(doubleRow, Position.Column));
                }
            }

            // 3. Regular Diagonal Captures
            int[] captureCols = { Position.Column - 1, Position.Column + 1 };
            foreach (int targetCol in captureCols)
            {
                if (Board.IsPositionOnBoard(nextRow, targetCol))
                {
                    var targetPos = new Position(nextRow, targetCol);
                    var targetPiece = board.GetPieceAt(targetPos);
                    if (targetPiece != null && targetPiece.Color != this.Color)
                    {
                        moves.Add(targetPos);
                    }

                    // 4. FIDE En Passant Implementation
                    if (Position.Row == enPassantRow && board.EnPassantVulnerableColumn == targetCol)
                    {
                        moves.Add(targetPos);
                    }
                }
            }
            return moves;
        }
    }
}

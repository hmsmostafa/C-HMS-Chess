using System;
using System.Collections.Generic;
using HMS_Chess.Engine.Models;
using HMS_Chess.Engine.Modes;

namespace HMS_Chess.Engine.Pieces
{
    public class King : Piece
    {
        public bool HasMoved { get; private set; } = false;

        public King(PieceColor color, Position position) : base(color, position) { }

        public void MarkAsMoved() => HasMoved = true;

        public override IEnumerable<Position> GetLegalMoves(Board board)
        {
            var moves = new List<Position>();
            int[] rowOffsets = { -1, -1, -1, 0, 0, 1, 1, 1 };
            int[] colOffsets = { -1, 0, 1, -1, 1, -1, 0, 1 };

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
            AddCastlingMoves(board, moves);
            return moves;
        }

        private void AddCastlingMoves(Board board, List<Position> moves)
        {
            if (HasMoved) return;
            int row = Position.Row;

            var kingsideRook = board.GetPieceAt(new Position(row, 7)) as Rook;
            if (kingsideRook != null && !kingsideRook.HasMoved)
            {
                if (board.GetPieceAt(new Position(row, 5)) == null && board.GetPieceAt(new Position(row, 6)) == null)
                {
                    moves.Add(new Position(row, 6));
                }
            }

            var queensideRook = board.GetPieceAt(new Position(row, 0)) as Rook;
            if (queensideRook != null && !queensideRook.HasMoved)
            {
                if (board.GetPieceAt(new Position(row, 1)) == null && board.GetPieceAt(new Position(row, 2)) == null && board.GetPieceAt(new Position(row, 3)) == null)
                {
                    moves.Add(new Position(row, 2));
                }
            }
        }
    }
}

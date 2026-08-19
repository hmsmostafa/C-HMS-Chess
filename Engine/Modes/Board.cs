using System;
using System.Collections.Generic;
using HMS_Chess.Engine.Models;
using HMS_Chess.Engine.Pieces;

namespace HMS_Chess.Engine.Modes
{
    public class Board
    {
        private readonly Piece?[,] grid = new Piece?[8, 8];

        // Track the current active En Passant vulnerable column (-1 if none)
        public int EnPassantVulnerableColumn { get; set; } = -1;

        public Board() => InitializeBoard();

        private void InitializeBoard()
        {
            // White Back Row
            grid[0, 0] = new Rook(PieceColor.White, new Position(0, 0));
            grid[0, 1] = new Knight(PieceColor.White, new Position(0, 1));
            grid[0, 2] = new Bishop(PieceColor.White, new Position(0, 2));
            grid[0, 3] = new Queen(PieceColor.White, new Position(0, 3));
            grid[0, 4] = new King(PieceColor.White, new Position(0, 4));
            grid[0, 5] = new Bishop(PieceColor.White, new Position(0, 5));
            grid[0, 6] = new Knight(PieceColor.White, new Position(0, 6));
            grid[0, 7] = new Rook(PieceColor.White, new Position(0, 7));
            for (int col = 0; col < 8; col++) grid[1, col] = new Pawn(PieceColor.White, new Position(1, col));

            // Black Back Row
            grid[7, 0] = new Rook(PieceColor.Black, new Position(7, 0));
            grid[7, 1] = new Knight(PieceColor.Black, new Position(7, 1));
            grid[7, 2] = new Bishop(PieceColor.Black, new Position(7, 2));
            grid[7, 3] = new Queen(PieceColor.Black, new Position(7, 3));
            grid[7, 4] = new King(PieceColor.Black, new Position(7, 4));
            grid[7, 5] = new Bishop(PieceColor.Black, new Position(7, 5));
            grid[7, 6] = new Knight(PieceColor.Black, new Position(7, 6));
            grid[7, 7] = new Rook(PieceColor.Black, new Position(7, 7));
            for (int col = 0; col < 8; col++) grid[6, col] = new Pawn(PieceColor.Black, new Position(6, col));
        }

        public static bool IsPositionOnBoard(int row, int col) => row >= 0 && row < 8 && col >= 0 && col < 8;

        public Piece? GetPieceAt(Position position)
        {
            if (!IsPositionOnBoard(position.Row, position.Column)) return null;
            return grid[position.Row, position.Column];
        }

        public void SetPieceAt(Position position, Piece? piece)
        {
            if (IsPositionOnBoard(position.Row, position.Column))
            {
                grid[position.Row, position.Column] = piece;
                if (piece != null) piece.Position = position;
            }
        }

        // Returns a full shallow clone list of all active pieces remaining on the layout board
        public List<Piece> GetAllPieces()
        {
            var pieces = new List<Piece>();
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Piece? p = grid[r, c];
                    if (p != null) pieces.Add(p);
                }
            }
            return pieces;
        }

        public void ExecuteMove(Position from, Position to)
        {
            Piece? piece = GetPieceAt(from);
            if (piece == null) return;

            int prevEPColumn = EnPassantVulnerableColumn;
            EnPassantVulnerableColumn = -1; // Reset by default every move

            // FIDE Castle Execution
            if (piece is King king)
            {
                int colDiff = to.Column - from.Column;
                if (Math.Abs(colDiff) == 2)
                {
                    int rookFromCol = (colDiff > 0) ? 7 : 0;
                    int rookToCol = (colDiff > 0) ? 5 : 3;
                    Position rookFrom = new Position(from.Row, rookFromCol);
                    Position rookTo = new Position(from.Row, rookToCol);

                    Piece? rook = GetPieceAt(rookFrom);
                    SetPieceAt(rookTo, rook);
                    SetPieceAt(rookFrom, null);
                    if (rook is Rook r) r.MarkAsMoved();
                }
                king.MarkAsMoved();
            }

            // FIDE En Passant Capture Clear Logic
            if (piece is Pawn)
            {
                // If a pawn moves diagonally to an empty square, it must be an En Passant capture
                if (from.Column != to.Column && GetPieceAt(to) == null)
                {
                    SetPieceAt(new Position(from.Row, to.Column), null); // Clear enemy pawn
                }

                // Set En Passant vulnerability if pawn moves 2 squares forward
                if (Math.Abs(to.Row - from.Row) == 2)
                {
                    EnPassantVulnerableColumn = from.Column;
                }
            }

            if (piece is Rook rookPiece) rookPiece.MarkAsMoved();

            SetPieceAt(to, piece);
            SetPieceAt(from, null);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using HMS_Chess.Engine.Models;
using HMS_Chess.Engine.Pieces;

namespace HMS_Chess.Engine.Modes
{
    public class GameEngine
    {
        public static bool IsKingInCheck(Board board, PieceColor color)
        {
            Position? kingPos = FindKingPosition(board, color);
            if (kingPos == null) return false;

            // Scan all opponent pieces to see if any can strike the King's square
            var opponentPieces = board.GetAllPieces().Where(p => p.Color != color);
            foreach (var piece in opponentPieces)
            {
                if (piece.GetLegalMoves(board).Any(m => m.Equals(kingPos)))
                {
                    return true;
                }
            }
            return false;
        }

        public static List<Position> GetAbsoluteLegalMoves(Board board, Piece piece)
        {
            var rawMoves = piece.GetLegalMoves(board);
            var legalMoves = new List<Position>();

            foreach (var move in rawMoves)
            {
                // Simulate the move on a structural lookup scratchpad
                Position originalPos = piece.Position;
                Piece? targetPiece = board.GetPieceAt(move);

                board.SetPieceAt(move, piece);
                board.SetPieceAt(originalPos, null);

                // If our own King remains safe, the move is absolutely legal
                if (!IsKingInCheck(board, piece.Color))
                {
                    legalMoves.Add(move);
                }

                // Revert simulation frames instantly
                board.SetPieceAt(originalPos, piece);
                board.SetPieceAt(move, targetPiece);
            }
            return legalMoves;
        }

        public static bool IsCheckmate(Board board, PieceColor color)
        {
            if (!IsKingInCheck(board, color)) return false;
            return !HasAnyLegalMoves(board, color);
        }

        public static bool IsStalemate(Board board, PieceColor color)
        {
            if (IsKingInCheck(board, color)) return false;
            return !HasAnyLegalMoves(board, color);
        }

        private static bool HasAnyLegalMoves(Board board, PieceColor color)
        {
            var alliedPieces = board.GetAllPieces().Where(p => p.Color == color);
            foreach (var piece in alliedPieces)
            {
                if (GetAbsoluteLegalMoves(board, piece).Any()) return true;
            }
            return false;
        }

        public static Position? FindKingPosition(Board board, PieceColor color)
        {
            return board.GetAllPieces().FirstOrDefault(p => p is King && p.Color == color)?.Position;
        }
    }
}

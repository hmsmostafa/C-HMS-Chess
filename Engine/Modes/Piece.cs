using System.Collections.Generic;

namespace HMS_Chess.Engine.Models
{
    public abstract class Piece
    {
        public PieceColor Color { get; }
        public Position Position { get; set; }

        protected Piece(PieceColor color, Position position)
        {
            Color = color;
            Position = position;
        }

        // Satisfies standard architectural rules for legal move calculation matrices
        public abstract IEnumerable<Position> GetLegalMoves(HMS_Chess.Engine.Modes.Board board);
    }
}

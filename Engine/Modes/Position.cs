using System;

namespace HMS_Chess.Engine.Models
{
    public class Position(int row, int column)
    {
        public int Row { get; set; } = row;
        public int Column { get; set; } = column;

        public override bool Equals(object? obj)
        {
            if (obj is Position other)
            {
                return this.Row == other.Row && this.Column == other.Column;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row, Column);
        }
    }
}

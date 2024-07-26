using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Turbulence.BitManipulation;
using static Turbulence.GenerateMove;

namespace Turbulence
{
    public static class MoveMethod
    {
        public struct Move
        {
            public int From;
            public int To;
            public int Type;
            public int Piece;

            public Move(int from, int to, int type, int piece)
            {
                From = from; To = to; Type = type; Piece = piece;
            }

            public bool Equals(Move other)
            {
                if(other.From == From)
                {
                    if(other.To == To)
                    {
                        if(other.Type == Type)
                        {
                            if (other.Piece == Piece)
                            {
                                return true;
                            }

                        }
                    }
                }

                return false;
            }
        }
        public static void PrintLegalMoves(List<Move> moveList)
        {
            int num = 0;
            foreach (Move move in moveList)
            {
                Console.Write(num + CoordinatesToChessNotation(move.From) + CoordinatesToChessNotation(move.To));
                if (move.Type == queen_promo || move.Type == queen_promo_capture) Console.Write("q");
                if (move.Type == rook_promo || move.Type == rook_promo_capture) Console.Write("r");
                if (move.Type == bishop_promo || move.Type == bishop_promo_capture) Console.Write("b");
                if (move.Type == knight_promo || move.Type == knight_promo_capture) Console.Write("n");

                Console.Write(": 1 \n ");

                num++;
            }
        }

        public static string CoordinatesToChessNotation(int square)
        {
            int rawFile = square % 8;
            int rawRank = square == 0 ? 8 : 8 - square / 8;
            char File = (char)('a' + rawFile); // Convert column index to letter ('a' to 'h')
            int row = rawRank; // Row number (1 to 8)

            // Validate row
            if (row < 0 || row > 8)
            {
                throw new ArgumentException("Invalid chess square.");
            }

            return File.ToString() + row;
        }



        public static void printMove(Move move)
        {
            Console.Write(CoordinatesToChessNotation(move.From) + CoordinatesToChessNotation(move.To));
            if (move.Type == queen_promo || move.Type == queen_promo_capture) Console.Write("q");
            if (move.Type == rook_promo || move.Type == rook_promo_capture) Console.Write("r");
            if (move.Type == bishop_promo || move.Type == bishop_promo_capture) Console.Write("b");
            if (move.Type == knight_promo || move.Type == knight_promo_capture) Console.Write("n");
        }
    }
}

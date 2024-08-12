using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Turbulence.GenerateMove;
using static Turbulence.MoveMethod;
using static Turbulence.BitManipulation;

namespace Turbulence
{
    public static class BoardMethod
    {
        public const string empty_board = "8/8/8/8/8/8/8/8 w - - ";
        public const string start_position = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 ";
        public const string tricky_position = "r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1 ";
        public const string killer_position = "rnbqkblr/pplp1pPp/8/2p1pP2/1P1P4/3P3P/P1P1P3/RNBQKBNR w KQkq e6 0 1";
        public const string cmk_position = "r2q1rk1/ppp2ppp/2n1bn2/2b1p3/3pP3/3P1NPP/PPP1NPB1/R1BQ1RK1 b - - 0 9 ";
        public const string pawn_test = "8/pppppppp/8/8/8/8/PPPPPPPP/8 w - - 0 1"; //illegal position
        public static Dictionary<int, char> ascii_pieces = new()
        {
            { Piece.P, 'P'  },
            {Piece.N,  'N' },
            {  Piece.B , 'B'},
            { Piece.R , 'R' },
            {Piece.Q ,'Q' },
            { Piece.K, 'K' },
            {Piece.p,  'p' },
            { Piece.n,  'n' },
            { Piece.b, 'b'},
            { Piece.r, 'r' },
            { Piece.q , 'q' },
            {Piece.k , 'k'},


        };
        static Dictionary<char, int> char_pieces = new()
        {
            { 'P', Piece.P },
            { 'N', Piece.N },
            { 'B', Piece.B },
            { 'R', Piece.R },
            { 'Q', Piece.Q },
            { 'K', Piece.K },
            { 'p', Piece.p },
            { 'n', Piece.n },
            { 'b', Piece.b },
            { 'r', Piece.r },
            { 'q', Piece.q },
            { 'k', Piece.k },


        };
        public class Board
        {
            public ulong[] bitboards = new ulong[12];
            public ulong[] occupancies = new ulong[3];
            public int[] mailbox = new int[64];
            public int side;
            public int enpassent = (int)Square.no_sq;
            public ulong castle;
            public int halfmove;
        }
        public class Side
        {
            public const int White = 0;
            public const int Black = 1;
            public const int Both = 2;

        }
        public static void print_mailbox(int[] mailbox)
        {
            Console.Write("\n MAILBOX \n");
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    if (file == 0)
                    {
                        Console.Write(" " + (8 - rank) + " ");
                    }
                    //int piece = 0;
                    if (mailbox[square] != -1) //
                    {

                        Console.Write(" " + ascii_pieces[mailbox[square]]);
                    }
                    else
                    {
                        Console.Write(" .");
                    }
                }
                Console.Write("\n");
            }

        }
        public static void PrintBoards(Board board)
        {
            Console.Write("\n");
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    if (file == 0)
                    {
                        Console.Write(" " + (8 - rank) + " ");
                    }

                    int piece = -1;

                    for (int bb_piece = (int)Piece.P; bb_piece <= (int)Piece.k; bb_piece++)
                    {
                        if (Get_bit(board.bitboards[bb_piece], square))
                        {
                            piece = bb_piece;
                        }
                    }

                    Console.Write(" " + ((piece == -1) ? '.' : ascii_pieces[piece]));
                }
                Console.Write("\n");
            }

            Console.Write("\n    a b c d e f g h");
            Console.Write("\n    Side :     " + (board.side == 0 ? "w" : "b"));
            Console.Write("\n    Enpassent :     " + (board.enpassent != (int)Square.no_sq ? CoordinatesToChessNotation(board.enpassent) : "no"));
            Console.Write("\n    Castling :     " + ((((ulong)board.castle & WhiteKingCastle) != 0) ? 'K' : '-') + ((((ulong)board.castle & WhiteQueenCastle) != 0) ? 'Q' : '-') + ((((ulong)board.castle & BlackKingCastle) != 0) ? 'k' : '-') + ((((ulong)board.castle & BlackQueenCastle) != 0) ? 'q' : '-'));
            Console.Write("\n");
        }

        public static void PrintBitboard(ulong bitboard)
        {
            Console.Write("\n");
            for (int rank = 0; rank < 8; rank++)
            {
                for (int file = 0; file < 8; file++)
                {
                    int square = rank * 8 + file;
                    Console.Write((bitboard & (1UL << square)) != 0 ? "1 " : "0 ");
                }
                Console.WriteLine();
            }
            Console.WriteLine();
        }

        public static void parse_fen(string fen, ref Board board)
        {
            //start_position
            //r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1 "
            //tricky_position
            //rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1 
            for (int i = 0; i < 64; i++)
            {
                board.mailbox[i] = -1;
            }
            //board.mailbox
            for (int i = 0; i < board.bitboards.Length; i++)
            {
                board.bitboards[i] = 0;
            }
            for (int i = 0; i < board.occupancies.Length; i++)
            {
                board.occupancies[i] = 0;
            }
            board.side = 0;
            board.enpassent = (int)Square.no_sq;
            //int index = 0;
            // Console.WriteLine(fen);
            int square = 0;
            int index = 0;
            for (int i = 0; i < fen.Length; i++)
            {
                char text = fen[i];
                //int file = square % 8;
                //int rank = square == 0 ? 0 : 8 - square / 8;
                if (text == ' ')
                {
                    index = i + 1;
                    break;
                }
                if (text == '/')
                {
                    //i++;
                    continue;

                }
                if (text >= '0' && text <= '9')
                {
                    //Console.WriteLine(square);
                    square += text - '0';
                    //Console.WriteLine(square);
                }

                //Console.WriteLine(i);
                if (text >= 'a' && text <= 'z' || text >= 'A' && text <= 'Z')
                {
                    int piece = char_pieces[text];
                    board.mailbox[square] = piece;
                    Set_bit(ref board.bitboards[piece], square);
                    square++;
                    //Console.WriteLine(piece);
                }

                //if (square >= 64) Console.WriteLine("bug");

            }
            if (fen[index] == 'w')
                board.side = (int)Side.White;
            else
                board.side = (int)Side.Black;

            index += 2; 

            board.castle = 0;
            for (int i = 0; i < 4; i++)
            {
               
                if (fen[index] == 'K') board.castle |= WhiteKingCastle;
                if (fen[index] == 'Q') board.castle |= WhiteQueenCastle;
                if (fen[index] == 'k') board.castle |= BlackKingCastle;
                if (fen[index] == 'q') board.castle |= BlackQueenCastle;
                if (fen[index] == ' ') break;
                if (fen[index] == '-')
                {
                    board.castle = 0;
                    break;
                }


                
                index++;
            }
            //PrintBoards(board);
            index++;
            if (fen[index] == ' ') index++;
            if (fen[index] != '-')
            {
                //Console.WriteLine(fen[index]);
                int file = fen[index] - 'a';
                int rank = 8 - (fen[index + 1] - '0');

                board.enpassent = rank * 8 + file;

            }
            else
            {
                //Console.WriteLine(fen[index]);
                board.enpassent = (int)Square.no_sq;
            }
            for (int piece = (int)Piece.P; piece <= (int)Piece.K; piece++)
            {
                board.occupancies[(int)Side.White] |= board.bitboards[piece];
            }
            for (int piece = (int)Piece.p; piece <= (int)Piece.k; piece++)
            {
                board.occupancies[(int)Side.Black] |= board.bitboards[piece];
            }
            board.occupancies[(int)Side.Both] |= board.occupancies[(int)Side.Black];
            board.occupancies[(int)Side.Both] |= board.occupancies[(int)Side.White];
            
        
        }
    }


}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Turbulence.BitManipulation;
using static Turbulence.GenerateMove;
using static Turbulence.BoardMethod;
using static Turbulence.MoveMethod;
using static Turbulence.Search;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace Turbulence
{

    static class GenerateMove
    {
        //public struct Movelist
        //{
        //    Move[] list;
        //    int length;

        //    public Movelist()
        //    {
        //        length = 0;
        //        list = new Move[218];
        //    }
        //}
        
        const int promotionFlag = 0x1000;
        const int captureFlag = 0x0100;
        const int special1Flag = 0x0010;
        const int special0Flag = 0x0001;

        public const int quiet_move = 0;
        public const int double_pawn_push = special0Flag;
        public const int king_castle = special1Flag;
        public const int queen_castle = special0Flag | special1Flag;
        public const int capture = captureFlag;
        public const int ep_capture = captureFlag | special0Flag;
        public const int knight_promo = promotionFlag;
        public const int bishop_promo = promotionFlag | special0Flag;
        public const int rook_promo = promotionFlag | special1Flag;
        public const int queen_promo = promotionFlag | special1Flag | special0Flag;
        public const int knight_promo_capture = knight_promo | capture;
        public const int bishop_promo_capture = bishop_promo | capture;
        public const int rook_promo_capture = rook_promo | capture;
        public const int queen_promo_capture = queen_promo | capture;


        public static int[] Side_value = { 0, 6 };
        public static int[] Get_Whitepiece = { 0, 1, 2, 3, 4, 5, 0, 1, 2, 3, 4, 5 };

        //ulong[] bitboards = new ulong[12];
        //ulong[] occupancies = new ulong[3];
        //int side;
        //int enpassent = (int)Square.no_sq;
        //ulong castle;

                //uint state = 1804289383;
        const ulong NotAFile = 18374403900871474942;
        const ulong NotHFile = 9187201950435737471;
        const ulong NotHGFile = 4557430888798830399;
        const ulong NotABFile = 18229723555195321596;

        static ulong[,] pawn_attacks = new ulong[2, 64];
        static ulong[] Knight_attacks = new ulong[64];
        static ulong[] King_attacks = new ulong[64];

        static ulong[] bishop_masks = new ulong[64];
        static ulong[] rook_masks = new ulong[64];

        static ulong[,] bishop_attacks = new ulong[64, 512];
        static ulong[,] rook_attacks = new ulong[64, 4096];

        static ulong[,] betweenTable = new ulong[64, 64];

        public const ulong WhiteKingCastle = 0x0001;
        public const ulong WhiteQueenCastle = 0x0010;
        public const ulong BlackKingCastle = 0x0100;
        public const ulong BlackQueenCastle = 0x1000;


        


        public static class Square
        {
            public const int a8 = 0, b8 = 1, c8 = 2, d8 = 3, e8 = 4, f8 = 5, g8 = 6, h8 = 7;
            public const int a7 = 8, b7 = 9, c7 = 10, d7 = 11, e7 = 12, f7 = 13, g7 = 14, h7 = 15;
            public const int a6 = 16, b6 = 17, c6 = 18, d6 = 19, e6 = 20, f6 = 21, g6 = 22, h6 = 23;
            public const int a5 = 24, b5 = 25, c5 = 26, d5 = 27, e5 = 28, f5 = 29, g5 = 30, h5 = 31;
            public const int a4 = 32, b4 = 33, c4 = 34, d4 = 35, e4 = 36, f4 = 37, g4 = 38, h4 = 39;
            public const int a3 = 40, b3 = 41, c3 = 42, d3 = 43, e3 = 44, f3 = 45, g3 = 46, h3 = 47;
            public const int a2 = 48, b2 = 49, c2 = 50, d2 = 51, e2 = 52, f2 = 53, g2 = 54, h2 = 55;
            public const int a1 = 56, b1 = 57, c1 = 58, d1 = 59, e1 = 60, f1 = 61, g1 = 62, h1 = 63;
            public const int no_sq = 64;

            public static int GetSquare(string squareName)
            {
                if (squareName.Length != 2)
                    throw new ArgumentException("Invalid square name format. Must be in the format 'file rank', e.g., 'e2'.");

                char file = squareName[0];
                char rank = squareName[1];

                int fileIndex = -1;
                switch (file)
                {
                    case 'a': fileIndex = 0; break;
                    case 'b': fileIndex = 1; break;
                    case 'c': fileIndex = 2; break;
                    case 'd': fileIndex = 3; break;
                    case 'e': fileIndex = 4; break;
                    case 'f': fileIndex = 5; break;
                    case 'g': fileIndex = 6; break;
                    case 'h': fileIndex = 7; break;
                    default:
                        throw new ArgumentException("Invalid file character. Must be one of 'a' to 'h'.");
                }

                int rankIndex = -1;
                switch (rank)
                {
                    case '1': rankIndex = 7; break;
                    case '2': rankIndex = 6; break;
                    case '3': rankIndex = 5; break;
                    case '4': rankIndex = 4; break;
                    case '5': rankIndex = 3; break;
                    case '6': rankIndex = 2; break;
                    case '7': rankIndex = 1; break;
                    case '8': rankIndex = 0; break;
                    default:
                        throw new ArgumentException("Invalid rank character. Must be one of '1' to '8'.");
                }

                return rankIndex * 8 + fileIndex;
            }
        }

        static int[] bishop_relevant_bits =
{
        6, 5, 5, 5, 5, 5, 5, 6,
        5, 5, 5, 5, 5, 5, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 9, 9, 7, 5, 5,
        5, 5, 7, 7, 7, 7, 5, 5,
        5, 5, 5, 5, 5, 5, 5, 5,
        6, 5, 5, 5, 5, 5, 5, 6
        };

        static int[] rook_relevant_bits =
        {
        12, 11, 11, 11, 11, 11, 11, 12,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        11, 10, 10, 10, 10, 10, 10, 11,
        12, 11, 11, 11, 11, 11, 11, 12,
        };
        static ulong[] rook_magic_numbers = new ulong[64] {0x8A80104000800020UL,
0x140002000100040UL,
0x2801880A0017001UL,
0x100081001000420UL,
0x200020010080420UL,
0x3001C0002010008UL,
0x8480008002000100UL,
0x2080088004402900UL,
0x800098204000UL,
0x2024401000200040UL,
0x100802000801000UL,
0x120800800801000UL,
0x208808088000400UL,
0x2802200800400UL,
0x2200800100020080UL,
0x801000060821100UL,
0x80044006422000UL,
0x100808020004000UL,
0x12108A0010204200UL,
0x140848010000802UL,
0x481828014002800UL,
0x8094004002004100UL,
0x4010040010010802UL,
0x20008806104UL,
0x100400080208000UL,
0x2040002120081000UL,
0x21200680100081UL,
0x20100080080080UL,
0x2000A00200410UL,
0x20080800400UL,
0x80088400100102UL,
0x80004600042881UL,
0x4040008040800020UL,
0x440003000200801UL,
0x4200011004500UL,
0x188020010100100UL,
0x14800401802800UL,
0x2080040080800200UL,
0x124080204001001UL,
0x200046502000484UL,
0x480400080088020UL,
0x1000422010034000UL,
0x30200100110040UL,
0x100021010009UL,
0x2002080100110004UL,
0x202008004008002UL,
0x20020004010100UL,
0x2048440040820001UL,
0x101002200408200UL,
0x40802000401080UL,
0x4008142004410100UL,
0x2060820C0120200UL,
0x1001004080100UL,
0x20C020080040080UL,
0x2935610830022400UL,
0x44440041009200UL,
0x280001040802101UL,
0x2100190040002085UL,
0x80C0084100102001UL,
0x4024081001000421UL,
0x20030A0244872UL,
0x12001008414402UL,
0x2006104900A0804UL,
0x1004081002402UL};
        static ulong[] bishop_magic_numbers = new ulong[64]
       {
            0x40040844404084UL,
0x2004208A004208UL,
0x10190041080202UL,
0x108060845042010UL,
0x581104180800210UL,
0x2112080446200010UL,
0x1080820820060210UL,
0x3C0808410220200UL,
0x4050404440404UL,
0x21001420088UL,
0x24D0080801082102UL,
0x1020A0A020400UL,
0x40308200402UL,
0x4011002100800UL,
0x401484104104005UL,
0x801010402020200UL,
0x400210C3880100UL,
0x404022024108200UL,
0x810018200204102UL,
0x4002801A02003UL,
0x85040820080400UL,
0x810102C808880400UL,
0xE900410884800UL,
0x8002020480840102UL,
0x220200865090201UL,
0x2010100A02021202UL,
0x152048408022401UL,
0x20080002081110UL,
0x4001001021004000UL,
0x800040400A011002UL,
0xE4004081011002UL,
0x1C004001012080UL,
0x8004200962A00220UL,
0x8422100208500202UL,
0x2000402200300C08UL,
0x8646020080080080UL,
0x80020A0200100808UL,
0x2010004880111000UL,
0x623000A080011400UL,
0x42008C0340209202UL,
0x209188240001000UL,
0x400408A884001800UL,
0x110400A6080400UL,
0x1840060A44020800UL,
0x90080104000041UL,
0x201011000808101UL,
0x1A2208080504F080UL,
0x8012020600211212UL,
0x500861011240000UL,
0x180806108200800UL,
0x4000020E01040044UL,
0x300000261044000AUL,
0x802241102020002UL,
0x20906061210001UL,
0x5A84841004010310UL,
0x4010801011C04UL,
0xA010109502200UL,
0x04A02012000UL,
0x500201010098B028UL,
0x8040002811040900UL,
0x28000010020204UL,
0x6000020202D0240UL,
0x8918844842082200UL,
0x4010011029020020UL
       };

        public static Dictionary<int, string> MoveType = new()
        {
            {quiet_move, "quiet_move" },
            {double_pawn_push, "double_pawn_push" },
            {king_castle, "king_castle" },
            {queen_castle, "queen_castle" },
            {capture, "capture" },
            {ep_capture, "ep_capture" },
            {knight_promo, "knight_promo" },
            {bishop_promo, "bishop_promo" },
            {rook_promo, "rook_promo" },
            {queen_promo, "queen_promo" },
            {knight_promo_capture, "knight_promo_capture" },
            {bishop_promo_capture, "bishop_promo_capture" },
            {rook_promo_capture, "rook_promo_capture" },
            {queen_promo_capture, "queen_promo_capture" },


        };




        

        public class Piece // 0~5 white 6~11 black
        {
            public const int P = 0;
            public const int N = 1;
            public const int B = 2;
            public const int R = 3;
            public const int Q = 4;
            public const int K = 5;
            public const int p = 6;
            public const int n = 7;
            public const int b = 8;
            public const int r = 9;
            public const int q = 10;
            public const int k = 11;

        }
        





        public static void InitializeBetweenTable()
        {
            for (int a = 0; a < 64; a++)
            {
                for (int b = 0; b < 64; b++)
                {
                    betweenTable[a, b] = Calcbetween(a, b);
                }
            }
        }

        static ulong Calcbetween(int a, int b)
        {
            ulong between = 0;


            int xDiff = getFile(a) - getFile(b);
            int yDiff = getRank(a) - getRank(b);


            int totalSteps;
            if (xDiff == 0)
            {
                totalSteps = Math.Abs(yDiff);
            }
            else
            {
                totalSteps = Math.Abs(xDiff);
            }

            if (totalSteps == 0) return 0;

            float testx = -xDiff / (float)totalSteps;
            float testy = yDiff / (float)totalSteps;

            //Console.WriteLine(xStep + " ," + yStep);
            if (testx > 1 || testx < -1 || testy > 1 || testy < -1) return 0;
            if (testx == 0 && testy == 0) return 0;
            if ((testx % 1 != 0) || (testy % 1 != 0)) return 0;


            int xStep = (int)testx;
            int yStep = (int)testy;
            int pos = a;
            int howmuch = 0;
            //Console.WriteLine(pos);
            //Set_bit(ref between, pos);
            while (pos != b)
            {
                //CoordinatesToChessNotation
                pos += xStep;
                pos += yStep * 8;
                Set_bit(ref between, pos);

                //if (howmuch > 10) Console.WriteLine(CoordinatesToChessNotation(a) + "," + CoordinatesToChessNotation(b) + " " + xStep + "," + yStep + " " + totalSteps);
                howmuch++;
                //Console.WriteLine(pos);

            }
            between &= ~(1UL << b);
            return between;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong between(int a, int b)
        {
            return betweenTable[a, b];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int getSide(int piece)
        {
            return (piece > 5) ? Side.Black : Side.White;
            //Piece
        }

        public static void MakeMove(ref Board board, Move move, ref ulong Zobrist)
        {
            //Console.WriteLine(board.side);

            ulong lastCastle = board.castle;
            int lastEp = board.enpassent;
            board.enpassent = (int)Square.no_sq;
            int side = board.side;
            // change castling flag
            if (get_piece(move.Piece, Side.White) == Piece.K) //if king moved
            {
                if (side == Side.White)
                {
                    board.castle &= ~WhiteKingCastle;
                    board.castle &= ~WhiteQueenCastle;

                    //PIECES

                    //Zobrist ^= W_CASTLING_RIGHTS[get_castle(WhiteKingCastle | WhiteQueenCastle, side)];

                }
                else
                {
                    board.castle &= ~BlackKingCastle;
                    board.castle &= ~BlackQueenCastle;

                    //Zobrist ^= B_CASTLING_RIGHTS[get_castle(BlackKingCastle | BlackQueenCastle, side)];

                }
            }
            if (get_piece(move.Piece, Side.White) == Piece.R) //if rook moved
            {
                if (side == Side.White)
                {
                    if ((board.castle & WhiteQueenCastle) != 0 && move.From == (int)Square.a1) // no q castle
                    {
                        board.castle &= ~WhiteQueenCastle;
                        //Zobrist ^= B_CASTLING_RIGHTS[get_castle(BlackKingCastle | BlackQueenCastle, side)];
                    }
                    else if ((board.castle & WhiteKingCastle) != 0 && move.From == (int)Square.h1) // no k castle
                    {
                        board.castle &= ~WhiteKingCastle;
                    }



                }
                else
                {
                    if ((board.castle & BlackQueenCastle) != 0 && move.From == (int)Square.a8) // no q castle
                    {
                        board.castle &= ~BlackQueenCastle;
                    }
                    else if ((board.castle & BlackKingCastle) != 0 && move.From == (int)Square.h8) // no k castle
                    {
                        board.castle &= ~BlackKingCastle;
                    }
                }
            }

            switch (move.Type)
            {
                case double_pawn_push:
                    {
                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[move.Piece] |= (1UL << move.To);

                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);
                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = move.Piece;

                        //update enpassent square

                        if (side == Side.White)
                        {
                            board.enpassent = move.To + 8;
                        }
                        else
                        {
                            board.enpassent = move.To - 8;
                        }



                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[move.Piece][move.To];

                        break;
                    }
                case quiet_move:
                    {
                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[move.Piece] |= (1UL << move.To);

                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);
                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = move.Piece;

                        //update enpassent square
                        //if (move.Type == double_pawn_push)
                        //{
                        //    if (side == Side.White)
                        //    {
                        //        board.enpassent = move.To + 8;
                        //    }
                        //    else
                        //    {
                        //        board.enpassent = move.To - 8;
                        //    }

                        //}

                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[move.Piece][move.To];

                        break;
                    }
                case capture:
                    {
                        if (board.mailbox[move.To] == get_piece(Piece.r, 1 - side))
                        {
                            if (getFile(move.To) == 0) // a file rook captured; delete queen castle
                            {
                                if (side == Side.White) // have to delete black queen castle
                                {
                                    if (getRank(move.To) == 7)
                                    {
                                        board.castle &= ~(BlackQueenCastle);
                                    }

                                    //Console.WriteLine("here");
                                }
                                else
                                {
                                    if (getRank(move.To) == 0)
                                    {
                                        board.castle &= ~(WhiteQueenCastle);
                                    }

                                }
                            }
                            else if (getFile(move.To) == 7) // h file rook captured; delete king castle
                            {
                                //Console.WriteLine("H capture");
                                if (side == Side.White) // have to delete black king castle
                                {
                                    if (getRank(move.To) == 7)
                                    {
                                        board.castle &= ~(BlackKingCastle);
                                    }

                                }
                                else
                                {
                                    if (getRank(move.To) == 0)
                                    {
                                        board.castle &= ~(WhiteKingCastle);
                                    }

                                }
                            }
                        }
                        //update piece bitboard
                        int captured_piece = board.mailbox[move.To];
                        //PrintBoards(board);
                        //print_mailbox(board.mailbox);
                        //printMove(move);
                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[move.Piece] |= (1UL << move.To);


                        //Console.WriteLine(captured_piece);
                        board.bitboards[captured_piece] &= ~(1UL << move.To);

                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        //update captured piece occupancy
                        board.occupancies[1 - side] &= ~(1UL << move.To);

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);
                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = move.Piece;


                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[move.Piece][move.To];
                        Zobrist ^= PIECES[captured_piece][move.To];

                        //Console.WriteLine(side);


                        //Console.WriteLine(get_piece(Piece.r, side));
                        break;
                    }
                case king_castle:
                    {
                        //update castling right & find rook square


                        int rookSquare;
                        if (side == Side.White)
                        {
                            rookSquare = (int)Square.h1;
                            //board.castle &= ~WhiteKingCastle;
                        }
                        else
                        {
                            rookSquare = (int)Square.h8;
                            //board.castle &= ~BlackKingCastle;

                        }


                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[move.Piece] |= (1UL << move.To);

                        board.bitboards[get_piece(Piece.r, side)] &= ~(1UL << rookSquare);
                        board.bitboards[get_piece(Piece.r, side)] |= (1UL << (rookSquare - 2));


                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        board.occupancies[side] &= ~(1UL << rookSquare);
                        board.occupancies[side] |= (1UL << (rookSquare - 2));

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);

                        board.occupancies[Side.Both] &= ~(1UL << rookSquare);
                        board.occupancies[Side.Both] |= (1UL << (rookSquare - 2));
                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = move.Piece;

                        board.mailbox[rookSquare] = -1;
                        board.mailbox[rookSquare - 2] = get_piece(Piece.r, side);


                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[move.Piece][move.To];
                        Zobrist ^= PIECES[get_piece(Piece.r, side)][rookSquare];
                        Zobrist ^= PIECES[get_piece(Piece.r, side)][rookSquare - 2];
                        break;
                    }
                case queen_castle:
                    {
                        //update castling right & find rook square


                        int rookSquare;
                        if (side == Side.White)
                        {
                            rookSquare = (int)Square.a1;
                            //board.castle &= ~WhiteKingCastle;
                        }
                        else
                        {
                            rookSquare = (int)Square.a8;
                            //board.castle &= ~BlackKingCastle;

                        }
                        //Console.WriteLine(CoordinatesToChessNotation(rookSquare));

                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[move.Piece] |= (1UL << move.To);

                        board.bitboards[get_piece(Piece.r, side)] &= ~(1UL << rookSquare);
                        board.bitboards[get_piece(Piece.r, side)] |= (1UL << (rookSquare + 3));


                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        board.occupancies[side] &= ~(1UL << rookSquare);
                        board.occupancies[side] |= (1UL << (rookSquare + 3));

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);

                        board.occupancies[Side.Both] &= ~(1UL << rookSquare);
                        board.occupancies[Side.Both] |= (1UL << (rookSquare + 3));
                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = move.Piece;

                        board.mailbox[rookSquare] = -1;
                        board.mailbox[rookSquare + 3] = get_piece(Piece.r, side);

                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[move.Piece][move.To];
                        Zobrist ^= PIECES[get_piece(Piece.r, side)][rookSquare];
                        Zobrist ^= PIECES[get_piece(Piece.r, side)][rookSquare + 3];
                        break;
                    }
                case queen_promo:
                    {
                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[get_piece(Piece.q, side)] |= (1UL << move.To);

                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);
                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = get_piece(Piece.q, side);

                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[get_piece(Piece.q, side)][move.To];
                        break;
                    }
                case rook_promo:
                    {
                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[get_piece(Piece.r, side)] |= (1UL << move.To);

                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);
                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = get_piece(Piece.r, side);
                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[get_piece(Piece.r, side)][move.To];

                        break;
                    }
                case bishop_promo:
                    {
                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[get_piece(Piece.b, side)] |= (1UL << move.To);

                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);
                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = get_piece(Piece.b, side);
                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[get_piece(Piece.b, side)][move.To];
                        break;
                    }
                case knight_promo:
                    {
                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[get_piece(Piece.n, side)] |= (1UL << move.To);

                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);
                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = get_piece(Piece.n, side);
                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[get_piece(Piece.n, side)][move.To];
                        break;
                    }
                case queen_promo_capture:
                    {
                        if (board.mailbox[move.To] == get_piece(Piece.r, 1 - side))
                        {
                            if (getFile(move.To) == 0) // a file rook captured; delete queen castle
                            {
                                if (side == Side.White) // have to delete black queen castle
                                {
                                    if (getRank(move.To) == 7)
                                    {
                                        board.castle &= ~(BlackQueenCastle);
                                    }

                                    //Console.WriteLine("here");
                                }
                                else
                                {
                                    if (getRank(move.To) == 0)
                                    {
                                        board.castle &= ~(WhiteQueenCastle);
                                    }

                                }
                            }
                            else if (getFile(move.To) == 7) // h file rook captured; delete king castle
                            {
                                //Console.WriteLine("H capture");
                                if (side == Side.White) // have to delete black king castle
                                {
                                    if (getRank(move.To) == 7)
                                    {
                                        board.castle &= ~(BlackKingCastle);
                                    }

                                }
                                else
                                {
                                    if (getRank(move.To) == 0)
                                    {
                                        board.castle &= ~(WhiteKingCastle);
                                    }

                                }
                            }
                        }
                        int captured_piece = board.mailbox[move.To];
                        //update piece bitboard
                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[get_piece(Piece.q, side)] |= (1UL << move.To);

                        board.bitboards[captured_piece] &= ~(1UL << move.To);

                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        //update captured piece occupancy
                        board.occupancies[1 - side] &= ~(1UL << move.To);

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);

                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = get_piece(Piece.q, side);

                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[get_piece(Piece.q, side)][move.To];
                        Zobrist ^= PIECES[captured_piece][move.To];

                        break;
                    }
                case rook_promo_capture:
                    {
                        if (board.mailbox[move.To] == get_piece(Piece.r, 1 - side))
                        {
                            if (getFile(move.To) == 0) // a file rook captured; delete queen castle
                            {
                                if (side == Side.White) // have to delete black queen castle
                                {
                                    if (getRank(move.To) == 7)
                                    {
                                        board.castle &= ~(BlackQueenCastle);
                                    }

                                    //Console.WriteLine("here");
                                }
                                else
                                {
                                    if (getRank(move.To) == 0)
                                    {
                                        board.castle &= ~(WhiteQueenCastle);
                                    }

                                }
                            }
                            else if (getFile(move.To) == 7) // h file rook captured; delete king castle
                            {
                                //Console.WriteLine("H capture");
                                if (side == Side.White) // have to delete black king castle
                                {
                                    if (getRank(move.To) == 7)
                                    {
                                        board.castle &= ~(BlackKingCastle);
                                    }

                                }
                                else
                                {
                                    if (getRank(move.To) == 0)
                                    {
                                        board.castle &= ~(WhiteKingCastle);
                                    }

                                }
                            }
                        }
                        int captured_piece = board.mailbox[move.To];
                        //update piece bitboard
                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[get_piece(Piece.r, side)] |= (1UL << move.To);

                        board.bitboards[captured_piece] &= ~(1UL << move.To);

                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        //update captured piece occupancy
                        board.occupancies[1 - side] &= ~(1UL << move.To);

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);

                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = get_piece(Piece.r, side);

                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[get_piece(Piece.r, side)][move.To];
                        Zobrist ^= PIECES[captured_piece][move.To];

                        break;
                    }
                case bishop_promo_capture:
                    {
                        if (board.mailbox[move.To] == get_piece(Piece.r, 1 - side))
                        {
                            if (getFile(move.To) == 0) // a file rook captured; delete queen castle
                            {
                                if (side == Side.White) // have to delete black queen castle
                                {
                                    if (getRank(move.To) == 7)
                                    {
                                        board.castle &= ~(BlackQueenCastle);
                                    }

                                    //Console.WriteLine("here");
                                }
                                else
                                {
                                    if (getRank(move.To) == 0)
                                    {
                                        board.castle &= ~(WhiteQueenCastle);
                                    }

                                }
                            }
                            else if (getFile(move.To) == 7) // h file rook captured; delete king castle
                            {
                                //Console.WriteLine("H capture");
                                if (side == Side.White) // have to delete black king castle
                                {
                                    if (getRank(move.To) == 7)
                                    {
                                        board.castle &= ~(BlackKingCastle);
                                    }

                                }
                                else
                                {
                                    if (getRank(move.To) == 0)
                                    {
                                        board.castle &= ~(WhiteKingCastle);
                                    }

                                }
                            }
                        }
                        int captured_piece = board.mailbox[move.To];
                        //update piece bitboard
                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[get_piece(Piece.b, side)] |= (1UL << move.To);

                        board.bitboards[captured_piece] &= ~(1UL << move.To);

                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        //update captured piece occupancy
                        board.occupancies[1 - side] &= ~(1UL << move.To);

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);

                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = get_piece(Piece.b, side);


                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[get_piece(Piece.b, side)][move.To];
                        Zobrist ^= PIECES[captured_piece][move.To];

                        break;
                    }
                case knight_promo_capture:
                    {
                        if (board.mailbox[move.To] == get_piece(Piece.r, 1 - side))
                        {
                            if (getFile(move.To) == 0) // a file rook captured; delete queen castle
                            {
                                if (side == Side.White) // have to delete black queen castle
                                {
                                    if (getRank(move.To) == 7)
                                    {
                                        board.castle &= ~(BlackQueenCastle);
                                    }

                                    //Console.WriteLine("here");
                                }
                                else
                                {
                                    if (getRank(move.To) == 0)
                                    {
                                        board.castle &= ~(WhiteQueenCastle);
                                    }

                                }
                            }
                            else if (getFile(move.To) == 7) // h file rook captured; delete king castle
                            {
                                //Console.WriteLine("H capture");
                                if (side == Side.White) // have to delete black king castle
                                {
                                    if (getRank(move.To) == 7)
                                    {
                                        board.castle &= ~(BlackKingCastle);
                                    }

                                }
                                else
                                {
                                    if (getRank(move.To) == 0)
                                    {
                                        board.castle &= ~(WhiteKingCastle);
                                    }

                                }
                            }
                        }
                        int captured_piece = board.mailbox[move.To];
                        //update piece bitboard
                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[get_piece(Piece.n, side)] |= (1UL << move.To);

                        board.bitboards[captured_piece] &= ~(1UL << move.To);

                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        //update captured piece occupancy
                        board.occupancies[1 - side] &= ~(1UL << move.To);

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] |= (1UL << move.To);

                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = get_piece(Piece.n, side);

                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[get_piece(Piece.n, side)][move.To];
                        Zobrist ^= PIECES[captured_piece][move.To];

                        break;
                    }
                case ep_capture:
                    {
                        int capture_square;
                        if (side == Side.White)
                        {
                            capture_square = move.To + 8;
                        }
                        else
                        {
                            capture_square = move.To - 8;
                        }


                        int captured_piece = board.mailbox[capture_square];
                        //update piece bitboard
                        board.bitboards[move.Piece] &= ~(1UL << move.From);
                        board.bitboards[move.Piece] |= (1UL << move.To);

                        board.bitboards[captured_piece] &= ~(1UL << capture_square);

                        //update moved piece occupancy
                        board.occupancies[side] &= ~(1UL << move.From);
                        board.occupancies[side] |= (1UL << move.To);

                        //update captured piece occupancy
                        board.occupancies[1 - side] &= ~(1UL << capture_square);

                        //update both occupancy
                        board.occupancies[Side.Both] &= ~(1UL << move.From);
                        board.occupancies[Side.Both] &= ~(1UL << capture_square);
                        board.occupancies[Side.Both] |= (1UL << move.To);

                        //update mailbox
                        board.mailbox[move.From] = -1;
                        board.mailbox[move.To] = move.Piece;
                        board.mailbox[capture_square] = -1;

                        Zobrist ^= PIECES[move.Piece][move.From];
                        Zobrist ^= PIECES[move.Piece][move.To];
                        Zobrist ^= PIECES[captured_piece][capture_square];
                        break;
                    }

            }


            if (board.enpassent != lastEp) //enpassent updated
            {
                if (lastEp != Square.no_sq)
                {

                    Zobrist ^= EN_passent[lastEp];
                }
                if (board.enpassent != Square.no_sq)
                {
                    Zobrist ^= EN_passent[board.enpassent];
                }



            }
            if (board.castle != lastCastle)
            {
                int lastWhite = get_castle(lastCastle, Side.White);
                int lastBlack = get_castle(lastCastle, Side.Black);

                Zobrist ^= W_CASTLING_RIGHTS[lastWhite];
                Zobrist ^= B_CASTLING_RIGHTS[lastBlack];

                Zobrist ^= W_CASTLING_RIGHTS[get_castle(board.castle, Side.White)];
                Zobrist ^= B_CASTLING_RIGHTS[get_castle(board.castle, Side.Black)];

            }

            board.side = 1 - board.side;
            Zobrist ^= SIDE;

            //zobrist key

            //build hashkey
            //ulong hash_from_scratch = generate_hash_key();

            //if(hash_key != hash_from_scratch)
            //{
            //    Console.WriteLine("hash doens't match");
            //}
        }


        public static void UnmakeMove(ref Board board, Move move, int captured_piece)
        {

            int side = 1 - board.side;
            // change castling flag

            if (move.Type == quiet_move || move.Type == double_pawn_push)
            {
                //Console.WriteLine("q");
                //Console.WriteLine(CoordinatesToChessNotation(move.From) + "," + CoordinatesToChessNotation(move.To));
                //update piece bitboard
                board.bitboards[move.Piece] &= ~(1UL << move.To);
                board.bitboards[move.Piece] |= (1UL << move.From);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.To);
                board.occupancies[side] |= (1UL << move.From);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                board.occupancies[Side.Both] |= (1UL << move.From);
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;

            }
            else if (move.Type == capture)
            {

                //update piece bitboard
                //int captured_piece = board.mailbox[move.To];
                board.bitboards[move.Piece] &= ~(1UL << move.To);
                board.bitboards[move.Piece] |= (1UL << move.From);

                board.bitboards[captured_piece] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.To);
                board.occupancies[side] |= (1UL << move.From);

                //update captured piece occupancy
                board.occupancies[1 - side] |= (1UL << move.To);

                //update both occupancy
                //board.occupancies[Side.Both] &= ~(1UL << move.To);
                board.occupancies[Side.Both] |= (1UL << move.From);
                //update mailbox
                board.mailbox[move.To] = captured_piece;
                board.mailbox[move.From] = move.Piece;

            }
            else if (move.Type == king_castle)
            {
                //update castling right & find rook square


                int rookSquare;
                if (side == Side.White)
                {
                    rookSquare = (int)Square.h1;
                    //board.castle &= ~WhiteKingCastle;
                }
                else
                {
                    rookSquare = (int)Square.h8;
                    //board.castle &= ~BlackKingCastle;

                }


                board.bitboards[move.Piece] &= ~(1UL << move.To);
                board.bitboards[move.Piece] |= (1UL << move.From);

                board.bitboards[get_piece(Piece.r, side)] &= ~(1UL << rookSquare - 2);
                board.bitboards[get_piece(Piece.r, side)] |= (1UL << (rookSquare));


                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.To);
                board.occupancies[side] |= (1UL << move.From);

                board.occupancies[side] &= ~(1UL << rookSquare - 2);
                board.occupancies[side] |= (1UL << (rookSquare));

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                board.occupancies[Side.Both] |= (1UL << move.From);

                board.occupancies[Side.Both] &= ~(1UL << rookSquare - 2);
                board.occupancies[Side.Both] |= (1UL << (rookSquare));
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;

                board.mailbox[rookSquare - 2] = -1;
                board.mailbox[rookSquare] = get_piece(Piece.r, side);


            }
            else if (move.Type == queen_castle)
            {
                //update castling right & find rook square


                int rookSquare;
                if (side == Side.White)
                {
                    rookSquare = (int)Square.a1;
                    //board.castle &= ~WhiteKingCastle;
                }
                else
                {
                    rookSquare = (int)Square.a8;
                    //board.castle &= ~BlackKingCastle;

                }
                //Console.WriteLine(CoordinatesToChessNotation(rookSquare));

                board.bitboards[move.Piece] &= ~(1UL << move.To);
                board.bitboards[move.Piece] |= (1UL << move.From);

                board.bitboards[get_piece(Piece.r, side)] &= ~(1UL << rookSquare + 3);
                board.bitboards[get_piece(Piece.r, side)] |= (1UL << (rookSquare));


                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.To);
                board.occupancies[side] |= (1UL << move.From);

                board.occupancies[side] &= ~(1UL << rookSquare + 3);
                board.occupancies[side] |= (1UL << (rookSquare));

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                board.occupancies[Side.Both] |= (1UL << move.From);

                board.occupancies[Side.Both] &= ~(1UL << rookSquare + 3);
                board.occupancies[Side.Both] |= (1UL << (rookSquare));
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;

                board.mailbox[rookSquare + 3] = -1;
                board.mailbox[rookSquare] = get_piece(Piece.r, side);
            }
            else if (move.Type == queen_promo)
            {
                //update piece bitboard
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.q, side)] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] |= (1UL << move.From);
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;

            }
            else if (move.Type == rook_promo)
            {
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.r, side)] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] |= (1UL << move.From);
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;
            }
            else if (move.Type == bishop_promo)
            {
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.b, side)] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] |= (1UL << move.From);
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;
            }
            else if (move.Type == knight_promo)
            {
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.n, side)] &= ~(1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update both occupancy
                board.occupancies[Side.Both] |= (1UL << move.From);
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                //update mailbox
                board.mailbox[move.To] = -1;
                board.mailbox[move.From] = move.Piece;

            }
            else if (move.Type == queen_promo_capture)
            {
                //int captured_piece = board.mailbox[move.To];
                //update piece bitboard


                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.q, side)] &= ~(1UL << move.To);

                board.bitboards[captured_piece] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] |= (1UL << move.To);

                //update both occupancy
                //board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.From);

                //update mailbox
                board.mailbox[move.To] = captured_piece;
                board.mailbox[move.From] = move.Piece;










            }
            else if (move.Type == rook_promo_capture)
            {
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.r, side)] &= ~(1UL << move.To);

                board.bitboards[captured_piece] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] |= (1UL << move.To);

                //update both occupancy
                //board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.From);

                //update mailbox
                board.mailbox[move.To] = captured_piece;
                board.mailbox[move.From] = move.Piece;
            }
            else if (move.Type == bishop_promo_capture)
            {
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.b, side)] &= ~(1UL << move.To);

                board.bitboards[captured_piece] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] |= (1UL << move.To);

                //update both occupancy
                //board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.From);

                //update mailbox
                board.mailbox[move.To] = captured_piece;
                board.mailbox[move.From] = move.Piece;
            }
            else if (move.Type == knight_promo_capture)
            {
                board.bitboards[move.Piece] |= (1UL << move.From);
                board.bitboards[get_piece(Piece.n, side)] &= ~(1UL << move.To);

                board.bitboards[captured_piece] |= (1UL << move.To);

                //update moved piece occupancy
                board.occupancies[side] |= (1UL << move.From);
                board.occupancies[side] &= ~(1UL << move.To);

                //update captured piece occupancy
                board.occupancies[1 - side] |= (1UL << move.To);

                //update both occupancy
                //board.occupancies[Side.Both] &= ~(1UL << move.From);
                board.occupancies[Side.Both] |= (1UL << move.From);

                //update mailbox
                board.mailbox[move.To] = captured_piece;
                board.mailbox[move.From] = move.Piece;
            }
            else if (move.Type == ep_capture)
            {
                int capture_square;
                int captured_pawn = get_piece(Piece.p, 1 - side);
                if (side == Side.White)
                {
                    capture_square = move.To + 8;
                }
                else
                {
                    capture_square = move.To - 8;
                }


                //int captured_piece = board.mailbox[capture_square];
                //update piece bitboard
                board.bitboards[move.Piece] &= ~(1UL << move.To);
                board.bitboards[move.Piece] |= (1UL << move.From);

                board.bitboards[captured_pawn] |= (1UL << capture_square);

                //update moved piece occupancy
                board.occupancies[side] &= ~(1UL << move.To);
                board.occupancies[side] |= (1UL << move.From);

                //update captured piece occupancy
                board.occupancies[1 - side] |= (1UL << capture_square);

                //update both occupancy
                board.occupancies[Side.Both] &= ~(1UL << move.To);
                board.occupancies[Side.Both] |= (1UL << capture_square);
                board.occupancies[Side.Both] |= (1UL << move.From);

                //update mailbox
                board.mailbox[move.From] = move.Piece;
                board.mailbox[move.To] = -1;
                board.mailbox[capture_square] = captured_pawn;
            }



            board.side = 1 - board.side;
        }

        public static void Generate_Legal_Moves(ref List<Move> MoveList, ref Board board, bool isCapture)
        {
            int WK_Square = get_ls1b(board.bitboards[Piece.K]);
            int BK_Square = get_ls1b(board.bitboards[Piece.k]);

            int my_king = (board.side == Side.White) ? WK_Square : BK_Square;

            //Console.WriteLine(my_king);
            //ulong Attacked_square = get_attacked_squares(oppSide, board, (board.occupancies[Side.Both] & ~KingBB));
            //Console.WriteLine((board.side == Side.White));
            List<ulong> pin_ray = new();
            List<ulong> pinned_piece = new();


            ulong check_attackers = 0;
            //ulong attacked_square = is_square_attacked



            Detect_pinned_pieces(my_king, ref pinned_piece, ref pin_ray, board);


            //Console.WriteLine(pinned_piece.Count);
            //PrintBitboard(pin_ray[0]);


            Detect_Check_Attackers(my_king, ref check_attackers, board);


            //Console.WriteLine(count_bits(check_attackers));

            //PrintBitboard(check_attackers); 
            ulong move_mask = 0xFFFFFFFFFFFFFFFF;

            

            //PrintBitboard(move_mask);
            ulong capture_mask = 0xFFFFFFFFFFFFFFFF;
            //print_bitboard(move_mask);
            if (count_bits(check_attackers) == 1) // single check
            {
                move_mask = between(my_king, get_ls1b(check_attackers));

                capture_mask = check_attackers;

                if (board.enpassent != (int)Square.no_sq)
                {
                    int pawnToCapture;
                    if (board.side == Side.White)
                    {
                        pawnToCapture = board.enpassent + 8;
                    }
                    else
                    {
                        pawnToCapture = board.enpassent - 8;
                    }

                    if ((check_attackers & (1UL << pawnToCapture)) != 0)
                    {
                        capture_mask |= (1UL << board.enpassent);
                    }




                }

            }

            //PrintBitboard(move_mask | capture_mask);

            //Console.WriteLine(count_bits(check_attackers));
            //print_bitboard(pinned_ray);
            //for (int i = 0; i < pinned_piece.Count; i++)
            //{
            //    PrintBitboard(pinned_piece[i]); 
            //    PrintBitboard(pin_ray[i]); 
            //}

            //ulong prevCastle = board.castle;
            //PrintBitboard(board.castle);
            Generate_Pawn_Moves(ref MoveList, board, check_attackers, move_mask, capture_mask, pin_ray, pinned_piece, isCapture);

            //if (prevCastle != board.castle) Console.WriteLine(0);
            Generate_Knight_Moves(ref MoveList, board, check_attackers, move_mask, capture_mask, pin_ray, pinned_piece, isCapture);
            //PrintBitboard(board.castle);
            //if (prevCastle != board.castle) Console.WriteLine(1);
            Generate_Bishop_Moves(ref MoveList, board, check_attackers, move_mask, capture_mask, pin_ray, pinned_piece, isCapture);
            //PrintBitboard(board.castle);
            //if (prevCastle != board.castle) Console.WriteLine(2);
            Generate_Rook_Moves(ref MoveList, board, check_attackers, move_mask, capture_mask, pin_ray, pinned_piece, isCapture);
            //PrintBitboard(board.castle);
            //if (prevCastle != board.castle) Console.WriteLine(3);
            Generate_Queen_Moves(ref MoveList, board, check_attackers, move_mask, capture_mask, pin_ray, pinned_piece, isCapture);
            //PrintBitboard(board.castle);
            //if (prevCastle != board.castle) Console.WriteLine(4);
            Generate_King_Moves(ref MoveList, board, check_attackers, isCapture);

        }
        static void Detect_pinned_pieces(int King, ref List<ulong> pinned_piece, ref List<ulong> pin_ray, Board board)
        {
            int side = board.side;
            int oppSide = 1 - board.side;

            ulong poss_pinning_hor = (side == Side.White)
                ? (board.bitboards[Piece.r] | board.bitboards[Piece.q])
                : (board.bitboards[Piece.R] | board.bitboards[Piece.Q]);
            ulong poss_pinning_dia = (side == Side.White)
                ? (board.bitboards[Piece.b] | board.bitboards[Piece.q])
                : (board.bitboards[Piece.B] | board.bitboards[Piece.Q]);

            ulong pinned_ray = get_queen_attacks(King, board.occupancies[oppSide]);
            ulong possible_pinned_piece = pinned_ray & board.occupancies[side];
            ulong possible_pinning_piece = pinned_ray & (poss_pinning_hor | poss_pinning_dia);

            while (possible_pinning_piece != 0)
            {
                int pos = get_ls1b(possible_pinning_piece);
                int posX = getFile(pos);
                int posY = getRank(pos);
                int kingX = getFile(King);
                int kingY = getRank(King);

                ulong KingLine = between(pos, King);

                if (KingLine == 0UL)
                {
                    Pop_bit(ref possible_pinning_piece, pos);
                    continue;
                }

                bool isHorizontalOrVertical = (posX == kingX || posY == kingY);
                if (isHorizontalOrVertical)
                {
                    if ((poss_pinning_hor & (1UL << pos)) == 0)
                    {
                        Pop_bit(ref possible_pinning_piece, pos);
                        continue;
                    }
                }
                else
                {
                    if ((poss_pinning_dia & (1UL << pos)) == 0)
                    {
                        Pop_bit(ref possible_pinning_piece, pos);
                        continue;
                    }
                }

                if (count_bits(KingLine & board.occupancies[side]) == 1) // possibly pinned
                {
                    pin_ray.Add(KingLine | (1UL << pos));
                    pinned_piece.Add(KingLine & board.occupancies[side]);
                }

                Pop_bit(ref possible_pinning_piece, pos);
            }
        }
        static void Detect_Check_Attackers(int King, ref ulong attackers, Board board)
        {

            int side = board.side;
            int oppSide = 1 - board.side;

            ulong oppKnight = ((side == Side.White) ? board.bitboards[Piece.n] : board.bitboards[Piece.N]);
            ulong oppBishop = ((side == Side.White) ? board.bitboards[Piece.b] : board.bitboards[Piece.B]);
            ulong oppRook = ((side == Side.White) ? board.bitboards[Piece.r] : board.bitboards[Piece.R]);
            ulong oppQueen = ((side == Side.White) ? board.bitboards[Piece.q] : board.bitboards[Piece.Q]);
            ulong oppPawn = ((side == Side.White) ? board.bitboards[Piece.p] : board.bitboards[Piece.P]);

            ulong check_attackers = 0;
            check_attackers |= Knight_attacks[King] & oppKnight;

            check_attackers |= get_bishop_attacks(King, board.occupancies[Side.Both]) & oppBishop;

            check_attackers |= get_rook_attacks(King, board.occupancies[Side.Both]) & oppRook;

            check_attackers |= get_queen_attacks(King, board.occupancies[Side.Both]) & oppQueen;

            check_attackers |= pawn_attacks[side, King] & oppPawn;

            //PrintBitboard(pawn_attacks[side, King]);
            attackers = check_attackers;

        }

        static void Generate_Pawn_Moves(ref List<Move> MoveList, Board board, ulong attackers, ulong move_mask, ulong capture_mask, List<ulong> pin_ray, List<ulong> pinned_piece, bool isCapture)
        {
            //int check_num = ;

            if (count_bits(attackers) >= 2) return;


            int side = board.side;


            ulong PawnBB = (board.side == Side.White) ? board.bitboards[Piece.P] : board.bitboards[Piece.p];
            List<int> pinned = new();
            List<int> pinned_Loc = new();
            ulong pin_mask, BB, pawnPromo, pawnOnePush, pawnTwoPush, pawn_capture, pawn_capture_mask, enpassent;
            for (int i = 0; i < pin_ray.Count; i++)
            {
                if ((PawnBB & pinned_piece[i]) != 0) // found pinned bishop
                {

                    pinned.Add(i);
                    pinned_Loc.Add(get_ls1b(pinned_piece[i]));

                    //Console.WriteLine(CoordinatesToChessNotation(get_ls1b(pinned_piece[i])));
                }

            }
            for (; PawnBB != 0;)
            {
                int From = get_ls1b(PawnBB);
                int pinnedloc = pinned_Loc.IndexOf(From);
               
                if (pinnedloc == -1)
                {
                    pin_mask = 0xFFFFFFFFFFFFFFFF;
                    //PrintBitboard(pin_mask);
                }
                else
                {
                    pin_mask = pin_ray[pinned[pinnedloc]];

                }
                BB = 1UL << From;
                if ((board.side == Side.White) ? (From >= (int)Square.a7 && From <= (int)Square.h7) : (From >= (int)Square.a2 && From <= (int)Square.h2))
                {
                    // =======promotion======= //
                    if (!isCapture)
                    {
                        pawnPromo = (((side == Side.White) ? (BB >> 8) : (BB << 8)) & ~board.occupancies[Side.Both]) & move_mask & pin_mask;
                        //bool isPossible = pawnOnePush != 0;


                        for (; pawnPromo != 0;)
                        {
                            //Console.WriteLine(pawnPromo);
                            int To = get_ls1b(pawnPromo);
                            MoveList.Add(new Move(From, To, knight_promo, get_piece(Piece.p, side)));
                            MoveList.Add(new Move(From, To, bishop_promo, get_piece(Piece.p, side)));
                            MoveList.Add(new Move(From, To, rook_promo, get_piece(Piece.p, side)));
                            MoveList.Add(new Move(From, To, queen_promo, get_piece(Piece.p, side)));
                            Pop_bit(ref pawnPromo, To);
                            //Console.WriteLine(pawnPromo);

                        }
                    }
                        

                    
                    // =======promo_capture======= //
                    pawn_capture_mask = pawn_attacks[board.side, From];
                    pawn_capture = ((board.side == Side.White) ? pawn_capture_mask & board.occupancies[Side.Black] : pawn_capture_mask & board.occupancies[Side.White]) & (move_mask | capture_mask) & pin_mask;

                    for (; pawn_capture != 0;)
                    {
                        int To = get_ls1b(pawn_capture);



                        MoveList.Add(new Move(From, To, knight_promo_capture, get_piece(Piece.p, side)));
                        MoveList.Add(new Move(From, To, bishop_promo_capture, get_piece(Piece.p, side)));
                        MoveList.Add(new Move(From, To, rook_promo_capture, get_piece(Piece.p, side)));
                        MoveList.Add(new Move(From, To, queen_promo_capture, get_piece(Piece.p, side)));
                        Pop_bit(ref pawn_capture, To);
                    }


                }
                else
                {
                    // =======pawn one square push======= //
                    if (!isCapture)
                    {
                        pawnOnePush = (((side == Side.White) ? (BB >> 8) : (BB << 8)) & ~board.occupancies[Side.Both]) & move_mask & pin_mask;
                        bool isPossible = (((side == Side.White) ? (BB >> 8) : (BB << 8)) & ~board.occupancies[Side.Both]) != 0;


                        for (; pawnOnePush != 0;)
                        {
                            int To = get_ls1b(pawnOnePush);
                            MoveList.Add(new Move(From, To, quiet_move, get_piece(Piece.p, side)));
                            Pop_bit(ref pawnOnePush, To);
                        }

                        // =======pawn two square push======= //

                        pawnTwoPush = 0;
                        if (isPossible)
                        {
                            if ((board.side == Side.White) ? (From >= (int)Square.a2 && From <= (int)Square.h2) : (From >= (int)Square.a7 && From <= (int)Square.h7))//pawn on second rank
                            {

                                pawnTwoPush = (((side == Side.White) ? (BB >> 16) : (BB << 16)) & ~board.occupancies[Side.Both]) & move_mask & pin_mask;

                            }
                        }

                        if (pawnTwoPush != 0)
                        {
                            for (; pawnTwoPush != 0;)
                            {
                                int To = get_ls1b(pawnTwoPush);
                                MoveList.Add(new Move(From, To, double_pawn_push, get_piece(Piece.p, side)));
                                Pop_bit(ref pawnTwoPush, To);
                            }
                        }
                    }
                    // =======pawn capture======= //
                    pawn_capture_mask = pawn_attacks[board.side, From];
                    pawn_capture = ((board.side == Side.White) ? pawn_capture_mask & board.occupancies[Side.Black] : pawn_capture_mask & board.occupancies[Side.White]) & (move_mask | capture_mask) & pin_mask;

                    for (; pawn_capture != 0;)
                    {
                        int To = get_ls1b(pawn_capture);
                        MoveList.Add(new Move(From, To, capture, get_piece(Piece.p, side)));
                        Pop_bit(ref pawn_capture, To);
                    }

                    // =======pawn Enpassent =======//
                    enpassent = 0;
                    if (board.enpassent != (int)Square.no_sq) // enpassent possible
                    {
                        //Console.WriteLine("here");
                        enpassent = (pawn_capture_mask & (1UL << board.enpassent)) & (capture_mask) & pin_mask;




                    }

                    for (; enpassent != 0;)
                    {
                        int To = get_ls1b(enpassent);

                        int pawnToCapture = 0;
                        if (side == Side.White)
                        {
                            pawnToCapture = To + 8;
                        }
                        else
                        {
                            pawnToCapture = To - 8;
                        }
                        int King = (side == Side.White) ? get_ls1b(board.bitboards[Piece.K]) : get_ls1b(board.bitboards[Piece.k]);

                        board.bitboards[get_piece(Piece.p, 1 - side)] &= ~(1UL << pawnToCapture);
                        bool isAttacked = false;
                        if (getRank(pawnToCapture) == getRank(King))
                        {
                            isAttacked = is_square_attacked(King, 1 - side, board, board.occupancies[Side.Both] & ~(1UL << From) & ~(1UL << pawnToCapture));
                        }

                        board.bitboards[get_piece(Piece.p, 1 - side)] |= (1UL << pawnToCapture);
                        //PrintBitboard(enpassent);
                        //PrintBitboard(board.occupancies[Side.Both] & ~(1UL << From) & ~(1UL << pawnToCapture));
                        //Console.WriteLine(isAttacked);
                        if (!isAttacked)
                        {
                            MoveList.Add(new Move(From, To, ep_capture, get_piece(Piece.p, side)));
                        }

                        Pop_bit(ref enpassent, To);
                    }


                }
                Pop_bit(ref PawnBB, From);
            }
        }

        static void Generate_Knight_Moves(ref List<Move> MoveList, Board board, ulong attackers, ulong move_mask, ulong capture_mask, List<ulong> pin_ray, List<ulong> pinned_piece, bool isCapture)
        {
            int check_num = count_bits(attackers);

            if (check_num >= 2) return;

            
            int side = board.side;

            int oppSide = 1 - side;
            List<int> pinned = new();
            List<int> pinned_Loc = new();
            ulong KnightBB = (board.side == Side.White) ? board.bitboards[Piece.N] : board.bitboards[Piece.n];
            //ulong pin_mask = 0xFFFFFFFFFFFFFFFF;
            for (int i = 0; i < pin_ray.Count; i++)
            {
                if ((KnightBB & pinned_piece[i]) != 0) // found pinned bishop
                {
                    pinned.Add(i);
                    pinned_Loc.Add(get_ls1b(pinned_piece[i]));
                }

            }

            //PrintBitboard(KnightBB);
            for (; KnightBB != 0;)
            {
                int From = get_ls1b(KnightBB);
                int pinnedloc = pinned_Loc.IndexOf(From);
                //Console.WriteLine("a" + pinned_Loc.Count);
                //Console.WriteLine("a" + pinnedloc);

                //Console.WriteLine(CoordinatesToChessNotation(From));
                if (pinnedloc != -1)
                {
                    Pop_bit(ref KnightBB, From);
                    continue;
                }
                //PrintBitboard(1UL << From);

                ulong KnightMove = (Knight_attacks[From] & ~board.occupancies[side]) & (move_mask | capture_mask);
                //PrintBitboard((Knight_attacks[From] & ~board.occupancies[side]) );

                for (; KnightMove != 0;)
                {
                    //Console.WriteLine(pawnPromo);
                    int To = get_ls1b(KnightMove);
                    if ((board.occupancies[oppSide] & (1UL << To)) != 0) // capture
                    {
                        if (((1UL << To) & capture_mask) != 0)
                        {
                            MoveList.Add(new Move(From, To, capture, get_piece(Piece.n, side)));
                        }
                    }
                    else
                    {
                        if (capture_mask == 0xFFFFFFFFFFFFFFFF || ((1UL << To) & capture_mask) == 0)
                        {
                            if (((1UL << To) & move_mask) != 0)
                            {
                                if(!isCapture)
                                {
                                    MoveList.Add(new Move(From, To, quiet_move, get_piece(Piece.n, side)));
                                }
                                
                            }
                            
                        }

                    }

                    Pop_bit(ref KnightMove, To);
                    //Console.WriteLine(pawnPromo);

                }
                Pop_bit(ref KnightBB, From);
            }
        }

        static void Generate_Bishop_Moves(ref List<Move> MoveList, Board board, ulong attackers, ulong move_mask, ulong capture_mask, List<ulong> pin_ray, List<ulong> pinned_piece, bool isCapture)
        {
            int check_num = count_bits(attackers);

            if (check_num >= 2) return;
            int side = board.side;

            int oppSide = 1 - side;

            ulong BishopBB = (board.side == Side.White) ? board.bitboards[Piece.B] : board.bitboards[Piece.b];
            List<int> pinned = new();
            List<int> pinned_Loc = new();

            ulong pin_mask = 0xFFFFFFFFFFFFFFFF;
            for (int i = 0; i < pin_ray.Count; i++)
            {
                if ((BishopBB & pinned_piece[i]) != 0) // found pinned bishop
                {
                    pinned.Add(i);
                    pinned_Loc.Add(get_ls1b(pinned_piece[i]));
                }

            }
            for (; BishopBB != 0;)
            {
                pin_mask = 0xFFFFFFFFFFFFFFFF;
                int From = get_ls1b(BishopBB);
                int pinnedloc = pinned_Loc.IndexOf(From);

                //Console.WriteLine(pinnedloc);
                if (pinnedloc != -1)
                {
                    pin_mask = pin_ray[pinned[pinnedloc]];
                }


                //PrintBitboard(pin_mask);
                ulong BishopMoves = (get_bishop_attacks(From, board.occupancies[Side.Both]) & ~board.occupancies[side]) & (move_mask | capture_mask) & pin_mask;
                //PrintBitboard(BishopMoves);

                for (; BishopMoves != 0;)
                {
                    //Console.WriteLine(pawnPromo);
                    int To = get_ls1b(BishopMoves);
                    if ((board.occupancies[oppSide] & (1UL << To)) != 0) // capture
                    {
                        if (((1UL << To) & capture_mask) != 0)
                        {
                            MoveList.Add(new Move(From, To, capture, get_piece(Piece.b, side)));
                        }
                    }
                    else
                    {
                        if (capture_mask == 0xFFFFFFFFFFFFFFFF || ((1UL << To) & capture_mask) == 0)
                        {
                            if (!isCapture)
                            {
                                MoveList.Add(new Move(From, To, quiet_move, get_piece(Piece.b, side)));
                            }
                        }
                    }

                    Pop_bit(ref BishopMoves, To);
                    //Console.WriteLine(pawnPromo);

                }
                Pop_bit(ref BishopBB, From);
            }
        }

        static void Generate_Rook_Moves(ref List<Move> MoveList, Board board, ulong attackers, ulong move_mask, ulong capture_mask, List<ulong> pin_ray, List<ulong> pinned_piece, bool isCapture)
        {
            int check_num = count_bits(attackers);

            if (check_num >= 2) return;
            int side = board.side;

            int oppSide = 1 - side;

            ulong RookBB = (board.side == Side.White) ? board.bitboards[Piece.R] : board.bitboards[Piece.r];
            List<int> pinned = new();
            List<int> pinned_Loc = new();

            ulong pin_mask = 0xFFFFFFFFFFFFFFFF;
            for (int i = 0; i < pin_ray.Count; i++)
            {
                if ((RookBB & pinned_piece[i]) != 0) // found pinned rook
                {
                    pinned.Add(i);
                    pinned_Loc.Add(get_ls1b(pinned_piece[i]));
                }

            }

            for (; RookBB != 0;)
            {
                pin_mask = 0xFFFFFFFFFFFFFFFF;
                int From = get_ls1b(RookBB);
                //PrintBitboard(1UL << From);
                int pinnedloc = pinned_Loc.IndexOf(From);
                if (pinnedloc != -1)
                {
                    pin_mask = pin_ray[pinned[pinnedloc]];
                }
                ulong RookMoves = (get_rook_attacks(From, board.occupancies[Side.Both]) & ~board.occupancies[side]) & (move_mask | capture_mask) & pin_mask;
                //PrintBitboard(BishopMoves);

                for (; RookMoves != 0;)
                {
                    //Console.WriteLine(pawnPromo);
                    int To = get_ls1b(RookMoves);
                    if ((board.occupancies[oppSide] & (1UL << To)) != 0) // capture
                    {
                        //PrintBitboard(capture_mask);
                        if (((1UL << To) & capture_mask) != 0)
                        {
                            MoveList.Add(new Move(From, To, capture, get_piece(Piece.r, side)));
                        }

                    }
                    else
                    {
                        //PrintBitboard((1UL << To));
                        //PrintBitboard(capture_mask);
                        if (capture_mask == 0xFFFFFFFFFFFFFFFF || ((1UL << To) & capture_mask) == 0)
                        {
                            if (!isCapture)
                            {
                                MoveList.Add(new Move(From, To, quiet_move, get_piece(Piece.r, side)));
                            }
                        }
                    }

                    Pop_bit(ref RookMoves, To);
                    //Console.WriteLine(pawnPromo);

                }
                Pop_bit(ref RookBB, From);
            }
        }

        static void Generate_Queen_Moves(ref List<Move> MoveList, Board board, ulong attackers, ulong move_mask, ulong capture_mask, List<ulong> pin_ray, List<ulong> pinned_piece, bool isCapture)
        {
            int check_num = count_bits(attackers);

            if (check_num >= 2) return;
            int side = board.side;

            int oppSide = 1 - side;

            ulong QueenBB = (board.side == Side.White) ? board.bitboards[Piece.Q] : board.bitboards[Piece.q];
            List<int> pinned = new();
            List<int> pinned_Loc = new();
            ulong pin_mask = 0xFFFFFFFFFFFFFFFF;
            for (int i = 0; i < pin_ray.Count; i++)
            {
                if ((QueenBB & pinned_piece[i]) != 0) // found pinned queen
                {
                    pinned.Add(i);
                    pinned_Loc.Add(get_ls1b(pinned_piece[i]));
                }

            }
            for (; QueenBB != 0;)
            {
                pin_mask = 0xFFFFFFFFFFFFFFFF;
                int From = get_ls1b(QueenBB);
                int pinnedloc = pinned_Loc.IndexOf(From);
                if (pinnedloc != -1)
                {
                    pin_mask = pin_ray[pinned[pinnedloc]];
                }
                //PrintBitboard(1UL << From);
                ulong QueenMoves = (get_queen_attacks(From, board.occupancies[Side.Both]) & ~board.occupancies[side]) & (move_mask | capture_mask) & pin_mask;
                //PrintBitboard(BishopMoves);

                for (; QueenMoves != 0;)
                {
                    //Console.WriteLine(pawnPromo);
                    int To = get_ls1b(QueenMoves);
                    if ((board.occupancies[oppSide] & (1UL << To)) != 0) // capture
                    {
                        if (((1UL << To) & capture_mask) != 0)
                        {
                            MoveList.Add(new Move(From, To, capture, get_piece(Piece.q, side)));
                        }
                    }
                    else
                    {
                        if (capture_mask == 0xFFFFFFFFFFFFFFFF || ((1UL << To) & capture_mask) == 0)
                        {
                            if (!isCapture)
                            {
                                MoveList.Add(new Move(From, To, quiet_move, get_piece(Piece.q, side)));
                            }
                        }
                    }

                    Pop_bit(ref QueenMoves, To);
                    //Console.WriteLine(pawnPromo);

                }
                Pop_bit(ref QueenBB, From);
            }
        }

        static void Generate_King_Moves(ref List<Move> MoveList, Board board, ulong attackers, bool isCapture)
        {
            //PrintBitboard(board.castle);
            //int check_num = count_bits(attackers);
            int side = board.side;

            int oppSide = 1 - side;

            ulong KingBB = (board.side == Side.White) ? board.bitboards[Piece.K] : board.bitboards[Piece.k];
            int From = get_ls1b(KingBB);

            ulong Attacked_square = get_attacked_squares(oppSide, board, (board.occupancies[Side.Both] & ~KingBB));

            //PrintBitboard(Attacked_square);

            //PrintBitboard(1UL << From);

            ulong KingMoves = King_attacks[From] & ~board.occupancies[side] & ~Attacked_square;
            //PrintBitboard(BishopMoves);

            for (; KingMoves != 0;)
            {
                //Console.WriteLine(pawnPromo);

                int To = get_ls1b(KingMoves);
                if ((board.occupancies[oppSide] & (1UL << To)) != 0) // capture //fuck up here fuck
                {

                    MoveList.Add(new Move(From, To, capture, get_piece(Piece.k, side)));
                }
                else
                {
                    if (!isCapture)
                    {
                        MoveList.Add(new Move(From, To, quiet_move, get_piece(Piece.k, side)));
                    }

                }

                Pop_bit(ref KingMoves, To);
                //Console.WriteLine(pawnPromo);

            }
            if (!isCapture)
            {
                if (side == Side.White)
                {
                    if ((board.castle & WhiteKingCastle) != 0) // kingside castling
                    {
                        if ((board.occupancies[Side.Both] & (1UL << (int)Square.f1)) == 0 && (board.occupancies[Side.Both] & (1UL << (int)Square.g1)) == 0)
                        {
                            if ((Attacked_square & KingBB) == 0 && (Attacked_square & (1UL << (int)Square.f1)) == 0 && (Attacked_square & (1UL << (int)Square.g1)) == 0) // not check
                            {

                                MoveList.Add(new Move((int)Square.e1, (int)Square.g1, king_castle, get_piece(Piece.k, side)));
                            }

                        }

                    }
                    if ((board.castle & WhiteQueenCastle) != 0)
                    {
                        if ((board.occupancies[Side.Both] & (1UL << (int)Square.d1)) == 0 && (board.occupancies[Side.Both] & (1UL << (int)Square.c1)) == 0 && (board.occupancies[Side.Both] & (1UL << (int)Square.b1)) == 0)
                        {

                            if ((Attacked_square & KingBB) == 0 && (Attacked_square & (1UL << (int)Square.d1)) == 0 && (Attacked_square & (1UL << (int)Square.c1)) == 0) // not check
                            {
                                MoveList.Add(new Move((int)Square.e1, (int)Square.c1, queen_castle, get_piece(Piece.k, side)));
                            }
                        }

                    }
                }
                else
                {
                    if ((board.castle & BlackKingCastle) != 0) // kingside castling
                    {
                        //Console.WriteLine("pass1");
                        if ((board.occupancies[Side.Both] & (1UL << (int)Square.f8)) == 0 && (board.occupancies[Side.Both] & (1UL << (int)Square.g8)) == 0)
                        {
                            if ((Attacked_square & KingBB) == 0 && (Attacked_square & (1UL << (int)Square.f8)) == 0 && (Attacked_square & (1UL << (int)Square.g8)) == 0) // not check
                            {

                                MoveList.Add(new Move((int)Square.e8, (int)Square.g8, king_castle, get_piece(Piece.k, side)));
                            }

                        }

                    }

                    if ((board.castle & BlackQueenCastle) != 0)
                    {

                        if ((board.occupancies[Side.Both] & (1UL << (int)Square.d8)) == 0 && (board.occupancies[Side.Both] & (1UL << (int)Square.c8)) == 0 && (board.occupancies[Side.Both] & (1UL << (int)Square.b8)) == 0)
                        {
                            if ((Attacked_square & KingBB) == 0 && (Attacked_square & (1UL << (int)Square.d8)) == 0 && (Attacked_square & (1UL << (int)Square.c8)) == 0) // not check
                            {
                                MoveList.Add(new Move((int)Square.e8, (int)Square.c8, queen_castle, get_piece(Piece.k, side)));
                            }
                        }

                    }
                }
            }
            Pop_bit(ref KingBB, From);

        }

        public static bool is_square_attacked(int square, int side, Board board, ulong occupancy)
        {
            if ((side == Side.White) && ((pawn_attacks[Side.Black, square] & board.bitboards[Piece.P])) != 0) return true;
            if ((side == Side.Black) && ((pawn_attacks[Side.White, square] & board.bitboards[Piece.p])) != 0) return true;
            if ((get_bishop_attacks(square, occupancy) & ((side == Side.White) ? board.bitboards[Piece.B] : board.bitboards[Piece.b])) != 0) return true;
            if ((get_rook_attacks(square, occupancy) & ((side == Side.White) ? board.bitboards[Piece.R] : board.bitboards[Piece.r])) != 0) return true;
            if ((get_queen_attacks(square, occupancy) & ((side == Side.White) ? board.bitboards[Piece.Q] : board.bitboards[Piece.q])) != 0) return true;
            if ((Knight_attacks[square] & ((side == Side.White) ? board.bitboards[Piece.N] : board.bitboards[Piece.n])) != 0) return true;
            if ((King_attacks[square] & ((side == Side.White) ? board.bitboards[Piece.K] : board.bitboards[Piece.k])) != 0) return true;


            return false;
        }
        static ulong get_attacked_squares(int side, Board board, ulong occupancy)
        {
            ulong attack_map = 0;
            ulong bb;

            // Precompute piece bitboards for the given side
            ulong kingBB = (side == Side.White) ? board.bitboards[Piece.K] : board.bitboards[Piece.k];
            ulong knightBB = (side == Side.White) ? board.bitboards[Piece.N] : board.bitboards[Piece.n];
            ulong bishopBB = (side == Side.White) ? board.bitboards[Piece.B] : board.bitboards[Piece.b];
            ulong rookBB = (side == Side.White) ? board.bitboards[Piece.R] : board.bitboards[Piece.r];
            ulong queenBB = (side == Side.White) ? board.bitboards[Piece.Q] : board.bitboards[Piece.q];
            ulong pawnBB = (side == Side.White) ? board.bitboards[Piece.P] : board.bitboards[Piece.p];

            // Process King
            attack_map |= King_attacks[get_ls1b(kingBB)];

            // Process Knights
            for (bb = knightBB; bb != 0;)
            {
                int loc = get_ls1b(bb);
                attack_map |= Knight_attacks[loc];
                Pop_bit(ref bb, loc);
            }

            // Process Bishops
            for (bb = bishopBB; bb != 0;)
            {
                int loc = get_ls1b(bb);
                attack_map |= get_bishop_attacks(loc, occupancy);
                Pop_bit(ref bb, loc);
            }

            // Process Rooks
            for (bb = rookBB; bb != 0;)
            {
                int loc = get_ls1b(bb);
                attack_map |= get_rook_attacks(loc, occupancy);
                Pop_bit(ref bb, loc);
            }

            // Process Queens
            for (bb = queenBB; bb != 0;)
            {
                int loc = get_ls1b(bb);
                attack_map |= get_queen_attacks(loc, occupancy);
                Pop_bit(ref bb, loc);
            }

            // Process Pawns
            for (bb = pawnBB; bb != 0;)
            {
                int loc = get_ls1b(bb);
                attack_map |= pawn_attacks[side, loc];
                Pop_bit(ref bb, loc);
            }

            return attack_map;
        }

        static ulong get_bishop_attacks(int square, ulong occupancy)
        {
            occupancy &= bishop_masks[square];
            occupancy *= bishop_magic_numbers[square];
            occupancy >>= 64 - bishop_relevant_bits[square];

            return bishop_attacks[square, occupancy];
        }
        static ulong get_rook_attacks(int square, ulong occupancy)
        {
            occupancy &= rook_masks[square];
            occupancy *= rook_magic_numbers[square];
            occupancy >>= 64 - rook_relevant_bits[square];

            return rook_attacks[square, occupancy];
        }
        static ulong get_queen_attacks(int square, ulong occupancy)
        {

            //Console.WriteLine(square);
            ulong queen_attacks;
            ulong bishop_occupancies = occupancy;
            ulong rook_occupancies = occupancy;

            rook_occupancies &= rook_masks[square];
            rook_occupancies *= rook_magic_numbers[square];
            rook_occupancies >>= 64 - rook_relevant_bits[square];
            queen_attacks = rook_attacks[square, rook_occupancies];

            bishop_occupancies &= bishop_masks[square];
            bishop_occupancies *= bishop_magic_numbers[square];
            bishop_occupancies >>= 64 - bishop_relevant_bits[square];
            queen_attacks |= bishop_attacks[square, bishop_occupancies];

            return queen_attacks;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int get_piece(int piece, int col)
        {
            return Get_Whitepiece[piece] + Side_value[col];
            //return 1;
            //bool isBlack = piece >= 6;
            //return (col == Side.White) ? (isBlack ? piece - 6 : piece) : (isBlack ? piece : piece + 6);
            if (col == Side.White)
            {
                //Piece.r
                //6이상 흑
                if (piece >= 6)
                {
                    return piece - 6;
                }
                return piece;
            }
            else
            {
                if (piece >= 6)
                {
                    return piece;

                }
                return piece + 6;
            }
        }

        public static void InitializeLeaper()
        {
            for (int i = 0; i < 64; i++)
            {
                pawn_attacks[0, i] = CalculatePawnAttack(i, 0);
                pawn_attacks[1, i] = CalculatePawnAttack(i, 1);

                Knight_attacks[i] = CalculateKnightAttack(i);
                King_attacks[i] = CalculateKingAttack(i);


            }
        }

        static ulong CalculatePawnAttack(int square, int side)
        {
            ulong attacks = 0UL;
            ulong bitboard = 0UL;
            Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            if (side == 0)//white
            {
                if (((bitboard >> 7) & NotAFile) != 0)
                    attacks |= (bitboard >> 7);
                if (((bitboard >> 9) & NotHFile) != 0)
                    attacks |= (bitboard >> 9);
            }
            else //black
            {
                if (((bitboard << 7) & NotHFile) != 0)
                    attacks |= (bitboard << 7);
                if (((bitboard << 9) & NotAFile) != 0)
                    attacks |= (bitboard << 9);
            }

            return attacks;
        }

        static ulong CalculateKnightAttack(int square)
        {
            ulong attacks = 0UL;
            ulong bitboard = 0UL;
            Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            //17 15 10 6

            if (((bitboard >> 17) & NotHFile) != 0)
                attacks |= (bitboard >> 17);
            if (((bitboard >> 15) & NotAFile) != 0)
                attacks |= (bitboard >> 15);
            if (((bitboard >> 10) & NotHGFile) != 0)
                attacks |= (bitboard >> 10);
            if (((bitboard >> 6) & NotABFile) != 0)
                attacks |= (bitboard >> 6);

            if (((bitboard << 17) & NotAFile) != 0)
                attacks |= (bitboard << 17);
            if (((bitboard << 15) & NotHFile) != 0)
                attacks |= (bitboard << 15);
            if (((bitboard << 10) & NotABFile) != 0)
                attacks |= (bitboard << 10);
            if (((bitboard << 6) & NotHGFile) != 0)
                attacks |= (bitboard << 6);
            return attacks;
        }
        static ulong CalculateKingAttack(int square)
        {
            ulong attacks = 0UL;
            ulong bitboard = 0UL;
            Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            //17 15 10 6

            if (((bitboard >> 8)) != 0)
                attacks |= (bitboard >> 8);
            if (((bitboard >> 9) & NotHFile) != 0)
                attacks |= (bitboard >> 9);
            if (((bitboard >> 7) & NotAFile) != 0)
                attacks |= (bitboard >> 7);
            if (((bitboard >> 1) & NotHFile) != 0)
                attacks |= (bitboard >> 1);

            if (((bitboard << 8)) != 0)
                attacks |= (bitboard << 8);
            if (((bitboard << 9) & NotAFile) != 0)
                attacks |= (bitboard << 9);
            if (((bitboard << 7) & NotHFile) != 0)
                attacks |= (bitboard << 7);
            if (((bitboard << 1) & NotAFile) != 0)
                attacks |= (bitboard << 1);
            return attacks;
        }


        public static void init_sliders_attacks(int bishop)
        {
            for (int square = 0; square < 64; square++)
            {
                bishop_masks[square] = MaskBishopAttack(square);
                rook_masks[square] = MaskRookAttack(square);

                ulong attack_mask = bishop != 0 ? bishop_masks[square] : rook_masks[square];
                int relevant_bits_count = count_bits(attack_mask);
                int occupancy_indicies = (1 << relevant_bits_count);

                for (int index = 0; index < occupancy_indicies; index++)
                {
                    if (bishop != 0)
                    {
                        ulong occupancy = set_occupancy(index, relevant_bits_count, attack_mask);

                        int magic_index = (int)((occupancy * bishop_magic_numbers[square]) >> (64 - bishop_relevant_bits[square]));
                        
                        if (magic_index < 512)
                        {
                            bishop_attacks[square, magic_index] = CalculateBishopAttack(square, occupancy);
                        }
                        else
                        {
                            Console.WriteLine($"bishop magic_index out of range: {magic_index} for square: {square}");
                        }
                    }
                    else
                    {
                        ulong occupancy = set_occupancy(index, relevant_bits_count, attack_mask);

                        int magic_index = (int)((occupancy * rook_magic_numbers[square]) >> (64 - rook_relevant_bits[square]));
                        
                        if (magic_index < 4096)
                        {
                            rook_attacks[square, magic_index] = CalculateRookAttack(square, occupancy);
                        }
                        else
                        {
                            Console.WriteLine($"rook magic_index out of range: {magic_index} for square: {square}");
                        }
                    }
                }
            }
        }


        static ulong MaskBishopAttack(int square)
        {
            ulong attacks = 0UL;
            //ulong bitboard = 0UL;

            int r, f;
            int tr = square / 8;
            int tf = square % 8;

            for (r = tr + 1, f = tf + 1; r <= 6 && f <= 6; r++, f++)
            {

                attacks |= (1UL << (r * 8 + f));

            }
            for (r = tr - 1, f = tf + 1; r >= 1 && f <= 6; r--, f++)
            {

                attacks |= (1UL << (r * 8 + f));

            }
            for (r = tr + 1, f = tf - 1; r <= 6 && f >= 1; r++, f--)
            {

                attacks |= (1UL << (r * 8 + f));

            }
            for (r = tr - 1, f = tf - 1; r >= 1 && f >= 1; r--, f--)
            {

                attacks |= (1UL << (r * 8 + f));

            }
            //Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            //17 15 10 6


            return attacks;
        }

        static ulong MaskRookAttack(int square)
        {
            ulong attacks = 0UL;
            //ulong bitboard = 0UL;

            int r, f;
            int tr = square / 8;
            int tf = square % 8;

            for (r = tr + 1; r <= 6; r++)
            {
                attacks |= (1UL << (r * 8 + tf));

            }
            for (r = tr - 1; r >= 1; r--)
            {
                attacks |= (1UL << (r * 8 + tf));

            }
            for (f = tf + 1; f <= 6; f++)
            {
                attacks |= (1UL << (tr * 8 + f));

            }
            for (f = tf - 1; f >= 1; f--)
            {
                attacks |= (1UL << (tr * 8 + f));

            }
            //Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            //17 15 10 6


            return attacks;
        }

        static ulong CalculateRookAttack(int square, ulong block)
        {
            ulong attacks = 0UL;
            //ulong bitboard = 0UL;

            int r, f;
            int tr = square / 8;
            int tf = square % 8;

            for (r = tr + 1; r <= 7; r++)
            {
                attacks |= (1UL << (r * 8 + tf));
                if ((1UL << (r * 8 + tf) & block) != 0)
                {
                    break;
                }
            }
            for (r = tr - 1; r >= 0; r--)
            {
                attacks |= (1UL << (r * 8 + tf));
                if ((1UL << (r * 8 + tf) & block) != 0)
                {
                    break;
                }
            }
            for (f = tf + 1; f <= 7; f++)
            {
                attacks |= (1UL << (tr * 8 + f));
                if ((1UL << (tr * 8 + f) & block) != 0)
                {
                    break;
                }
            }
            for (f = tf - 1; f >= 0; f--)
            {
                attacks |= (1UL << (tr * 8 + f));
                if ((1UL << (tr * 8 + f) & block) != 0)
                {
                    break;
                }
            }
            //Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            //17 15 10 6


            return attacks;
        }

        static ulong CalculateBishopAttack(int square, ulong block)
        {
            ulong attacks = 0UL;
            //ulong bitboard = 0UL;

            int r, f;
            int tr = square / 8;
            int tf = square % 8;

            for (r = tr + 1, f = tf + 1; r <= 7 && f <= 7; r++, f++)
            {
                attacks |= (1UL << (r * 8 + f));
                if ((1UL << (r * 8 + f) & block) != 0)
                {
                    break;
                }
            }
            for (r = tr - 1, f = tf + 1; r >= 0 && f <= 7; r--, f++)
            {
                attacks |= (1UL << (r * 8 + f));
                if ((1UL << (r * 8 + f) & block) != 0)
                {
                    break;
                }
            }
            for (r = tr + 1, f = tf - 1; r <= 7 && f >= 0; r++, f--)
            {
                attacks |= (1UL << (r * 8 + f));
                if ((1UL << (r * 8 + f) & block) != 0)
                {
                    break;
                }
            }
            for (r = tr - 1, f = tf - 1; r >= 0 && f >= 0; r--, f--)
            {
                attacks |= (1UL << (r * 8 + f));
                if ((1UL << (r * 8 + f) & block) != 0)
                {
                    break;
                }
            }
            //Set_bit(ref bitboard, square);
            //PrintBitboard(bitboard);
            //17 15 10 6


            return attacks;
        }
    }
}

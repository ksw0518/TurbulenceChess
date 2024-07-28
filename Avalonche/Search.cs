using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Turbulence.BitManipulation;
using static Turbulence.GenerateMove;

using static Turbulence.MoveMethod;
using static Turbulence.BoardMethod;
using static Turbulence.Evaluation;
using static Turbulence.UCI;
using System.Threading.Tasks.Sources;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections;
namespace Turbulence
{
    public static class Search
    {
        public const int valUNKNOWN = int.MinValue;
        public const int HASH_EXACT = 0;
        public const int HASH_ALPHA = 1;
        public const int HASH_BETA = 2;
        static bool isSuccess = true;
        public static bool IS_SEARCH_STOPPED = false;
        
        //public class Board
        //{
        //    public ulong[] bitboards = new ulong[12];
        //    public ulong[] occupancies = new ulong[3];
        //    public int[] mailbox = new int[64];
        //    public int side;
        //    public int enpassent = (int)Square.no_sq;
        //    public ulong castle;
        //}
        public class Transposition
        {
            public ulong key;
            public int ply;
            public int flags;
            public int value;
            public Move bestMove;
        }

        //public struct ThreeFold
        //{
        //    public ulong key;
        //    //public int repet;
        //}
        
        const int MAX_PLY = 64;

        static bool isMoveorder = true;
        public static int nodes = 0;
        public static int[][] MVVLVA_T = new int[6][] ;
        public static Dictionary<int, int> MVVLVA_PieceInd = new Dictionary<int, int> { { Piece.P, 0 }, { Piece.N, 1 }, { Piece.B, 2 }, { Piece.R, 3 }, { Piece.Q, 4 }, { Piece.K, 5 } };

        const int plusINFINITY = 300000;
        const int minusINFINITY = -300000;
        static int ply = 0;

        static int MAX_QDEPTH = 6;

        static private Move[,] killerMoves = new Move[MAX_PLY, 2];
        static int q_count = 0;

        public static Stopwatch time_ellapsed;

        public static ulong[][] PIECES = new ulong[12][];
        public static ulong[] W_CASTLING_RIGHTS = new ulong[4];
        public static ulong[] B_CASTLING_RIGHTS = new ulong[4];
        public static ulong[] EN_passent = new ulong[64];
        public static ulong SIDE;
        public static int StartSearch(ref Board board, int depth, ref int[] pv_length, ref Move[][] pv_table, ulong Zobrist, ref Transposition[] TT_Table, ref List<ulong>Three_fold) 
        {

            
            int eval = 0;
            time_ellapsed = new Stopwatch();
            time_ellapsed.Reset();
            time_ellapsed.Start();
            Stopwatch st = new Stopwatch();
            st.Reset();
            st.Start();
            Move bestmove = new Move();
            Move lastBmove = new Move();
            isSuccess = true;
            //Console.WriteLine(depth);
            //Transposition[] tt = new Transposition[TT_SIZE];
            for (int i = 1; i < depth + 1; i++)
            {
                TT_hit = 0;

                List<ulong> Three_fold_forSearch = new List<ulong>(Three_fold);
                //Console.WriteLine(DetectThreefold(ref Three_fold_forSearch));
                //for (int j = 0; j < Three_fold_forSearch.Count; j++)
                //{
                //    Console.WriteLine(Three_fold_forSearch[j]);
                //}
                nodes = 0;
                q_count = 0;
                //Console.WriteLine(i);
                lastBmove = new Move(bestmove.From, bestmove.To, bestmove.Type, bestmove.Piece);
                if (time_ellapsed.ElapsedMilliseconds >= THINK_TIME)
                {
                    //Console.WriteLine("quit");
                    
                    IS_SEARCH_STOPPED = true;
                    isSuccess = false;
                    //break;
                }
                else
                {
                    isSuccess = true;
                }
                st.Reset();
                st.Start();



                //Console.WriteLine((timeMS / 1000));

                //Console.WriteLine("Nodes: " + nodes + " NPS: " + NPSinM + "M" + " time(MS) " + timeMS);
                ////Console.WriteLine(nodes);
                
                
                eval = negaMax(ref board, i, minusINFINITY, plusINFINITY, ref pv_length, ref pv_table, new Move(), Zobrist, ref TT_Table, ref Three_fold_forSearch);
                //Console.WriteLine(isSuccess);
                //if (time_ellapsed.ElapsedMilliseconds >= THINK_TIME) IS_SEARCH_STOPPED = true;
                st.Stop();
                bestmove = pv_table[0][0];


                if (!isSuccess) break;
                int realNodes = nodes + q_count;
                float time = (float)st.Elapsed.TotalNanoseconds;
                float timeMS = time / 1000000;
                float NPS = realNodes / ((timeMS / 1000));
                float NPSinM = NPS / 1000000;
                Console.Write("info depth " + i);
                
                if(eval > 30000 || eval < -30000)  //mate
                {
                    string eval_mate = "";
                    if (eval > 30000)
                    {
                        eval_mate = "mate " + ((49000 - eval + 1) / 2);
                    }
                    else
                    {
                        eval_mate = "mate " + -(49000 - (-eval)) / 2;
                    }
                    Console.Write(" score " + eval_mate);

                }
                else
                {
                    Console.Write(" score cp " + eval);
                }
                
                Console.Write(" time " + (int)timeMS);
                Console.Write(" nodes " + realNodes);
                if ((int)NPS < 0) NPS = 0;
                Console.Write(" nps " + (int)NPS);
               // Console.Write(" q_move " + q_count);

                Console.Write(" pv ");
                for (int count = 0; count < pv_length[0]; count++)
                {
                    printMove(pv_table[0][count]);
                    Console.Write(" ");
                }

                //Console.WriteLine(TT_hit);
                Console.Write("\n");

                if (IS_SEARCH_STOPPED) break;

            
            }
            if(!isSuccess)
            {
                bestmove = lastBmove;
            }
            Console.Write("bestmove ");
            printMove(bestmove);

            MakeMove(ref board, bestmove, ref Zobrist);


            if (isMoveIrreversible(bestmove))
            {
                Three_fold.Clear();
                board.halfmove = 0;
            }

            board.halfmove++;

            Three_fold.Add(Zobrist);
            //Console.WriteLine(DetectThreefold(ref Three_fold));
            //for (int j = 0; j < Three_fold.Count; j++)
            //{
            //    Console.WriteLine(Three_fold[j]);
            //}
            Console.Write("\n");
            //Move bestmove = 


            return eval;
        }
        static int Get_MVVLVA_Value(Move move, ref Board board)
        {
            int from = move.From;
            int to = move.To;
            if (move.Type == ep_capture) //ep capture
            {
                return MVVLVA_T[MVVLVA_PieceInd[Piece.P]][ MVVLVA_PieceInd[Piece.P]];
            }
            //printMove(move);
            //Debug.Log(ValueToPiece(cb[from]) + "," + ValueToPiece(cb[to]));
            //Console.WriteLine(get_piece(board.mailbox[from], Side.White));
            return MVVLVA_T[MVVLVA_PieceInd[get_piece(board.mailbox[from], Side.White)]][MVVLVA_PieceInd[get_piece(board.mailbox[to], Side.White)]];
        }
        static int negaMax(ref Board board, int depth, int alpha, int beta, ref int[] pv_length, ref Move[][] pv_table, Move lmove, ulong ZobristKey, ref Transposition[] TTtable, ref List<ulong> threeFoldRep )
        {
            if (time_ellapsed.ElapsedMilliseconds >= THINK_TIME)
            {
                //Console.WriteLine(time_ellapsed.ElapsedMilliseconds);
                IS_SEARCH_STOPPED = true;

            }
            if (time_ellapsed.ElapsedMilliseconds >= THINK_TIME)
            {
                //Console.WriteLine(time_ellapsed.ElapsedMilliseconds);
                IS_SEARCH_STOPPED = true;
                
            }
            if (IS_SEARCH_STOPPED)
            {
                isSuccess = false;
                return 0;
            }
            if(board.halfmove >= 100)
            {
                return 0;
            }
            int score;

            int hashf = HASH_ALPHA;
            Move TT_Best_Move;
            score = probeHash(depth, alpha, beta, ref TTtable, ZobristKey, out TT_Best_Move);
            if (score != valUNKNOWN)
            { 
                //if (ply == 0)
                //{
                //    Console.WriteLine("depth 1");
                //    pv_length[ply] = ply;
                //    pv_table[ply][ply] = TT_Best_Move;
                //}
                TT_hit++;
                return score;   
            }


            pv_length[ply] = ply;
            if (depth == 0)
            {
                nodes++;

                int quiescence = Quiescence(ref board, MAX_QDEPTH, alpha, beta, lmove, ZobristKey);
                
                //updateTT(quies)
                
                
                return quiescence;
            }
            //if (lmove.To == Square.d4 && lmove.From == Square.f5)
            //{
            //    Console.WriteLine(DetectThreefold(ref threeFoldRep));
            //    for (int j = 0; j < threeFoldRep.Count; j++)
            //    {
            //        Console.WriteLine(threeFoldRep[j]);
            //    }
            //}

            if (DetectThreefold(ref threeFoldRep))
            {
                //Console.WriteLine("threefold");
                return 0;
            }
            int best_score = int.MinValue;
           
            List<Move> movelist = new();
            Generate_Legal_Moves(ref movelist, ref board, false);

           
            
            if (movelist.Count == 0)
            {
                //Console.WriteLine("no move");
                int KingSq = (board.side == Side.White) ? get_ls1b(board.bitboards[Piece.K]) : get_ls1b(board.bitboards[Piece.k]);

                if(is_square_attacked(KingSq, 1 - board.side, board, board.occupancies[Side.Both]))
                {
                    //Console.WriteLine(-49000 + ply);
                    return -49000 + ply;

                }
                else
                {
                    return 0;
                }
            }
            if (isMoveorder && depth != 1 )
            {
                SortMoves(movelist, lmove, ref board);
                //movelist.Sort((move1, move2) => CompareMoves(move1, move2, lmove, ref board));

            }

            Move best_move = movelist[0];
            Move killer1 = killerMoves[ply, 0];
            Move killer2 = killerMoves[ply, 1];


            int killer1Pos = movelist.IndexOf(killer1);
            if(killer1Pos != -1)
            {
                movelist.RemoveAt(killer1Pos);
                movelist.Insert(0, killer1);
            }
            int killer2Pos = movelist.IndexOf(killer2);
            if (killer2Pos != -1)
            {
                movelist.RemoveAt(killer2Pos);
                movelist.Insert(0, killer2);
            }
            int org_halfmove = board.halfmove;

            for (int i = 0; i < movelist.Count; i++)
            {
                if (IS_SEARCH_STOPPED)
                {
                    isSuccess = false;
                    return 0;
                }
                ply++;
                int lastEp = board.enpassent;
                ulong lastCastle = board.castle;
                int lastside = board.side;
                int captured_piece = board.mailbox[movelist[i].To];
                ulong lastZobrist = ZobristKey;

                if (isMoveIrreversible(movelist[i]))
                {
                    threeFoldRep.Clear();
                    board.halfmove = 0;
                }

                board.halfmove++;
                MakeMove(ref board, movelist[i], ref ZobristKey);
                threeFoldRep.Add(ZobristKey);



                score = -negaMax(ref board, depth - 1, -beta, -alpha, ref pv_length, ref pv_table, movelist[i] , ZobristKey, ref TTtable, ref threeFoldRep);


                ply--;

                UnmakeMove(ref board, movelist[i], captured_piece);
                board.halfmove = org_halfmove;
                board.enpassent = lastEp;
                board.castle = lastCastle;
                board.side = lastside;
                ZobristKey = lastZobrist;
                
                
                if (score > best_score)
                {
                    hashf = HASH_EXACT;
                    best_move = movelist[i];
                    best_score = score;
                }
                if (best_score > alpha)
                {
                    alpha = best_score;

                    best_move = movelist[i];
                    pv_table[ply][ply] = movelist[i];

                    for (int next_ply = ply + 1; next_ply < pv_length[ply + 1]; next_ply++)
                    {
                        pv_table[ply][next_ply] = pv_table[ply + 1][next_ply];
                    }
                    pv_length[ply] = pv_length[ply + 1];
                }

                if (best_score >= beta)
                {

                    updateTT(beta, depth, HASH_BETA, ref TTtable, ZobristKey, movelist[i]);
                    if (!killerMoves[ply, 0].Equals(movelist[i]) && !killerMoves[ply, 1].Equals(movelist[i]))
                    {
                        killerMoves[ply, 1] = killerMoves[ply, 0];
                        killerMoves[ply, 0] = movelist[i];
                    }
                    return beta;
                }
                
            }
            updateTT(alpha, depth, hashf, ref TTtable, ZobristKey, best_move );
            return alpha;


            
        }

        public static bool DetectThreefold(ref List<ulong> Rep_table)
        {
            if(Rep_table.Count < 3) return false;

            int table_length = Rep_table.Count - 1;
            ulong currentHash = Rep_table[table_length];
            int rep_count = 0;


            //Console.WriteLine("currhash " + currentHash);
            //Console.WriteLine(table_length);
            for (int i = table_length - 2; i >= 0; i -= 2)
            {
                //Console.WriteLine(i);
                //Console.WriteLine("compare " + Rep_table[i]);

                if (Rep_table[i] == currentHash)
                {
                    rep_count++;
                    if(rep_count == 2)
                    {
                        return true;
                    }
                }
            }
            //Console.WriteLine(rep_count);
            return false;

        }
        public static bool isMoveIrreversible(Move move)
        {
            if (IsCapture(move) || (move.Piece == Piece.P) || (move.Piece == Piece.p))
            {
                return true;
            }
            return false;
        }
        static int probeHash(int ply, int alpha, int beta, ref Transposition[] TTtable, ulong Zobrist, out Move move)
        {
            Transposition hashEntry = TTtable[get_hash_Key(Zobrist, TT_SIZE)];
            if(hashEntry != null && hashEntry.key == Zobrist)
            {
                if(hashEntry.ply >= ply)
                {
                    move = hashEntry.bestMove;
                    if (hashEntry.flags == HASH_EXACT)
                        return hashEntry.value;
                    if (hashEntry.flags == HASH_ALPHA && hashEntry.value <= alpha)
                        return alpha;
                    if(hashEntry.flags == HASH_BETA && hashEntry.value >= beta)
                        return beta;

                }
            }
            move = new Move();
            return valUNKNOWN;

        }
        static void updateTT(int score, int ply, int hash_flag, ref Transposition[] TTtable, ulong Zobrist, Move bestmove)
        {
            //Transposition hash_entry = TTtable[get_hash_Key(Zobrist, TT_SIZE)]
            if (TTtable[get_hash_Key(Zobrist, TT_SIZE)] == null) // hash empty
            {
                //Console.WriteLine("a");
                TTtable[get_hash_Key(Zobrist, TT_SIZE)] = new Transposition();
                TTtable[get_hash_Key(Zobrist, TT_SIZE)].key = Zobrist;
                TTtable[get_hash_Key(Zobrist, TT_SIZE)].value = score;
                TTtable[get_hash_Key(Zobrist, TT_SIZE)].flags = hash_flag;
                TTtable[get_hash_Key(Zobrist, TT_SIZE)].ply = ply;
                TTtable[get_hash_Key(Zobrist, TT_SIZE)].bestMove = bestmove;

            }
            else if(ply > TTtable[get_hash_Key(Zobrist, TT_SIZE)].ply) //depth scheme
            {
                
                //Console.WriteLine(TTtable[get_hash_Key(Zobrist, TT_SIZE)].ply);
                //Console.WriteLine(ply);
                //Console.Write("\n");


                TTtable[get_hash_Key(Zobrist, TT_SIZE)].key = Zobrist;
                TTtable[get_hash_Key(Zobrist, TT_SIZE)].value = score;
                TTtable[get_hash_Key(Zobrist, TT_SIZE)].flags = hash_flag;
                TTtable[get_hash_Key(Zobrist, TT_SIZE)].ply = ply;
                TTtable[get_hash_Key(Zobrist, TT_SIZE)].bestMove = bestmove;
            }
        }
        static int get_hash_Key(ulong zobrist, int hashsize)
        {
            return (int)(zobrist % (ulong)hashsize);
        }
        static int Quiescence(ref Board board, int depth, int alpha, int beta, Move lmove, ulong ZobristKey)
        {
            if (IS_SEARCH_STOPPED)
            {
                isSuccess = false;
                return 0;
            }
            q_count++;
            int stand_pat = EvalPos(board);
            if (stand_pat >= beta)
            {
                return beta;
            }
            if(alpha < stand_pat)
            {
                alpha = stand_pat;
            }
            if(depth == 0)
            {
                return stand_pat;
            }    
            List<Move> movelist = new();
            Generate_Legal_Moves(ref movelist, ref board, true);
            if (isMoveorder & depth != 1)
            {
                SortMoves(movelist, lmove, ref board);
                //movelist.Sort((move1, move2) => CompareMoves(move1, move2, lmove, ref board));

            }

            if (movelist.Count == 0) //no capture available
            {
                return stand_pat;
            }

            for(int i = 0; i < movelist.Count; i++)
            {
                if (IS_SEARCH_STOPPED)
                {
                    isSuccess = false;
                    return 0;
                }
                int lastEp = board.enpassent;
                ulong lastCastle = board.castle;
                int lastside = board.side;
                int captured_piece = board.mailbox[movelist[i].To];
                ulong lastZobrist = ZobristKey;

                MakeMove(ref board, movelist[i], ref ZobristKey);

                int score = -Quiescence(ref board, depth - 1, -beta, -alpha, lmove, ZobristKey);
                
                UnmakeMove(ref board, movelist[i], captured_piece);
                board.enpassent = lastEp;
                board.castle = lastCastle;
                board.side = lastside;
                ZobristKey = lastZobrist;

                if(score >= beta)
                {
                    return beta;
                }
                if(score > alpha)
                {
                    return alpha;
                }
            }


            return alpha;
        }
        
        
        
        public class MoveComparer : IComparer<Move>
        {
            private Board _board;
            private readonly Move _lmove;

            public MoveComparer(ref Board board, Move lmove)
            {
                _board = board;
                _lmove = lmove;
            }

            public int Compare(Move move1, Move move2)
            {
                return CompareMoves(move1, move2, _lmove, ref _board);
            }
        }
        static void SortMoves(List<Move> movelist, Move lmove, ref Board board)
        {
            MoveComparer comparer = new MoveComparer(ref board, lmove);
            movelist.Sort(comparer.Compare);
        }

        static int CompareMoves(Move move1, Move move2, Move lmove, ref Board board)
        {
            if (move1.To == lmove.To)
            {
                if (move2.To == lmove.To)
                    return 0; // Both moves capture the lastly moved piece.
                else
                    return -1; // move1 captures the lastly moved piece.
            }
            else if (move2.To == lmove.To)
            {
                return 1; // move2 captures the lastly moved piece.
            }
            if (IsCapture(move1) && IsCapture(move2))
            {
                int value1 = Get_MVVLVA_Value(move1, ref board);
                int value2 = Get_MVVLVA_Value(move2, ref board);
                return value2.CompareTo(value1); // Sort in descending order
            }
            else if (IsCapture(move1))
            {
                return -1; // move1 is a capture, move2 is not
            }
            else if (IsCapture(move2))
            {
                return 1; // move2 is a capture, move1 is not
            }
            else
            {
                return 0; // Both moves are non-captures; maintain their order
            }
        }
        static bool IsCapture(Move move)
        {
            if((move.Type & capture) != 0)
            {
                return true;
            }
            return false;
        }

        public static void Get_Zobrist(ref Board board, ref ulong zobrist)
        {
            zobrist = 0;

            for(int i = 0; i < 64;  i++)
            {
                int piece = board.mailbox[i];

                if (piece == -1) continue;
                zobrist ^= PIECES[piece][i];
            }

            zobrist ^= W_CASTLING_RIGHTS[get_castle(board.castle, Side.White)];
            zobrist ^= B_CASTLING_RIGHTS[get_castle(board.castle, Side.Black)];

            if (board.enpassent != Square.no_sq)
            {
                zobrist ^= EN_passent[board.enpassent];
            }
            if (board.side == Side.Black)
            {
                zobrist ^= SIDE;
            }
        }

        public static int get_castle(ulong castle, int side)
        {
            if(side == Side.White)
            {
                if ((castle & (WhiteKingCastle | WhiteQueenCastle)) != 0)
                {
                    return 0;
                }
                else if ((castle & WhiteKingCastle) != 0)
                {
                    return 1;
                }
                else if ((castle & WhiteQueenCastle) != 0)
                {
                    return 2;
                }
                else 
                {
                    return 3;
                }
            }
            else
            {
                if ((castle & (BlackKingCastle | BlackQueenCastle)) != 0)
                {
                    return 0;
                }
                else if ((castle & BlackKingCastle) != 0)
                {
                    return 1;
                }
                else if ((castle & BlackQueenCastle) != 0)
                {
                    return 2;
                }
                else
                {
                    return 3;
                }
            }

        }

    }
}


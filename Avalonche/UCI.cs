using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Turbulence;
using static Turbulence.BitManipulation;
using static Turbulence.GenerateMove;

using static Turbulence.MoveMethod;
using static Turbulence.BoardMethod;
using static Turbulence.Search;
using static Turbulence.Evaluation;
using System.Numerics;


namespace Turbulence
{
    public class UCI
    {
        public class Transposition_Perft
        {
            public ulong key;
            public int ply;
            public ulong nodecount;


        }
        //public static int main_halfmove;
        public static int TT_hit = 0;
        public const int TT_SIZE = 33554432;//33554432
        static Transposition[] TT_Table = new Transposition[TT_SIZE];

        static Transposition_Perft[] TT_Table_perft = new Transposition_Perft[TT_SIZE];


        static List<ulong> Repetition_table = new ();


        static int Perft_DEPTH = 0;
        static ulong perft_TTCount = 0;
        Board main_board;
        ulong main_Zobrist;
        bool isBoardSet = false;

        public static int THINK_TIME = 0;

        public static int[] pv_length = new int[64];
        public static Move[][] pv_table = new Move[64][]; 
        static readonly string[] goLabels = new[] { "go", "movetime", "wtime", "btime", "winc", "binc", "movestogo" };
        static void Main(string[] args) 
        {
            
            InitAll();
           
            UCI uci = new UCI();
            uci.main_board = new Board();
            parse_fen(start_position, ref uci.main_board);
            Get_Zobrist(ref uci.main_board, ref uci.main_Zobrist);

            
            //Console.WriteLine(Square.GetSquare("h1"));
            //Perft_DEPTH = 3;
            //List<Move> movelist = new List<Move>();

            //Generate_Legal_Moves(ref movelist, ref uci.main_board, true);

            //PrintLegalMoves(movelist);

            
           
            
            
            //Move bestMove = pv_table[0][0];
            //printMove(bestMove);

            //Console.WriteLine(eval);

            while (true)
            {
                string input = Console.ReadLine();

                // Check if user wants to quit


                // Call your function for processing user input
                uci.Process_UCI(input);
            }
            //parse_fen(start_position, ref main_board);

            //Perft_DEPTH = int.Parse(Console.ReadLine());
            //Stopwatch st = new Stopwatch();
            //st.Reset();
            //st.Start();

            //ulong nodes = uci.perft(Perft_DEPTH, ref main_board);
            //float time = (float)st.Elapsed.TotalNanoseconds;
            //float timeMS = time / 1000000;
            //float NPS = nodes / ((timeMS / 1000));
            //float NPSinM = NPS / 1000000;
            //Console.WriteLine((timeMS / 1000));

            //Console.WriteLine("Nodes: " + nodes + " NPS: " + NPSinM + "M" + " time(MS) " + timeMS);
            ////Console.WriteLine(nodes);
            //st.Stop();
        }

        void Process_UCI(string input)
        {

            string[] parts = input.Split(' ');
            //Console.WriteLine(parts.Length);
            string main_command = parts[0];
            if(main_command == "rep")
            {            //    Console.WriteLine(DetectThreefold(ref threeFoldRep));
                for (int j = 0; j < Repetition_table.Count; j++)
                {
                    Console.WriteLine(Repetition_table[j]);
                }
            }
            if (main_command == "position")
            {
                if (parts[1] == "startpos")
                {
                    if (parts.Length == 2)
                    {
                        
                        parse_fen(start_position, ref main_board);
                        Get_Zobrist(ref main_board, ref main_Zobrist);
                       

                    }
                    else
                    {
                        int startindex = 2;
                        if (parts[2] == "moves") startindex = 3;
                        Repetition_table.Clear();
                        //Console.WriteLine("1");
                        //Console.WriteLine(parts.Length);
                        //TT_Table = new Transposition[TT_SIZE];
                        Reset_PV();
                        IS_SEARCH_STOPPED = false;

                        parse_fen(start_position, ref main_board);
                        Get_Zobrist(ref main_board, ref main_Zobrist);
                        List<string> moves = new List<string>();
                        for (int i = startindex; i < parts.Length; i++)
                        {
                            //Console.WriteLine(i); 
                            moves.Add(parts[i]);
                            //Console.WriteLine(moves[moves.Count - 1]);

                        }
                        List<Move> moveList = new List<Move>();
                        List<Move> Move_to_do = new List<Move>();
                        //Generate_Legal_Moves(ref moveList, ref main_board, false);
                        //Console.Write("                        ");
                        for (int i = 0; i < moves.Count; i++)
                        {
                            string From = moves[i][0].ToString() + moves[i][1].ToString();
                            string To = moves[i][2].ToString() + moves[i][3].ToString();

                            string promo = "";
                            if (moves[i].Length > 4)
                            {
                                promo = moves[i][4].ToString();
                            }

                            Move movetofind = new Move();
                            movetofind.From = Square.GetSquare(From);
                            //Console.WriteLine(CoordinatesToChessNotation(movetofind.From));
                            movetofind.To = Square.GetSquare(To);
                            //Console.WriteLine(CoordinatesToChessNotation(movetofind.To));
                            moveList.Clear();
                            Generate_Legal_Moves(ref moveList, ref main_board, false);
                            //PrintLegalMoves(moveList);
                            for (int j = 0; j < moveList.Count; j++)
                            {
                                //Console.WriteLine("12");
                                nodes = 0;

                                if ((movetofind.From == moveList[j].From) && (movetofind.To == moveList[j].To)) //found same move
                                {



                                    if ((moveList[j].Type & knight_promo) != 0) // promo
                                    {
                                        if (promo == "q")
                                        {
                                            if ((moveList[j].Type == queen_promo) | (moveList[j].Type == queen_promo_capture))
                                            {
                                                MakeMove(ref main_board, moveList[j], ref main_Zobrist);
                                                break;

                                                //Move_to_do.Add(moveList[j]);
                                            }
                                        }
                                        else if (promo == "r")
                                        {
                                            if ((moveList[j].Type == rook_promo) | (moveList[j].Type == rook_promo_capture))
                                            {
                                                MakeMove(ref main_board, moveList[j], ref main_Zobrist);
                                                break;
                                                //Move_to_do.Add(moveList[j]);
                                            }
                                        }
                                        else if (promo == "b")
                                        {
                                            if ((moveList[j].Type == bishop_promo) | (moveList[j].Type == bishop_promo_capture))
                                            {
                                                MakeMove(ref main_board, moveList[j], ref main_Zobrist);
                                                break;
                                                //Move_to_do.Add(moveList[j]);
                                            }
                                        }
                                        else if (promo == "n")
                                        {
                                            if ((moveList[j].Type == knight_promo) | (moveList[j].Type == knight_promo_capture))
                                            {
                                                MakeMove(ref main_board, moveList[j], ref main_Zobrist);
                                                break;
                                                //Move_to_do.Add(moveList[j]);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //Console.WriteLine(MoveType[moveList[j].Type]);
                                        //Console.WriteLine(ascii_pieces[moveList[j].Piece]);
                                       // printMove(moveList[j]);
                                        //Console.Write(" ");
                                        //Move_to_do.Add(moveList[j]); 
                                        MakeMove(ref main_board, moveList[j], ref main_Zobrist);
                                        if (isMoveIrreversible(moveList[j]))
                                        {
                                            //Console.WriteLine("aaa");
                                            Repetition_table.Clear();
                                            main_board.halfmove = 0;
                                        }
                                        Repetition_table.Add(main_Zobrist);
                                        main_board.halfmove++;

                                        
                                        break;
                                    }

                                    

                                }
                            }

                            //PrintBoards(main_board);
                        }
                    }
                    //Console.WriteLine(main_board.halfmove);
                   // Console.WriteLine(Repetition_table.Count);
                    //Console.WriteLine(DetectThreefold(ref Repetition_table));
                    //for (int j = 0; j < Repetition_table.Count; j++)
                    //{
                    //    Console.WriteLine(Repetition_table[j]);
                    //}

                }
                else if (parts[1] == "test")
                {
                    int startindex = 2;
                    if (parts[2] == "moves") startindex = 3;
                    Repetition_table.Clear();
                    //Console.WriteLine("1");
                    //Console.WriteLine(parts.Length);
                    //TT_Table = new Transposition[TT_SIZE];
                    Reset_PV();
                    IS_SEARCH_STOPPED = false;

                    parse_fen("5k2/p5pp/8/3BrP2/3R4/3p2P1/PP5P/6K1 b - - 0 1", ref main_board);
                    Get_Zobrist(ref main_board, ref main_Zobrist);
                    List<string> moves = new List<string>();
                    for (int i = startindex; i < parts.Length; i++)
                    {
                        //Console.WriteLine(i); 
                        moves.Add(parts[i]);
                        //Console.WriteLine(moves[moves.Count - 1]);

                    }
                    List<Move> moveList = new List<Move>();
                    List<Move> Move_to_do = new List<Move>();
                    //Generate_Legal_Moves(ref moveList, ref main_board, false);
                    //Console.Write("                        ");
                    for (int i = 0; i < moves.Count; i++)
                    {
                        string From = moves[i][0].ToString() + moves[i][1].ToString();
                        string To = moves[i][2].ToString() + moves[i][3].ToString();

                        string promo = "";
                        if (moves[i].Length > 4)
                        {
                            promo = moves[i][4].ToString();
                        }

                        Move movetofind = new Move();
                        movetofind.From = Square.GetSquare(From);
                        //Console.WriteLine(CoordinatesToChessNotation(movetofind.From));
                        movetofind.To = Square.GetSquare(To);
                        //Console.WriteLine(CoordinatesToChessNotation(movetofind.To));
                        moveList.Clear();
                        Generate_Legal_Moves(ref moveList, ref main_board, false);
                        //PrintLegalMoves(moveList);
                        for (int j = 0; j < moveList.Count; j++)
                        {
                            //Console.WriteLine("12");
                            nodes = 0;

                            if ((movetofind.From == moveList[j].From) && (movetofind.To == moveList[j].To)) //found same move
                            {



                                if ((moveList[j].Type & knight_promo) != 0) // promo
                                {
                                    if (promo == "q")
                                    {
                                        if ((moveList[j].Type == queen_promo) | (moveList[j].Type == queen_promo_capture))
                                        {
                                            MakeMove(ref main_board, moveList[j], ref main_Zobrist);
                                            break;

                                            //Move_to_do.Add(moveList[j]);
                                        }
                                    }
                                    else if (promo == "r")
                                    {
                                        if ((moveList[j].Type == rook_promo) | (moveList[j].Type == rook_promo_capture))
                                        {
                                            MakeMove(ref main_board, moveList[j], ref main_Zobrist);
                                            break;
                                            //Move_to_do.Add(moveList[j]);
                                        }
                                    }
                                    else if (promo == "b")
                                    {
                                        if ((moveList[j].Type == bishop_promo) | (moveList[j].Type == bishop_promo_capture))
                                        {
                                            MakeMove(ref main_board, moveList[j], ref main_Zobrist);
                                            break;
                                            //Move_to_do.Add(moveList[j]);
                                        }
                                    }
                                    else if (promo == "n")
                                    {
                                        if ((moveList[j].Type == knight_promo) | (moveList[j].Type == knight_promo_capture))
                                        {
                                            MakeMove(ref main_board, moveList[j], ref main_Zobrist);
                                            break;
                                            //Move_to_do.Add(moveList[j]);
                                        }
                                    }
                                }
                                else
                                {
                                    //Console.WriteLine(MoveType[moveList[j].Type]);
                                    //Console.WriteLine(ascii_pieces[moveList[j].Piece]);
                                    // printMove(moveList[j]);
                                    //Console.Write(" ");
                                    //Move_to_do.Add(moveList[j]); 
                                    MakeMove(ref main_board, moveList[j], ref main_Zobrist);
                                    if (isMoveIrreversible(moveList[j]))
                                    {
                                        //Console.WriteLine("aaa");
                                        Repetition_table.Clear();
                                        main_board.halfmove = 0;
                                    }
                                    Repetition_table.Add(main_Zobrist);
                                    main_board.halfmove++;


                                    break;
                                }



                            }
                        }

                        //PrintBoards(main_board);
                    }
                }
                else if(parts[1] == "fen")
                {
                    string fen = "";
                    int i = 2;
                    while (i < parts.Length)
                    {
                        fen += parts[i] + " ";
                        i++;
                    }
                    //Console.WriteLine(fen);
                    parse_fen(fen, ref main_board);

                    Get_Zobrist(ref main_board, ref main_Zobrist);
                    //MakeMove(ref main_board, new Move(Square.d8, Square.d4, capture, Piece.r), ref main_Zobrist);
                }
                else
                {
                    //Console.WriteLine("a");
                    string fen = "";
                    int i = 1;
                    while (i < parts.Length)
                    {
                        fen += parts[i] + " ";
                        i++;
                    }
                    //Console.WriteLine(fen);
                    parse_fen(fen, ref main_board);
                    Get_Zobrist(ref main_board, ref main_Zobrist);
                }




                
               
            }
            else if (main_command == "perft")
            {
                Perft_DEPTH = int.Parse(parts[1]);
                Stopwatch st = new Stopwatch();
                st.Reset();
                st.Start();

                ulong nodes = perft(Perft_DEPTH, ref main_board, main_Zobrist);
                st.Stop();

                float time = (float)st.Elapsed.TotalNanoseconds;
                float timeMS = time / 1000000;
                float NPS = nodes / ((timeMS / 1000));
                float NPSinM = NPS / 1000000;
                

                Console.WriteLine("Nodes: " + nodes + " NPS: " + NPSinM + "M" + " time(MS) " + timeMS);
                //Console.WriteLine(nodes);
                
            }
            else if (main_command == "perfth")
            {
                perft_TTCount = 0;
                Perft_DEPTH = int.Parse(parts[1]);
                Stopwatch st = new Stopwatch();
                st.Reset();
                st.Start();

                ulong nodes = perft_withHash(Perft_DEPTH, ref main_board, main_Zobrist, ref TT_Table_perft);
                st.Stop();

                float time = (float)st.Elapsed.TotalNanoseconds;
                float timeMS = time / 1000000;
                float NPS = nodes / ((timeMS / 1000));
                float NPSinM = NPS / 1000000;


                Console.WriteLine("Nodes: " + nodes + " NPS: " + NPSinM + "M" + " time(MS) " + timeMS + "TThit " + perft_TTCount);
                //Console.WriteLine(nodes);

            }
            else if(main_command == "eval")
            {
                int eval = EvalPos(main_board);

                Console.WriteLine(eval + "cp");
            }
            else if(main_command == "uci")
            {
                Console.WriteLine("id name Turbulence");
                Console.WriteLine("id author ksw0518");
                Console.WriteLine("uciok");

            }
            else if (main_command == "isready")
            {
                Console.WriteLine("readyok");


            }
            else if (main_command == "ucinewgame")
            {
                //Console.WriteLine("readyok");

                parse_fen(start_position, ref main_board);
            }
            else if(main_command == "show")
            {
                PrintBoards(main_board);
                print_mailbox(main_board.mailbox);
            }
            else if (main_command == "printmove")
            {
                List<Move> moves = new List<Move>();
                Generate_Legal_Moves(ref moves, ref main_board, false);
                PrintLegalMoves(moves);
            }
            else if(main_command == "go")
            {
                TT_Table = new Transposition[TT_SIZE];
                Reset_PV();
                IS_SEARCH_STOPPED = false;
                if (parts.Contains("movetime"))
                {
                    THINK_TIME = TryGetLabelledValueInt(input, "movetime", goLabels, 0);
                    int eval = StartSearch(ref main_board, 64, ref pv_length, ref pv_table, main_Zobrist, ref TT_Table, ref Repetition_table);
                }
                else if(parts.Contains("wtime"))
                {
                    int timeRemainingWhiteMs = TryGetLabelledValueInt(input, "wtime", goLabels, 0);
                    int timeRemainingBlackMs = TryGetLabelledValueInt(input, "btime", goLabels, 0);
                    int incrementWhiteMs = TryGetLabelledValueInt(input, "winc", goLabels, 0);
                    int incrementBlackMs = TryGetLabelledValueInt(input, "binc", goLabels, 0);


                    
                    int myRemTime = timeRemainingBlackMs;
                    if(main_board.side == Side.White)
                    {
                        myRemTime = timeRemainingWhiteMs;
                    }
                    int myIncTime = incrementBlackMs;
                    if (main_board.side == Side.White)
                    {
                        myIncTime = incrementWhiteMs;
                    }
                    //Console.WriteLine(timeRemainingWhiteMs);
                    THINK_TIME = ChooseThinkTime(myRemTime, myIncTime);
                    //Console.WriteLine(THINK_TIME);
                    int eval = StartSearch(ref main_board, 64, ref pv_length, ref pv_table, main_Zobrist, ref TT_Table, ref Repetition_table);

                    //LogToFile("Thinking for: " + thinkTime + " ms.");
                    //player.ThinkTimed(thinkTime);
                }
                else if(parts.Contains("depth"))
                {
                    THINK_TIME = 10000000;
                    int depth  = TryGetLabelledValueInt(input, "depth", goLabels, 0);
                    int eval = StartSearch(ref main_board, depth, ref pv_length, ref pv_table, main_Zobrist, ref TT_Table, ref Repetition_table);
                }
            }
            else if(main_command == "quit")
            {
                Environment.Exit(Environment.ExitCode);
            }
        }

        int ChooseThinkTime(int time, int incre)
        {
            
            return (int)(time / 20 + incre / 2);
        }
        static int get_hash_Key(ulong zobrist, int hashsize)
        {
            return (int)(zobrist & (ulong)(hashsize - 1));
        }
        ulong perft(int depth, ref Board board, ulong Zobrist)
        {

            List<Move> movelist = new();
            int n_moves, i;
            ulong nodes = 0;

           

            if (depth == 0) return 1UL;
            //if (depth == 1 && Perft_DEPTH != 1)
            //{
            //    //if () 
            //    Generate_Legal_Moves(ref movelist, ref board, false);
            //    return (ulong)movelist.Count;
            //}

            Generate_Legal_Moves(ref movelist, ref board, false);
            n_moves = movelist.Count;

            for (i = 0; i < n_moves; i++)
            {
                //if(depth == 1)
                //{
                //    if (board.mailbox[Square.c6] == Piece.P)
                //    {
                //        //if (movelist[i].From == Square.b5)
                //        //{
                //        //    PrintBoards(board);
                //        //    print_mailbox(board.mailbox);
                //        //}
                //        printMove(movelist[i]);
                //        Console.Write(": 1\n");
                //    }
                //}
                int lastEp = board.enpassent;
                ulong lastCastle = board.castle;
                int lastside = board.side;
                int captured_piece = board.mailbox[movelist[i].To];
                ulong lastZobrist = Zobrist;
                

                //board.side = 1 - board.side;
                MakeMove(ref board, movelist[i], ref Zobrist);
                ulong added_nodes = perft(depth - 1, ref board, Zobrist);

                ulong zobrish_fordebug = 0;
                //Get_Zobrist(ref board, ref zobrish_fordebug);
                //if(zobrish_fordebug != Zobrist)
                //{
                //    Console.WriteLine("bug");
                //    Console.WriteLine("key should be " );
                //    PrintBitboard(zobrish_fordebug);
                //    Console.WriteLine("key is :" );
                //    PrintBitboard(Zobrist);
                //}
                //if(lastCastle != )
                //if (movelist[i].From == (int)Square.a2 && movelist[i].To == (int)Square.a3 && movelist[i].Type == quiet_move)
                //{
                //    List<Move> test = new();
                //    Generate_Legal_Moves(ref test, ref board);
                //    Console.WriteLine(test.Count);
                //    //PrintBoards(board);
                //}
                if (depth == Perft_DEPTH)
                {


                    printMove(movelist[i]);

                    Console.Write(":" + added_nodes + "\n");
                }

                nodes += added_nodes;
                UnmakeMove(ref board, movelist[i], captured_piece);
                board.enpassent = lastEp;
                board.castle = lastCastle;
                board.side = lastside;
                Zobrist = lastZobrist;

            }


            return nodes;
        }


        ulong perft_withHash(int depth, ref Board board, ulong Zobrist, ref Transposition_Perft[] TT)
        {

            
            int n_moves, i;
            ulong nodes = 0;




            if (depth == 0) return 1UL;
            Transposition_Perft ttval = TT[get_hash_Key(Zobrist, TT_SIZE)];
            //Console.WriteLine("depth" + depth);
            //Console.WriteLine("read" + get_hash_Key(Zobrist, TT_SIZE));
            //Console.WriteLine("read" + Zobrist);
            if (ttval != null)
            {
                //Console.WriteLine("tt_NotEmpty");
                if (ttval.key == Zobrist)
                {
                    if (ttval.ply == depth)
                    {
                        perft_TTCount += ttval.nodecount;
                        //Console.WriteLine("ttCut");
                        return ttval.nodecount;
                    }
                }
            }
            //if (depth == 1 && Perft_DEPTH != 1)
            //{
            //    //if () 
            //    Generate_Legal_Moves(ref movelist, ref board, false);
            //    return (ulong)movelist.Count;
            //}
            List<Move> movelist = new();
            Generate_Legal_Moves(ref movelist, ref board, false);
            n_moves = movelist.Count;

            for (i = 0; i < n_moves; i++)
            {

                int lastEp = board.enpassent;
                ulong lastCastle = board.castle;
                int lastside = board.side;
                int captured_piece = board.mailbox[movelist[i].To];
                ulong lastZobrist = Zobrist;


                //board.side = 1 - board.side;
                MakeMove(ref board, movelist[i], ref Zobrist);


                ulong added_nodes = perft_withHash(depth - 1, ref board, Zobrist, ref TT);
                int hashKey = get_hash_Key(Zobrist, TT_SIZE);

                if (TT[hashKey] == null)
                {
                    TT[hashKey] = new Transposition_Perft
                    {
                        key = Zobrist,
                        nodecount = added_nodes,
                        ply = depth - 1
                    };
                }
                else
                {
                    TT[hashKey].key = Zobrist;
                    TT[hashKey].nodecount = added_nodes;
                    TT[hashKey].ply = depth - 1;
                    //if (TT[hashKey].ply < depth - 1)
                    //{
                    //    TT[hashKey].key = Zobrist;
                    //    TT[hashKey].nodecount = added_nodes;
                    //    TT[hashKey].ply = depth - 1;
                    //}
                }

                //ulong zobrish_fordebug = 0;
                //Get_Zobrist(ref board, ref zobrish_fordebug);
                //if(zobrish_fordebug != Zobrist)
                //{
                //    Console.WriteLine("bug");
                //    Console.WriteLine("key should be " );
                //    PrintBitboard(zobrish_fordebug);
                //    Console.WriteLine("key is :" );
                //    PrintBitboard(Zobrist);
                //}
                //if(lastCastle != )
                //if (movelist[i].From == (int)Square.a2 && movelist[i].To == (int)Square.a3 && movelist[i].Type == quiet_move)
                //{
                //    List<Move> test = new();
                //    Generate_Legal_Moves(ref test, ref board);
                //    Console.WriteLine(test.Count);
                //    //PrintBoards(board);
                //}
                if (depth == Perft_DEPTH)
                {


                    printMove(movelist[i]);

                    Console.Write(":" + added_nodes + "\n");
                }

                nodes += added_nodes;
                UnmakeMove(ref board, movelist[i], captured_piece);
                board.enpassent = lastEp;
                board.castle = lastCastle;
                board.side = lastside;
                Zobrist = lastZobrist;

            }


            return nodes;
        }
        static int TryGetLabelledValueInt(string text, string label, string[] allLabels, int defaultValue = 0)
        {
            string valueString = TryGetLabelledValue(text, label, allLabels, defaultValue + "");
            if (int.TryParse(valueString.Split(' ')[0], out int result))
            {
                return result;
            }
            return defaultValue;
        }

        static string TryGetLabelledValue(string text, string label, string[] allLabels, string defaultValue = "")
        {
            text = text.Trim();
            if (text.Contains(label))
            {
                int valueStart = text.IndexOf(label) + label.Length;
                int valueEnd = text.Length;
                foreach (string otherID in allLabels)
                {
                    if (otherID != label && text.Contains(otherID))
                    {
                        int otherIDStartIndex = text.IndexOf(otherID);
                        if (otherIDStartIndex > valueStart && otherIDStartIndex < valueEnd)
                        {
                            valueEnd = otherIDStartIndex;
                        }
                    }
                }

                return text.Substring(valueStart, valueEnd - valueStart).Trim();
            }
            return defaultValue;
        }

        static void Reset_PV()
        {
            for(int i = 0; i < pv_length.Length; i++)
            {
                pv_length[i] = 0;
            }
            for (int i = 0; i < pv_table.Length; i++)
            {
                pv_table[i] = new Move[64];
            }
        }
        static void InitAll()
        {
            InitializeBetweenTable();
            InitializeLeaper();
            init_sliders_attacks(1);
            init_sliders_attacks(0);

            for(int i = 0; i < 64; i++)
            {
                pv_table[i] = new Move[64];
            }
            MVVLVA_T[0] = new int[] { 6002, 20225, 20250, 20400, 20800, 26900 };
            MVVLVA_T[1] = new int[] { 4775, 6004, 20025, 20175, 20575, 26675 };
            MVVLVA_T[2] = new int[] { 4750,  4975,  6006 ,20150, 20550 ,26650 };
            MVVLVA_T[3] = new int[] { 4600,  4825,  4850 , 6008, 20400, 26500 };
            MVVLVA_T[4] = new int[] { 4200,  4425 , 4450 , 4600 , 6010 ,26100 };
            MVVLVA_T[5] = new int[] { 3100 , 3325,  3350 , 3500,  3900 ,26000 };


            
            Random rnd = new Random(1);
            //Console.WriteLine(rnd.NextInt64());
            SIDE = (ulong)rnd.NextInt64();



            for(int i = 0; i < 4; i++)
            {
                W_CASTLING_RIGHTS[i] = (ulong)rnd.NextInt64();
                B_CASTLING_RIGHTS[i] = (ulong)rnd.NextInt64();
            }
            for (int i = 0; i < 12; i++)
            {
                PIECES[i] = new ulong[64];

                
                for (int k = 0; k < 64; k++)
                {
                    PIECES[i][k] = (ulong)rnd.NextInt64();
                    EN_passent[k] = (ulong)rnd.NextInt64();
                }
            }
                //Console.WriteLine(i);
            

        }

    }
}

# Turbulence

------Turbulence chess engine------
- Turbulence is a still-in-develop, uci chess engine in c#

- [lichess link](https://lichess.org/@/turbulencebot) 

- still in development

## approximate NPS(node per second)
nps(at startpos)
- perft nps without bulk = ~15mnps
- perft nps with bulk = ~50mnps
- search nps = ~1.5mnps


## applied techniques
### Move generation
- bitboard
- magic bitboard
- pawn,knight,king precalculated attack table
- makemove,unmakemove func

### Search
- negamax
- alpha,beta pruning
- transposition table (needs a fix)
- killer move heuristic
- MVVLVA move ordering

### Eval
- Tapered Eval
- piece square table


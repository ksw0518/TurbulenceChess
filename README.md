# Turbulence

------Turbulence chess engine------
- Turbulence is a still-in-develop, uci chess engine in `c#`
- it's really terrible probably you can even beat it

- [lichess link](https://lichess.org/@/turbulencebot) 

- still in development

## it's really terrible probably you can even beat it
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


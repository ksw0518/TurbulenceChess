using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Turbulence
{
    public static class BitManipulation
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int getFile(int square)
        {
            return square % 8;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int getRank(int square)
        {
            return square != 0 ? 7 - square / 8 : 7;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong set_occupancy(int index, int bits_in_mask, ulong attack_mask)
        {
            ulong occupancy = 0;

            for (int count = 0; count < bits_in_mask; count++)
            {
                int square = get_ls1b(attack_mask);

                Pop_bit(ref attack_mask, square);
                if ((index & (1 << count)) != 0)
                {
                    occupancy |= (1UL << square);
                }
            }
            return occupancy;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Get_bit(ulong bit, int a)
        {
            return ((bit & (1UL << a)) != 0);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Set_bit(ref ulong bit, int a)
        {
            bit |= 1UL << a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Pop_bit(ref ulong bit, int a)
        {
            // Create a mask for the a-th bit
            ulong mask = 1UL << a;

            // Check if the a-th bit is set and clear it if so
            bit &= ~mask;
            //bit |= 1UL << a;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int count_bits(ulong bitboard)
        {
            return (int)ulong.PopCount(bitboard);
            

            
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int get_ls1b(ulong bitboard)
        {
            //return count_bits((bitboard & 0 - bitboard) - 1);
            //if (bitboard == 0UL) Console.WriteLine("fuked up");
            return (int)ulong.TrailingZeroCount(bitboard);

        }
    }
}

﻿using System;

namespace Engine.Core
{
    /// <summary>
    /// Taken from https://github.com/ColeDeanShepherd/TESUnity/blob/f4d5e19f68da380da9da745356c7904f3428b9d6/Assets/Scripts/Core/Utils.cs
    /// </summary>
    public static class Utils
    {
        public static void Swap<T>(ref T a, ref T b)
        {
            (a, b) = (b, a);
        }

        /// <summary>
        /// Checks if a bit string (an unsigned integer) contains a collection of bit flags.
        /// </summary>
        public static bool ContainsBitFlags(uint bitString, params uint[] bitFlags)
        {
            // Construct a bit string containing all the bit flags.
            uint allBitFlags = 0;

            foreach (var bitFlag in bitFlags)
            {
                allBitFlags |= bitFlag;
            }

            // Check if the bit string contains all the bit flags.
            return (bitString & allBitFlags) == allBitFlags;
        }

        /// <summary>
        /// Extracts a range of bits from a byte array.
        /// </summary>
        /// <param name="bitOffset">An offset in bits from the most significant bit (byte 0, bit 0) of the byte array.</param>
        /// <param name="bitCount">The number of bits to extract. Cannot exceed 64.</param>
        /// <param name="bytes">A big-endian byte array.</param>
        /// <returns>A ulong containing the right-shifted extracted bits.</returns>
        public static ulong GetBits(uint bitOffset, uint bitCount, byte[] bytes)
        {
            System.Diagnostics.Debug.Assert((bitCount <= 64) && ((bitOffset + bitCount) <= (8 * bytes.Length)));

            ulong bits = 0;
            var remainingBitCount = bitCount;
            var byteIndex = bitOffset / 8;
            var bitIndex = bitOffset - (8 * byteIndex);

            while (remainingBitCount > 0)
            {
                // Read bits from the byte array.
                var numBitsLeftInByte = 8 - bitIndex;
                var numBitsReadNow = Math.Min(remainingBitCount, numBitsLeftInByte);
                var unmaskedBits = (uint)bytes[byteIndex] >> (int)(8 - (bitIndex + numBitsReadNow));
                var bitMask = 0xFFu >> (int)(8 - numBitsReadNow);
                uint bitsReadNow = unmaskedBits & bitMask;

                // Store the bits we read.
                bits <<= (int)numBitsReadNow;
                bits |= bitsReadNow;

                // Prepare for the next iteration.
                bitIndex += numBitsReadNow;

                if (bitIndex == 8)
                {
                    byteIndex++;
                    bitIndex = 0;
                }

                remainingBitCount -= numBitsReadNow;
            }

            return bits;
        }

        /// <summary>
        /// Transforms x from an element of [min0, max0] to an element of [min1, max1].
        /// </summary>
        public static float ChangeRange(float x, float min0, float max0, float min1, float max1)
        {
            System.Diagnostics.Debug.Assert((min0 <= max0) && (min1 <= max1) && (x >= min0) && (x <= max0));

            var range0 = max0 - min0;
            var range1 = max1 - min1;

            var xPct = (x - min0) / range0;

            return min1 + (xPct * range1);
        }

        /// <summary>
        /// Calculates the minimum and maximum values of a 2D array.
        /// </summary>
        public static void GetExtrema(float[,] array, out float min, out float max)
        {
            min = float.MaxValue;
            max = float.MinValue;

            foreach (var element in array)
            {
                min = Math.Min(min, element);
                max = Math.Max(max, element);
            }
        }
    }
}
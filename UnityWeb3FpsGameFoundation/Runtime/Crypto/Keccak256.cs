using System;
using System.Text;

namespace Web3Fps.GameFoundation.Crypto
{
    /// <summary>Dependency-free Ethereum keccak256 (not FIPS SHA3-256).</summary>
    public static class Keccak256
    {
        private const int RateBytes = 136;

        private static readonly ulong[] RoundConstants =
        {
            0x0000000000000001UL, 0x0000000000008082UL, 0x800000000000808AUL,
            0x8000000080008000UL, 0x000000000000808BUL, 0x0000000080000001UL,
            0x8000000080008081UL, 0x8000000000008009UL, 0x000000000000008AUL,
            0x0000000000000088UL, 0x0000000080008009UL, 0x000000008000000AUL,
            0x000000008000808BUL, 0x800000000000008BUL, 0x8000000000008089UL,
            0x8000000000008003UL, 0x8000000000008002UL, 0x8000000000000080UL,
            0x000000000000800AUL, 0x800000008000000AUL, 0x8000000080008081UL,
            0x8000000000008080UL, 0x0000000080000001UL, 0x8000000080008008UL
        };

        private static readonly int[] Rotation =
        {
             0,  1, 62, 28, 27,
            36, 44,  6, 55, 20,
             3, 10, 43, 25, 39,
            41, 45, 15, 21,  8,
            18,  2, 61, 56, 14
        };

        public static byte[] ComputeHash(byte[] data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var state = new ulong[25];
            var offset = 0;

            while (data.Length - offset >= RateBytes)
            {
                AbsorbBlock(state, data, offset);
                Permute(state);
                offset += RateBytes;
            }

            var finalBlock = new byte[RateBytes];
            var remaining = data.Length - offset;
            if (remaining > 0) Buffer.BlockCopy(data, offset, finalBlock, 0, remaining);
            finalBlock[remaining] = 0x01; // Keccak domain suffix.
            finalBlock[RateBytes - 1] |= 0x80;
            AbsorbBlock(state, finalBlock, 0);
            Permute(state);

            var output = new byte[32];
            for (var i = 0; i < output.Length; i++)
                output[i] = (byte)(state[i / 8] >> (8 * (i % 8)));
            return output;
        }

        public static string ComputeHex(byte[] data, bool withPrefix = true)
        {
            var hash = ComputeHash(data);
            var builder = new StringBuilder(withPrefix ? 66 : 64);
            if (withPrefix) builder.Append("0x");
            for (var i = 0; i < hash.Length; i++) builder.Append(hash[i].ToString("x2"));
            return builder.ToString();
        }

        public static string ComputeUtf8Hex(string value, bool withPrefix = true)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            return ComputeHex(Encoding.UTF8.GetBytes(value), withPrefix);
        }

        public static bool Verify(byte[] data, string expectedHex)
        {
            if (data == null || string.IsNullOrWhiteSpace(expectedHex)) return false;
            var expected = NormalizeHex(expectedHex);
            var actual = ComputeHex(data, false);
            if (expected.Length != actual.Length) return false;
            var diff = 0;
            for (var i = 0; i < actual.Length; i++) diff |= actual[i] ^ expected[i];
            return diff == 0;
        }

        private static string NormalizeHex(string value)
        {
            var trimmed = value.Trim();
            if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) trimmed = trimmed.Substring(2);
            return trimmed.ToLowerInvariant();
        }

        private static void AbsorbBlock(ulong[] state, byte[] block, int offset)
        {
            for (var lane = 0; lane < RateBytes / 8; lane++)
            {
                ulong value = 0;
                for (var b = 0; b < 8; b++) value |= (ulong)block[offset + lane * 8 + b] << (8 * b);
                state[lane] ^= value;
            }
        }

        private static void Permute(ulong[] state)
        {
            var c = new ulong[5];
            var d = new ulong[5];
            var b = new ulong[25];

            for (var round = 0; round < 24; round++)
            {
                for (var x = 0; x < 5; x++)
                    c[x] = state[x] ^ state[x + 5] ^ state[x + 10] ^ state[x + 15] ^ state[x + 20];
                for (var x = 0; x < 5; x++) d[x] = c[(x + 4) % 5] ^ RotateLeft(c[(x + 1) % 5], 1);
                for (var y = 0; y < 5; y++)
                    for (var x = 0; x < 5; x++) state[x + 5 * y] ^= d[x];

                for (var y = 0; y < 5; y++)
                {
                    for (var x = 0; x < 5; x++)
                    {
                        var destinationX = y;
                        var destinationY = (2 * x + 3 * y) % 5;
                        b[destinationX + 5 * destinationY] = RotateLeft(state[x + 5 * y], Rotation[x + 5 * y]);
                    }
                }

                for (var y = 0; y < 5; y++)
                    for (var x = 0; x < 5; x++)
                        state[x + 5 * y] = b[x + 5 * y] ^
                            ((~b[(x + 1) % 5 + 5 * y]) & b[(x + 2) % 5 + 5 * y]);

                state[0] ^= RoundConstants[round];
            }
        }

        private static ulong RotateLeft(ulong value, int shift)
        {
            if (shift == 0) return value;
            return (value << shift) | (value >> (64 - shift));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace R2API.Utils {
    internal static class CompressedFlagArrayUtilities {
        public const byte flagsPerValue = 8;
        public const byte valuesPerBlock = 18;
        public const byte flagsPerSection = flagsPerValue * valuesPerBlock;
        public const byte sectionsCount = 8;
        public const byte blockPartsCount = 4;

        private const uint blockValuesMask = 0b_00111111_00011111_00001111_00000111;
        private const uint fullBlockHeader = 0b_00000000_01000000_01100000_01110000;

        private const uint block1HeaderMask = 0b_01000000;
        private const uint block2HeaderMask = 0b_01100000;
        private const uint block3HeaderMask = 0b_01110000;
        private const uint block4HeaderMask = 0b_01111000;

        private const uint block1HeaderXor = 0b_00000000;
        private const uint block2HeaderXor = 0b_01000000;
        private const uint block3HeaderXor = 0b_01100000;
        private const uint block4HeaderXor = 0b_01110000;

        private const uint highestBitInInt = 0b_10000000_00000000_00000000_00000000;
        private const uint highestBitInByte = 0b_10000000;

        public static void Add(ref byte[] values, int index) {
            var valueIndex = index / flagsPerValue;
            var flagIndex = index % flagsPerValue;

            ResizeOrInitIfNeeded(ref values, valueIndex);
            values[valueIndex] = (byte)(values[valueIndex] | highestBitInByte >> flagIndex);
        }

        public static bool Remove(ref byte[] values, int index) {
            var valueIndex = index / flagsPerValue;
            var flagIndex = index % flagsPerValue;

            if (values == null || valueIndex >= values.Length || valueIndex < 0) {
                return false;
            }

            values[valueIndex] = (byte)(values[valueIndex] & ~(highestBitInByte >> flagIndex));
            DownsizeOrNullIfNeeded(ref values);

            return true;
        }

        public static bool Has(byte[] values, int index) {
            var valueIndex = index / flagsPerValue;
            var flagIndex = index % flagsPerValue;

            if (values == null || valueIndex >= values.Length) {
                return false;
            }

            return (values[valueIndex] & (highestBitInByte >> flagIndex)) != 0;
        }

        /// <summary>
        /// Reads compressed value from the NerworkReader. More info about that can be found in the PR: https://github.com/risk-of-thunder/R2API/pull/284
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static byte[] ReadFromNetworkReader(NetworkReader reader) {
            var sectionByte = reader.ReadByte();
            if (sectionByte == 0) {
                return null;
            }

            var values = new byte[0];

            for (var i = 0; i < 8; i++) {
                if ((sectionByte & 1 << i) != 0) {
                    ReadBlock(ref values, reader, i);
                }
            }

            return values;
        }

        /// <summary>
        /// Reads compressed value from the NerworkReader. More info about that can be found in the PR: https://github.com/risk-of-thunder/R2API/pull/284
        /// </summary>
        /// <param name="values"></param>
        /// <param name="writer"></param>
        public static void WriteToNetworkWriter(byte[] values, NetworkWriter writer) {
            int section = 0;
            for (var i = 0; i < sectionsCount; i++) {
                if (!IsBlockEmpty(values, i)) {
                    section |= 1 << i;
                }
            }
            writer.Write((byte)section);

            for (var i = 0; i < sectionsCount; i++) {
                if (!IsBlockEmpty(values, i)) {
                    WriteBlock(values, writer, i);
                }
            }
        }


        private static void ReadBlock(ref byte[] values, NetworkReader reader, int blockIndex) {
            var fullBlockMask = new FullBlockMask();

            var maskIndex = 0;
            while (true) {
                var blockBytes = reader.ReadByte();
                uint mask, xor;
                do {
                    (mask, xor) = GetMask(maskIndex++);
                }
                while ((blockBytes & mask ^ xor) != 0);

                fullBlockMask[maskIndex - 1] = blockBytes;

                if ((blockBytes & highestBitInByte) != 0) {
                    break;
                }
            }

            var bitesSkipped = 0;
            for (var i = 0; i < 32; i++) {
                if ((blockValuesMask & highestBitInInt >> i) == 0) {
                    bitesSkipped++;
                    continue;
                }
                if ((fullBlockMask.integer & highestBitInInt >> i) != 0) {
                    var valueIndex = (blockIndex * valuesPerBlock) + i - bitesSkipped;
                    ResizeOrInitIfNeeded(ref values, valueIndex);
                    values[valueIndex] = reader.ReadByte();
                }
            }
        }

        private static void ResizeOrInitIfNeeded(ref byte[] values, int valueIndex) {
            if (values == null) {
                values = new byte[valueIndex + 1];
            }
            if (valueIndex >= values.Length) {
                Array.Resize(ref values, valueIndex + 1);
            }
        }

        private static void DownsizeOrNullIfNeeded(ref byte[] value) {
            if (value == null || value.Length == 0) {
                return;
            }
            if (value[value.Length - 1] != 0) {
                return;
            }
            for (var i = value.Length - 2; i >= 0; i--) {
                if (value[i] != 0) {
                    Array.Resize(ref value, i + 1);
                    return;
                }
            }
            value = null;
        }

        private static void WriteBlock(byte[] values, NetworkWriter writer, int blockIndex) {
            var bitesSkipped = 0;
            var fullBlockMask = new FullBlockMask();
            var orderedValues = new List<byte>();
            for (var i = 0; i < 32; i++) {
                fullBlockMask.integer <<= 1;
                if ((blockValuesMask & highestBitInInt >> i) == 0) {
                    bitesSkipped++;
                    continue;
                }
                var valueIndex = blockIndex * valuesPerBlock + (i - bitesSkipped);
                if (valueIndex >= values.Length) {
                    continue;
                }
                if (values[valueIndex] != 0) {
                    fullBlockMask.integer |= 1;
                    orderedValues.Add(values[valueIndex]);
                }
            }
            var lastIndex = 0;
            for (var i = 0; i < blockPartsCount; i++) {
                if (fullBlockMask[i] != 0) {
                    lastIndex = i;
                }
            }

            fullBlockMask.integer |= fullBlockHeader;
            fullBlockMask[lastIndex] = (byte)(fullBlockMask[lastIndex] | highestBitInByte);

            for (var i = 0; i <= lastIndex; i++) {
                var (headerMask, _) = GetMask(i);
                if ((fullBlockMask[i] & (~headerMask)) != 0) {
                    writer.Write(fullBlockMask[i]);
                }
            }
            foreach (var value in orderedValues) {
                writer.Write(value);
            }
        }

        private static bool IsBlockEmpty(byte[] values, int blockIndex) {
            if (values == null || values.Length == 0 || values.Length / valuesPerBlock < blockIndex) {
                return true;
            }

            for (var i = blockIndex * valuesPerBlock; i < Math.Min((blockIndex + 1) * valuesPerBlock, values.Length); i++) {
                if (values[i] != 0) {
                    return false;
                }
            }

            return true;
        }

        private static (uint mask, uint xor) GetMask(int i) {
            return i switch {
                0 => (block1HeaderMask, block1HeaderXor),
                1 => (block2HeaderMask, block2HeaderXor),
                2 => (block3HeaderMask, block3HeaderXor),
                3 => (block4HeaderMask, block4HeaderXor),
                _ => throw new IndexOutOfRangeException()
            };
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FullBlockMask {
            [FieldOffset(3)]
            public byte byte0;
            [FieldOffset(2)]
            public byte byte1;
            [FieldOffset(1)]
            public byte byte2;
            [FieldOffset(0)]
            public byte byte3;

            [FieldOffset(0)]
            public uint integer;

            public byte this[int i] {
                get {
                    return i switch {
                        0 => byte0,
                        1 => byte1,
                        2 => byte2,
                        3 => byte3,
                        _ => throw new IndexOutOfRangeException(),
                    };
                }
                set {
                    switch (i) {
                        case 0:
                            byte0 = value;
                            break;
                        case 1:
                            byte1 = value;
                            break;
                        case 2:
                            byte2 = value;
                            break;
                        case 3:
                            byte3 = value;
                            break;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
            }
        }

    }
}

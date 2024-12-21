using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine.Networking;

namespace R2API.Utils;

internal static class CompressedFlagArrayUtilities
{
    private static readonly byte[] tempBlockValues = new byte[valuesPerBlock];
    private static readonly int[] tempBlockPartValuesCounts = new int[blockPartsCount];

    public const byte flagsPerValue = 8;
    public const byte valuesPerBlock = 18;
    public const byte flagsPerSection = flagsPerValue * valuesPerBlock;
    public const byte sectionsCount = 8;
    public const byte blockPartsCount = 4;

    private const uint fullBlockHeader = block1HeaderXor << 24 | block2HeaderXor << 16 | block3HeaderXor << 8 | block4HeaderXor;

    private const uint block1HeaderMask = 0b_01000000;
    private const uint block2HeaderMask = 0b_01100000;
    private const uint block3HeaderMask = 0b_01110000;
    private const uint block4HeaderMask = 0b_01111000;

    private const uint block1HeaderXor = 0b_00000000;
    private const uint block2HeaderXor = 0b_01000000;
    private const uint block3HeaderXor = 0b_01100000;
    private const uint block4HeaderXor = 0b_01110000;

    private const int block1HeaderSkip = 2;
    private const int block2HeaderSkip = 3;
    private const int block3HeaderSkip = 4;
    private const int block4HeaderSkip = 5;

    private const int block1HeaderValuesCount = 8 - block1HeaderSkip;
    private const int block2HeaderValuesCount = 8 - block2HeaderSkip;
    private const int block3HeaderValuesCount = 8 - block3HeaderSkip;
    private const int block4HeaderValuesCount = 8 - block4HeaderSkip;

    private const int block1HeaderOffset = block1HeaderValuesCount - 8;
    private const int block2HeaderOffset = block1HeaderOffset + block2HeaderValuesCount;
    private const int block3HeaderOffset = block2HeaderOffset + block3HeaderValuesCount;
    private const int block4HeaderOffset = block3HeaderOffset + block4HeaderValuesCount;

    private const uint highestBitInByte = 0b_10000000;

    public static void Add(ref byte[] values, int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var valueIndex = index / flagsPerValue;
        var flagIndex = index - (valueIndex * flagsPerValue);

        ResizeIfNeeded(ref values, valueIndex);
        values[valueIndex] = (byte)(values[valueIndex] | highestBitInByte >> flagIndex);
    }

    public static void AddImmutable(ref byte[] values, int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var valueIndex = index / flagsPerValue;
        var flagIndex = index - (valueIndex * flagsPerValue);

        if (!ResizeIfNeeded(ref values, valueIndex))
        {
            var newValue = (byte)(values[valueIndex] | highestBitInByte >> flagIndex);
            if (values[valueIndex] == newValue)
            {
                return;
            }

            var oldValues = values;
            values = new byte[values.Length];
            oldValues.CopyTo(values, 0);
            values[valueIndex] = newValue;
        }
        else
        {
            var newValue = (byte)(highestBitInByte >> flagIndex);
            values[valueIndex] = newValue;
        }
    }

    public static void Add(ref byte[] values, byte[] operand){
        ResizeIfNeeded(ref values,operand.Length);
        for(int i = 0 ; i < operand.Length ; i++){
          values[i] |= operand[i];
        }
    }

    public static bool Remove(ref byte[] values, int index)
    {
        if (index < 0)
        {
            return false;
        }

        var valueIndex = index / flagsPerValue;
        if (valueIndex >= values.Length)
        {
            return false;
        }

        var flagIndex = index - (valueIndex * flagsPerValue);
        values[valueIndex] = (byte)(values[valueIndex] & ~(highestBitInByte >> flagIndex));
        DownsizeIfNeeded(ref values);

        return true;
    }

    public static bool RemoveImmutable(ref byte[] values, int index)
    {
        if (index < 0 || values is null)
        {
            return false;
        }

        var valueIndex = index / flagsPerValue;
        if (valueIndex >= values.Length)
        {
            return false;
        }

        var flagIndex = index - (valueIndex * flagsPerValue);
        var newValue = (byte)(values[valueIndex] & ~(highestBitInByte >> flagIndex));
        if (values[valueIndex] == newValue)
        {
            return true;
        }

        if (newValue == 0)
        {
            DownsizeIgnoreLast(ref values);
            if (values.Length == 0)
            {
                values = null;
            }
        }
        else
        {
            var oldValues = values;
            values = new byte[values.Length];
            oldValues.CopyTo(values, 0);
            values[valueIndex] = newValue;
        }

        return true;
    }

    public static bool Remove(ref byte[] values, byte[] operand){
        bool result = false;
        int length = Math.Min(values.Length,operand.Length);
        for(int i = 0 ; i < length ; i++){
           result |= ((values[i] & operand[i]) != Byte.MinValue);
           values[i] &= (byte)(~operand[i]);
        }
        DownsizeIfNeeded(ref values);
        return result;
    }

    public static bool Has(byte[] values, int index)
    {
        if (index < 0 || values is null)
        {
            return false;
        }

        var valueIndex = index / flagsPerValue;
        if (valueIndex >= values.Length)
        {
            return false;
        }

        var flagIndex = index - (valueIndex * flagsPerValue);

        return (values[valueIndex] & highestBitInByte >> flagIndex) != 0;
    }

    /// <summary>
    /// Reads compressed value from the NetworkReader.
    /// More info about that can be found in the PRs:
    /// https://github.com/risk-of-thunder/R2API/pull/284
    /// https://github.com/risk-of-thunder/R2API/pull/464
    /// </summary>
    /// <param name="reader"></param>
    /// <param name="maxValue"></param>
    /// <returns></returns>
    public static byte[] ReadFromNetworkReader(NetworkReader reader, int maxValue)
    {
        var values = Array.Empty<byte>();

        var sectionByte = reader.ReadByte();
        if (sectionByte == 0)
        {
            return values;
        }

        if (maxValue <= 8)
        {
            return new[] { sectionByte };
        }

        if (maxValue <= 64)
        {
            var lastValueIndex = 0;
            for (var i = 0; i < sectionsCount; i++)
            {
                if ((sectionByte & (1 << i)) != 0)
                {
                    lastValueIndex = i;
                    tempBlockValues[i] = reader.ReadByte();
                }
                else
                {
                    tempBlockValues[i] = 0;
                }
            }

            values = new byte[lastValueIndex + 1];
            Array.Copy(tempBlockValues, 0, values, 0, lastValueIndex + 1);

            return values;
        }

        for (var i = 0; i < sectionsCount; i++)
        {
            if ((sectionByte & (1 << i)) != 0)
            {
                ReadBlock(ref values, reader, i);
            }
        }

        return values;
    }

    /// <summary>
    /// Reads compressed value from the NetworkReader.
    /// More info about that can be found in the PRs:
    /// https://github.com/risk-of-thunder/R2API/pull/284
    /// https://github.com/risk-of-thunder/R2API/pull/464
    /// <param name="values"></param>
    /// <param name="writer"></param>
    /// <param name="maxValue"></param>
    public static void WriteToNetworkWriter(byte[] values, NetworkWriter writer, int maxValue)
    {
        var section = 0;

        if (maxValue <= 8)
        {
            writer.Write(values.Length == 0 ? (byte)0 : values[0]);
            return;
        }

        if (maxValue <= 64)
        {
            var valueCount = 0;
            for (var i = Math.Min(sectionsCount, values.Length) - 1; i >= 0; i--)
            {
                section <<= 1;
                if (values[i] != 0)
                {
                    tempBlockValues[valueCount++] = values[i];
                    section |= 1;
                }
            }

            writer.Write((byte)section);
            for (var i = valueCount - 1; i >= 0; i--)
            {
                writer.Write(tempBlockValues[i]);
            }
            return;
        }

        for (var i = 0; i < sectionsCount; i++)
        {
            if (!IsBlockEmpty(values, i, out var end))
            {
                section |= 1 << i;
            }
            if (end)
            {
                break;
            }
        }

        writer.Write((byte)section);

        for (var i = 0; i < sectionsCount; i++)
        {
            if ((section & (1 << i)) > 0)
            {
                WriteBlock(values, writer, i);
            }
        }
    }

    private static void ReadBlock(ref byte[] values, NetworkReader reader, int blockIndex)
    {
        Array.Clear(tempBlockValues, 0, valuesPerBlock);
        var maskIndex = 0;
        var lastValueIndex = 0;
        while (true)
        {
            var blockByte = reader.ReadByte();
            var (mask, xor, skip, offset) = GetMaskValues(maskIndex);
            while ((blockByte & mask ^ xor) != 0)
            {
                (mask, xor, skip, offset) = GetMaskValues(++maskIndex);
            }

            ReadBlockValues(ref lastValueIndex, offset, skip, 8, reader, blockByte);

            if ((blockByte & highestBitInByte) != 0)
            {
                break;
            }
        }

        ResizeIfNeeded(ref values, blockIndex * valuesPerBlock + lastValueIndex);
        Array.Copy(tempBlockValues, 0, values, blockIndex * valuesPerBlock, lastValueIndex + 1);
    }

    private static void ReadBlockValues(ref int lastValueIndex, int valueBitesOffset, int fromIndex, int toIndex, NetworkReader reader, byte blockByte)
    {
        for (var i = fromIndex; i < toIndex; i++)
        {
            if ((blockByte & highestBitInByte >> i) != 0)
            {
                lastValueIndex = i + valueBitesOffset;
                tempBlockValues[lastValueIndex] = reader.ReadByte();
            }
        }
    }

    private static bool ResizeIfNeeded(ref byte[] values, int valueIndex)
    {
        if (values is null)
        {
            values = new byte[valueIndex + 1];
            return true;
        }
        else if (valueIndex >= values.Length)
        {
            Array.Resize(ref values, valueIndex + 1);
            return true;
        }

        return false;
    }

    private static void DownsizeIfNeeded(ref byte[] value)
    {
        if (value.Length == 0 || value[value.Length - 1] != 0)
        {
            return;
        }
        DownsizeIgnoreLast(ref value);
    }

    private static void DownsizeIgnoreLast(ref byte[] value)
    {
        for (var i = value.Length - 2; i >= 0; i--)
        {
            if (value[i] != 0)
            {
                Array.Resize(ref value, i + 1);
                return;
            }
        }
        value = Array.Empty<byte>();
    }

    private static void WriteBlock(byte[] values, NetworkWriter writer, int blockIndex)
    {
        var blockValuesCount = 0;
        var fullBlockMask = new FullBlockMask();

        PrepareBlockValues(values, blockIndex, 0, 6, ref blockValuesCount, ref fullBlockMask);
        tempBlockPartValuesCounts[0] = blockValuesCount;
        fullBlockMask.integer <<= block2HeaderSkip;
        PrepareBlockValues(values, blockIndex, 6, 11, ref blockValuesCount, ref fullBlockMask);
        tempBlockPartValuesCounts[1] = blockValuesCount;
        fullBlockMask.integer <<= block3HeaderSkip;
        PrepareBlockValues(values, blockIndex, 11, 15, ref blockValuesCount, ref fullBlockMask);
        tempBlockPartValuesCounts[2] = blockValuesCount;
        fullBlockMask.integer <<= block4HeaderSkip;
        PrepareBlockValues(values, blockIndex, 15, 18, ref blockValuesCount, ref fullBlockMask);
        tempBlockPartValuesCounts[3] = blockValuesCount;

        var lastIndex = 0;
        for (var i = blockPartsCount - 1; i > 0; i--)
        {
            if (fullBlockMask[i] != 0)
            {
                lastIndex = i;
                break;
            }
        }

        fullBlockMask.integer |= fullBlockHeader;
        fullBlockMask[lastIndex] |= (byte)highestBitInByte;

        var valueIndex = 0;
        for (var i = 0; i <= lastIndex; i++)
        {
            if (valueIndex == tempBlockPartValuesCounts[i])
            {
                continue;
            }

            writer.Write(fullBlockMask[i]);
            for (; valueIndex < tempBlockPartValuesCounts[i]; valueIndex++)
            {
                writer.Write(tempBlockValues[valueIndex]);
            }
        }
    }

    private static void PrepareBlockValues(byte[] values, int blockIndex, int fromIndex, int toIndex, ref int blockValuesCount, ref FullBlockMask fullBlockMask)
    {
        for (var i = fromIndex; i < toIndex; i++)
        {
            fullBlockMask.integer <<= 1;
            var valueIndex = blockIndex * valuesPerBlock + i;
            if (valueIndex >= values.Length)
            {
                continue;
            }
            if (values[valueIndex] != 0)
            {
                fullBlockMask.integer |= 1;
                tempBlockValues[blockValuesCount++] = values[valueIndex];
            }
        }
    }

    private static bool IsBlockEmpty(byte[] values, int blockIndex, out bool end)
    {
        if (values.Length == 0 || values.Length / valuesPerBlock < blockIndex)
        {
            end = true;
            return true;
        }
        end = false;

        var lastIndex = Math.Min((blockIndex + 1) * valuesPerBlock, values.Length);
        for (var i = blockIndex * valuesPerBlock; i < lastIndex; i++)
        {
            if (values[i] != 0)
            {
                return false;
            }
        }

        return true;
    }

    private static (uint mask, uint xor, int skip, int offset) GetMaskValues(int i)
    {
        return i switch
        {
            0 => (block1HeaderMask, block1HeaderXor, block1HeaderSkip, block1HeaderOffset),
            1 => (block2HeaderMask, block2HeaderXor, block2HeaderSkip, block2HeaderOffset),
            2 => (block3HeaderMask, block3HeaderXor, block3HeaderSkip, block3HeaderOffset),
            3 => (block4HeaderMask, block4HeaderXor, block4HeaderSkip, block4HeaderOffset),
            _ => throw new IndexOutOfRangeException()
        };
    }

    [StructLayout(LayoutKind.Explicit)]
    [DebuggerDisplay("{ToString()}")]
    private ref struct FullBlockMask
    {
        [FieldOffset(3)]
        private byte byte0;
        [FieldOffset(2)]
        private byte byte1;
        [FieldOffset(1)]
        private byte byte2;
        [FieldOffset(0)]
        private byte byte3;

        [FieldOffset(0)]
        public uint integer;

        public byte this[int i]
        {
            get
            {
                if (BitConverter.IsLittleEndian)
                {
                    return i switch
                    {
                        0 => byte0,
                        1 => byte1,
                        2 => byte2,
                        3 => byte3,
                        _ => throw new IndexOutOfRangeException(),
                    };
                }
                else
                {
                    return i switch
                    {
                        0 => byte3,
                        1 => byte2,
                        2 => byte1,
                        3 => byte0,
                        _ => throw new IndexOutOfRangeException(),
                    };
                }
            }
            set
            {
                if (BitConverter.IsLittleEndian)
                {
                    switch (i)
                    {
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
                else
                {
                    switch (i)
                    {
                        case 0:
                            byte3 = value;
                            break;
                        case 1:
                            byte2 = value;
                            break;
                        case 2:
                            byte1 = value;
                            break;
                        case 3:
                            byte0 = value;
                            break;
                        default:
                            throw new IndexOutOfRangeException();
                    }
                }
            }
        }

        public override string ToString()
        {
            return Convert.ToString(integer, 2).PadLeft(32, '0');
        }
    }
}

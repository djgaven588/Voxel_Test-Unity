using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SavedChunk
{
    public Vector3Int ChunkPosition;
    public DataPalette<Block> ChunkData;

    public static SavedChunk ReadChunk(BinaryReader reader)
    {
        SavedChunk chunk = new SavedChunk
        {
            ChunkPosition = new Vector3Int(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32())
        };

        int paletteSize = reader.ReadInt32();
        int entryBits = MinimumBitsToStore(paletteSize - 1);
        Block[] paletteBlocks = new Block[paletteSize];
        for (int i = 0; i < paletteSize; i++)
        {
            string blockName = reader.ReadString();
            paletteBlocks[i] = BlockManager.Inst.GetBlockOrDefault(blockName);
        }

        int runsCount = reader.ReadInt32();

        int bitIndex = 0;
        BitArray data = new BitArray(reader.ReadBytes(reader.ReadInt32()));

        int typeIndex = 0;
        int[] typeIndexes = new int[Chunk.CHUNK_SIZE_CUBE];

        for (int i = 0; i < runsCount; i++)
        {
            byte count = (byte)GetValue(data, bitIndex, 8);
            bitIndex += 8;

            ushort type = (ushort)GetValue(data, bitIndex, entryBits);
            bitIndex += entryBits;

            for (int j = 0; j < count + 1; j++)
            {
                typeIndexes[typeIndex] = type;
                typeIndex++;
            }
        }

        chunk.ChunkData = new DataPalette<Block>(Chunk.CHUNK_SIZE_CUBE, paletteBlocks, typeIndexes);

        return chunk;
    }

    private static int GetValue(BitArray data, int offset, int length)
    {
        int value = 0;
        for (int j = 0; j < length; j++)
        {
            value |= (data[offset + j] == true ? 1 : 0) << j;
        }

        return value;
    }

    private static void SetValue(BitArray data, int offset, int length, int value)
    {
        for (int j = 0; j < length; j++)
        {
            data[offset + j] = ((value >> j) & 1) == 1;
        }
    }

    /// <summary>
    /// Finds the amount of bytes it takes to contain the given bits
    /// </summary>
    /// <param name="bits"></param>
    /// <returns></returns>
    public static int BytesToContainBits(int bits)
    {
        return (bits / 8) + (bits % 8 > 0 ? 1 : 0);
    }

    /// <summary>
    /// Finds the minimum amount of bits to store a value
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static int MinimumBitsToStore(int value)
    {
        int bits = 1;
        int p = 1;
        while (p < value)
        {
            p <<= 1;
            bits++;
        }

        return bits;
    }

    public static void WriteChunk(BinaryWriter writer, Chunk chunk)
    {
        writer.Write(chunk.Position.x);
        writer.Write(chunk.Position.y);
        writer.Write(chunk.Position.z);

        string[] palette = chunk.GetPaletteNames();
        writer.Write(palette.Length);

        for (int i = 0; i < palette.Length; i++)
        {
            writer.Write(palette[i]);
        }

        int entryBits = MinimumBitsToStore(palette.Length - 1);

        ushort currentType = 0;
        byte currentRunLength = 0;
        List<(ushort, byte)> _runs = new List<(ushort, byte)>();
        for (int i = 0; i < Chunk.CHUNK_SIZE_CUBE; i++)
        {
            if (i == 0)
            {
                currentType = (ushort)chunk.GetBlockPaletteIndex(i);
                currentRunLength = 0;
            }
            else if (currentType != chunk.GetBlockPaletteIndex(i) || (currentRunLength == 255 && i + 1 != Chunk.CHUNK_SIZE_CUBE))
            {
                _runs.Add((currentType, currentRunLength));
                currentType = (ushort)chunk.GetBlockPaletteIndex(i);
                currentRunLength = 0;
            }
            else
            {
                currentRunLength++;
            }
        }

        _runs.Add((currentType, currentRunLength));

        writer.Write(_runs.Count);

        BitArray data = new BitArray(_runs.Count * 8 + _runs.Count * entryBits);
        int dataIndex = 0;

        for (int i = 0; i < _runs.Count; i++)
        {
            (ushort type, byte runLength) = _runs[i];

            SetValue(data, dataIndex, 8, runLength);
            dataIndex += 8;

            SetValue(data, dataIndex, entryBits, type);
            dataIndex += entryBits;
        }

        byte[] output = new byte[BytesToContainBits(data.Length)];
        data.CopyTo(output, 0);
        writer.Write(output.Length);
        writer.Write(output);
    }
}

using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

/// <summary>
/// A data structure which uses palette compression.
/// </summary>
public class DataPalette<T> where T : class, new()
{
    public readonly int Length;

    private int DataSize;
    public BitArray Data;
    public T[] PaletteEntries;
    public int[] PaletteReferences;

    public ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();

    /// <summary>
    /// Creates a new chunk storage with a given starting minimum pallet size
    /// </summary>
    /// <param name="size"></param>
    /// <param name="minimumPalletSize"></param>
    public DataPalette(int size, int minimumPalletSize, T defaultBlock)
    {
        Length = size;

        int startingSize = NextPowerOfTwo(minimumPalletSize);

        PaletteEntries = new T[startingSize];
        PaletteReferences = new int[startingSize];

        PaletteEntries[0] = defaultBlock;
        PaletteReferences[0] = size;

        DataSize = MinimumBitsToStore(startingSize - 1);
        Data = new BitArray(DataSize * Length);
    }

    public DataPalette(int size, T[] palette, int[] indexes)
    {
        Length = size;

        PaletteEntries = palette;
        PaletteReferences = new int[palette.Length];

        DataSize = MinimumBitsToStore(PaletteEntries.Length - 1);
        Data = new BitArray(Length * DataSize);

        for (int i = 0; i < indexes.Length; i++)
        {
            PaletteReferences[indexes[i]]++;
            for (int j = 0; j < DataSize; j++)
            {
                Data[i * DataSize + j] = ((indexes[i] >> j & 1) == 1);
            }
        }
    }

    /// <summary>
    /// Set the block at a given index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="block"></param>
    public void SetEntry(int index, T block, bool shouldLock = true)
    {
        if(shouldLock) Lock.EnterWriteLock();
        if (!Set(index, block))
        {
            // We have no free space left, so we need to expand this storage
            ExpandPalette();

            Set(index, block);
        }
        if (shouldLock) Lock.ExitWriteLock();
    }

    private bool Set(int index, T block)
    {
        for (int i = 0; i < PaletteEntries.Length; i++)
        {
            if (PaletteEntries[i] == block)
            {
                int previousEntry = GetValueFromBits(index);

                PaletteReferences[previousEntry]--;

                SetBitsFromValue(index, i);

                PaletteReferences[i]++;
                return true;
            }
        }

        // Do we have a free entry available?
        for (int i = 0; i < PaletteReferences.Length; i++)
        {
            if (PaletteReferences[i] == 0)
            {
                PaletteEntries[i] = block;

                int previousEntry = GetValueFromBits(index);

                PaletteReferences[previousEntry]--;

                SetBitsFromValue(index, i);

                PaletteReferences[i]++;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Get the block at a given index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public T GetEntry(int index, bool shouldLock = true)
    {
        if (shouldLock) Lock.EnterReadLock();
        T value = PaletteEntries[GetValueFromBits(index)];
        if (shouldLock) Lock.ExitReadLock();
        return value;
    }

    public int GetPaletteIndex(int index)
    {
        Lock.EnterReadLock();
        int value = GetValueFromBits(index);
        Lock.ExitReadLock();
        return value;
    }

    /// <summary>
    /// Attempts to compress this chunk storage the maximum amount possible, useful when chunks are being unloaded from view or otherwise
    /// </summary>
    public void CompressToMaximum()
    {
        int usedPaletteCount = 0;
        for (int i = 0; i < PaletteReferences.Length; i++)
        {
            if (PaletteReferences[i] > 0)
            {
                usedPaletteCount++;
            }
        }

        int containingPower = NextPowerOfTwo(usedPaletteCount);

        if (PaletteReferences.Length <= containingPower)
        {
            // No point in attempting to compress something already compressed
            return;
        }

        int[] currentData = new int[Length];
        for (int i = 0; i < currentData.Length; i++)
        {
            currentData[i] = GetValueFromBits(i);
        }

        DataSize = MinimumBitsToStore(containingPower - 1);

        T[] newPalette = new T[containingPower];
        int[] newReferences = new int[containingPower];

        int paletteIndex = 0;
        for (int i = 0; i < PaletteEntries.Length; i++)
        {
            int referencesLeft = PaletteReferences[i];

            if (referencesLeft == 0)
            {
                continue;
            }

            newPalette[paletteIndex] = PaletteEntries[i];
            newReferences[paletteIndex] = referencesLeft;

            for (int j = 0; j < currentData.Length; j++)
            {
                if (currentData[j] == i)
                {
                    currentData[j] = paletteIndex;
                    referencesLeft--;
                    if (referencesLeft == 0)
                    {
                        break;
                    }
                }
            }

            paletteIndex++;
        }

        PaletteEntries = newPalette;
        PaletteReferences = newReferences;

        Data = new BitArray(Length * DataSize);

        for (int i = 0; i < currentData.Length; i++)
        {
            SetBitsFromValue(i, currentData[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpandPalette()
    {
        int[] currentData = new int[Length];
        for (int i = 0; i < currentData.Length; i++)
        {
            currentData[i] = GetValueFromBits(i);
        }

        DataSize <<= 1;

        int startingCount = NextPowerOfTwo(PaletteEntries.Length + 1);

        T[] newPallet = new T[startingCount];
        Array.Copy(PaletteEntries, newPallet, PaletteEntries.Length);

        int[] newReferences = new int[startingCount];
        Array.Copy(PaletteReferences, newReferences, PaletteReferences.Length);

        PaletteReferences = newReferences;
        PaletteEntries = newPallet;

        Data = new BitArray(DataSize * Length);
        for (int i = 0; i < currentData.Length; i++)
        {
            SetBitsFromValue(i, currentData[i]);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetValueFromBits(int entryOffset)
    {
        int value = 0;
        for (int j = 0; j < DataSize; j++)
        {
            value |= (Data.Get(entryOffset * DataSize + j) == true ? 1 : 0) << j;
        }

        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetBitsFromValue(int entryOffset, int value)
    {
        for (int j = 0; j < DataSize; j++)
        {
            Data.Set(entryOffset * DataSize + j, ((value >> j) & 1) == 1);
        }
    }

    /// <summary>
    /// Finds the next power of two which contains or equals the given value
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NextPowerOfTwo(int value)
    {
        int p = 1;
        while (p < value) p <<= 1;

        return p;
    }

    /// <summary>
    /// Finds the minimum amount of bits to store a value
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

    /// <summary>
    /// Finds the amount of bytes it takes to contain the given bits
    /// </summary>
    /// <param name="bits"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int BytesToContainBits(int bits)
    {
        return (bits / 8) + (bits % 8 > 0 ? 1 : 0);
    }
}
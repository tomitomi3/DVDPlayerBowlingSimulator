﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DemoContentLoader
{
    public unsafe class Texture2DContent : IContent
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int MipLevels { get; private set; }
        public int TexelSizeInBytes { get; private set; }

        GCHandle handle;
        public byte[] Data { get; private set; }

        public ContentType ContentType => ContentType.Image;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Texture2DContent(int width, int height, int mipLevels, int texelSizeInBytes)
        {
            Width = width;
            Height = height;
            MipLevels = mipLevels;
            TexelSizeInBytes = texelSizeInBytes;
            var dataSize = 0;
            for (int i = 0; i < mipLevels; ++i)
            {
                dataSize += texelSizeInBytes * (width >> i) * (height >> i);
            }
            Data = new byte[dataSize];
        }

        //Note that all of these operate in units of texels, not bytes.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRowPitch(int mipLevel)
        {
            Debug.Assert(mipLevel >= 0 && mipLevel < MipLevels);
            return (Width >> mipLevel);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetMipStartIndex(int mipLevel)
        {
            Debug.Assert(mipLevel >= 0 && mipLevel < MipLevels);
            int start = 0;
            for (int i = 0; i < mipLevel; ++i)
            {
                start += (Width >> i) * (Height >> i);
            }
            return start;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRowOffsetFromMipStart(int mipLevel, int rowIndex)
        {
            Debug.Assert(mipLevel >= 0 && mipLevel < MipLevels);
            Debug.Assert(rowIndex >= 0 && rowIndex < (Height >> mipLevel));
            return GetRowPitch(mipLevel) * rowIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOffset(int columnIndex, int rowIndex, int mipLevel)
        {
            Debug.Assert(mipLevel >= 0 && mipLevel < MipLevels);
            Debug.Assert(columnIndex >= 0 && columnIndex < Width && rowIndex >= 0 && rowIndex < Height);
            return GetMipStartIndex(mipLevel) + GetRowPitch(mipLevel) * rowIndex + columnIndex;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetOffsetForMip0(int columnIndex, int rowIndex)
        {
            Debug.Assert(columnIndex >= 0 && columnIndex < Width && rowIndex >= 0 && rowIndex < Height);
            return Width * rowIndex + columnIndex;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetRowOffsetForMip0(int rowIndex)
        {
            Debug.Assert(rowIndex >= 0 && rowIndex < Height);
            return Width * rowIndex;
        }

        public byte* Pin()
        {
            if (handle.IsAllocated)
                throw new InvalidOperationException("Cannot pin an already-pinned texture.");
            handle = GCHandle.Alloc(Data, GCHandleType.Pinned);
            return (byte*)handle.AddrOfPinnedObject();
        }

        public void Unpin()
        {
            if (!handle.IsAllocated)
                throw new InvalidOperationException("Should only unpin textures that have been pinned.");
            handle.Free();
        }

#if DEBUG
        ~Texture2DContent()
        {
            Debug.Assert(!handle.IsAllocated, "Any resource getting finalized should no longer be pinned in memory. Check for Pins without Unpins.");
        }
#endif
    }
}

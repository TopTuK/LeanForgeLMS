using System.Buffers.Binary;

namespace LF.Infrastructure.Services.Documents;

// Reads pixel dimensions from the encoded header only, so an image can be admitted or rejected
// before a decoder allocates its full bitmap. Every read is bounds-checked; anything unrecognised
// or truncated reports failure rather than a guess.
internal static class ImageHeaderReader
{
    public static bool TryReadSize(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = height = 0;

        if (data.StartsWith((ReadOnlySpan<byte>)[0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A]))
            return TryReadPng(data, out width, out height);
        if (data.Length >= 2 && data[0] == 0xFF && data[1] == 0xD8)
            return TryReadJpeg(data, out width, out height);
        if (data.StartsWith("GIF87a"u8) || data.StartsWith("GIF89a"u8))
            return TryReadGif(data, out width, out height);
        if (data.StartsWith("BM"u8))
            return TryReadBmp(data, out width, out height);
        if (data.Length >= 12 && data.StartsWith("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8))
            return TryReadWebp(data, out width, out height);

        return false;
    }

    private static bool TryReadPng(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = height = 0;
        // Signature (8) + IHDR length (4) + "IHDR" (4) + width (4) + height (4).
        if (data.Length < 24 || !data[12..16].SequenceEqual("IHDR"u8)) return false;

        return TrySet(BinaryPrimitives.ReadUInt32BigEndian(data[16..]), BinaryPrimitives.ReadUInt32BigEndian(data[20..]), out width, out height);
    }

    private static bool TryReadJpeg(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = height = 0;
        var offset = 2;

        while (offset + 4 <= data.Length)
        {
            if (data[offset] != 0xFF) return false;

            var marker = data[offset + 1];
            if (marker == 0xFF)
            {
                offset++; // fill byte
                continue;
            }

            // Standalone markers carry no length.
            if (marker is 0x01 or (>= 0xD0 and <= 0xD7))
            {
                offset += 2;
                continue;
            }

            if (marker is 0xD9 or 0xDA) return false; // end of image / start of scan before any frame header

            var length = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 2)..]);
            if (length < 2) return false;

            // SOF0-SOF15 except DHT (C4), JPG (C8) and DAC (CC).
            if (marker is >= 0xC0 and <= 0xCF and not 0xC4 and not 0xC8 and not 0xCC)
            {
                if (offset + 9 > data.Length) return false;
                return TrySet(
                    BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 7)..]),
                    BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 5)..]),
                    out width, out height);
            }

            offset += 2 + length;
        }

        return false;
    }

    private static bool TryReadGif(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = height = 0;
        if (data.Length < 10) return false;

        return TrySet(BinaryPrimitives.ReadUInt16LittleEndian(data[6..]), BinaryPrimitives.ReadUInt16LittleEndian(data[8..]), out width, out height);
    }

    private static bool TryReadBmp(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = height = 0;
        if (data.Length < 26) return false;

        var dibHeaderSize = BinaryPrimitives.ReadUInt32LittleEndian(data[14..]);
        if (dibHeaderSize == 12)
            return TrySet(BinaryPrimitives.ReadUInt16LittleEndian(data[18..]), BinaryPrimitives.ReadUInt16LittleEndian(data[20..]), out width, out height);

        // Negative height means a top-down bitmap.
        var w = BinaryPrimitives.ReadInt32LittleEndian(data[18..]);
        var h = BinaryPrimitives.ReadInt32LittleEndian(data[22..]);
        return w > 0 && h != int.MinValue && TrySet((uint)w, (uint)Math.Abs(h), out width, out height);
    }

    private static bool TryReadWebp(ReadOnlySpan<byte> data, out int width, out int height)
    {
        width = height = 0;
        if (data.Length < 30) return false;

        var chunk = data[12..16];
        if (chunk.SequenceEqual("VP8 "u8))
        {
            // Keyframe start code 9D 01 2A precedes the 14-bit dimensions.
            if (data[23] != 0x9D || data[24] != 0x01 || data[25] != 0x2A) return false;
            return TrySet(
                (uint)(BinaryPrimitives.ReadUInt16LittleEndian(data[26..]) & 0x3FFF),
                (uint)(BinaryPrimitives.ReadUInt16LittleEndian(data[28..]) & 0x3FFF),
                out width, out height);
        }

        if (chunk.SequenceEqual("VP8L"u8))
        {
            if (data[20] != 0x2F) return false;
            var bits = BinaryPrimitives.ReadUInt32LittleEndian(data[21..]);
            return TrySet((bits & 0x3FFF) + 1, ((bits >> 14) & 0x3FFF) + 1, out width, out height);
        }

        if (chunk.SequenceEqual("VP8X"u8))
        {
            var w = data[24] | (data[25] << 8) | (data[26] << 16);
            var h = data[27] | (data[28] << 8) | (data[29] << 16);
            return TrySet((uint)w + 1, (uint)h + 1, out width, out height);
        }

        return false;
    }

    private static bool TrySet(uint w, uint h, out int width, out int height)
    {
        width = height = 0;
        if (w == 0 || h == 0 || w > int.MaxValue || h > int.MaxValue) return false;

        width = (int)w;
        height = (int)h;
        return true;
    }
}

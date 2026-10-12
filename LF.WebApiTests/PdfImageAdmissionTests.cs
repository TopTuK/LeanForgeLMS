using System.Buffers.Binary;
using LF.Infrastructure.Services.Documents;
using Limits = LF.Infrastructure.Services.Documents.QuestPdfCourseRenderer.ImageLimits;

namespace LF.WebApiTests;

/// <summary>
/// Image admission for course PDFs: dimensions come from encoded headers only, and every budget is
/// enforced before the decoder allocates a bitmap.
/// </summary>
public class PdfImageAdmissionTests
{
    private static byte[] Png(uint width, uint height)
    {
        var data = new byte[33];
        new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A }.CopyTo(data, 0);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(8), 13);
        "IHDR"u8.CopyTo(data.AsSpan(12));
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(16), width);
        BinaryPrimitives.WriteUInt32BigEndian(data.AsSpan(20), height);
        return data;
    }

    private static byte[] Jpeg(ushort width, ushort height) =>
    [
        0xFF, 0xD8,
        0xFF, 0xE0, 0x00, 0x04, 0x00, 0x00, // APP0 with a 2-byte payload, skipped by length
        0xFF, 0xC0, 0x00, 0x11, 0x08,
        (byte)(height >> 8), (byte)height,
        (byte)(width >> 8), (byte)width,
        0x03,
    ];

    private static byte[] Gif(ushort width, ushort height)
    {
        var data = new byte[13];
        "GIF89a"u8.CopyTo(data);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(6), width);
        BinaryPrimitives.WriteUInt16LittleEndian(data.AsSpan(8), height);
        return data;
    }

    private static byte[] Bmp(int width, int height)
    {
        var data = new byte[54];
        "BM"u8.CopyTo(data);
        BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan(14), 40);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(18), width);
        BinaryPrimitives.WriteInt32LittleEndian(data.AsSpan(22), height);
        return data;
    }

    private static byte[] WebpVp8X(int width, int height)
    {
        var data = new byte[30];
        "RIFF"u8.CopyTo(data);
        "WEBP"u8.CopyTo(data.AsSpan(8));
        "VP8X"u8.CopyTo(data.AsSpan(12));
        var w = width - 1;
        var h = height - 1;
        data[24] = (byte)w; data[25] = (byte)(w >> 8); data[26] = (byte)(w >> 16);
        data[27] = (byte)h; data[28] = (byte)(h >> 8); data[29] = (byte)(h >> 16);
        return data;
    }

    public static TheoryData<byte[], int, int> KnownHeaders() => new()
    {
        { Png(640, 480), 640, 480 },
        { Jpeg(1024, 768), 1024, 768 },
        { Gif(32, 16), 32, 16 },
        { Bmp(100, -50), 100, 50 },
        { WebpVp8X(3000, 2000), 3000, 2000 },
    };

    [Theory]
    [MemberData(nameof(KnownHeaders))]
    public void TryReadSize_ReadsDimensionsFromHeader(byte[] data, int expectedWidth, int expectedHeight)
    {
        // Act
        var ok = ImageHeaderReader.TryReadSize(data, out var width, out var height);

        // Assert
        Assert.True(ok);
        Assert.Equal(expectedWidth, width);
        Assert.Equal(expectedHeight, height);
    }

    [Fact]
    public void TryReadSize_RealPng_MatchesItsHeader()
    {
        // Arrange — the 1x1 PNG used by the renderer smoke tests.
        var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

        // Act / Assert
        Assert.True(ImageHeaderReader.TryReadSize(png, out var width, out var height));
        Assert.Equal((1, 1), (width, height));
    }

    [Theory]
    [InlineData(new byte[] { })]
    [InlineData(new byte[] { 1, 2, 3, 4 })]
    [InlineData(new byte[] { 0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A, 0, 0 })]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xC0, 0x00 })]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0xFF, 0xFF })]
    public void TryReadSize_UnknownOrTruncated_Fails(byte[] data)
        => Assert.False(ImageHeaderReader.TryReadSize(data, out _, out _));

    [Fact]
    public void Admit_OrdinaryImage_IsAcceptedWithDecodedEstimate()
    {
        // Act
        var rejection = QuestPdfCourseRenderer.Admit(Png(1000, 500), 0, 0, 0, out var estimate);

        // Assert
        Assert.Null(rejection);
        Assert.Equal((long)(1000 * 500 * Limits.BytesPerPixel * Limits.DecoderHeadroom), estimate);
    }

    [Fact]
    public void Admit_DecompressionBomb_IsRejectedBeforeDecoding()
    {
        // Arrange — a 33-byte file claiming 50000x50000 pixels.
        var bomb = Png(50_000, 50_000);

        // Act / Assert
        Assert.NotNull(QuestPdfCourseRenderer.Admit(bomb, 0, 0, 0, out _));
    }

    [Fact]
    public void Admit_WithinSideLimitButTooManyPixels_IsRejected()
        => Assert.NotNull(QuestPdfCourseRenderer.Admit(Png(Limits.MaxDimension, Limits.MaxDimension), 0, 0, 0, out _));

    [Fact]
    public void Admit_UnrecognisedHeader_IsRejected()
        => Assert.NotNull(QuestPdfCourseRenderer.Admit([1, 2, 3, 4], 0, 0, 0, out _));

    [Fact]
    public void Admit_CountLimitReached_IsRejected()
        => Assert.NotNull(QuestPdfCourseRenderer.Admit(Png(10, 10), Limits.MaxCount, 0, 0, out _));

    [Fact]
    public void Admit_EncodedBudgetExhausted_IsRejected()
        => Assert.NotNull(QuestPdfCourseRenderer.Admit(Png(10, 10), 0, Limits.MaxTotalEncodedBytes - 1, 0, out _));

    [Fact]
    public void Admit_DecodedBudgetExhausted_IsRejected()
        => Assert.NotNull(QuestPdfCourseRenderer.Admit(Png(4000, 4000), 0, 0, Limits.MaxTotalDecodedBytes - 1024, out _));
}

using MatrixClient.Imaging;
using PngSharp.Api;
using PngSharp.Spec.Chunks.IHDR;
using PngSharp.Spec.Chunks.PLTE;
using PngSharp.Spec.Chunks.tRNS;

namespace MatrixClient.Tests;

public class PixelImageTests
{
    private static IhdrChunkData Ihdr(int w, int h, ColorType type, byte depth = 8) => new()
    {
        Width = (uint)w,
        Height = (uint)h,
        BitDepth = depth,
        ColorType = type,
        CompressionMethod = CompressionMethod.DeflateWithSlidingWindow,
        FilterMethod = FilterMethod.AdaptiveFiltering,
        InterlaceMethod = InterlaceMethod.None,
    };

    [Fact]
    public void NewImage_IsFilled()
    {
        var img = new PixelImage(3, 2, new Rgba32(1, 2, 3));

        Assert.Equal(new Rgba32(1, 2, 3, 255), img[2, 1]);
        Assert.Equal(default, new PixelImage(1, 1)[0, 0]);
    }

    [Fact]
    public void Indexer_RejectsPixelsOutsideTheImage()
    {
        var img = new PixelImage(2, 2);

        Assert.Throws<ArgumentOutOfRangeException>(() => img[2, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => img[0, -1]);
    }

    [Fact]
    public void Png_RoundTripsEveryPixelIncludingAlpha()
    {
        var img = new PixelImage(32, 32);
        for (var y = 0; y < 32; y++)
            for (var x = 0; x < 32; x++)
                img[x, y] = new Rgba32((byte)(x * 8), (byte)(y * 8), (byte)(x ^ y), (byte)(255 - x - y));

        var decoded = PixelImage.Load(img.ToPng());

        Assert.Equal((32, 32), (decoded.Width, decoded.Height));
        for (var y = 0; y < 32; y++)
            for (var x = 0; x < 32; x++)
                Assert.Equal(img[x, y], decoded[x, y]);
    }

    [Fact]
    public void Load_ReadsRgbAsOpaque()
    {
        var png = Png.EncodeToByteArray(Png.CreateRgb(2, 1, [10, 20, 30, 40, 50, 60]));

        var img = PixelImage.Load(png);

        Assert.Equal(new Rgba32(10, 20, 30), img[0, 0]);
        Assert.Equal(new Rgba32(40, 50, 60), img[1, 0]);
    }

    [Fact]
    public void Load_ReadsGrayscaleWithAlpha()
    {
        var png = Png.EncodeToByteArray(Png.CreateGrayscaleWithAlpha(1, 1, [77, 128]));

        Assert.Equal(new Rgba32(77, 77, 77, 128), PixelImage.Load(png)[0, 0]);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(2)]
    public void Load_ResolvesPaletteAndTransparency(byte depth)
    {
        var png = Png.EncodeToByteArray(Png.Builder()
            .WithIhdr(Ihdr(3, 1, ColorType.IndexedColor, depth))
            .WithPlte(new PlteChunkData { Entries = [255, 0, 0, 0, 255, 0, 0, 0, 255] })
            .WithTrns(new TrnsChunkData { Data = [255, 0] })   // entry 2 has no alpha: opaque
            .WithPixelData([2, 1, 0])          // one index per byte, also for 2-bit
            .Build());

        var img = PixelImage.Load(png);

        Assert.Equal(new Rgba32(0, 0, 255, 255), img[0, 0]);
        Assert.Equal(new Rgba32(0, 255, 0, 0), img[1, 0]);
        Assert.Equal(new Rgba32(255, 0, 0, 255), img[2, 0]);
    }

    [Fact]
    public void Load_RejectsSixteenBitImages()
    {
        var png = Png.EncodeToByteArray(Png.Builder()
            .WithIhdr(Ihdr(1, 1, ColorType.TrueColor, 16))
            .WithPixelData(new byte[6])
            .Build());

        var ex = Assert.Throws<ImageFormatException>(() => PixelImage.Load(png));
        Assert.Contains("16-bit", ex.Message);
    }

    [Fact]
    public void Load_RejectsNonPngData()
    {
        Assert.Throws<ImageFormatException>(() => PixelImage.Load("not a png"u8.ToArray()));
        Assert.Throws<ImageFormatException>(() => PixelImage.Load([]));
    }

    // PngSharp 0.6.0 hangs on these instead of failing; PixelImage must reject them first
    [Fact]
    public void Load_RejectsTruncatedPng()
    {
        var png = new PixelImage(32, 32, new Rgba32(1, 2, 3)).ToPng();

        for (var length = 0; length < png.Length; length++)
            Assert.Throws<ImageFormatException>(() => PixelImage.Load(png[..length]));
    }

    [Fact]
    public void Load_RejectsHugeDimensions()
    {
        var png = new PixelImage(2, 2).ToPng();
        png[16] = 0x7F;   // width's high byte

        var ex = Assert.Throws<ImageFormatException>(() => PixelImage.Load(png));
        Assert.Contains("size", ex.Message);
    }
}

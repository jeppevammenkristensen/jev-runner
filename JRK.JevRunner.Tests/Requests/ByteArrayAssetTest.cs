using System;
using System.IO;
using System.Text.Json.Nodes;
using JRK.JevRunner.Requests;
using Xunit;

namespace JRK.JevRunner.Tests.Requests;

/// <summary>Verifies byte-backed stream contents, metadata, validation, and multipart references.</summary>
public class ByteArrayAssetTest
{
    /// <summary>Constructor metadata is used for the asset entry and multipart reference, not Base64 encoding.</summary>
    [Fact]
    public void Constructor_PreservesMetadataAndSerializesMultipartReference()
    {
        var image = new ByteArrayAsset([1, 2, 3], "photo", "custom-part", "jpg");

        Assert.Equal("photo", image.Name);
        Assert.Equal("custom-part", image.PartName);
        Assert.Equal("jpg", image.Format);
        Asset asset = (IImageStreamAsset) image;
        var assets = new JsonObject();
        asset.AddTo(assets);
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(
            """{"photo":{"type":"image","format":"jpg","part":"custom-part"}}"""), assets));
    }

    /// <summary>Each stream is readable but cannot modify the image bytes.</summary>
    [Fact]
    public void OpenRead_ReturnsOriginalBytesInReadOnlyStream()
    {
        byte[] bytes = [0, 1, 2, 253, 254, 255];
        var asset = new ByteArrayAsset(bytes, "photo", "part", "png");

        using var stream = asset.OpenRead();
        using var buffer = new MemoryStream();
        Assert.True(stream.CanRead);
        Assert.False(stream.CanWrite);
        Assert.Equal(0L, stream.Position);
        stream.CopyTo(buffer);
        Assert.Equal(bytes, buffer.ToArray());
        Assert.Throws<NotSupportedException>(() => stream.WriteByte(42));
    }

    /// <summary>Reading or disposing one stream does not affect other streams or subsequent openings.</summary>
    [Fact]
    public void OpenRead_ReturnsIndependentStreamsAndSupportsReopeningAfterDisposal()
    {
        var asset = new ByteArrayAsset([1, 2, 3], "photo", "part", "jpg");
        using var first = asset.OpenRead();
        using var second = asset.OpenRead();

        Assert.NotSame(first, second);
        Assert.Equal(1, first.ReadByte());
        Assert.Equal(0L, second.Position);
        first.Dispose();
        Assert.Equal(1, second.ReadByte());
        using var reopened = asset.OpenRead();
        Assert.Equal(1, reopened.ReadByte());
    }

    /// <summary>Empty image buffers can still be opened as empty readable streams.</summary>
    [Fact]
    public void OpenRead_EmptyArray_ReturnsEmptyStream()
    {
        var asset = new ByteArrayAsset([], "photo", "part", "jpg");

        using var stream = asset.OpenRead();
        Assert.True(stream.CanRead);
        Assert.Equal(0L, stream.Length);
        Assert.Equal(-1, stream.ReadByte());
    }

    /// <summary>A null byte buffer is rejected during construction rather than at upload time.</summary>
    [Fact]
    public void Constructor_RejectsNullBytes()
    {
        var error = Assert.Throws<ArgumentNullException>(() => new ByteArrayAsset(null!, "photo", "part", "jpg"));

        Assert.Equal("bytes", error.ParamName);
    }

    /// <summary>Every metadata field must contain a non-whitespace value.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Constructor_RejectsInvalidMetadata(string? value)
    {
        Assert.Equal("name", Assert.ThrowsAny<ArgumentException>(() =>
            new ByteArrayAsset([], value!, "part", "jpg")).ParamName);
        Assert.Equal("partName", Assert.ThrowsAny<ArgumentException>(() =>
            new ByteArrayAsset([], "photo", value!, "jpg")).ParamName);
        Assert.Equal("format", Assert.ThrowsAny<ArgumentException>(() =>
            new ByteArrayAsset([], "photo", "part", value!)).ParamName);
    }
}
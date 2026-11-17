using System;
using System.IO;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using JRK.JevRunner.Requests;
using TruePath;
using Xunit;

namespace JRK.JevRunner.Tests.Requests;

/// <summary>
/// Verifies byte extraction across inline, file-backed, and custom stream image assets.
/// </summary>
public class AssetTest
{
    /// <summary>
    /// Inline image data is decoded regardless of whether it was supplied as bytes or Base64 text.
    /// </summary>
    [Fact]
    public void ReadAsBytes_InlineImages_ReturnsDecodedBytes()
    {
        byte[] bytes = [0, 1, 2, 253, 254, 255];
        Asset fromBytes = new ImageAsset("photo", "png", bytes);
        Asset fromBase64 = new ImageAsset("photo", "png", new Base64String(Convert.ToBase64String(bytes)));

        Assert.Equal(bytes, fromBytes.ReadAsBytes());
        Assert.Equal(bytes, fromBase64.ReadAsBytes());
    }

    /// <summary>
    /// File-backed assets read current mock file contents on each call without modifying the file.
    /// </summary>
    [Fact]
    public void ReadAsBytes_FileAsset_ReadsCurrentContentsOnEachCall()
    {
        var fs = new MockFileSystem();
        var path = AbsolutePath.Create("C:/images/photo.png");
        fs.AddFile(path.Value, new MockFileData(new byte[] {1, 2, 3}));
        Asset asset = (IImageStreamAsset)new FilePathAsset(path, fs);

        Assert.Equal(new byte[] {1, 2, 3}, asset.ReadAsBytes());
        path.WriteAllBytes([4, 5], fs);
        Assert.Equal(new byte[] {4, 5}, asset.ReadAsBytes());
        Assert.Equal(new byte[] {4, 5}, path.ReadAllBytes(fs));
    }

    /// <summary>
    /// Missing files are reported when bytes are requested rather than at asset construction.
    /// </summary>
    [Fact]
    public void ReadAsBytes_MissingFile_PropagatesError()
    {
        Asset asset = (IImageStreamAsset)new FilePathAsset(AbsolutePath.Create("C:/images/missing.png"), new MockFileSystem());

        Assert.Throws<FileNotFoundException>(() => asset.ReadAsBytes());
    }

    /// <summary>
    /// Stream byte extraction starts at the supplied position and disposes the opened stream.
    /// </summary>
    [Fact]
    public void ReadAsBytes_StreamAsset_ReadsRemainingBytesAndDisposesStream()
    {
        var stream = new MemoryStream([1, 2, 3]);
        stream.Position = 1;
        Asset asset = (IImageStreamAsset)new StreamAsset(stream);

        Assert.Equal(new byte[] {2, 3}, asset.ReadAsBytes());
        Assert.False(stream.CanRead);
    }

    /// <summary>
    /// Read failures propagate while still releasing the image stream.
    /// </summary>
    [Fact]
    public void ReadAsBytes_FailingStream_DisposesStreamAndPropagatesError()
    {
        var stream = new FailingStream();
        Asset asset = (IImageStreamAsset)new StreamAsset(stream);

        Assert.Throws<IOException>(() => asset.ReadAsBytes());
        Assert.False(stream.CanRead);
    }

    /// <summary>
    /// Supplies a caller-controlled stream to verify byte extraction and ownership.
    /// </summary>
    /// <param name="stream">The stream returned unchanged by the provider.</param>
    private sealed class StreamAsset(Stream stream) : IImageStreamAsset
    {
        public string Name => "part";
        public string FileName => "photo.png";
        public string AssetName => "photo";
        public string Format => "png";
        public Stream OpenRead() => stream;
    }

    /// <summary>
    /// Simulates an I/O failure during copying while retaining observable disposal behavior.
    /// </summary>
    private sealed class FailingStream : MemoryStream
    {
        /// <summary>
        /// Fails copying to exercise cleanup when image reading throws.
        /// </summary>
        /// <param name="destination">The destination that must not receive data.</param>
        /// <param name="bufferSize">The requested copy buffer size.</param>
        public override void CopyTo(Stream destination, int bufferSize) => throw new IOException("Read failed.");
    }
}

using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using JRK.JevRunner.Annotation;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;
using System.IO.Abstractions.TestingHelpers;
using TruePath;
using Xunit;

namespace JRK.JevRunner.Tests.Infrastructure;

/// <summary>Verifies image loading, asset references, and runtime and generated request serialization.</summary>
public partial class AssetStateTest
{
    /// <summary>An image asset supplies the API's image fields without changing the encoded bytes.</summary>
    [Fact]
    public void ImageAsset_Node_SerializesImageFields()
    {
        var image = new ImageAsset("image-ref", "jpg", new Base64String("AQID"));

        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(
            """{"type":"image","format":"jpg","data_base64":"AQID"}"""), image.Node));
        Asset asset = image;
        var assets = new JsonObject();
        asset.AddTo(assets);
        Assert.Equal("image-ref", asset.Name);
        Assert.True(JsonNode.DeepEquals(image.Node, assets["image-ref"]));
    }

    /// <summary>State field names and asset references can differ and multiple assets are collected.</summary>
    [Fact]
    public void Request_AssetState_CollectsAndSerializesAssets()
    {
        var state = new AssetState();
        Assert.Same(state, state.AddAsset("photo", new ImageAsset("image-one", "jpg", new Base64String("AQID"))));
        state.AddAsset("example", new ImageAsset("image-two", "png", new Base64String("BAUG")));

        var request = new Request(state, null);
        var json = JsonTranslator.CreateRequestJson(request);

        Assert.True(request.HasAssets);
        Assert.Equal(2, request.Assets.Count);
        Assert.Equal("image-one", json["state"]!["photo"]!.GetValue<string>());
        Assert.Equal("image-two", json["state"]!["example"]!.GetValue<string>());
        Assert.Equal("AQID", json["assets"]!["image-one"]!["data_base64"]!.GetValue<string>());
        Assert.Equal("png", json["assets"]!["image-two"]!["format"]!.GetValue<string>());
    }

    /// <summary>Assets are collected at construction rather than tracked as the state changes.</summary>
    [Fact]
    public void Request_StateChangesAfterConstruction_DoNotCollectNewAssets()
    {
        var state = new AssetState();
        var request = new Request(state, null);

        state.AddAsset("photo", new ImageAsset("later", "jpg", new Base64String("AQID")));

        Assert.False(request.HasAssets);
        Assert.Empty(request.Assets);
    }

    /// <summary>A duplicate state field must not replace its registered asset before throwing.</summary>
    [Fact]
    public void AddAsset_DuplicateField_PreservesOriginalAssetAndReference()
    {
        var state = new AssetState().AddAsset("photo", new ImageAsset("original", "jpg", new Base64String("AQID")));

        Assert.Throws<ArgumentException>(() =>
            state.AddAsset("photo", new ImageAsset("replacement", "png", new Base64String("BAUG"))));

        var json = JsonTranslator.CreateRequestJson(new Request(state, null));
        Assert.Equal("original", state["photo"]);
        Assert.Equal("AQID", json["assets"]!["original"]!["data_base64"]!.GetValue<string>());
        Assert.Single(json["assets"]!.AsObject());
    }

    /// <summary>The helper uses the injected file system and preserves derived state identity.</summary>
    [Theory]
    [InlineData("jpg")]
    [InlineData("jpeg")]
    [InlineData("png")]
    public async Task AddImageAsset_ExistingFile_EncodesBytesAndReturnsDerivedState(string format)
    {
        var fileSystem = new MockFileSystem();
        var path = AbsolutePath.Create($"C:/images/photo.{format}");
        byte[] bytes = [0, 1, 2, 253, 254, 255];
        fileSystem.AddFile(path.ToString(), new MockFileData(bytes));
        var state = new PhotoState();

        PhotoState result = await state.AddImageAsset("photo", "image-ref", path, fileSystem);

        Assert.Same(state, result);
        Assert.Equal("image-ref", state["photo"]);
        var request = new Request(state, null);
        var json = JsonTranslator.CreateRequestJson(request);
        Assert.Single(request.Assets);
        Assert.Equal(format, json["assets"]!["image-ref"]!["format"]!.GetValue<string>());
        Assert.Equal(Convert.ToBase64String(bytes),
            json["assets"]!["image-ref"]!["data_base64"]!.GetValue<string>());
    }

    /// <summary>A missing file is reported without adding a state entry or asset.</summary>
    [Fact]
    public async Task AddImageAsset_MissingFile_DoesNotMutateState()
    {
        var state = new AssetState();
        var fileSystem = new MockFileSystem();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => state.AddImageAsset(
            "photo", "missing-ref", AbsolutePath.Create("C:/images/missing.jpg"), fileSystem));

        Assert.Equal("No image found at the given location", error.Message);
        Assert.Empty(state);
        Assert.Empty(new Request(state, null).Assets);
    }

    /// <summary>Generated requests collect image assets from a derived state just like manually created requests.</summary>
    [Fact]
    public void GeneratedQuery_GetRequest_CollectsImageAssets()
    {
        var state = new PhotoState();
        state.AddAsset("photo", new ImageAsset("image-ref", "jpg", new Base64String("AQID")));

        var request = new ImageQuery(state).GetRequest();
        var json = JsonTranslator.CreateRequestJson(request);

        Assert.Single(request.Assets);
        Assert.Equal("image-ref", json["state"]!["photo"]!.GetValue<string>());
        Assert.Equal("AQID", json["assets"]!["image-ref"]!["data_base64"]!.GetValue<string>());
        Assert.Equal("noul", json["questions"]!["PersonPresent"]!["type"]!.GetValue<string>());
    }

    /// <summary>Provides a derived asset state to exercise generic fluent return types.</summary>
    private sealed class PhotoState : AssetState;

    /// <summary>Exercises the generated request contract with image-backed state.</summary>
    [JevQuery]
    private partial class ImageQuery(PhotoState state)
    {
        /// <summary>Gets the image-backed state.</summary>
        public PhotoState State => state;
        /// <summary>Gets the model used for the image question.</summary>
        public JevModel JevModel => global::JRK.JevRunner.Requests.JevModel.Latest;
        /// <summary>Gets the image presence question.</summary>
        public INoulQuestionDefinition<string> PersonPresent { get; } =
            new GenericNoulQuestionDefinition<string>("Is there a person in `photo`?", null, null);
    }
}

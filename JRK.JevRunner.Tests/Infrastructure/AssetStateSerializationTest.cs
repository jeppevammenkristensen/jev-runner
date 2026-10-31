using System;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using JRK.JevRunner.Infrastructure;
using JRK.JevRunner.Requests;
using Xunit;

namespace JRK.JevRunner.Tests.Infrastructure;

/// <summary>Verifies that derived state data and asset references reach request JSON together.</summary>
public class AssetStateSerializationTest
{
    [Fact]
    public void ToStateJson_PlainState_ContainsOnlyDictionaryEntries()
    {
        var state = new AssetState();
        state.AddAsset("Photo", new ImageAsset("person_image", "jpg", new Base64String("AQID")));

        var json = state.ToStateJson();

        Assert.Single(json);
        Assert.Equal("person_image", json["Photo"]!.GetValue<string>());
    }

    [Fact]
    public void Request_DerivedState_IncludesPropertiesAndAssetReferences()
    {
        var state = new FileState();
        state.AddAsset("Photo", new ImageAsset("person_image", "jpg", new Base64String("AQID")));

        var json = JsonTranslator.CreateRequestJson(new Request(state, null));
        var content = json["state"]!.AsObject();

        Assert.Equal(4, content.Count);
        Assert.Equal("Some.txt", content["File"]!.GetValue<string>());
        Assert.Equal("txt", content["extension"]!.GetValue<string>());
        Assert.Equal("inherited", content["Description"]!.GetValue<string>());
        Assert.Equal("person_image", content["Photo"]!.GetValue<string>());
        Assert.Equal("AQID", json["assets"]!["person_image"]!["data_base64"]!.GetValue<string>());
    }

    [Fact]
    public void ToStateJson_DuplicatePropertyAndEntry_Throws()
    {
        var state = new FileState { ["File"] = "conflicting" };

        Assert.Throws<InvalidOperationException>(() => state.ToStateJson());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CombineWith_PreservesDerivedStateProperties(bool stateFirst)
    {
        JevMessage state = new(new FileState());
        JevMessage text = new("Additional context");

        var json = (stateFirst ? state.CombineWith(text) : text.CombineWith(state)).Node.AsArray();

        Assert.Equal(2, json.Count);
        Assert.Equal("Some.txt", json[stateFirst ? 0 : 1]!["File"]!.GetValue<string>());
        Assert.Equal("Additional context", json[stateFirst ? 1 : 0]!.GetValue<string>());
    }

    [Fact]
    public void ToStateJson_ReturnsIndependentJsonObjects()
    {
        var state = new FileState();
        var first = state.ToStateJson();

        first["File"] = "changed";

        Assert.Equal("Some.txt", state.ToStateJson()["File"]!.GetValue<string>());
    }

    /// <summary>Provides inherited application data rather than dictionary infrastructure.</summary>
    private class DescribedState : AssetState
    {
        public string Description => "inherited";
    }

    /// <summary>Models playground file data with serialization metadata and an excluded indexer.</summary>
    private sealed class FileState : DescribedState
    {
        public string File => "Some.txt";

        [JsonPropertyName("extension")]
        public string Extension => "txt";

        [JsonIgnore]
        public string Secret => "not state";

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Missing => null;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public int DefaultCount => 0;

        public string this[int index] => throw new InvalidOperationException("Indexers must not be read.");
    }
}

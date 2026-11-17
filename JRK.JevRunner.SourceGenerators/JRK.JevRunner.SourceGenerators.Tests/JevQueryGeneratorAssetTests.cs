using System.Linq;
using Microsoft.CodeAnalysis;
using Xunit;

namespace JRK.JevRunner.SourceGenerators.Tests;

/// <summary>
/// Verifies automatic prepared-asset registration and state compatibility diagnostics.
/// </summary>
public partial class JevQueryGeneratorTests
{
    /// <summary>
    /// Registers inherited and private assets before the request snapshots them, including repeated calls.
    /// </summary>
    /// <param name="genericState">Whether the state is declared through an AssetState-constrained type parameter.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparedAssetsRegisterBeforeRequestConstruction(bool genericState)
    {
        var (result, output) = Generate($$"""
                                          using JRK.JevRunner.Annotation;
                                          using JRK.JevRunner.Infrastructure;
                                          public class PhotoState : AssetState { }
                                          public class QueryBase
                                          {
                                              protected AssetAndStateReference Inherited { get; } = new("inherited", new("second"));
                                          }
                                          [JevQuery]
                                          public partial class Query{{(genericState ? "<TState>" : "")}} : QueryBase
                                              {{(genericState ? "where TState : AssetState, new()" : "")}}
                                          {
                                              private {{(genericState ? "TState" : "PhotoState")}} State { get; } = new();
                                              private AssetAndStateReference @event { get; } = new("photo", new("first"));
                                              public AssetAndStateReference Optional => null;
                                              public AssetAndStateReference Manual { get; } = new("manual", new("third"));
                                              public Query() { State.AddAsset(Manual.Reference, Manual.Asset); }
                                          }
                                          public static class Consumer
                                          {
                                              public static bool Verify()
                                              {
                                                  var query = new Query{{(genericState ? "<PhotoState>" : "")}}();
                                                  var first = query.GetRequest();
                                                  var second = query.GetRequest();
                                                  var runner = new JRK.JevRunner.JevRunner();
                                                  query.Execute(runner).GetAwaiter().GetResult();
                                                  var state = (AssetState)first.State;
                                                  return first.Assets.Count == 3 && second.Assets.Count == 3
                                                      && runner.LastRequest.Assets.Count == 3
                                                      && state["photo"] == "first" && state["inherited"] == "second"
                                                      && state["manual"] == "third";
                                              }
                                          }
                                          """, AssetRuntimeContract);

        var generated = AssertSuccessful(result, output);
        Assert.Contains("this.@event", generated);
        Assert.Contains("this.@Inherited", generated);
        AssertConsumerSucceeds(output);
    }

    /// <summary>
    /// Existing fields and assets with conflicting values are not silently replaced.
    /// </summary>
    /// <param name="sameAssetName">Whether only the registered asset contents differ.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparedAssetConflictsPreserveOriginalRegistration(bool sameAssetName)
    {
        var (result, output) = Generate($$"""
                                          using JRK.JevRunner.Annotation;
                                          using JRK.JevRunner.Infrastructure;
                                          [JevQuery]
                                          public partial class Query
                                          {
                                              public AssetState State { get; } = new();
                                              public AssetAndStateReference Photo { get; } = new("photo", new("image", "new"));
                                              public Query() { State.AddAsset(new("photo"), new("{{(sameAssetName ? "image" : "original")}}", "old")); }
                                          }
                                          public static class Consumer
                                          {
                                              public static bool Verify()
                                              {
                                                  var query = new Query();
                                                  var original = query.State.GetRequiredAsset("photo");
                                                  try { query.GetRequest(); return false; }
                                                  catch (System.InvalidOperationException)
                                                  {
                                                      return object.Equals(original, query.State.GetRequiredAsset("photo"))
                                                          && query.State["photo"] == original.AssetName;
                                                  }
                                              }
                                          }
                                          """, AssetRuntimeContract);

        AssertSuccessful(result, output);
        AssertConsumerSucceeds(output);
    }

    /// <summary>
    /// Non-asset state warns only when prepared properties exist and still emits compilable query methods.
    /// </summary>
    /// <param name="hasAsset">Whether the query declares a prepared asset.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PreparedAssetsWithNonAssetStateWarnWithoutBlockingGeneration(bool hasAsset)
    {
        var (result, output) = Generate($$"""
                                          [JRK.JevRunner.Annotation.JevQuery]
                                          public partial class Query
                                          {
                                              public string State => "state";
                                              {{(hasAsset ? "public JRK.JevRunner.Annotation.AssetAndStateReference Photo => null;" : "")}}
                                          }
                                          """, AssetRuntimeContract);

        if (!hasAsset)
        {
            AssertSuccessful(result, output);
            return;
        }

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal("JEV007", diagnostic.Id);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("AssetState", diagnostic.GetMessage());
        Assert.Equal("public string State => \"state\";",
            diagnostic.Location.SourceTree!.GetText(TestContext.Current.CancellationToken)
                .ToString(diagnostic.Location.SourceSpan));
        Assert.Null(Assert.Single(result.Results).Exception);
        var generated = Assert.Single(result.Results[0].GeneratedSources).SourceText.ToString();
        Assert.DoesNotContain(".AddAsset(", generated);
        Assert.Empty(output.GetDiagnostics(TestContext.Current.CancellationToken)
            .Where(item => item.Severity == DiagnosticSeverity.Error));
    }

    /// <summary>
    /// Prepared properties follow the same readable instance contract as question properties.
    /// </summary>
    /// <param name="property">The invalid prepared property declaration.</param>
    [Theory]
    [InlineData("public static AssetAndStateReference Photo => null;")]
    [InlineData("public AssetAndStateReference Photo { set { } }")]
    [InlineData("public AssetAndStateReference this[int index] => null;")]
    public void UnreadablePreparedAssetsReportInvalidProperty(string property)
    {
        var (result, _) = Generate($$"""
                                     using JRK.JevRunner.Annotation;
                                     [JevQuery]
                                     public partial class Query
                                     {
                                         public JRK.JevRunner.Infrastructure.AssetState State { get; } = new();
                                         {{property}}
                                     }
                                     """, AssetRuntimeContract);

        Assert.Null(Assert.Single(result.Results).Exception);
        Assert.Empty(result.Results[0].GeneratedSources);
        Assert.Equal("JEV002", Assert.Single(result.Diagnostics).Id);
    }

    /// <summary>
    /// Extends the independent test runtime with asset contracts and request-time asset snapshots.
    /// </summary>
    private static readonly string AssetRuntimeContract = RuntimeContract
        .Replace("public Request(string state, string model = \"default-model\") { State = state; Model = model; }",
            """
            public Request(object state, string model = "default-model")
            {
                State = state;
                Model = model;
                if (state is Infrastructure.AssetState assets)
                    Assets.AddRange(assets.Assets.Values);
            }
            public System.Collections.Generic.List<Requests.Asset> Assets { get; } = new();
            """)
        .Replace("public string State { get; }", "public object State { get; }") + """

        namespace JRK.JevRunner
        {
            public record StateReference(string Value);
        }
        namespace JRK.JevRunner.Annotation
        {
            public record AssetAndStateReference(JRK.JevRunner.StateReference Reference, Requests.Asset Asset)
            {
                public AssetAndStateReference(string reference, Requests.Asset asset)
                    : this(new JRK.JevRunner.StateReference(reference), asset) { }
            }
        }
        namespace JRK.JevRunner.Requests
        {
            public record Asset(string AssetName, string Data = "");
        }
        namespace JRK.JevRunner.Infrastructure
        {
            public class AssetState : System.Collections.Generic.Dictionary<string, string>
            {
                public System.Collections.Generic.Dictionary<string, Requests.Asset> Assets { get; } = new();
                public AssetState AddAsset(JRK.JevRunner.StateReference reference, Requests.Asset asset)
                {
                    Add(reference.Value, asset.AssetName);
                    Assets.Add(reference.Value, asset);
                    return this;
                }
                public Requests.Asset GetRequiredAsset(string name) => Assets[name];
            }
        }
        """;
}
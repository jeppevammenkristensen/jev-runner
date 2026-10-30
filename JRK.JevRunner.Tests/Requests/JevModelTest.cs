using JRK.JevRunner.Requests;
using Xunit;

namespace JRK.JevRunner.Tests.Requests;

/// <summary>Verifies model identifiers and exact, case-sensitive model classification.</summary>
public class JevModelTest
{
    /// <summary>Built-in model factories remain aligned with the public version constants.</summary>
    [Fact]
    public void BuiltInModels_MatchVersionConstants()
    {
        Assert.Equal("jev-latest", JevVersions.Latest);
        Assert.Equal("jev-preview", JevVersions.Preview);
        Assert.Equal("jev-1.13.0", JevVersions.Version1_13_0);
        Assert.Equal("jev-1.14.0", JevVersions.Version1_14_0);
        Assert.Equal(JevVersions.Latest, JevModel.Latest.Model);
        Assert.Equal(JevVersions.Preview, JevModel.Preview.Model);
        Assert.Equal(JevVersions.Version1_13_0, JevModel.Version1_13_0.Model);
        Assert.Equal(JevVersions.Version1_14_0, JevModel.Version1_14_0.Model);
        Assert.Equal(4, JevVersions.Versions.Count);
    }

    /// <summary>Only the documented identifiers are known; aliases are not resolved to pinned versions.</summary>
    [Theory]
    [InlineData(JevVersions.Latest, true, false, true)]
    [InlineData(JevVersions.Preview, false, true, true)]
    [InlineData(JevVersions.Version1_13_0, false, false, true)]
    [InlineData(JevVersions.Version1_14_0, false, false, true)]
    [InlineData("jev-future", false, false, false)]
    [InlineData("JEV-LATEST", false, false, false)]
    [InlineData("", false, false, false)]
    public void ModelInspection_ClassifiesExactIdentifier(string identifier, bool latest, bool preview, bool known)
    {
        var model = new JevModel(identifier);

        Assert.Equal(latest, model.IsLatest);
        Assert.Equal(preview, model.IsPreview);
        Assert.Equal(known, model.IsKnownVersion);
        Assert.True(model.IsVersion(identifier));
        Assert.False(model.IsVersion(identifier + "-other"));
        Assert.Equal(identifier == JevVersions.Version1_14_0, model.IsVersion(JevVersions.Version1_14_0));
    }
}
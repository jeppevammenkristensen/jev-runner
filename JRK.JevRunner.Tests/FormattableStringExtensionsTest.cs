using System;
using JetBrains.Annotations;
using JRK.JevRunner.Annotation;
using JRK.JevRunner.Requests;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies interpolation of state references, paths, and ordinary arguments.</summary>
[TestSubject(typeof(FormattableStringExtensions))]
public class FormattableStringExtensionsTest
{
    /// <summary>
    /// Prepared assets interpolate as their quoted state field rather than their asset name or record representation.
    /// </summary>
    [Fact]
    public void Escaped_AssetAndStateReference_UsesQuotedStateReference()
    {
        var prepared = new AssetAndStateReference("photo", new ImageAsset("image-id", "png", new Base64String("AQID")));
        FormattableString message = $"Inspect {prepared} and {prepared}.";

        Assert.Equal("Inspect `photo` and `photo`.", message.Escaped);
    }

    [Fact]
    public void Escaped_QuotesReferencesAndPathsAndPreservesOtherArguments()
    {
        StateReference reference = "photo";
        var path = new StatePath("frames[0].photo");
        var count = 2;
        FormattableString message = $"Compare {reference} and {path}: {count} views.";

        Assert.Equal("Compare `photo` and `frames[0].photo`: 2 views.", message.Escaped);
    }

    [Fact]
    public void Escaped_PreservesFormatSpecifiersAlignmentAndEscapedBraces()
    {
        StateReference reference = "photo";
        var count = 7;
        FormattableString message = $"{{{reference}}}: {count,4:D3}";

        Assert.Equal("{`photo`}:  007", message.Escaped);
    }

    [Fact]
    public void Escaped_FormatsNullAndNonReferenceArgumentsWithoutBackticks()
    {
        string? missing = null;
        var text = "photo";
        FormattableString message = $"Name: {text}; missing: {missing}.";

        Assert.Equal("Name: photo; missing: .", message.Escaped);
    }
}

using System;
using JetBrains.Annotations;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies state-reference conversion, quoting, and validation.</summary>
[TestSubject(typeof(StateReference))]
public class StateReferenceTest
{
    [Fact]
    public void ImplicitConversion_PreservesValue()
    {
        StateReference name = "photo";

        Assert.Equal("photo", name.Value);
    }

    [Theory]
    [InlineData("p", "`p`")]
    [InlineData("photo", "`photo`")]
    public void BackTickQuoted_WrapsValueInBackticks(string value, string expected)
    {
        Assert.Equal(expected, new StateReference(value).BackTickQuoted);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_RejectsEmptyOrWhitespaceValue(string value)
    {
        Assert.Throws<ArgumentException>(() => new StateReference(value));
    }

    [Fact]
    public void Constructor_RejectsNullValue()
    {
        Assert.Throws<ArgumentNullException>(() => new StateReference(null!));
    }

    [Theory]
    [InlineData("my photo")]
    [InlineData("photo\tname")]
    [InlineData("photo\nname")]
    [InlineData("my`photo")]
    public void Constructor_RejectsWhitespaceOrBackticksInValue(string value)
    {
        Assert.Throws<InvalidOperationException>(() => new StateReference(value));
    }
}

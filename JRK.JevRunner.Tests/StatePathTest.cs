using System;
using JetBrains.Annotations;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies state-path preservation, interpolation, and validation.</summary>
[TestSubject(typeof(StatePath))]
public class StatePathTest
{
    [Theory]
    [InlineData("p")]
    [InlineData("photo")]
    [InlineData("ticket.messages[0].text")]
    public void Constructor_PreservesPath(string path)
    {
        Assert.Equal(path, new StatePath(path).Path);
    }

    [Fact]
    public void Escaped_QuotesPath()
    {
        var path = new StatePath("ticket.messages[0].text");
        FormattableString message = $"Evaluate {path}.";

        Assert.Equal("Evaluate `ticket.messages[0].text`.", message.Escaped);
    }

    [Fact]
    public void Constructor_RejectsNullPath()
    {
        Assert.Throws<ArgumentNullException>(() => new StatePath(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Constructor_RejectsEmptyOrWhitespacePath(string path)
    {
        Assert.Throws<ArgumentException>(() => new StatePath(path));
    }

    [Theory]
    [InlineData("ticket. messages")]
    [InlineData("ticket\tmessages")]
    [InlineData("ticket\nmessages")]
    [InlineData("ticket.`messages`")]
    public void Constructor_RejectsWhitespaceOrBackticksInPath(string path)
    {
        Assert.Throws<InvalidOperationException>(() => new StatePath(path));
    }
}

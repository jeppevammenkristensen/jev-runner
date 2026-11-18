using System;
using System.Linq.Expressions;
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
    [InlineData("ticket.messages\n")]
    [InlineData("ticket.messages\r\n")]
    [InlineData("ticket.`messages`")]
    public void Constructor_RejectsWhitespaceOrBackticksInPath(string path)
    {
        Assert.Throws<InvalidOperationException>(() => new StatePath(path));
    }

    /// <summary>
    /// Property selectors omit the lambda parameter name and do not read the state instance.
    /// </summary>
    [Fact]
    public void GetPath_PropertySelectors_ReturnPathWithoutReadingState()
    {
        SampleState state = null!;

        Assert.Equal("Name", state.GetPath(root => root.Name).Path);
        Assert.Equal("Primary.Name", state.GetPath(root => root.Primary.Name).Path);
    }

    /// <summary>
    /// Constant array indexes appear in brackets, including when the path ends at an array element.
    /// </summary>
    [Fact]
    public void GetPath_ArraySelectors_PreserveIndexes()
    {
        SampleState state = null!;

        Assert.Equal("Groups[0]", state.GetPath(root => root.Groups[0]).Path);
        Assert.Equal("Groups[1].Items[2].Name", state.GetPath(root => root.Groups[1].Items[2].Name).Path);
    }

    /// <summary>
    /// Paths built from selectors receive the same backtick quoting as manually specified paths.
    /// </summary>
    [Fact]
    public void GetPath_Interpolated_QuotesSelectedPath()
    {
        SampleState state = null!;
        FormattableString instructions = $"Evaluate {state.GetPath(root => root.Groups[0].Items[1].Name)}.";

        Assert.Equal("Evaluate `Groups[0].Items[1].Name`.", instructions.Escaped);
    }

    /// <summary>
    /// A null selector is rejected before the expression is inspected.
    /// </summary>
    [Fact]
    public void GetPath_NullSelector_Throws()
    {
        SampleState state = null!;

        Assert.Throws<ArgumentNullException>("expression", () => state.GetPath<SampleState, string>(null!));
    }

    /// <summary>
    /// A selector must select a member rather than the root state itself.
    /// </summary>
    [Fact]
    public void GetPath_RootSelector_Throws()
    {
        SampleState state = null!;

        Assert.Throws<ArgumentException>("expression", () => state.GetPath(root => root));
    }

    /// <summary>
    /// Captured and state-derived indexes are not supported.
    /// </summary>
    [Fact]
    public void GetPath_NonConstantArrayIndexes_Throw()
    {
        SampleState state = null!;
        var index = 1;

        Assert.Throws<ArgumentException>("expression", () => state.GetPath(root => root.Groups[index]));
        Assert.Throws<ArgumentException>("expression", () => state.GetPath(root => root.Groups[root.Index]));
    }

    /// <summary>
    /// A negative constant index is rejected even when the selector is never executed.
    /// </summary>
    [Fact]
    public void GetPath_NegativeArrayIndex_Throws()
    {
        SampleState state = null!;
        var root = Expression.Parameter(typeof(SampleState), "root");
        var groups = Expression.Property(root, nameof(SampleState.Groups));
        var element = Expression.ArrayIndex(groups, Expression.Constant(-1));
        var selector = Expression.Lambda<Func<SampleState, ItemGroup>>(element, root);

        Assert.Throws<ArgumentException>("expression", () => state.GetPath(selector));
    }

    /// <summary>
    /// Method calls and properties rooted outside the lambda parameter are not state paths.
    /// </summary>
    [Fact]
    public void GetPath_UnsupportedSelectors_Throw()
    {
        SampleState state = null!;

        Assert.Throws<ArgumentException>("expression", () => state.GetPath(root => root.Name.Trim()));
        Assert.Throws<ArgumentException>("expression", () => state.GetPath(root => state.Name));
    }

    private sealed record SampleState(string Name, Item Primary, ItemGroup[] Groups, int Index);
    private sealed record ItemGroup(Item[] Items);
    private sealed record Item(string Name);
}

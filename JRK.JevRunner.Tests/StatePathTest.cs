using System;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JRK.JevRunner.Requests;
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
    /// A root array can be indexed directly or followed by a property access or another index.
    /// </summary>
    [Fact]
    public void GetPath_RootArraySelectors_StartWithIndex()
    {
        Item[] state = null!;
        Item[][] jaggedState = null!;

        Assert.Equal("[0]", state.GetPath(root => root[0]).Path);
        Assert.Equal("[1].Name", state.GetPath(root => root[1].Name).Path);
        Assert.Equal("[1][2].Name", jaggedState.GetPath(root => root[1][2].Name).Path);
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

    /// <summary>
    /// Parameterless selectors omit the instruction instance and preserve property chains and indexes.
    /// </summary>
    [Fact]
    public void GetPath_ParameterlessSelector_ReturnsPathWithoutReadingInstruction()
    {
        var instruction = new SampleInstruction();

        Assert.Equal("SomeProperty", instruction.SelectArray().Path);
        Assert.Equal("SomeProperty[0].Text", instruction.SelectText().Path);
        Assert.Equal("SomeProperty[0]", instruction.SelectElement().Path);
    }

    /// <summary>
    /// Parameterless selectors follow the same constant-index restriction as parameterized selectors.
    /// </summary>
    [Fact]
    public void GetPath_ParameterlessSelector_RejectsNonConstantIndex()
    {
        Assert.Throws<ArgumentException>("expression", () => new SampleInstruction().SelectUsingIndex(0));
    }

    /// <summary>
    /// Instruction interpolation wraps the parameterless state path in backticks.
    /// </summary>
    [Fact]
    public void GetPath_ParameterlessSelector_QuotesPathInInstruction()
    {
        Assert.Equal("Evaluate `SomeProperty[0].Text`.", new SampleInstruction().Render());
    }

    /// <summary>
    /// Exposes selectors over an instruction property that must not be evaluated.
    /// </summary>
    private sealed class SampleInstruction : Instruction
    {
        private TextItem[] SomeProperty => throw new InvalidOperationException("The selector must not read the state.");

        public StatePath SelectArray() => StateExtensions.GetPath(() => SomeProperty);

        public StatePath SelectText() => StateExtensions.GetPath(() => SomeProperty[0].Text);

        public StatePath SelectElement() => StateExtensions.GetPath(() => SomeProperty[0]);

        public StatePath SelectUsingIndex(int index) => StateExtensions.GetPath(() => SomeProperty[index]);

        public string Render() => FormatInstruction($"Evaluate {GetInstructionPath(() => SomeProperty[0].Text)}.");
    }

    private sealed record TextItem(string Text);

    private sealed record SampleState(string Name, Item Primary, ItemGroup[] Groups, int Index);
    private sealed record ItemGroup(Item[] Items);
    private sealed record Item(string Name);
}

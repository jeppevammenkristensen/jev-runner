using System;
using JetBrains.Annotations;
using JRK.JevRunner.Requests;
using Xunit;

namespace JRK.JevRunner.Tests.Requests;

[TestSubject(typeof(JevMessage))]
public class JevMessageTest
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Node_FormatsFormattableStringArrayElements(bool combine)
    {
        StateReference name = "photo";
        var path = new StatePath("frames[0].photo");
        FormattableString content = $"Compare {name} with {path} in {2:D2} sentences.";
        JevMessage message = new(new object[] { "Instructions", content, new { Count = 2 } });
        if (combine)
            message = message.CombineWith(new JevMessage("End"));

        var node = message.Node.AsArray();

        Assert.Equal(combine ? 4 : 3, node.Count);
        Assert.Equal("Instructions", node[0]!.GetValue<string>());
        Assert.Equal("Compare `photo` with `frames[0].photo` in 02 sentences.", node[1]!.GetValue<string>());
        Assert.Equal(2, node[2]!["Count"]!.GetValue<int>());
        if (combine)
            Assert.Equal("End", node[3]!.GetValue<string>());
    }

    [Fact]
    public void Node_PreservesNullArrayElements()
    {
        JevMessage message = new(new object[] { "Before", null!, "After" });

        var node = message.Node.AsArray();

        Assert.Equal(3, node.Count);
        Assert.Equal("Before", node[0]!.GetValue<string>());
        Assert.Null(node[1]);
        Assert.Equal("After", node[2]!.GetValue<string>());
    }

    [Fact]
    public void Node_SerializesFormattableStringWithQuotedStateReferenceAndPath()
    {
        StateReference name = "photo";
        var path = new StatePath("frames[0].photo");
        var count = 2;
        FormattableString content = $"Compare {name} with {path} in {count} sentences.";
        JevMessage message = new(content);

        Assert.Equal("Compare `photo` with `frames[0].photo` in 2 sentences.", message.Node.GetValue<string>());
    }

    [Fact]
    public void CombineWith_FormatsFormattableStringsOnBothSides()
    {
        StateReference first = "front";
        var second = new StatePath("frames[1].photo");
        FormattableString firstContent = $"Describe {first}.";
        FormattableString secondContent = $"Compare with {second}.";
        JevMessage firstMessage = new(firstContent);
        JevMessage secondMessage = new(secondContent);

        var node = firstMessage.CombineWith(secondMessage).Node.AsArray();

        Assert.Equal(2, node.Count);
        Assert.Equal("Describe `front`.", node[0]!.GetValue<string>());
        Assert.Equal("Compare with `frames[1].photo`.", node[1]!.GetValue<string>());
    }

    [Fact]
    public void Node_SerializesNamedValueTupleUsingRuntimeFieldNames()
    {
        JevMessage message = new((Name: "Alice", Age: 42));

        var node = message.Node.AsObject();

        Assert.Equal(2, node.Count);
        Assert.Equal("Alice", node["Item1"]!.GetValue<string>());
        Assert.Equal(42, node["Item2"]!.GetValue<int>());
    }

    [Fact]
    public void Node_SerializesExplicitValueTupleItems()
    {
        var state = new System.ValueTuple<string, int>("Alice", 42);
        JevMessage message = new(state);

        var node = message.Node.AsObject();

        Assert.Equal(2, node.Count);
        Assert.Equal("Alice", node["Item1"]!.GetValue<string>());
        Assert.Equal(42, node["Item2"]!.GetValue<int>());
    }

    [Fact]
    public void Node_SerializesNestedValueTupleItems()
    {
        JevMessage message = new(("Alice", (42, true)));

        var node = message.Node.AsObject();

        Assert.Equal("Alice", node["Item1"]!.GetValue<string>());
        var nested = node["Item2"]!.AsObject();
        Assert.Equal(42, nested["Item1"]!.GetValue<int>());
        Assert.True(nested["Item2"]!.GetValue<bool>());
    }

    [Fact]
    public void Node_SerializesReferenceTupleItems()
    {
        JevMessage message = new(System.Tuple.Create("Alice", 42));

        var node = message.Node.AsObject();

        Assert.Equal(2, node.Count);
        Assert.Equal("Alice", node["Item1"]!.GetValue<string>());
        Assert.Equal(42, node["Item2"]!.GetValue<int>());
    }

}

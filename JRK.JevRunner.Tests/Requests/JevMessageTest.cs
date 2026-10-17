using JetBrains.Annotations;
using JRK.JevRunner.Requests;
using Xunit;

namespace JRK.JevRunner.Tests.Requests;

[TestSubject(typeof(JevMessage))]
public class JevMessageTest
{
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
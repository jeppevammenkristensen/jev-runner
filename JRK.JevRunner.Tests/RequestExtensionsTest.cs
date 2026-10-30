using System.Buffers.Text;
using JRK.JevRunner.Requests;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies request feature detection and fluent long-context configuration.</summary>
public class RequestExtensionsTest
{
    /// <summary>Long context is opt-in and enabling it repeatedly preserves the request and its contents.</summary>
    [Fact]
    public void EnableLongContext_IsChainableAndIdempotent()
    {
        var request = new Request("state").AddQuestion(new NoulQuestion("visible", "Is it visible?", null));
        var state = request.State;
        var model = request.Model;
        var question = Assert.Single(request.Questions);

        Assert.False(request.LongContextEnabled);
        Assert.False(request.StrictValidation);
        Assert.Same(request, request.EnableLongContext());
        Assert.Same(request, request.EnableLongContext());
        Assert.True(request.LongContextEnabled);
        Assert.Equal(state, request.State);
        Assert.Same(model, request.Model);
        Assert.Equal(question, Assert.Single(request.Questions));
        Assert.Empty(request.Assets);
    }

    /// <summary>Assets and Box questions independently select image opt-in; ordinary questions do not.</summary>
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void FeatureDetection_ReflectsCurrentQuestionsAndAssets(bool includeBox, bool includeAsset)
    {
        var request = new Request("state").AddQuestion(new NoulQuestion("visible", "Is it visible?", null));
        if (includeBox)
            request.AddQuestion(new BoxQuestion("box", "Find the object"));
        if (includeAsset)
            request.Assets.Add(new ImageAsset("photo", "jpg", (Base64String) "AQID"));

        Assert.Equal(includeBox, request.HasBoxQuestion);
        Assert.Equal(includeBox || includeAsset, request.HasImages);

        request.Questions.Clear();
        request.Assets.Clear();

        Assert.False(request.HasBoxQuestion);
        Assert.False(request.HasImages);
    }
}
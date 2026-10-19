using JRK.JevRunner.Image;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies result validity and scaling suggestions from typed error collections without image decoding.</summary>
public class ImageValidationResultTest
{
    /// <summary>An empty error collection is valid and needs no scaling.</summary>
    [Fact]
    public void NoErrors_IsValidAndNeedsNoScaling()
    {
        var result = new ImageValidationResult("png", 280, 280, 100, []);
        Assert.True(result.IsValid);
        Assert.Equal(ImageSuggestedScaling.None, result.SuggestedScaling);
    }

    /// <summary>String errors affect validity but do not themselves suggest a scaling direction.</summary>
    [Fact]
    public void StringError_IsInvalidWithoutDimensionSuggestion()
    {
        var result = new ImageValidationResult(null, null, null, 100, ["Invalid image."]);
        Assert.False(result.IsValid);
        Assert.Equal(ImageSuggestedScaling.None, result.SuggestedScaling);
    }

    /// <summary>Identical violations on both axes suggest one direction, even alongside a string error.</summary>
    [Theory]
    [InlineData(ImageDimensionViolation.ToSmall, ImageSuggestedScaling.Upscale)]
    [InlineData(ImageDimensionViolation.ToHigh, ImageSuggestedScaling.Downscale)]
    public void SameDirectionErrors_SuggestScaling(ImageDimensionViolation violation, ImageSuggestedScaling expected)
    {
        var limit = violation == ImageDimensionViolation.ToSmall ? 280 : 1568;
        var actual = violation == ImageDimensionViolation.ToSmall ? 100 : 2000;
        var result = new ImageValidationResult("png", actual, actual, 100,
        [
            new ImageDimensionError(ImageDimension.Width, violation, limit, actual),
            "Another validation failure.",
            new ImageDimensionError(ImageDimension.Height, violation, limit, actual)
        ]);
        Assert.False(result.IsValid);
        Assert.Equal(expected, result.SuggestedScaling);
    }

    /// <summary>Opposing dimension violations cannot be addressed by a single direction.</summary>
    [Theory]
    [InlineData(ImageDimension.Width, ImageDimension.Height)]
    [InlineData(ImageDimension.Height, ImageDimension.Width)]
    public void OpposingErrors_ScalingNotPossible(ImageDimension smaller, ImageDimension larger)
    {
        var width = smaller == ImageDimension.Width ? 100 : 2000;
        var height = smaller == ImageDimension.Height ? 100 : 2000;
        var result = new ImageValidationResult("png", width, height, 100,
        [
            new ImageDimensionError(smaller, ImageDimensionViolation.ToSmall, 280, 100),
            new ImageDimensionError(larger, ImageDimensionViolation.ToHigh, 1568, 2000)
        ]);
        Assert.False(result.IsValid);
        Assert.Equal(ImageSuggestedScaling.ScalingNotPossible, result.SuggestedScaling);
    }
}

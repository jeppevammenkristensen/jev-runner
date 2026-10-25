using System;
using JRK.JevRunner.Image;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies result validity and scaling suggestions from typed error collections without image decoding.</summary>
public class ImageValidationResultTest
{
    /// <summary>The valid case reports success and does not throw.</summary>
    [Fact]
    public void ValidResult_IsValidAndDoesNotThrow()
    {
        ImageValidationResult result = new ValidImageResult("png", 280, 280, 100, []);
        Assert.True(result.IsValid);
        result.ThrowIfInvalid();
    }

    /// <summary>String errors affect validity but do not themselves suggest a scaling direction.</summary>
    [Fact]
    public void StringError_IsInvalidWithoutDimensionSuggestion()
    {
        var result = new InvalidImageResult(["Invalid image."]);
        ImageValidationResult validation = result;
        Assert.False(validation.IsValid);
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
        var result = new InvalidImageResult(
        [
            new ImageDimensionError(ImageDimension.Width, violation, limit, actual),
            "Another validation failure.",
            new ImageDimensionError(ImageDimension.Height, violation, limit, actual)
        ]);
        ImageValidationResult validation = result;
        Assert.False(validation.IsValid);
        Assert.Equal(expected, result.SuggestedScaling);
    }

    /// <summary>Opposing dimension violations cannot be addressed by a single direction.</summary>
    [Theory]
    [InlineData(ImageDimension.Width, ImageDimension.Height)]
    [InlineData(ImageDimension.Height, ImageDimension.Width)]
    public void OpposingErrors_ScalingNotPossible(ImageDimension smaller, ImageDimension larger)
    {
        var result = new InvalidImageResult(
        [
            new ImageDimensionError(smaller, ImageDimensionViolation.ToSmall, 280, 100),
            new ImageDimensionError(larger, ImageDimensionViolation.ToHigh, 1568, 2000)
        ]);
        ImageValidationResult validation = result;
        Assert.False(validation.IsValid);
        Assert.Equal(ImageSuggestedScaling.ScalingNotPossible, result.SuggestedScaling);
    }

    /// <summary>The invalid case throws with each validation error message.</summary>
    [Fact]
    public void InvalidResult_ThrowIfInvalid_IncludesAllErrors()
    {
        var dimensionError = new ImageDimensionError(ImageDimension.Width, ImageDimensionViolation.ToSmall, 280, 100);
        ImageValidationResult result = new InvalidImageResult([dimensionError, "Invalid image."]);

        var exception = Assert.Throws<InvalidOperationException>(() => result.ThrowIfInvalid());

        Assert.Contains(dimensionError.ToString(), exception.Message);
        Assert.Contains("Invalid image.", exception.Message);
    }
}

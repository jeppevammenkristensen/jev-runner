using System.Linq;
using JRK.JevRunner.Image;
using SkiaSharp;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies typed dimension errors independently of image decoding and file access.</summary>
public class ImageDimensionErrorTest
{
    /// <summary>Each axis reports its violated limit and actual value.</summary>
    [Theory]
    [InlineData(279, 500, ImageDimension.Width, ImageDimensionViolation.ToSmall, 280, 279)]
    [InlineData(1569, 500, ImageDimension.Width, ImageDimensionViolation.ToHigh, 1568, 1569)]
    [InlineData(500, 279, ImageDimension.Height, ImageDimensionViolation.ToSmall, 280, 279)]
    [InlineData(500, 1569, ImageDimension.Height, ImageDimensionViolation.ToHigh, 1568, 1569)]
    public void GetErrors_OutsideBounds_ReturnsTypedError(int width, int height, ImageDimension dimension,
        ImageDimensionViolation violation, int limit, int actual)
    {
        var error = Assert.Single(ImageDimensionError.GetErrors(new SKImageInfo(width, height), 280, 1568));
        Assert.True(error is ImageDimensionError);
        if (error is ImageDimensionError details)
            Assert.Equal(new ImageDimensionError(dimension, violation, limit, actual), details);
    }

    /// <summary>Both boundaries are inclusive on both axes.</summary>
    [Theory]
    [InlineData(280, 280)]
    [InlineData(1568, 1568)]
    [InlineData(280, 1568)]
    [InlineData(1568, 280)]
    [InlineData(500, 500)]
    public void GetErrors_WithinBounds_ReturnsNoErrors(int width, int height) =>
        Assert.Empty(ImageDimensionError.GetErrors(new SKImageInfo(width, height), 280, 1568));

    /// <summary>Width and height violations are both returned, in deterministic order.</summary>
    [Theory]
    [InlineData(100, 200)]
    [InlineData(2000, 3000)]
    [InlineData(100, 2000)]
    public void GetErrors_BothAxesInvalid_ReturnsWidthThenHeight(int width, int height)
    {
        var errors = ImageDimensionError.GetErrors(new SKImageInfo(width, height), 280, 1568).ToArray();
        Assert.Equal(2, errors.Length);
        Assert.True(errors[0] is ImageDimensionError { Dimension: ImageDimension.Width });
        Assert.True(errors[1] is ImageDimensionError { Dimension: ImageDimension.Height });
    }

    /// <summary>The supplied limits, not fixed API constants, determine the errors.</summary>
    [Fact]
    public void GetErrors_CustomBounds_UsesSuppliedLimits()
    {
        var errors = ImageDimensionError.GetErrors(new SKImageInfo(99, 901), 100, 900).ToArray();
        Assert.Equal(2, errors.Length);
        Assert.True(errors[0] is ImageDimensionError { Limit: 100, Actual: 99 });
        Assert.True(errors[1] is ImageDimensionError { Limit: 900, Actual: 901 });
    }
}

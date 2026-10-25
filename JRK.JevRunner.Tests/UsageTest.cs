using System.Collections.Generic;
using JetBrains.Annotations;
using Xunit;

namespace JRK.JevRunner.Tests;

/// <summary>Verifies token usage costs using prices quoted per million tokens.</summary>
[TestSubject(typeof(Usage))]
public class UsageTest
{
    /// <summary>Input and output tokens contribute independently at their respective per-million rates.</summary>
    [Theory]
    [InlineData(0L, 0L, 0L)]
    [InlineData(100L, 0L, 250L)]
    [InlineData(0L, 100L, 1000L)]
    [InlineData(120L, 30L, 600L)]
    [InlineData(1_000_000L, 1_000_000L, 12_500_000L)]
    [InlineData(3_000_000_000L, 4_000_000_000L, 47_500_000_000L)]
    public void CalculateCost_PerMillionRates_ReturnsExpectedCost(long inputTokens, long outputTokens,
        long expectedMillionths)
    {
        var usage = new Usage(inputTokens, outputTokens);
        var price = new JevPrice(2.5m, 10m);

        var cost = usage.CalculateCost(price);

        Assert.Equal(expectedMillionths / 1_000_000m, cost);
    }

    /// <summary>Zero prices produce no charge even when tokens were consumed.</summary>
    [Fact]
    public void CalculateCost_ZeroPrices_ReturnsZero()
    {
        var usage = new Usage(120, 30);

        var cost = usage.CalculateCost(new JevPrice(0m, 0m));

        Assert.Equal(0m, cost);
    }

    /// <summary>The default output rate leaves output tokens uncharged.</summary>
    [Fact]
    public void CalculateCost_DefaultOutputPrice_ChargesOnlyInputTokens()
    {
        var usage = new Usage(1_000_000, 2_000_000);

        var cost = usage.CalculateCost(new JevPrice(2.5m));

        Assert.Equal(2.5m, cost);
    }
}

/// <summary>Verifies aggregation of input and output token counts.</summary>
[TestSubject(typeof(UsageExtensions))]
public class UsageExtensionsTest
{
    /// <summary>An empty sequence has no input or output tokens.</summary>
    [Fact]
    public void SumUsage_EmptySequence_ReturnsZero()
    {
        Usage[] usages = [];

        var result = usages.SumUsage();

        Assert.Equal(Usage.Zero, result);
    }

    /// <summary>A single usage retains both token counts.</summary>
    [Fact]
    public void SumUsage_SingleUsage_ReturnsEquivalentUsage()
    {
        Usage[] usages = [new Usage(120, 30)];

        var result = usages.SumUsage();

        Assert.Equal(new Usage(120, 30), result);
    }

    /// <summary>Aggregation sums each token category without changing the original usages.</summary>
    [Fact]
    public void SumUsage_MultipleUsages_SumsTokenCountsIndependently()
    {
        Usage[] usages = [new Usage(120, 30), Usage.Zero, new Usage(80, 0), new Usage(0, 70)];

        var result = usages.SumUsage();

        Assert.Equal(new Usage(200, 100), result);
        Assert.Equal(new Usage(120, 30), usages[0]);
        Assert.Equal(Usage.Zero, usages[1]);
        Assert.Equal(new Usage(80, 0), usages[2]);
        Assert.Equal(new Usage(0, 70), usages[3]);
    }

    /// <summary>Lazy sequences are supported and totals retain counts beyond the 32-bit integer range.</summary>
    [Fact]
    public void SumUsage_DeferredSequenceWithLargeCounts_ReturnsLongTotals()
    {
        var result = EnumerateUsages().SumUsage();

        Assert.Equal(new Usage(4_000_000_000L, 6_000_000_000L), result);
    }

    private static IEnumerable<Usage> EnumerateUsages()
    {
        yield return new Usage(2_000_000_000L, 3_000_000_000L);
        yield return new Usage(2_000_000_000L, 3_000_000_000L);
    }
}

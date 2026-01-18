using System;
using Xunit;
using HarfRust;

namespace HarfRust.Tests;

public class ApiEnhancementTests
{
    [Fact]
    public void Feature_Helpers_CreateCorrectTags()
    {
        var liga = Feature.StandardLigatures(true);
        Assert.Equal(CreateTag("liga"), liga.Tag);
        Assert.Equal(1u, liga.Value);

        var dlig = Feature.DiscretionaryLigatures(true);
        Assert.Equal(CreateTag("dlig"), dlig.Tag);

        var kern = Feature.Kerning(false);
        Assert.Equal(CreateTag("kern"), kern.Tag);
        Assert.Equal(0u, kern.Value);

        var smcp = Feature.SmallCaps(true);
        Assert.Equal(CreateTag("smcp"), smcp.Tag);

        var ss01 = Feature.StylisticSet(1, true);
        Assert.Equal(CreateTag("ss01"), ss01.Tag);

        var ss20 = Feature.StylisticSet(20, true);
        Assert.Equal(CreateTag("ss20"), ss20.Tag);
    }

    [Fact]
    public void Feature_StylisticSet_ThrowsOnInvalidIndex()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Feature.StylisticSet(0, true));
        Assert.Throws<ArgumentOutOfRangeException>(() => Feature.StylisticSet(21, true));
    }

    [Fact]
    public void Variation_Helpers_CreateCorrectTags()
    {
        var wght = Variation.Weight(400);
        Assert.Equal(CreateTag("wght"), wght.Tag);
        Assert.Equal(400f, wght.Value);

        var wdth = Variation.Width(100);
        Assert.Equal(CreateTag("wdth"), wdth.Tag);

        var slnt = Variation.Slant(0);
        Assert.Equal(CreateTag("slnt"), slnt.Tag);

        var opsz = Variation.OpticalSize(12);
        Assert.Equal(CreateTag("opsz"), opsz.Tag);

        var ital = Variation.Italic(1);
        Assert.Equal(CreateTag("ital"), ital.Tag);
    }

    private static uint CreateTag(string tag) => HarfRustBuffer.CreateScriptTag(tag);
}

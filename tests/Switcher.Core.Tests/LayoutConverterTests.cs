using Switcher.Core;

namespace Switcher.Core.Tests;

public class LayoutConverterTests
{
    private readonly LayoutConverter _converter = new();

    [Fact]
    public void Convert_ConvertsEnglishWordToUkrainian()
    {
        var actual = _converter.Convert("ghbdsn");
        Assert.Equal("привіт", actual);
    }

    [Fact]
    public void Convert_ConvertsUkrainianWordToEnglish()
    {
        var actual = _converter.Convert("привіт");
        Assert.Equal("ghbdsn", actual);
    }

    [Fact]
    public void Convert_PreservesPunctuationSpacesAndDigits()
    {
        var actual = _converter.Convert("ghbdsn, 123!");
        Assert.Equal("привіт, 123!", actual);
    }

    [Fact]
    public void Convert_IsReversibleForSupportedLetters()
    {
        const string source = "PryvitSvit";
        var once = _converter.Convert(source);
        var twice = _converter.Convert(once);

        Assert.Equal(source, twice);
    }

    [Fact]
    public void Convert_HandlesEmptyString()
    {
        var actual = _converter.Convert(string.Empty);
        Assert.Equal(string.Empty, actual);
    }
}

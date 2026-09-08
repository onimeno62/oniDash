using oniDash.Movies.Cataloging;

namespace oniDash.Movies.Tests;

public sealed class MovieNameParserTests
{
    [Theory]
    [InlineData("The Matrix (1999).mkv", "The Matrix", 1999)]
    [InlineData("Blade Runner (2049).mp4", "Blade Runner", 2049)]
    [InlineData("1941 (1979).mkv", "1941", 1979)]
    [InlineData("2001.A.Space.Odyssey.1968.1080p.BluRay.x264.mkv", "2001 A Space Odyssey", 1968)]
    [InlineData("Edge.of.Tomorrow.2014.1080p.WEB-DL.x264-GRP.mkv", "Edge of Tomorrow", 2014)]
    [InlineData("Interstellar 2014 1080p BRRip x264.mp4", "Interstellar", 2014)]
    [InlineData("The Terminator 1984 PART1.avi", "The Terminator", 1984)]
    [InlineData("A Quiet Day.avi", "A Quiet Day", null)]
    [InlineData("contact.mkv", "contact", null)]
    [InlineData("2012 (2009).mkv", "2012", 2009)]
    public void Parses_title_and_year_from_release_names(string fileName, string expectedTitle, int? expectedYear)
    {
        var info = MovieNameParser.Parse(fileName);

        Assert.Equal(expectedTitle, info.Title);
        Assert.Equal(expectedYear, info.Year);
    }
}

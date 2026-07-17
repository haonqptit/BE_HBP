using System.Text.RegularExpressions;
using HBP.Application.Abstractions;
using HBP.Infrastructure.Requests;

namespace HBP.UnitTests;

public sealed class ReferenceCodeGeneratorTests
{
    private sealed class Clock : IClock { public DateTimeOffset UtcNow => new(2026, 7, 17, 0, 0, 0, TimeSpan.Zero); }

    [Fact]
    public void GeneratesExpectedFormatsAndDistinctCodes()
    {
        var generator = new ReferenceCodeGenerator(new Clock());
        var first = generator.GenerateBookingCode();
        var second = generator.GenerateBookingCode();
        Assert.Matches(new Regex(@"^BK-260717-[0-9A-HJKMNP-TV-Z]{6}$"), first);
        Assert.Matches(new Regex(@"^CT-260717-[0-9A-HJKMNP-TV-Z]{6}$"), generator.GenerateContactCode());
        Assert.NotEqual(first, second);
    }
}

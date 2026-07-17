using HBP.Application.Email;

namespace HBP.UnitTests;

public sealed class EmailBackoffTests
{
    [Theory]
    [InlineData(1, 60)]
    [InlineData(2, 300)]
    [InlineData(3, 1800)]
    [InlineData(4, 7200)]
    [InlineData(5, 21600)]
    public void ReturnsExpectedDelay(int attempt, int seconds) =>
        Assert.Equal(TimeSpan.FromSeconds(seconds), EmailBackoff.ForAttempt(attempt));

    [Fact] public void SixthAttemptHasNoRetry() => Assert.Null(EmailBackoff.ForAttempt(6));
}

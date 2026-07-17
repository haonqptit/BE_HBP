using HBP.Infrastructure.Auth;

namespace HBP.UnitTests;

public sealed class PasswordHasherAdapterTests
{
    [Fact]
    public void Hash_RoundTrips_AndUsesRandomSalt()
    {
        var hasher = new PasswordHasherAdapter();
        var first = hasher.Hash("correct horse battery staple");
        var second = hasher.Hash("correct horse battery staple");
        Assert.True(hasher.Verify(first, "correct horse battery staple"));
        Assert.False(hasher.Verify(first, "wrong"));
        Assert.NotEqual(first, second);
    }
}

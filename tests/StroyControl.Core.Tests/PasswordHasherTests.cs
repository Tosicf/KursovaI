using StroyControl.Core.Security;
using Xunit;

namespace StroyControl.Core.Tests;

public sealed class PasswordHasherTests
{
    [Fact]
    public void Verify_ReturnsTrueForSamePassword()
    {
        var (hash, salt) = PasswordHasher.CreateHash("admin123");

        Assert.True(PasswordHasher.Verify("admin123", salt, hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForWrongPassword()
    {
        var (hash, salt) = PasswordHasher.CreateHash("admin123");

        Assert.False(PasswordHasher.Verify("admin124", salt, hash));
    }

    [Fact]
    public void CreateHash_UsesUniqueSalt()
    {
        var first = PasswordHasher.CreateHash("prorab123");
        var second = PasswordHasher.CreateHash("prorab123");

        Assert.NotEqual(first.Salt, second.Salt);
        Assert.NotEqual(first.Hash, second.Hash);
        Assert.True(PasswordHasher.Verify("prorab123", second.Salt, second.Hash));
    }

    [Fact]
    public void Verify_ReturnsFalseForGarbageValues()
    {
        Assert.False(PasswordHasher.Verify("x", "not-base64", "not-base64"));
        Assert.False(PasswordHasher.Verify(null, null, null));
    }
}

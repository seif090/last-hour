using System.Text;
using FluentAssertions;
using Isopoh.Cryptography.Argon2;
using LastHour.BuildingBlocks.Application.Contracts;
using LastHour.BuildingBlocks.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Tests.Security;

public class Argon2PasswordHasherTests
{
    private const string Password = "correct horse battery staple";

    /// <summary>
    /// Reduced hashing budget so the bulk of the suite stays fast. Parameter handling is
    /// verified explicitly: the production defaults are exercised where the encoded parameters
    /// themselves are asserted, and the rehash logic compares any hash against the options of
    /// the hasher under test.
    /// </summary>
    private static readonly PasswordHashingOptions FastOptions = new()
    {
        TimeCost = 1,
        MemoryCost = 8192,
        Lanes = 1,
        Threads = 1,
    };

    private static readonly PasswordHashingOptions DefaultOptions = new();

    private static Argon2PasswordHasher CreateHasher(PasswordHashingOptions? options = null)
        => new(Options.Create(options ?? DefaultOptions));

    [Fact]
    public void Hash_WithProductionDefaults_EncodesArgon2idPhcWithConfiguredParameters()
    {
        Argon2PasswordHasher hasher = CreateHasher();

        string encoded = hasher.Hash(Password);

        encoded.Should().StartWith("$argon2id$v=19$m=65536,t=3,p=4$");
        encoded.Split('$').Length.Should().Be(6, "a PHC string embeds algorithm, version, parameters, salt and hash");
    }

    [Fact]
    public void Hash_IsNotEqualToPlaintext()
    {
        Argon2PasswordHasher hasher = CreateHasher(FastOptions);

        string encoded = hasher.Hash(Password);

        encoded.Should().NotBe(Password);
    }

    [Fact]
    public void Hash_SamePasswordTwice_ProducesDifferentHashes()
    {
        Argon2PasswordHasher hasher = CreateHasher(FastOptions);

        string first = hasher.Hash(Password);
        string second = hasher.Hash(Password);

        first.Should().NotBe(second, "each hash uses a fresh random salt");
    }

    [Fact]
    public void Verify_CorrectPassword_ReturnsSuccess()
    {
        Argon2PasswordHasher hasher = CreateHasher(FastOptions);
        string encoded = hasher.Hash(Password);

        hasher.Verify(Password, encoded).Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void Verify_WrongPassword_ReturnsFailed()
    {
        Argon2PasswordHasher hasher = CreateHasher(FastOptions);
        string encoded = hasher.Hash(Password);

        hasher.Verify("wrong-password", encoded).Should().Be(PasswordVerificationResult.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-phc-string")]
    [InlineData("$argon2id$v=19$m=65536,t=3,p=4")]
    [InlineData("$argon2id$v=19$m=65536,t=3,p=4$only-salt")]
    [InlineData("$argon2id$v=19$m=not-a-number,t=3,p=4$c2FsdA$c2FsdA")]
    public void Verify_MalformedOrTruncatedHash_ReturnsFailedWithoutThrowing(string malformedHash)
    {
        Argon2PasswordHasher hasher = CreateHasher(FastOptions);

        hasher.Verify(Password, malformedHash).Should().Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Verify_UnicodePassword_RoundTrips()
    {
        const string unicodePassword = "pässwörd🔐密码 🚀";
        Argon2PasswordHasher hasher = CreateHasher(FastOptions);
        string encoded = hasher.Hash(unicodePassword);

        hasher.Verify(unicodePassword, encoded).Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void Verify_LongPassword_RoundTrips()
    {
        string longPassword = new string('a', 200);
        Argon2PasswordHasher hasher = CreateHasher(FastOptions);
        string encoded = hasher.Hash(longPassword);

        hasher.Verify(longPassword, encoded).Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void Verify_EmptyPassword_HashesAndVerifies()
    {
        // The hasher is a primitive: the password policy rejects empty passwords at the
        // application layer, but the provider must still handle the input deterministically.
        Argon2PasswordHasher hasher = CreateHasher(FastOptions);
        string encoded = hasher.Hash(string.Empty);

        hasher.Verify(string.Empty, encoded).Should().Be(PasswordVerificationResult.Success);
    }

    [Fact]
    public void Verify_NullArguments_Throw()
    {
        Argon2PasswordHasher hasher = CreateHasher(FastOptions);

        Assert.Throws<ArgumentNullException>(() => hasher.Hash(null!));
        Assert.Throws<ArgumentNullException>(() => hasher.Verify(Password, null!));
        Assert.Throws<ArgumentNullException>(() => hasher.Verify(null!, hasher.Hash(Password)));
        Assert.Throws<ArgumentNullException>(() => hasher.NeedsRehash(null!));
    }

    [Fact]
    public void NeedsRehash_CurrentParameters_ReturnsFalse()
    {
        Argon2PasswordHasher hasher = CreateHasher(DefaultOptions);
        string encoded = hasher.Hash(Password);

        hasher.NeedsRehash(encoded).Should().BeFalse();
    }

    [Fact]
    public void NeedsRehash_StaleParameters_ReturnsTrue()
    {
        string staleHash = CreateHasher(FastOptions).Hash(Password);

        // The production-default hasher recognizes that the reduced budget is below its own.
        CreateHasher(DefaultOptions).NeedsRehash(staleHash).Should().BeTrue();
    }

    [Fact]
    public void NeedsRehash_ForeignAlgorithmOrGarbage_ReturnsTrue()
    {
        Argon2PasswordHasher hasher = CreateHasher(DefaultOptions);

        hasher.NeedsRehash("$argon2i$v=19$m=65536,t=3,p=4$c2FsdA$c2FsdA").Should().BeTrue();
        hasher.NeedsRehash("bcrypt$2a$10$abcdefghijklmnopqrstuv").Should().BeTrue();
        hasher.NeedsRehash("garbage").Should().BeTrue();
    }

    [Fact]
    public void Verify_StaleHash_Succeeds_ButReportsRehashNeeded()
    {
        string staleHash = CreateHasher(FastOptions).Hash(Password);

        CreateHasher(DefaultOptions)
            .Verify(Password, staleHash)
            .Should()
            .Be(PasswordVerificationResult.SuccessRehashNeeded);
    }

    [Fact]
    public void Verify_ForeignAlgorithm_HashFailsVerification()
    {
        // An argon2i hash must not verify as argon2id; it is treated as a failed attempt.
        Argon2PasswordHasher hasher = CreateHasher(FastOptions);

        hasher.Verify(Password, "$argon2i$v=19$m=8192,t=1,p=1$c2FsdHNhbHRzYWx0c2FsdA$c2FsdHNhbHQ")
            .Should()
            .Be(PasswordVerificationResult.Failed);
    }

    [Fact]
    public void Hash_ResultIsUsableByTheIsopohLibraryDirectly()
    {
        Argon2PasswordHasher hasher = CreateHasher(FastOptions);
        string encoded = hasher.Hash(Password);

        // Guards the PHC encoding contract with the library itself, independent of our wrapper.
        Argon2.Verify(encoded, Encoding.UTF8.GetBytes(Password)).Should().BeTrue();
    }
}

using Microsoft.Extensions.Options;

namespace LastHour.BuildingBlocks.Infrastructure.Security;

/// <summary>
/// Validates <see cref="PasswordHashingOptions"/> so a misconfigured hashing budget fails fast
/// at startup instead of silently weakening credential security.
/// </summary>
public sealed class PasswordHashingOptionsValidator : IValidateOptions<PasswordHashingOptions>
{
    /// <inheritdoc/>
    public ValidateOptionsResult Validate(string? name, PasswordHashingOptions options)
    {
        if (options.TimeCost < 1)
        {
            return ValidateOptionsResult.Fail(
                $"'{PasswordHashingOptions.SectionName}:TimeCost' must be at least 1.");
        }

        if (options.MemoryCost < 8 * 1024)
        {
            return ValidateOptionsResult.Fail(
                $"'{PasswordHashingOptions.SectionName}:MemoryCost' must be at least 8192 KiB (8 MiB), " +
                "the Argon2 minimum recommended for password hashing.");
        }

        if (options.Lanes < 1)
        {
            return ValidateOptionsResult.Fail(
                $"'{PasswordHashingOptions.SectionName}:Lanes' must be at least 1.");
        }

        if (options.Threads < 1)
        {
            return ValidateOptionsResult.Fail(
                $"'{PasswordHashingOptions.SectionName}:Threads' must be at least 1.");
        }

        if (options.HashLength is < 16 or > 64)
        {
            return ValidateOptionsResult.Fail(
                $"'{PasswordHashingOptions.SectionName}:HashLength' must be between 16 and 64 bytes.");
        }

        if (options.SaltLength < 16)
        {
            return ValidateOptionsResult.Fail(
                $"'{PasswordHashingOptions.SectionName}:SaltLength' must be at least 16 bytes.");
        }

        return ValidateOptionsResult.Success;
    }
}

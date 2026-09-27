using System.Text.RegularExpressions;
using FluentValidation;

namespace IdentityHub.Application.Common.Validation;

/// <summary>
/// Shared validation rules for optional user contact/profile fields.
/// </summary>
public static class UserContactValidation
{
    public const int PhoneNumberMaxLength = 32;
    public static readonly DateOnly MinDateOfBirth = new(1900, 1, 1);

    /// <summary>
    /// E.164-style or lightly formatted international numbers.
    /// </summary>
    public const string PhoneNumberPattern = @"^\+?[0-9\s\-().]{7,32}$";

    private static readonly Regex PhoneNumberRegex = new(
        PhoneNumberPattern,
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IRuleBuilderOptions<T, string?> OptionalPhoneNumber<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .Must(BeValidOptionalPhoneNumber)
            .WithMessage("Phone number format is invalid.");
    }

    public static IRuleBuilderOptions<T, DateOnly?> OptionalDateOfBirth<T>(
        this IRuleBuilder<T, DateOnly?> ruleBuilder)
    {
        return ruleBuilder
            .Must(BeOnOrAfterMinDate)
            .WithMessage($"Date of birth must be on or after {MinDateOfBirth:yyyy-MM-dd}.")
            .Must(BeOnOrBeforeToday)
            .WithMessage("Date of birth cannot be in the future.");
    }

    /// <summary>
    /// Persists E.164-style values (<c>+5511987654321</c>) for round-trip with the country picker UI.
    /// </summary>
    public static string? NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return null;

        var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (digits.Length == 0)
            return null;

        if (digits.StartsWith("00", StringComparison.Ordinal) && digits.Length > 2)
            digits = digits[2..];

        var normalized = $"+{digits}";
        return normalized.Length <= PhoneNumberMaxLength ? normalized : null;
    }

    private static bool BeValidOptionalPhoneNumber(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var normalized = NormalizePhoneNumber(value);
        if (normalized is null)
            return false;

        // E.164 allows up to 15 digits after country calling rules; keep a practical floor.
        var digitCount = normalized.Count(char.IsDigit);
        return digitCount is >= 8 and <= 15
               && PhoneNumberRegex.IsMatch(normalized);
    }

    private static bool BeOnOrAfterMinDate(DateOnly? value) =>
        !value.HasValue || value.Value >= MinDateOfBirth;

    private static bool BeOnOrBeforeToday(DateOnly? value) =>
        !value.HasValue || value.Value <= DateOnly.FromDateTime(DateTime.UtcNow);
}

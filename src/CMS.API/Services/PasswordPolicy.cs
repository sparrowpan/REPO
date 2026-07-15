namespace CMS.API.Services;

/// <summary>
/// New-password complexity rule, shared by the API and mirrored client-side. A password is compliant
/// when it is at least 8 characters long <b>and</b> draws from at least 3 of these 4 character classes:
/// uppercase, lowercase, digit, symbol (anything that isn't a letter or digit).
/// </summary>
public static class PasswordPolicy
{
    /// <summary>Minimum length.</summary>
    public const int MinLength = 8;

    /// <summary>Minimum number of the four character classes that must appear.</summary>
    public const int MinCharacterClasses = 3;

    /// <summary>
    /// Bilingual rejection message shown in the UI. The frontend uses the same wording for its own
    /// client-side check so a rejected password reads identically whichever side catches it.
    /// </summary>
    public const string ComplexityMessage =
        "密碼長度至少需 8 碼，且內容須至少包含四種字元的其中三種：大寫英文／小寫英文／數字／符號 " +
        "(Password must be at least 8 characters and contain at least 3 of the 4 classes: " +
        "uppercase / lowercase / digit / symbol.)";

    /// <summary>True when <paramref name="password"/> meets the length and character-class rules.</summary>
    public static bool IsCompliant(string? password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinLength)
            return false;

        var classes = 0;
        if (password.Any(char.IsUpper)) classes++;
        if (password.Any(char.IsLower)) classes++;
        if (password.Any(char.IsDigit)) classes++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) classes++;

        return classes >= MinCharacterClasses;
    }
}

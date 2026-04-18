using System.Text.Json;
using CorditeWars.Systems.Localization;

namespace CorditeWars.Tests.Systems.Localization;

/// <summary>
/// Validates that the LocalizationManager's SupportedLocales list is consistent
/// with the JSON translation files in data/locale/, and that each locale file
/// is structurally complete.
/// </summary>
public class LocalizationTests
{
    private static readonly string LocaleDir = FindLocaleDir();

    private static string FindLocaleDir()
    {
        string dir = AppDomain.CurrentDomain.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(dir, "data", "locale");
            if (Directory.Exists(candidate))
                return candidate;
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new DirectoryNotFoundException(
            "Could not locate the 'data/locale' directory by walking up from the test assembly. " +
            "Ensure tests are run from within the repository checkout.");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    // ── SupportedLocales completeness ────────────────────────────────

    [Fact]
    public void SupportedLocales_HasNoDuplicateCodes()
    {
        var codes = LocalizationManager.SupportedLocales.Select(l => l.Code).ToList();
        var distinct = codes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        Assert.Equal(distinct.Count, codes.Count);
    }

    [Fact]
    public void SupportedLocales_HasNoDuplicateDisplayNames()
    {
        var names = LocalizationManager.SupportedLocales.Select(l => l.DisplayName).ToList();
        var distinct = names.Distinct(StringComparer.Ordinal).ToList();
        Assert.Equal(distinct.Count, names.Count);
    }

    [Fact]
    public void SupportedLocales_CodesAreNonEmpty()
    {
        foreach (var (code, name) in LocalizationManager.SupportedLocales)
        {
            Assert.False(string.IsNullOrWhiteSpace(code),
                $"Locale code is empty (display name: '{name}')");
            Assert.False(string.IsNullOrWhiteSpace(name),
                $"Display name is empty (code: '{code}')");
        }
    }

    // ── JSON file existence and coverage ─────────────────────────────

    [Fact]
    public void EachSupportedLocale_HasJsonFile()
    {
        foreach (var (code, displayName) in LocalizationManager.SupportedLocales)
        {
            string path = Path.Combine(LocaleDir, $"{code}.json");
            Assert.True(File.Exists(path),
                $"Missing locale file for '{code}' ({displayName}): {path}");
        }
    }

    [Fact]
    public void EachJsonFile_IsRepresentedInSupportedLocales()
    {
        var supportedCodes = LocalizationManager.SupportedLocales
            .Select(l => l.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (string file in Directory.GetFiles(LocaleDir, "*.json"))
        {
            string code = Path.GetFileNameWithoutExtension(file);
            Assert.True(supportedCodes.Contains(code),
                $"Locale file '{Path.GetFileName(file)}' has no entry in SupportedLocales (code: '{code}').");
        }
    }

    // ── JSON file validity ────────────────────────────────────────────

    [Fact]
    public void AllLocaleFiles_AreValidJson()
    {
        foreach (string file in Directory.GetFiles(LocaleDir, "*.json"))
        {
            string json = File.ReadAllText(file);
            var ex = Record.Exception(() =>
                JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions));
            Assert.Null(ex);
        }
    }

    [Fact]
    public void AllLocaleFiles_AreNonEmpty()
    {
        foreach (string file in Directory.GetFiles(LocaleDir, "*.json"))
        {
            string json = File.ReadAllText(file);
            var entries = JsonSerializer.Deserialize<Dictionary<string, string>>(json, JsonOptions);
            Assert.NotNull(entries);
            Assert.True(entries.Count > 0,
                $"Locale file '{Path.GetFileName(file)}' contains no entries.");
        }
    }

    // ── Key completeness (all locales must have the same keys as English) ──

    [Fact]
    public void AllLocaleFiles_HaveTheSameKeysAsEnglish()
    {
        string englishPath = Path.Combine(LocaleDir, "en.json");
        Assert.True(File.Exists(englishPath), $"English locale file not found: {englishPath}");

        var englishKeys = JsonSerializer
            .Deserialize<Dictionary<string, string>>(File.ReadAllText(englishPath), JsonOptions)!
            .Keys
            .ToHashSet(StringComparer.Ordinal);

        foreach (string file in Directory.GetFiles(LocaleDir, "*.json"))
        {
            if (string.Equals(Path.GetFileNameWithoutExtension(file), "en", StringComparison.OrdinalIgnoreCase))
                continue;

            var entries = JsonSerializer
                .Deserialize<Dictionary<string, string>>(File.ReadAllText(file), JsonOptions)!;

            var missing = englishKeys.Except(entries.Keys).ToList();
            Assert.True(missing.Count == 0,
                $"Locale '{Path.GetFileName(file)}' is missing key(s): {string.Join(", ", missing)}");

            var extra = entries.Keys.Except(englishKeys).ToList();
            Assert.True(extra.Count == 0,
                $"Locale '{Path.GetFileName(file)}' has extra key(s) not present in English: {string.Join(", ", extra)}");
        }
    }

    [Fact]
    public void AllLocaleFiles_HaveNoEmptyValues()
    {
        foreach (string file in Directory.GetFiles(LocaleDir, "*.json"))
        {
            var entries = JsonSerializer
                .Deserialize<Dictionary<string, string>>(File.ReadAllText(file), JsonOptions)!;

            var emptyValues = entries
                .Where(kv => string.IsNullOrEmpty(kv.Value))
                .Select(kv => kv.Key)
                .ToList();

            Assert.True(emptyValues.Count == 0,
                $"Locale '{Path.GetFileName(file)}' has empty value(s) for key(s): {string.Join(", ", emptyValues)}");
        }
    }

    // ── Static helper correctness ─────────────────────────────────────

    [Fact]
    public void IsLocaleSupported_ReturnsTrueForAllSupportedCodes()
    {
        foreach (var (code, _) in LocalizationManager.SupportedLocales)
            Assert.True(LocalizationManager.IsLocaleSupported(code), $"IsLocaleSupported(\"{code}\") returned false");
    }

    [Fact]
    public void IsLocaleSupported_ReturnsFalseForUnknownCode()
    {
        Assert.False(LocalizationManager.IsLocaleSupported("xx"));
        Assert.False(LocalizationManager.IsLocaleSupported(""));
        Assert.False(LocalizationManager.IsLocaleSupported("zz_ZZ"));
    }

    [Fact]
    public void IsLocaleSupported_IsCaseInsensitive()
    {
        Assert.True(LocalizationManager.IsLocaleSupported("EN"));
        Assert.True(LocalizationManager.IsLocaleSupported("De"));
        Assert.True(LocalizationManager.IsLocaleSupported("PT_BR"));
    }

    [Fact]
    public void GetLocaleIndex_ReturnsCorrectIndexForEachSupportedLocale()
    {
        for (int i = 0; i < LocalizationManager.SupportedLocales.Length; i++)
        {
            string code = LocalizationManager.SupportedLocales[i].Code;
            Assert.Equal(i, LocalizationManager.GetLocaleIndex(code));
        }
    }

    [Fact]
    public void GetLocaleIndex_ReturnsZeroForUnknownLocale()
    {
        Assert.Equal(0, LocalizationManager.GetLocaleIndex("xx"));
        Assert.Equal(0, LocalizationManager.GetLocaleIndex(""));
    }

    [Fact]
    public void EnglishLocale_IsFirstInSupportedLocales()
    {
        Assert.Equal("en", LocalizationManager.SupportedLocales[0].Code);
    }
}

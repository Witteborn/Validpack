using System.Text.Json;
using Validpack.Models;
using Validpack.Services;

namespace Validpack.Tests.Services;

[Collection("Console")]
public class ReportServiceTests
{
    private readonly ReportService _service;

    public ReportServiceTests()
    {
        _service = new ReportService();
    }

    private static ScanResult CreateScanResult(
        int validCount = 0,
        int notFoundCount = 0,
        int blacklistedCount = 0,
        int whitelistedCount = 0)
    {
        var result = new ScanResult
        {
            ScannedPath = "/test/path"
        };

        result.ScannedFiles.Add("package.json");

        for (int i = 0; i < validCount; i++)
        {
            var dep = new Dependency($"valid-pkg-{i}", "1.0.0", DependencyType.Npm, "package.json");
            result.AllDependencies.Add(dep);
            result.UniqueDependencies.Add(dep);
            result.ValidationResults.Add(new ValidationResult(dep, ValidationStatus.Valid, "OK"));
        }

        for (int i = 0; i < notFoundCount; i++)
        {
            var dep = new Dependency($"notfound-pkg-{i}", "1.0.0", DependencyType.Npm, "package.json");
            result.AllDependencies.Add(dep);
            result.UniqueDependencies.Add(dep);
            result.ValidationResults.Add(new ValidationResult(dep, ValidationStatus.NotFound, "Not found"));
        }

        for (int i = 0; i < blacklistedCount; i++)
        {
            var dep = new Dependency($"blacklisted-pkg-{i}", "1.0.0", DependencyType.Npm, "package.json");
            result.AllDependencies.Add(dep);
            result.UniqueDependencies.Add(dep);
            result.ValidationResults.Add(new ValidationResult(dep, ValidationStatus.Blacklisted, "Blacklisted"));
        }

        for (int i = 0; i < whitelistedCount; i++)
        {
            var dep = new Dependency($"whitelisted-pkg-{i}", "1.0.0", DependencyType.Npm, "package.json");
            result.AllDependencies.Add(dep);
            result.UniqueDependencies.Add(dep);
            result.ValidationResults.Add(new ValidationResult(dep, ValidationStatus.Whitelisted, "Whitelisted"));
        }

        return result;
    }

    [Fact]
    public void PrintConsoleReport_NoProblems_ShowsSuccess()
    {
        var result = CreateScanResult(validCount: 5);

        var output = CaptureConsoleOutput(() => _service.PrintConsoleReport(result));

        Assert.Contains("ERFOLGREICH", output);
        Assert.Contains("Keine Probleme gefunden", output);
    }

    [Fact]
    public void PrintConsoleReport_WithNotFoundPackages_ShowsProblems()
    {
        var result = CreateScanResult(validCount: 3, notFoundCount: 2);

        var output = CaptureConsoleOutput(() => _service.PrintConsoleReport(result));

        Assert.Contains("FEHLGESCHLAGEN", output);
        Assert.Contains("Probleme gefunden", output);
        Assert.Contains("PROBLEME GEFUNDEN", output);
        Assert.Contains("notfound-pkg", output);
    }

    [Fact]
    public void PrintConsoleReport_WithBlacklistedPackages_ShowsProblems()
    {
        var result = CreateScanResult(validCount: 3, blacklistedCount: 1);

        var output = CaptureConsoleOutput(() => _service.PrintConsoleReport(result));

        Assert.Contains("FEHLGESCHLAGEN", output);
        Assert.Contains("Blacklist", output);
        Assert.Contains("blacklisted-pkg", output);
    }

    [Fact]
    public void PrintConsoleReport_ShowsCorrectCounts()
    {
        var result = CreateScanResult(validCount: 5, whitelistedCount: 2, notFoundCount: 1, blacklistedCount: 1);

        var output = CaptureConsoleOutput(() => _service.PrintConsoleReport(result));

        // Use Contains with flexible whitespace matching
        Assert.Matches(@"Valide:\s+5", output);
        Assert.Matches(@"Whitelisted:\s+2", output);
        Assert.Matches(@"Nicht gefunden:\s+1", output);
        Assert.Matches(@"Blacklisted:\s+1", output);
    }

    [Fact]
    public void PrintConsoleReport_ShowsScannedPath()
    {
        var result = CreateScanResult(validCount: 1);

        var output = CaptureConsoleOutput(() => _service.PrintConsoleReport(result));

        Assert.Contains("/test/path", output);
    }

    [Fact]
    public void PrintJsonReport_ReturnsValidJson()
    {
        var result = CreateScanResult(validCount: 3, notFoundCount: 1);

        var output = CaptureConsoleOutput(() => _service.PrintJsonReport(result));

        var json = JsonDocument.Parse(ExtractJson(output));
        Assert.NotNull(json);
    }

    [Fact]
    public void PrintJsonReport_ContainsExpectedFields()
    {
        var result = CreateScanResult(validCount: 3, notFoundCount: 1);

        var output = CaptureConsoleOutput(() => _service.PrintJsonReport(result));

        var json = JsonDocument.Parse(ExtractJson(output));
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("scannedPath", out _));
        Assert.True(root.TryGetProperty("scanTime", out _));
        Assert.True(root.TryGetProperty("summary", out _));
        Assert.True(root.TryGetProperty("hasProblems", out _));
        Assert.True(root.TryGetProperty("problems", out _));
    }

    [Fact]
    public void PrintJsonReport_SummaryContainsCorrectCounts()
    {
        var result = CreateScanResult(validCount: 5, whitelistedCount: 2, notFoundCount: 1, blacklistedCount: 1);

        var output = CaptureConsoleOutput(() => _service.PrintJsonReport(result));

        var json = JsonDocument.Parse(ExtractJson(output));
        var summary = json.RootElement.GetProperty("summary");

        Assert.Equal(5, summary.GetProperty("valid").GetInt32());
        Assert.Equal(2, summary.GetProperty("whitelisted").GetInt32());
        Assert.Equal(1, summary.GetProperty("notFound").GetInt32());
        Assert.Equal(1, summary.GetProperty("blacklisted").GetInt32());
        Assert.Equal(9, summary.GetProperty("totalDependencies").GetInt32());
        Assert.Equal(9, summary.GetProperty("uniqueDependencies").GetInt32());
    }

    [Fact]
    public void PrintJsonReport_HasProblemsTrue_WhenProblemsExist()
    {
        var result = CreateScanResult(validCount: 3, notFoundCount: 1);

        var output = CaptureConsoleOutput(() => _service.PrintJsonReport(result));

        var json = JsonDocument.Parse(ExtractJson(output));
        Assert.True(json.RootElement.GetProperty("hasProblems").GetBoolean());
    }

    [Fact]
    public void PrintJsonReport_HasProblemsFalse_WhenNoProblems()
    {
        var result = CreateScanResult(validCount: 5);

        var output = CaptureConsoleOutput(() => _service.PrintJsonReport(result));

        var json = JsonDocument.Parse(ExtractJson(output));
        Assert.False(json.RootElement.GetProperty("hasProblems").GetBoolean());
    }

    [Fact]
    public void PrintJsonReport_ProblemsArrayContainsOnlyProblems()
    {
        var result = CreateScanResult(validCount: 3, notFoundCount: 2, blacklistedCount: 1);

        var output = CaptureConsoleOutput(() => _service.PrintJsonReport(result));

        var json = JsonDocument.Parse(ExtractJson(output));
        var problems = json.RootElement.GetProperty("problems");

        Assert.Equal(3, problems.GetArrayLength()); // 2 not found + 1 blacklisted
    }

    private static string CaptureConsoleOutput(Action action)
    {
        var originalOut = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);

        try
        {
            action();
            return writer.ToString();
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    private static string ExtractJson(string output)
    {
        var startIndex = output.IndexOf('{');
        var endIndex = output.LastIndexOf('}');
        if (startIndex < 0 || endIndex <= startIndex)
            throw new InvalidOperationException($"No JSON found in output: {output}");
        return output.Substring(startIndex, endIndex - startIndex + 1);
    }
}

using System.Text.Json;
using Validpack.Models;

namespace Validpack.Services;

/// <summary>
/// Service for report generation
/// </summary>
public class ReportService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Prints report to console
    /// </summary>
    public void PrintConsoleReport(ScanResult result)
    {
        Console.WriteLine();
        PrintHeader("SUPPLY CHAIN SECURITY SCAN REPORT");
        Console.WriteLine();

        // Scan info
        Console.WriteLine($"Scanned Path:  {result.ScannedPath}");
        Console.WriteLine($"Scan Time:     {result.ScanTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Scanned Files: {result.ScannedFiles.Count}");
        Console.WriteLine();

        // Summary
        PrintHeader("SUMMARY");
        Console.WriteLine();
        Console.WriteLine($"Total Dependencies:  {result.AllDependencies.Count}");
        Console.WriteLine($"Unique Dependencies: {result.UniqueDependencies.Count}");
        Console.WriteLine();

        PrintWithColor($"  Valid:       {result.ValidCount}", ConsoleColor.Green);
        PrintWithColor($"  Whitelisted: {result.WhitelistedCount}", ConsoleColor.Cyan);
        PrintWithColor($"  Not Found:   {result.NotFoundCount}",
            result.NotFoundCount > 0 ? ConsoleColor.Red : ConsoleColor.Green);
        PrintWithColor($"  Blacklisted: {result.BlacklistedCount}",
            result.BlacklistedCount > 0 ? ConsoleColor.Red : ConsoleColor.Green);
        Console.WriteLine();

        // Problems in detail
        var problems = result.ValidationResults.Where(r => r.HasProblem).ToList();

        if (problems.Count > 0)
        {
            PrintHeader("PROBLEMS FOUND");
            Console.WriteLine();

            // Not found packages
            var notFound = problems.Where(p => p.Status == ValidationStatus.NotFound).ToList();
            if (notFound.Count > 0)
            {
                PrintWithColor("Packages not found in registry (Supply Chain Attack Risk):", ConsoleColor.Red);
                Console.WriteLine();
                foreach (var item in notFound)
                {
                    PrintWithColor($"  ! {item.Dependency.Type}: {item.Dependency.Name}", ConsoleColor.Red);
                    Console.WriteLine($"    Source: {item.Dependency.SourceFile}");
                }
                Console.WriteLine();
            }

            // Blacklisted packages
            var blacklisted = problems.Where(p => p.Status == ValidationStatus.Blacklisted).ToList();
            if (blacklisted.Count > 0)
            {
                PrintWithColor("Blacklisted packages:", ConsoleColor.Yellow);
                Console.WriteLine();
                foreach (var item in blacklisted)
                {
                    PrintWithColor($"  X {item.Dependency.Type}: {item.Dependency.Name}", ConsoleColor.Yellow);
                    Console.WriteLine($"    Source: {item.Dependency.SourceFile}");
                }
                Console.WriteLine();
            }
        }

        // Final result
        PrintHeader("RESULT");
        Console.WriteLine();

        if (result.HasProblems)
        {
            PrintWithColor("FAILED - Problems found!", ConsoleColor.Red);
            Console.WriteLine();
            Console.WriteLine("Please review the problems listed above.");
            Console.WriteLine("If these are false positives, add them to the whitelist.");
        }
        else
        {
            PrintWithColor("PASSED - No problems found.", ConsoleColor.Green);
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Prints report as JSON
    /// </summary>
    public void PrintJsonReport(ScanResult result)
    {
        var report = new
        {
            scannedPath = result.ScannedPath,
            scanTime = result.ScanTime,
            scannedFiles = result.ScannedFiles,
            summary = new
            {
                totalDependencies = result.AllDependencies.Count,
                uniqueDependencies = result.UniqueDependencies.Count,
                valid = result.ValidCount,
                whitelisted = result.WhitelistedCount,
                notFound = result.NotFoundCount,
                blacklisted = result.BlacklistedCount
            },
            hasProblems = result.HasProblems,
            problems = result.ValidationResults
                .Where(r => r.HasProblem)
                .Select(r => new
                {
                    packageName = r.Dependency.Name,
                    packageType = r.Dependency.Type.ToString(),
                    status = r.Status.ToString(),
                    sourceFile = r.Dependency.SourceFile,
                    message = r.Message
                })
        };

        Console.WriteLine(JsonSerializer.Serialize(report, _jsonOptions));
    }

    private void PrintHeader(string text)
    {
        var line = new string('=', text.Length + 4);
        Console.WriteLine(line);
        Console.WriteLine($"  {text}");
        Console.WriteLine(line);
    }

    private void PrintWithColor(string text, ConsoleColor color)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = originalColor;
    }
}

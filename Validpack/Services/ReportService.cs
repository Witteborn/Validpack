using System.Text.Json;
using Validpack.Models;

namespace Validpack.Services;

/// <summary>
/// Service für die Report-Generierung
/// </summary>
public class ReportService
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };
    
    /// <summary>
    /// Gibt den Report auf der Konsole aus
    /// </summary>
    public void PrintConsoleReport(ScanResult result)
    {
        Console.WriteLine();
        PrintHeader("SUPPLY CHAIN SECURITY SCAN REPORT");
        Console.WriteLine();
        
        // Scan-Info
        Console.WriteLine($"Gescannter Pfad: {result.ScannedPath}");
        Console.WriteLine($"Scan-Zeitpunkt:  {result.ScanTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"Gescannte Dateien: {result.ScannedFiles.Count}");
        Console.WriteLine();
        
        // Zusammenfassung
        PrintHeader("ZUSAMMENFASSUNG");
        Console.WriteLine();
        Console.WriteLine($"Gefundene Abhängigkeiten:    {result.AllDependencies.Count}");
        Console.WriteLine($"Eindeutige Abhängigkeiten:   {result.UniqueDependencies.Count}");
        Console.WriteLine();
        
        PrintWithColor($"  Valide:        {result.ValidCount}", ConsoleColor.Green);
        PrintWithColor($"  Whitelisted:   {result.WhitelistedCount}", ConsoleColor.Cyan);
        PrintWithColor($"  Nicht gefunden: {result.NotFoundCount}", 
            result.NotFoundCount > 0 ? ConsoleColor.Red : ConsoleColor.Green);
        PrintWithColor($"  Blacklisted:   {result.BlacklistedCount}", 
            result.BlacklistedCount > 0 ? ConsoleColor.Red : ConsoleColor.Green);
        Console.WriteLine();
        
        // Probleme im Detail
        var problems = result.ValidationResults.Where(r => r.HasProblem).ToList();
        
        if (problems.Count > 0)
        {
            PrintHeader("PROBLEME GEFUNDEN");
            Console.WriteLine();
            
            // Nicht gefundene Pakete
            var notFound = problems.Where(p => p.Status == ValidationStatus.NotFound).ToList();
            if (notFound.Count > 0)
            {
                PrintWithColor("Pakete die nicht in der Registry existieren (Supply Chain Attack Risiko):", ConsoleColor.Red);
                Console.WriteLine();
                foreach (var item in notFound)
                {
                    PrintWithColor($"  ! {item.Dependency.Type}: {item.Dependency.Name}", ConsoleColor.Red);
                    Console.WriteLine($"    Quelle: {item.Dependency.SourceFile}");
                }
                Console.WriteLine();
            }
            
            // Blacklisted Pakete
            var blacklisted = problems.Where(p => p.Status == ValidationStatus.Blacklisted).ToList();
            if (blacklisted.Count > 0)
            {
                PrintWithColor("Pakete auf der Blacklist:", ConsoleColor.Yellow);
                Console.WriteLine();
                foreach (var item in blacklisted)
                {
                    PrintWithColor($"  X {item.Dependency.Type}: {item.Dependency.Name}", ConsoleColor.Yellow);
                    Console.WriteLine($"    Quelle: {item.Dependency.SourceFile}");
                }
                Console.WriteLine();
            }
        }
        
        // Endergebnis
        PrintHeader("ERGEBNIS");
        Console.WriteLine();
        
        if (result.HasProblems)
        {
            PrintWithColor("FEHLGESCHLAGEN - Probleme gefunden!", ConsoleColor.Red);
            Console.WriteLine();
            Console.WriteLine("Bitte prüfen Sie die oben aufgeführten Probleme.");
            Console.WriteLine("Wenn es sich um False Positives handelt, fügen Sie die Pakete zur Whitelist hinzu.");
        }
        else
        {
            PrintWithColor("ERFOLGREICH - Keine Probleme gefunden.", ConsoleColor.Green);
        }
        
        Console.WriteLine();
    }
    
    /// <summary>
    /// Gibt den Report als JSON aus
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

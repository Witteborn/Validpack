using Validpack.Models;
using Validpack.Services;

namespace Validpack;

/// <summary>
/// Supply Chain Security Scanner - Prüft Projektabhängigkeiten auf Existenz
/// </summary>
public class Program
{
    private const string Version = "1.0.0";
    
    public static async Task<int> Main(string[] args)
    {
        var options = ParseArguments(args);
        
        if (options.ShowHelp)
        {
            PrintHelp();
            return 0;
        }
        
        if (!options.IsValid)
        {
            foreach (var error in options.Errors)
            {
                Console.Error.WriteLine($"Fehler: {error}");
            }
            Console.Error.WriteLine();
            Console.Error.WriteLine("Verwenden Sie --help für Hilfe.");
            return 2;
        }
        
        try
        {
            // Konfiguration laden
            var configService = new ConfigService();
            var config = configService.LoadConfiguration(options.ConfigFile);
            
            if (options.Verbose)
            {
                Console.WriteLine($"Konfiguration geladen: {options.ConfigFile}");
                Console.WriteLine($"  Whitelist: {config.Whitelist.Count} Einträge");
                Console.WriteLine($"  Blacklist: {config.Blacklist.Count} Einträge");
                Console.WriteLine();
            }
            
            // Scanner ausführen
            var scanner = new ScannerService(config, options.Verbose);
            var result = await scanner.ScanAsync(options.Path!);
            
            // Report ausgeben
            var reportService = new ReportService();
            
            if (options.OutputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                reportService.PrintJsonReport(result);
            }
            else
            {
                reportService.PrintConsoleReport(result);
            }
            
            // Exit-Code: 0 = OK, 1 = Probleme gefunden
            return result.HasProblems ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unerwarteter Fehler: {ex.Message}");
            if (options.Verbose)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            return 2;
        }
    }
    
    private static CliOptions ParseArguments(string[] args)
    {
        var options = new CliOptions();
        
        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            
            switch (arg.ToLowerInvariant())
            {
                case "-h":
                case "--help":
                case "/help":
                case "/?":
                    options.ShowHelp = true;
                    break;
                    
                case "-v":
                case "--verbose":
                    options.Verbose = true;
                    break;
                    
                case "-c":
                case "--config":
                    if (i + 1 < args.Length)
                    {
                        options.ConfigFile = args[++i];
                    }
                    else
                    {
                        options.Errors.Add("--config benötigt einen Dateipfad");
                    }
                    break;
                    
                case "-o":
                case "--output":
                    if (i + 1 < args.Length)
                    {
                        var format = args[++i].ToLowerInvariant();
                        if (format != "console" && format != "json")
                        {
                            options.Errors.Add($"Ungültiges Ausgabeformat: {format}. Erlaubt: console, json");
                        }
                        else
                        {
                            options.OutputFormat = format;
                        }
                    }
                    else
                    {
                        options.Errors.Add("--output benötigt ein Format (console, json)");
                    }
                    break;
                    
                default:
                    if (arg.StartsWith("-") || arg.StartsWith("/"))
                    {
                        options.Errors.Add($"Unbekannte Option: {arg}");
                    }
                    else if (string.IsNullOrEmpty(options.Path))
                    {
                        options.Path = arg;
                    }
                    else
                    {
                        options.Errors.Add($"Unerwartetes Argument: {arg}");
                    }
                    break;
            }
        }
        
        // Validierung
        if (string.IsNullOrEmpty(options.Path) && !options.ShowHelp)
        {
            options.Errors.Add("Pfad zum zu scannenden Verzeichnis fehlt");
        }
        else if (!string.IsNullOrEmpty(options.Path) && !Directory.Exists(options.Path))
        {
            options.Errors.Add($"Verzeichnis existiert nicht: {options.Path}");
        }
        
        return options;
    }
    
    private static void PrintHelp()
    {
        Console.WriteLine($@"
Validpack - Supply Chain Security Scanner v{Version}
=========================================================

Validates project dependencies against official registries.
Detects potential Supply Chain Attacks by finding non-existent packages.

VERWENDUNG:
  validpack <pfad> [optionen]

ARGUMENTE:
  <pfad>              Pfad zum zu scannenden Verzeichnis

OPTIONEN:
  -c, --config <datei>  Pfad zur Konfigurationsdatei
                        (Standard: validpack.json)
  -o, --output <format> Ausgabeformat: console, json
                        (Standard: console)
  -v, --verbose         Detaillierte Ausgabe
  -h, --help            Diese Hilfe anzeigen

BEISPIELE:
  validpack .
      Scannt das aktuelle Verzeichnis

  validpack ./mein-projekt --verbose
      Scannt mit detaillierter Ausgabe

  validpack ./projekt --config custom-config.json
      Verwendet eine benutzerdefinierte Konfiguration

  validpack ./projekt --output json
      Gibt das Ergebnis als JSON aus (für CI/CD Pipelines)

KONFIGURATIONSDATEI (validpack.json):
{{
  ""whitelist"": [
    ""internes-paket"",
    ""bekanntes-false-positive""
  ],
  ""blacklist"": [
    ""Newtonsoft.Json"",
    ""moment""
  ]
}}

EXIT-CODES:
  0  Keine Probleme gefunden
  1  Probleme gefunden (nicht existierende oder blacklisted Pakete)
  2  Konfigurationsfehler oder unerwarteter Fehler

SUPPORTED PACKAGE MANAGERS:
  - npm      (package.json)
  - NuGet    (*.csproj)
  - PyPI     (requirements.txt, pyproject.toml)
  - Crates   (Cargo.toml)
  - Maven    (pom.xml)
  - Gradle   (build.gradle, build.gradle.kts)
");
    }
}

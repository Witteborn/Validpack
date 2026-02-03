using Validpack.Models;
using Validpack.Services;

namespace Validpack;

/// <summary>
/// Supply Chain Security Scanner - Validates project dependencies
/// </summary>
public class Program
{
    private const string Version = "1.1.0";

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
                Console.Error.WriteLine($"Error: {error}");
            }
            Console.Error.WriteLine();
            Console.Error.WriteLine("Use --help for usage information.");
            return 2;
        }

        try
        {
            // Load configuration
            var configService = new ConfigService();
            var config = configService.LoadConfiguration(options.ConfigFile);

            if (options.Verbose)
            {
                Console.WriteLine($"Configuration loaded: {options.ConfigFile}");
                Console.WriteLine($"  Whitelist: {config.Whitelist.Count} entries");
                Console.WriteLine($"  Blacklist: {config.Blacklist.Count} entries");
                Console.WriteLine($"  Exclude:   {config.Exclude.Count} patterns");
                Console.WriteLine();
            }

            // Run scanner
            var scanner = new ScannerService(config, options.Verbose);
            var result = await scanner.ScanAsync(options.Path!);

            // Output report
            var reportService = new ReportService();

            if (options.OutputFormat.Equals("json", StringComparison.OrdinalIgnoreCase))
            {
                reportService.PrintJsonReport(result);
            }
            else
            {
                reportService.PrintConsoleReport(result);
            }

            // Exit code: 0 = OK, 1 = Problems found
            return result.HasProblems ? 1 : 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unexpected error: {ex.Message}");
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
                        options.Errors.Add("--config requires a file path");
                    }
                    break;

                case "-o":
                case "--output":
                    if (i + 1 < args.Length)
                    {
                        var format = args[++i].ToLowerInvariant();
                        if (format != "console" && format != "json")
                        {
                            options.Errors.Add($"Invalid output format: {format}. Allowed: console, json");
                        }
                        else
                        {
                            options.OutputFormat = format;
                        }
                    }
                    else
                    {
                        options.Errors.Add("--output requires a format (console, json)");
                    }
                    break;

                default:
                    if (arg.StartsWith("-") || arg.StartsWith("/"))
                    {
                        options.Errors.Add($"Unknown option: {arg}");
                    }
                    else if (string.IsNullOrEmpty(options.Path))
                    {
                        options.Path = arg;
                    }
                    else
                    {
                        options.Errors.Add($"Unexpected argument: {arg}");
                    }
                    break;
            }
        }

        // Validation
        if (string.IsNullOrEmpty(options.Path) && !options.ShowHelp)
        {
            options.Errors.Add("Path to directory to scan is required");
        }
        else if (!string.IsNullOrEmpty(options.Path) && !Directory.Exists(options.Path))
        {
            options.Errors.Add($"Directory does not exist: {options.Path}");
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

USAGE:
  validpack <path> [options]

ARGUMENTS:
  <path>              Path to directory to scan

OPTIONS:
  -c, --config <file>   Path to configuration file
                        (default: validpack.json)
  -o, --output <format> Output format: console, json
                        (default: console)
  -v, --verbose         Verbose output
  -h, --help            Show this help

EXAMPLES:
  validpack .
      Scan current directory

  validpack ./my-project --verbose
      Scan with verbose output

  validpack ./project --config custom-config.json
      Use custom configuration file

  validpack ./project --output json
      Output as JSON (for CI/CD pipelines)

CONFIGURATION FILE (validpack.json):
{{
  ""whitelist"": [
    ""internal-package"",
    ""known-false-positive""
  ],
  ""blacklist"": [
    ""Newtonsoft.Json"",
    ""moment""
  ],
  ""exclude"": [
    ""test-projects/**"",
    ""samples/**""
  ]
}}

EXIT CODES:
  0  No problems found
  1  Problems found (non-existent or blacklisted packages)
  2  Configuration error or unexpected failure

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

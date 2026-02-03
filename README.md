# Validpack

A command-line tool to detect **Supply Chain Attacks** by verifying that package dependencies actually exist in their official registries.

---

## The Problem

Supply Chain Attacks can occur when a project references packages that don't exist in official registries. Attackers can then register these package names and inject malicious code. This tool helps you detect such vulnerabilities before they become a security risk.

## Features

- Scans **npm** (`package.json`) and **NuGet** (`.csproj`) dependencies
- Verifies package existence via official registry APIs
- **Whitelist** support for internal/private packages
- **Blacklist** support to enforce package policies
- JSON output for CI/CD pipeline integration
- Zero external dependencies (pure .NET)

---

## Installation

### Global Installation

```bash
dotnet tool install --global Validpack
```

### Local Installation (per project)

```bash
dotnet new tool-manifest   # if not already present
dotnet tool install Validpack
```

### From Source

```bash
git clone https://github.com/YOUR_USERNAME/Validpack.git
cd Validpack/src/Validpack
dotnet pack -c Release
dotnet tool install --global --add-source ./nupkg Validpack
```

---

## Usage

### Basic Scan

```bash
# Scan current directory
validpack .

# Scan a specific project
validpack ./my-project
```

### Command Line Options

```
validpack <path> [options]

Arguments:
  <path>                    Path to the directory to scan

Options:
  -c, --config <file>       Path to configuration file (default: validpack.json)
  -o, --output <format>     Output format: console, json (default: console)
  -v, --verbose             Show detailed output
  -h, --help                Display help
```

### Examples

```bash
# Verbose scan with details
validpack ./my-project --verbose

# Use custom configuration
validpack ./my-project --config security-policy.json

# JSON output for CI/CD pipelines
validpack ./my-project --output json
```

---

## Configuration

Create a `validpack.json` file in your project root:

```json
{
  "whitelist": [
    "my-internal-package",
    "company-private-lib"
  ],
  "blacklist": [
    "Newtonsoft.Json",
    "moment"
  ]
}
```

### Whitelist

Packages on the whitelist are **skipped** during validation. Use this for:

- Internal or private registry packages
- Known false positives

### Blacklist

Packages on the blacklist are **immediately flagged** as problems, regardless of whether they exist. Use this to:

- Enforce package policies (e.g., prefer `System.Text.Json` over `Newtonsoft.Json`)
- Block deprecated or vulnerable packages

---

## CI/CD Integration

### Exit Codes

| Code | Meaning |
|------|---------|
| `0`  | All checks passed |
| `1`  | Problems found (missing or blacklisted packages) |
| `2`  | Configuration error or unexpected failure |

### GitHub Actions

```yaml
name: Security Scan

on: [push, pull_request]

jobs:
  supply-chain-check:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      
      - name: Install Validpack
        run: dotnet tool install --global Validpack
      
      - name: Run Supply Chain Security Scan
        run: validpack .
```

### Azure DevOps

```yaml
trigger:
  - main

pool:
  vmImage: 'ubuntu-latest'

steps:
  - task: UseDotNet@2
    inputs:
      version: '10.0.x'

  - script: dotnet tool install --global Validpack
    displayName: 'Install Validpack'

  - script: validpack .
    displayName: 'Supply Chain Security Scan'
```

### GitLab CI

```yaml
supply-chain-scan:
  image: mcr.microsoft.com/dotnet/sdk:10.0
  script:
    - dotnet tool install --global Validpack
    - export PATH="$PATH:$HOME/.dotnet/tools"
    - validpack .
```

---

## Example Output

### Console Output

```
=====================================
  SUPPLY CHAIN SECURITY SCAN REPORT
=====================================

Scanned Path: /home/user/my-project
Scan Time:    2026-01-30 18:00:00
Scanned Files: 3

===================
  SUMMARY
===================

Total Dependencies:    15
Unique Dependencies:   12

  Valid:        10
  Whitelisted:   1
  Not Found:     1
  Blacklisted:   0

=====================
  PROBLEMS FOUND
=====================

Packages that don't exist in the registry (Supply Chain Attack Risk):

  ! Npm: suspicious-package-xyz
    Source: /home/user/my-project/package.json

============
  RESULT
============

FAILED - Problems found!
```

### JSON Output

```json
{
  "scannedPath": "/home/user/my-project",
  "scanTime": "2026-01-30T18:00:00",
  "summary": {
    "totalDependencies": 15,
    "uniqueDependencies": 12,
    "valid": 10,
    "whitelisted": 1,
    "notFound": 1,
    "blacklisted": 0
  },
  "hasProblems": true,
  "problems": [
    {
      "packageName": "suspicious-package-xyz",
      "packageType": "Npm",
      "status": "NotFound",
      "sourceFile": "package.json"
    }
  ]
}
```

---

## Supported Package Managers

| Manager | Files Scanned | Registry |
|---------|---------------|----------|
| **npm** | `package.json` | registry.npmjs.org |
| **NuGet** | `*.csproj` | api.nuget.org |
| **PyPI** | `requirements.txt`, `pyproject.toml` | pypi.org |
| **Crates** | `Cargo.toml` | crates.io |
| **Maven** | `pom.xml` | repo1.maven.org |
| **Gradle** | `build.gradle`, `build.gradle.kts` | repo1.maven.org |

### npm

Scans the following dependency sections:
- `dependencies`
- `devDependencies`
- `peerDependencies`
- `optionalDependencies`

Automatically skips:
- `node_modules` directories
- Local references (`file:`, `link:`, `git:`)

### NuGet

Scans `PackageReference` elements in `.csproj` files.

Automatically skips:
- `bin` and `obj` directories

### PyPI (Python)

Scans:
- `requirements.txt` (all `requirements*.txt` files)
- `pyproject.toml` (`[project.dependencies]` section)

Automatically skips:
- Virtual environments (`venv/`, `.venv/`, `env/`)
- `__pycache__` directories
- Editable installs and git references

### Crates (Rust)

Scans `Cargo.toml` files:
- `[dependencies]`
- `[dev-dependencies]`
- `[build-dependencies]`

Automatically skips:
- `target/` directory
- Path and git dependencies

### Maven (Java)

Scans `pom.xml` files for `<dependency>` elements.

Automatically skips:
- `target/` directory
- `.mvn/` directory

### Gradle (Java/Kotlin/Android)

Scans `build.gradle` (Groovy DSL) and `build.gradle.kts` (Kotlin DSL) files.

Supported configurations:
- `implementation`, `api`, `compileOnly`, `runtimeOnly`
- `testImplementation`, `testCompileOnly`, `testRuntimeOnly`
- `annotationProcessor`, `kapt`, `ksp`

Automatically skips:
- `build/` directory
- `.gradle/` directory
- `project(...)` local dependencies
- Variable references (`${version}`, `$variable`)

---

## How It Works

```
┌─────────────────┐
│  Scan Directory │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Find Projects  │  (package.json, *.csproj, requirements.txt, Cargo.toml, pom.xml, build.gradle)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Parse & Extract │  Dependencies
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Deduplicate   │  (save API calls)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Blacklist Check │  → Immediate fail
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Whitelist Check │  → Skip validation
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  API Validation │  (npm, NuGet, PyPI, Crates, Maven)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Generate Report │
└─────────────────┘
```

---

## Building from Source

### Prerequisites

- .NET 10.0 SDK

### Build

```bash
cd src/Validpack
dotnet build
```

### Run Tests

```bash
# Test with valid packages
dotnet run -- ../../test-projects/valid-npm --verbose

# Test with invalid packages (should fail)
dotnet run -- ../../test-projects/invalid-npm --verbose
```

### Create NuGet Package

```bash
dotnet pack -c Release
# Output: ./nupkg/Validpack.1.0.0.nupkg
```

---

## Contributing

Contributions are welcome! Please feel free to submit issues and pull requests.

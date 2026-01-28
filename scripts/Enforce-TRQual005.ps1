<#
.SYNOPSIS
    Enforces TR-QUAL-005: No Console.WriteLine/Write/Read in C# code; use ILogger.

.DESCRIPTION
    Scans .cs files for Console.WriteLine, Console.Write, Console.Read*, Console.Error.Write*.
    Excludes: tools/PlantUmlRender (CLI tool; stdout/stderr is the interface),
    AndroidLogcatLoggerProvider.cs (ILogger sink that writes to logcat via Console).

.EXAMPLE
    .\scripts\Enforce-TRQual005.ps1
    exit 0 if clean, 1 and Write-Error if violations found.
#>
param(
    [string] $RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
)

$ErrorActionPreference = 'Stop'
$excludeDirs = @(
    (Join-Path $RepoRoot 'tools' 'PlantUmlRender'),
    (Join-Path $RepoRoot 'lib' 'NSubstitute.6.0.0')
)
$excludeFileNames = @('AndroidLogcatLoggerProvider.cs')
$pattern = 'Console\.(Write|WriteLine|Read|Error\.Write)'

$violations = @()
Get-ChildItem -Path $RepoRoot -Recurse -Include '*.cs' -File | ForEach-Object {
    $full = $_.FullName
    $skip = $excludeDirs.Where({ $full.StartsWith($_, [StringComparison]::OrdinalIgnoreCase) }, 'First').Count -gt 0
    $skip = $skip -or ($excludeFileNames -contains $_.Name) -or ($full -match '[/\\](bin|obj)[/\\]')
    if ($skip) { return }
    $matches = Select-String -LiteralPath $full -Pattern $pattern -AllMatches
    if ($matches) {
        foreach ($m in $matches) {
            $rel = $full.Substring($RepoRoot.Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
            $violations += [pscustomobject]@{ File = $rel; Line = $m.LineNumber; Text = $m.Line.Trim() }
        }
    }
}

if ($violations.Count -eq 0) {
    exit 0
}

$msg = "TR-QUAL-005: Do not use Console.WriteLine/Write/Read; use ILogger. Violations:`n"
$msg += ($violations | ForEach-Object { "  $($_.File)($($_.Line)): $($_.Text)" }) -join "`n"
Write-Error $msg
exit 1

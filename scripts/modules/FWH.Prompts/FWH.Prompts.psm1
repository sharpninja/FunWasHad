#Requires -Version 5.1

<#
.SYNOPSIS
    FunWasHad Prompts Module - Templatized prompts for AI interactions

.DESCRIPTION
    This module provides templatized prompts that can be filled with parameters
    via PowerShell commands. Prompts are stored as templates with placeholders
    that are replaced when the prompt is invoked.

.NOTES
    Module: FWH.Prompts
    Version: 1.0.5
#>

# Initialize prompt templates storage
$script:PromptTemplates = @{}
$script:SharedContext = ''
$script:ModuleVersion = '1.0.5'
# Default prompts file path (used when -PromptsFile is not specified)
$script:PromptsFilePath = Join-Path $PSScriptRoot 'prompts.md'
# CR-PSM-2.3.1: cache for Read-CcliPromptsFile keyed by Path (full) and LastWriteTime
$script:ReadCcliPromptsFileCache = @{}

# CR-PSM-2.4.3: single manifest for Get-CcliHelp and Export-ModuleMember
$script:CcliCommandManifest = @(
    @{ Name = 'Get-CcliAvailablePrompts'; Alias = 'Get-AvailablePrompts'; Desc = 'List all prompt templates with descriptions and parameters.' }
    @{ Name = 'Get-CcliPrompt';         Alias = 'Get-Prompt';         Desc = 'Get a filled prompt from a template by providing parameter values.' }
    @{ Name = 'Invoke-CcliPrompt';     Alias = 'Invoke-Prompt';     Desc = 'Get a prompt and output to console, clipboard, or file.' }
    @{ Name = 'Get-CcliPromptTemplate'; Alias = 'Get-PromptTemplate'; Desc = 'Get details about a specific prompt template.' }
    @{ Name = 'New-CcliPromptTemplate'; Alias = 'New-PromptTemplate'; Desc = 'Create a custom prompt template.' }
    @{ Name = 'Remove-CcliPromptTemplate'; Alias = 'Remove-PromptTemplate'; Desc = 'Remove a prompt template.' }
    @{ Name = 'Write-CcliPromptToCli';  Alias = 'Write-PromptToCli';  Desc = 'Write a prompt to CLI.md for agent consumption.' }
    @{ Name = 'Invoke-CcliClean';      Alias = 'clean-cli';          Desc = 'Archive CLI.md to CLI-history.md and reset CLI.md.' }
    @{ Name = 'Watch-CcliResults';     Alias = 'Watch-CliResults';   Desc = 'Watch CLI.md for command results.' }
    @{ Name = 'Get-CcliHelp';          Alias = 'Get-CliHelp';        Desc = 'Show this help (module, commands, or a prompt).' }
)

# CR-PSM-2.4.4: single default CLI.md template; reused by Write-CcliPromptToCli and Invoke-CcliClean
$script:CcliDefaultCliTemplate = @'
# CLI Agent

This file is monitored by the CLI Agent. Add commands below and the agent will execute them.

## Usage

Add commands in the format:
```cli
help
```

The agent will execute the command and append results below.

## Commands

## Prompts

## Results

_Results will appear here after commands are executed._

---
*Last updated: PLACEHOLDER*
'@

function Get-CcliDefaultCliContent { param([string]$Timestamp = (Get-Date -Format 'yyyy-MM-dd HH:mm:ss')); return $script:CcliDefaultCliTemplate -replace 'PLACEHOLDER', $Timestamp }

<#
.SYNOPSIS
    Internal. Finds the project root (directory containing FunWasHad.sln) by walking up from StartPath.
.NOTES
    Not exported. Returns the first directory that contains FunWasHad.sln, or $PWD.Path if not found.
#>
function Find-CcliProjectRoot {
    param([string]$StartPath = $PWD.Path)
    $dir = $StartPath
    while ($dir) {
        if (Test-Path (Join-Path $dir "FunWasHad.sln")) { return $dir }
        $parent = Split-Path -Parent $dir
        if (-not $parent -or $parent -eq $dir) { break }
        $dir = $parent
    }
    return $PWD.Path
}

<#
.SYNOPSIS
    Internal. Reads cli-agent.json and returns the CliAgent section.
.DESCRIPTION
    Returns $null if the file is missing or CliAgent is missing. Paths in the object are not resolved.
.PARAMETER ProjectRoot
    Optional. Project root (directory containing FunWasHad.sln). If not given, uses Find-CcliProjectRoot -StartPath $PWD.Path.
#>
<#
.SYNOPSIS
    Internal. Returns $true if ResolvedPath is under ProjectRoot (CR-PSM-2.1.1, 2.1.3).
#>
function Test-CliPathUnderProjectRoot {
    param([string]$ProjectRoot, [string]$ResolvedPath)
    if ([string]::IsNullOrWhiteSpace($ResolvedPath)) { return $false }
    $base = [System.IO.Path]::GetFullPath($ProjectRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar))
    if (-not $base.EndsWith([System.IO.Path]::DirectorySeparatorChar)) { $base = $base + [System.IO.Path]::DirectorySeparatorChar }
    $r = [System.IO.Path]::GetFullPath($ResolvedPath)
    return $r.StartsWith($base, [StringComparison]::OrdinalIgnoreCase)
}

function Read-CcliAgentConfig {
    param([string]$ProjectRoot)
    if ([string]::IsNullOrWhiteSpace($ProjectRoot)) { $ProjectRoot = Find-CcliProjectRoot -StartPath $PWD.Path }
    $configPath = Join-Path $ProjectRoot "cli-agent.json"
    if (-not (Test-Path -LiteralPath $configPath)) { return $null }
    try {
        # CR-PSM-2.2.4: -LiteralPath. CR-PSM-2.1.2: limit to first 64KB to avoid DoS from huge config.
        $raw = Get-Content -LiteralPath $configPath -Raw
        if ($raw -and $raw.Length -gt 65536) { $raw = $raw.Substring(0, 65536) }
        $obj = $raw | ConvertFrom-Json
        return $obj.CliAgent
    } catch { return $null }
}

<# CR-PSM-2.4.1: Extracted path resolvers; use in Write-CcliPromptToCli, Invoke-CcliClean, init. #>
function Resolve-CliFilePath {
    param([string]$ProjectRoot)
    if ([string]::IsNullOrWhiteSpace($ProjectRoot)) { $ProjectRoot = Find-CcliProjectRoot -StartPath $PWD.Path }
    $config = Read-CcliAgentConfig -ProjectRoot $ProjectRoot
    if ($config -and -not [string]::IsNullOrWhiteSpace($config.CliMdPath)) {
        $p = $config.CliMdPath.Trim()
        $resolved = if ([System.IO.Path]::IsPathRooted($p)) { $p } else { [System.IO.Path]::GetFullPath((Join-Path $ProjectRoot $p)) }
    } else {
        $resolved = [System.IO.Path]::GetFullPath((Join-Path $ProjectRoot "CLI.md"))
    }
    if (-not (Test-CliPathUnderProjectRoot -ProjectRoot $ProjectRoot -ResolvedPath $resolved)) {
        Write-Error "Resolved CliMdPath is outside project root: $resolved"
        return $null
    }
    return $resolved
}

function Resolve-PromptsFilePath {
    param([string]$ProjectRoot)
    if ([string]::IsNullOrWhiteSpace($ProjectRoot)) { $ProjectRoot = Find-CcliProjectRoot -StartPath $PWD.Path }
    $config = Read-CcliAgentConfig -ProjectRoot $ProjectRoot
    if (-not $config -or [string]::IsNullOrWhiteSpace($config.PromptsMdPath)) { return $null }
    $p = $config.PromptsMdPath.Trim()
    $resolved = if ([System.IO.Path]::IsPathRooted($p)) { $p } else { [System.IO.Path]::GetFullPath((Join-Path $ProjectRoot $p)) }
    if (-not (Test-CliPathUnderProjectRoot -ProjectRoot $ProjectRoot -ResolvedPath $resolved)) { return $null }
    return $resolved
}

# Apply cli-agent.json PromptsMdPath if present (must run before Initialize-CcliPromptTemplates). CR-PSM-2.4.1: use Resolve-PromptsFilePath.
$script:_initProjectRoot = $null
try {
    $script:_initProjectRoot = Find-CcliProjectRoot -StartPath $PSScriptRoot
    $resolvedPrompts = Resolve-PromptsFilePath -ProjectRoot $script:_initProjectRoot
    if ($resolvedPrompts) { $script:PromptsFilePath = $resolvedPrompts }
} catch { /* ignore; keep default PromptsFilePath */ }
Remove-Variable -Scope Script -Name _initProjectRoot -ErrorAction SilentlyContinue

<#
.SYNOPSIS
    Reads and parses a prompts file, returning templates and shared context.

.DESCRIPTION
    Internal helper. Parses a prompts markdown file and returns PromptTemplates and SharedContext.
    CR-PSM-2.2.1: $null = file missing; @{ PromptTemplates=@{}; SharedContext='' } = file empty or no parseable sections.
    Initialize-CcliPromptTemplates treats $null as error (file not found); empty PromptTemplates is allowed.
#>
function Read-CcliPromptsFile {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )
    if (-not (Test-Path -LiteralPath $Path)) {
        return $null
    }
    $fullPath = [System.IO.Path]::GetFullPath((Resolve-Path -LiteralPath $Path).Path)
    $lwt = (Get-Item -LiteralPath $Path).LastWriteTime
    if ($script:ReadCcliPromptsFileCache[$fullPath] -and $script:ReadCcliPromptsFileCache[$fullPath].LastWrite -eq $lwt) {
        return $script:ReadCcliPromptsFileCache[$fullPath].Result
    }
    $content = Get-Content -LiteralPath $Path -Raw
    if ([string]::IsNullOrWhiteSpace($content)) {
        Write-Warning "Prompts file is empty: $Path"
        $empty = @{ PromptTemplates = @{}; SharedContext = '' }
        $script:ReadCcliPromptsFileCache[$fullPath] = @{ LastWrite = $lwt; Result = $empty }
        return $empty
    }
    $pt = @{}
    $sc = ''
    $sections = $content -split '(?m)^---\s*$'
    foreach ($section in $sections) {
        if ([string]::IsNullOrWhiteSpace($section.Trim())) { continue }
        if (-not ($section -match '(?m)^##\s+(.+)$')) { continue }
        $promptName = $matches[1].Trim()
        if ($promptName -eq 'shared-context') {
            if ($section -match '(?m)^##\s+shared-context\s*\n\n([\s\S]*?)(?=\n###\s+Parameters|\s*---|\Z)') {
                $sc = $matches[1].Trim()
            }
            continue
        }
        $description = ''
        if ($section -match '(?ms)^\s*##\s+[^\n]+\r?\n\r?\n([\s\S]+?)(?=\r?\n\r?\n|\s*###|\Z)') { $description = $matches[1].Trim() }
        $templateBody = ''
        if ($section -match '(?ms)^\s*##\s+[^\n]+\r?\n\r?\n(.*?)(?=\r?\n*###\s+Parameters|\Z)') { $templateBody = $matches[1].Trim() }
        $parameters = @()
        $parameterDefaults = @{}
        if ($section -match '(?m)###\s+Parameters\s*\r?\n\r?\n([\s\S]+?)(?=\r?\n---|\Z)') {
            $tableContent = $matches[1]
            $lines = $tableContent -split '\r?\n'
            $inTable = $false
            foreach ($line in $lines) {
                if ($line -match '^\|') {
                    if (-not $inTable) { $inTable = $true; continue }
                    if ($line -match '^\|\s*---') { continue }
                    if ($line -match '\|\s*([^\|]+?)\s*\|\s*([^\|]+?)\s*\|\s*([^\|]+?)\s*\|\s*([^\|]*?)\s*\|') {
                        $paramName = $matches[1].Trim()
                        $defaultVal = $matches[4].Trim()
                        if ($paramName -ne 'Parameter') {
                            $parameters += $paramName
                            $parameterDefaults[$paramName] = if ([string]::IsNullOrWhiteSpace($defaultVal) -or $defaultVal -eq '-') { '' } else { $defaultVal }
                        }
                    }
                    elseif ($line -match '\|\s*([^\|]+?)\s*\|\s*([^\|]+?)\s*\|\s*([^\|]+?)\s*\|') {
                        $paramName = $matches[1].Trim()
                        if ($paramName -ne 'Parameter') {
                            $parameters += $paramName
                            $parameterDefaults[$paramName] = ''
                        }
                    }
                }
            }
        }
        if (-not [string]::IsNullOrWhiteSpace($promptName) -and -not [string]::IsNullOrWhiteSpace($templateBody)) {
            $pt[$promptName] = @{
                Description = $description
                Template = $templateBody
                Parameters = $parameters
                ParameterDefaults = $parameterDefaults
            }
        }
    }
    $result = @{ PromptTemplates = $pt; SharedContext = $sc }
    $script:ReadCcliPromptsFileCache[$fullPath] = @{ LastWrite = $lwt; Result = $result }
    return $result
}

<#
.SYNOPSIS
    Loads prompt templates from the default prompts file.

.DESCRIPTION
    Parses the default prompts file (prompts.md) and loads all prompt templates into memory.
    Each template is separated by `---` markers and contains a body and parameters table.

.EXAMPLE
    Initialize-CcliPromptTemplates
#>
function Initialize-CcliPromptTemplates {
    [CmdletBinding()]
    param()
    $result = Read-CcliPromptsFile -Path $script:PromptsFilePath
    if ($null -eq $result) {
        Write-Error "Prompts file not found: $script:PromptsFilePath"
        return
    }
    $script:PromptTemplates = $result.PromptTemplates
    $script:SharedContext = $result.SharedContext
    Write-Verbose "Loaded $($script:PromptTemplates.Count) prompt templates from $script:PromptsFilePath"
}

# Load prompts from the default prompts file on module initialization
Initialize-CcliPromptTemplates

# Banner: module ready for use (displayed on Import-Module, after prompts are loaded)
Write-Host ''
Write-Host '  ' -NoNewline
Write-Host 'FWH.Prompts' -ForegroundColor Cyan -NoNewline
Write-Host " v$script:ModuleVersion " -NoNewline
Write-Host 'Ready for use.' -ForegroundColor Green
Write-Host '  ' -NoNewline
Write-Host 'Get-AvailablePrompts' -ForegroundColor DarkGray -NoNewline
Write-Host ' to list prompts. '
Write-Host '  ' -NoNewline
Write-Host 'Get-CcliHelp' -ForegroundColor DarkGray -NoNewline
Write-Host ' for help.'
Write-Host ''

<#
.SYNOPSIS
    Gets a prompt template by name.

.DESCRIPTION
    Retrieves a prompt template from the module's template collection or from a specified prompts file.

.PARAMETER Name
    The name of the prompt template to retrieve.

.PARAMETER PromptsFile
    Path to the prompts file to use. Defaults to prompts.md in the module directory when not specified.

.EXAMPLE
    Get-PromptTemplate -Name 'code-review'

.EXAMPLE
    Get-PromptTemplate -Name 'implement-feature' -PromptsFile '.\test-prompts.md'
#>
function Get-CcliPromptTemplate {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $false)]
        [string]$PromptsFile
    )
    $templates = $script:PromptTemplates
    if (-not [string]::IsNullOrWhiteSpace($PromptsFile)) {
        $data = Read-CcliPromptsFile -Path $PromptsFile
        if ($null -eq $data) {
            Write-Error "Prompts file not found: $PromptsFile"
            return $null
        }
        $templates = $data.PromptTemplates
    }
    if ($templates.ContainsKey($Name)) {
        return $templates[$Name]
    }
    Write-Error "Prompt template '$Name' not found. Use Get-AvailablePrompts to see available templates."
    return $null
}

<#
.SYNOPSIS
    Gets a filled prompt by template name and parameters.

.DESCRIPTION
    Retrieves a prompt template and fills it with the provided parameters.

.PARAMETER Name
    The name of the prompt template to use.

.PARAMETER Parameters
    Optional. Hashtable of parameters to fill in the template. When omitted or when a parameter key is omitted, the default values from the template in prompts.md are used.

.PARAMETER PromptsFile
    Path to the prompts file to use. Defaults to prompts.md in the module directory when not specified.

.EXAMPLE
    Get-Prompt -Name 'code-review'
    Uses defaults from prompts.md for FeatureName, FilePath, and Code.

.EXAMPLE
    Get-Prompt -Name 'code-review' -Parameters @{
        FeatureName = 'User Authentication'
        FilePath = 'src/AuthService.cs'
        Code = Get-Content 'src/AuthService.cs' -Raw
    }

.EXAMPLE
    Get-Prompt -Name 'debug-issue' -Parameters @{...} -PromptsFile '.\test-prompts.md'
#>
function Get-CcliPrompt {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $false)]
        [hashtable]$Parameters = @{},

        [Parameter(Mandatory = $false)]
        [string]$PromptsFile
    )
    if ($null -eq $Parameters) { $Parameters = @{} }
    $sharedContextToUse = $script:SharedContext
    if (-not [string]::IsNullOrWhiteSpace($PromptsFile)) {
        $data = Read-CcliPromptsFile -Path $PromptsFile
        if ($null -eq $data) {
            Write-Error "Prompts file not found: $PromptsFile"
            return $null
        }
        $template = $data.PromptTemplates[$Name]
        $sharedContextToUse = $data.SharedContext
    }
    else {
        $template = Get-CcliPromptTemplate -Name $Name
    }
    if ($null -eq $template) {
        if (-not [string]::IsNullOrWhiteSpace($PromptsFile)) {
            # CR-PSM-2.2.2: message includes the file used when -PromptsFile is specified
            Write-Error "Prompt template '$Name' not found in $PromptsFile. Use Get-AvailablePrompts -PromptsFile '$PromptsFile' to list templates."
        } else {
            Write-Error "Prompt template '$Name' not found. Use Get-AvailablePrompts to see available templates."
        }
        return $null
    }
    $prompt = $template.Template
    $defaults = if ($template.ParameterDefaults) { $template.ParameterDefaults } else { @{} }
    foreach ($paramName in $template.Parameters) {
        $placeholder = "{$paramName}"
        $value = if ($Parameters.ContainsKey($paramName)) {
            $Parameters[$paramName]
        }
        elseif ($defaults.ContainsKey($paramName)) {
            $defaults[$paramName]
        }
        else {
            ''
        }
        $prompt = $prompt -replace [regex]::Escape($placeholder), $value
    }
    $remainingPlaceholders = [regex]::Matches($prompt, '\{([^}]+)\}')
    foreach ($m in $remainingPlaceholders) {
        $n = $m.Groups[1].Value
        $v = if ($defaults.ContainsKey($n)) { $defaults[$n] } else { '' }
        $prompt = $prompt -replace [regex]::Escape("{$n}"), $v
    }
    $stillRemaining = [regex]::Matches($prompt, '\{([^}]+)\}')
    if ($stillRemaining.Count -gt 0) {
        $missing = $stillRemaining | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique
        Write-Warning "The following placeholders were not replaced: $($missing -join ', ')"
    }
    if (-not [string]::IsNullOrWhiteSpace($sharedContextToUse)) {
        $prompt = $sharedContextToUse + "`n`n---`n`n" + $prompt
    }
    return $prompt
}

<#
.SYNOPSIS
    Invokes a prompt (gets and optionally outputs/copies to clipboard).

.DESCRIPTION
    Gets a filled prompt and optionally outputs it, copies to clipboard, or saves to file.

.PARAMETER Name
    The name of the prompt template to use.

.PARAMETER Parameters
    Optional. Hashtable of parameters to fill in the template. When omitted or when a parameter key is omitted, the default values from the template in prompts.md are used.

.PARAMETER PromptsFile
    Path to the prompts file to use. Defaults to prompts.md in the module directory when not specified.

.PARAMETER OutputToClipboard
    If specified, copies the prompt to clipboard.

.PARAMETER OutputToFile
    If specified, saves the prompt to a file.

.PARAMETER OutputPath
    Path to save the prompt file (required if OutputToFile is specified).

.EXAMPLE
    Invoke-CcliPrompt -Name 'code-review'
    Uses defaults from prompts.md; outputs the filled prompt to the host.

.EXAMPLE
    Invoke-CcliPrompt -Name 'code-review' -Parameters @{
        FeatureName = 'User Auth'
        FilePath = 'Auth.cs'
        Code = 'public class Auth { }'
    } -OutputToClipboard

.EXAMPLE
    Invoke-CcliPrompt -Name 'implement-feature' -Parameters @{...} -OutputToFile -OutputPath 'prompt.txt'
#>
function Invoke-CcliPrompt {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $false)]
        [hashtable]$Parameters = @{},

        [Parameter(Mandatory = $false)]
        [string]$PromptsFile,

        [Parameter(Mandatory = $false)]
        [switch]$OutputToClipboard,

        [Parameter(Mandatory = $false)]
        [switch]$OutputToFile,

        [Parameter(Mandatory = $false)]
        [string]$OutputPath
    )
    if ($null -eq $Parameters) { $Parameters = @{} }
    $getParams = @{ Name = $Name; Parameters = $Parameters }
    if (-not [string]::IsNullOrWhiteSpace($PromptsFile)) { $getParams['PromptsFile'] = $PromptsFile }
    $prompt = Get-CcliPrompt @getParams

    if ($null -eq $prompt) {
        return
    }

    # Copy to clipboard if requested
    if ($OutputToClipboard) {
        try {
            $prompt | Set-Clipboard
            Write-Host "Prompt copied to clipboard!" -ForegroundColor Green
        }
        catch {
            Write-Warning "Could not copy to clipboard: $_"
        }
    }

    # Save to file if requested
    if ($OutputToFile) {
        if ([string]::IsNullOrWhiteSpace($OutputPath)) {
            # CR-PSM-2.2.5: sanitize $Name for use in filename (replace path-invalid chars with _)
            $safeName = $Name -replace '[\/:*?"<>|]', '_'
            $OutputPath = "prompt_${safeName}_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
        }

        try {
            $prompt | Out-File -FilePath $OutputPath -Encoding UTF8
            Write-Host "Prompt saved to: $OutputPath" -ForegroundColor Green
        }
        catch {
            Write-Error "Could not save to file: $_"
        }
    }

    return $prompt
}

<#
.SYNOPSIS
    Gets a list of all available prompt templates.

.DESCRIPTION
    Returns information about all available prompt templates in the module or from a specified prompts file.

.PARAMETER PromptsFile
    Path to the prompts file to use. Defaults to prompts.md in the module directory when not specified.

.EXAMPLE
    Get-AvailablePrompts

.EXAMPLE
    Get-AvailablePrompts -PromptsFile '.\test-prompts.md' | Format-Table Name, Description
#>
function Get-CcliAvailablePrompts {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [string]$PromptsFile
    )
    $templates = $script:PromptTemplates
    if (-not [string]::IsNullOrWhiteSpace($PromptsFile)) {
        $data = Read-CcliPromptsFile -Path $PromptsFile
        if ($null -eq $data) {
            Write-Error "Prompts file not found: $PromptsFile"
            return @()
        }
        $templates = $data.PromptTemplates
    }
    $templates.GetEnumerator() | ForEach-Object {
        [PSCustomObject]@{
            Name = $_.Key
            Description = $_.Value.Description
            Parameters = $_.Value.Parameters -join ', '
            ParameterCount = $_.Value.Parameters.Count
        }
    } | Sort-Object Name
}

<#
.SYNOPSIS
    Shows help for the FWH.Prompts module, its commands, or a specific prompt.

.DESCRIPTION
    Get-CcliHelp displays an overview of the FWH.Prompts module, available commands, and quick start.
    Use -Topic to get help for a specific command (e.g. Get-CcliPrompt) or prompt template (e.g. code-review).

.PARAMETER Topic
    Optional. A command name (e.g. Get-CcliPrompt, Invoke-CcliPrompt) or a prompt template name (e.g. code-review).
    When specified, shows Get-Help for that command or Get-CcliPromptTemplate output for that prompt.

.PARAMETER Full
    When used with -Topic and a command name, passes -Full to Get-Help.

.EXAMPLE
    Get-CcliHelp
    Shows module overview and command list.

.EXAMPLE
    Get-CcliHelp -Topic Get-CcliPrompt
    Shows Get-Help for Get-CcliPrompt.

.EXAMPLE
    Get-CcliHelp -Topic code-review
    Shows template details for the code-review prompt.
#>
function Get-CcliHelp {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false, Position = 0)]
        [string]$Topic,

        [Parameter(Mandatory = $false)]
        [switch]$Full
    )

    $commands = $script:CcliCommandManifest

    if (-not [string]::IsNullOrWhiteSpace($Topic)) {
        $t = $Topic.Trim()
        $cmdMatch = $commands | Where-Object { $_.Name -eq $t -or $_.Alias -eq $t } | Select-Object -First 1
        if ($cmdMatch) {
            if ($Full) {
                Get-Help $cmdMatch.Name -Full
            } else {
                Get-Help $cmdMatch.Name
            }
            return
        }
        if ($script:PromptTemplates.ContainsKey($t)) {
            Get-CcliPromptTemplate -Name $t
            return
        }
        Write-Warning "Unknown topic: $t. Use Get-CcliAvailablePrompts for prompt names, or one of: $(($commands | ForEach-Object { $_.Name }) -join ', ')"
    }

    Write-Host ''
    Write-Host '  FWH.Prompts' -ForegroundColor Cyan -NoNewline
    Write-Host " v$script:ModuleVersion" -NoNewline
    Write-Host ' - Templatized prompts for AI interactions.'
    Write-Host ''
    Write-Host '  Quick start:' -ForegroundColor Yellow
    Write-Host '    Get-CcliAvailablePrompts     List prompts.'
    Write-Host '    Get-CcliPrompt -Name <name> -Parameters @{...}  Get a filled prompt.'
    Write-Host '    Invoke-CcliPrompt -Name <name> -Parameters @{...} -OutputToClipboard'
    Write-Host ''
    Write-Host '  Commands:' -ForegroundColor Yellow
    foreach ($c in $commands) {
        Write-Host ('    {0,-28} {1}' -f $c.Name, $c.Desc)
    }
    Write-Host ''
    Write-Host '  Get-CcliHelp -Topic <command|prompt>  Help for a command or prompt (e.g. Get-CcliPrompt, code-review).'
    Write-Host ''
}

<#
.SYNOPSIS
    Creates a new prompt template.

.DESCRIPTION
    Adds a new prompt template to the module's template collection.

.PARAMETER Name
    The name of the new prompt template.

.PARAMETER Description
    Description of what the prompt is used for.

.PARAMETER Template
    The template string with placeholders in {ParameterName} format.

.PARAMETER Parameters
    Array of parameter names that should be provided when using this template.

.EXAMPLE
    New-PromptTemplate -Name 'custom-review' -Description 'Custom code review' -Template 'Review {Code} for {Issues}' -Parameters @('Code', 'Issues')
#>
function New-CcliPromptTemplate {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $true)]
        [string]$Description,

        [Parameter(Mandatory = $true)]
        [string]$Template,

        [Parameter(Mandatory = $false)]
        [string[]]$Parameters = @()
    )
    if ($null -eq $Parameters) { $Parameters = @() }

    if ($script:PromptTemplates.ContainsKey($Name)) {
        Write-Warning "Template '$Name' already exists. Use Remove-PromptTemplate first to replace it."
        return $false
    }

    # Validate template contains all parameter placeholders
    $missingParams = @()
    foreach ($param in $Parameters) {
        if ($Template -notmatch [regex]::Escape("{$param}")) {
            $missingParams += $param
        }
    }

    if ($missingParams.Count -gt 0) {
        Write-Warning "Template does not contain placeholders for: $($missingParams -join ', ')"
    }

    $script:PromptTemplates[$Name] = @{
        Description = $Description
        Template = $Template
        Parameters = $Parameters
        ParameterDefaults = @{}
    }

    Write-Host "Prompt template '$Name' created successfully!" -ForegroundColor Green
    return $true
}

<#
.SYNOPSIS
    Removes a prompt template.

.DESCRIPTION
    Removes a prompt template from the module's template collection.

.PARAMETER Name
    The name of the prompt template to remove.

.PARAMETER Force
    If specified, removes the template without confirmation.

.EXAMPLE
    Remove-PromptTemplate -Name 'custom-review'

.EXAMPLE
    Remove-PromptTemplate -Name 'custom-review' -Force
#>
function Remove-CcliPromptTemplate {
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $false)]
        [switch]$Force
    )

    if (-not $script:PromptTemplates.ContainsKey($Name)) {
        Write-Warning "Template '$Name' does not exist."
        return $false
    }

    if ($Force -or $PSCmdlet.ShouldProcess("Template '$Name'", "Remove")) {
        $script:PromptTemplates.Remove($Name)
        Write-Host "Prompt template '$Name' removed successfully!" -ForegroundColor Green
        return $true
    }

    return $false
}

<#
.SYNOPSIS
    Writes a prompt to CLI.md and optionally monitors for results.

.DESCRIPTION
    Writes a filled prompt to the CLI.md file in a format that can be processed.
    Optionally monitors the file for results and displays them in the terminal.

.PARAMETER Name
    The name of the prompt template to use.

.PARAMETER Parameters
    Optional. Hashtable of parameters to fill in the template. When omitted or when a parameter key is omitted, the default values from the template in prompts.md are used.

.PARAMETER PromptsFile
    Path to the prompts file to use. Defaults to prompts.md in the module directory when not specified.

.PARAMETER Watch
    If specified, monitors CLI.md for results and displays them.

.PARAMETER ProjectRoot
    Root directory of the project (default: auto-detected).

.EXAMPLE
    Write-PromptToCli -Name 'code-review'
    Uses defaults from prompts.md; writes to CLI.md.

.EXAMPLE
    Write-PromptToCli -Name 'code-review' -Parameters @{
        FeatureName = 'User Auth'
        FilePath = 'Auth.cs'
        Code = Get-Content 'Auth.cs' -Raw
    }

.EXAMPLE
    Write-PromptToCli -Name 'code-review' -Parameters @{...} -PromptsFile '.\test-prompts.md'
#>
function Write-CcliPromptToCli {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Name,

        [Parameter(Mandatory = $false)]
        [hashtable]$Parameters = @{},

        [Parameter(Mandatory = $false)]
        [string]$PromptsFile,

        [Parameter(Mandatory = $false)]
        [switch]$Watch,

        [Parameter(Mandatory = $false)]
        [string]$ProjectRoot
    )
    if ($null -eq $Parameters) { $Parameters = @{} }
    # Project root = directory containing FunWasHad.sln (same rule as CLI Agent so both use the same CLI.md).
    # Try $PWD first; if not under the repo (e.g. standalone terminal started in $HOME), fall back to $PSScriptRoot
    # so the module can find the repo when imported from a path under it.
    if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
        $ProjectRoot = Find-CcliProjectRoot -StartPath $PWD.Path
    }
    $ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar))
    if (-not (Test-Path (Join-Path $ProjectRoot "FunWasHad.sln"))) {
        $alt = Find-CcliProjectRoot -StartPath $PSScriptRoot
        if ($alt -and (Test-Path (Join-Path $alt "FunWasHad.sln"))) { $ProjectRoot = [System.IO.Path]::GetFullPath($alt) }
    }
    if (-not (Test-Path (Join-Path $ProjectRoot "FunWasHad.sln"))) {
        Write-Host 'CLI.md was NOT updated.' -ForegroundColor Red
        Write-Host ('Reason: Project root not found (FunWasHad.sln missing). Run from the repository or set -ProjectRoot. Resolved: {0}' -f $ProjectRoot) -ForegroundColor Yellow
        Write-Error "Project root not found (FunWasHad.sln missing). Run from the repository or set -ProjectRoot. Resolved: $ProjectRoot"
        return
    }

    $cliFilePath = Resolve-CliFilePath -ProjectRoot $ProjectRoot
    if (-not $cliFilePath) { return }
    $sizeBefore = if (Test-Path -LiteralPath $cliFilePath) { (Get-Item -LiteralPath $cliFilePath).Length } else { 0 }
    Write-Verbose "Writing to CLI file: $cliFilePath"

    $getParams = @{ Name = $Name; Parameters = $Parameters }
    if (-not [string]::IsNullOrWhiteSpace($PromptsFile)) { $getParams['PromptsFile'] = $PromptsFile }
    $prompt = Get-CcliPrompt @getParams
    if ($null -eq $prompt) {
        Write-Host 'CLI.md was NOT updated.' -ForegroundColor Red
        Write-Host ('Reason: Prompt template ''{0}'' not found or failed to load.' -f $Name) -ForegroundColor Yellow
        Write-Host ('Target: {0}' -f $cliFilePath) -ForegroundColor Gray
        Write-Host ('Size before: {0:N0} bytes' -f $sizeBefore) -ForegroundColor Gray
        return
    }

    # Read current CLI.md content (normalize \r\n to \n so regex and edits behave consistently). CR-PSM-2.4.4: use shared default when missing.
    $content = if (Test-Path -LiteralPath $cliFilePath) {
        (Get-Content -LiteralPath $cliFilePath -Raw) -replace '\r\n', "`n"
    } else {
        Get-CcliDefaultCliContent
    }

    # Find or create Prompts section
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $promptBlock = @"

### Prompt: $Name ($timestamp)

``````prompt
$prompt
``````

"@

    # Check if Prompts section exists; use \r?\n for line endings
    if ($content -match '(?s)(## Prompts\s*\r?\n)') {
        # Append to existing Prompts section (before Results or ---)
        if ($content -match '(?s)(## Prompts\s*\r?\n)(.*?)(?=\r?\n## Results|\r?\n---|\Z)') {
            $existingPrompts = $matches[2]
            $newPromptsContent = $existingPrompts + $promptBlock
            $content = $content -replace '(?s)(## Prompts\s*\r?\n)(.*?)(?=\r?\n## Results|\r?\n---|\Z)', "`$1$newPromptsContent"
        } else {
            $content = $content -replace '(?s)(## Prompts\s*\r?\n)(.*?)(\r?\n## Results|\r?\n---|\Z)', "`$1`$2$promptBlock`$3"
        }
    } else {
        # Insert Prompts section before Results
        if ($content -match '(?s)(## Results)') {
            $content = $content -replace '(?s)(## Results)', "## Prompts$promptBlock`n`$1"
        } else {
            # Append before last ---
            $lastDashIndex = $content.LastIndexOf("---")
            if ($lastDashIndex -ge 0) {
                $content = $content.Substring(0, $lastDashIndex) +
                          "`n## Prompts$promptBlock" +
                          $content.Substring($lastDashIndex)
            } else {
                $content += "`n## Prompts$promptBlock"
            }
        }
    }

    # Insert a ```cli prompt <name>``` block under ## Commands so the CLI Agent runs it and updates ## Results.
    # Content is normalized to \n; match \r?\n in case normalization is skipped.
    $cliBlock = "``````cli`nprompt $Name`n``````" + "`n`n"
    if ($content -match '(?s)(## Commands\s*\r?\n)(\r?\n## Prompts)') {
        $content = $content -replace '(?s)(## Commands\s*\r?\n)(\r?\n## Prompts)', "`$1$cliBlock`$2"
    } elseif ($content -match '(?s)(## Commands\s*\r?\n)(\r?\n## Results)') {
        $content = $content -replace '(?s)(## Commands\s*\r?\n)(\r?\n## Results)', "`$1$cliBlock`$2"
    } elseif ($content -match '(?s)(## Commands\s*\r?\n)') {
        # Fallback: insert right after ## Commands line
        $content = $content -replace '(?s)(## Commands\s*\r?\n)', "`$1$cliBlock"
    }

    # Update last updated timestamp
    $content = [regex]::Replace($content, '\*Last updated: .*\*', "*Last updated: $timestamp*")

    # Write to file (ensure target directory exists when CliMdPath is in a subdir, e.g. docs/CLI.md)
    $cliDir = [System.IO.Path]::GetDirectoryName($cliFilePath)
    if (-not [string]::IsNullOrEmpty($cliDir) -and -not (Test-Path -LiteralPath $cliDir)) {
        [System.IO.Directory]::CreateDirectory($cliDir) | Out-Null
    }
    try {
        $utf8NoBom = New-Object System.Text.UTF8Encoding $false
        [System.IO.File]::WriteAllText($cliFilePath, $content, $utf8NoBom)
        $sizeAfter = (Get-Item -LiteralPath $cliFilePath).Length
        Write-Host '[OK] CLI.md updated' -ForegroundColor Green
        Write-Host ('   Target: ' + $cliFilePath) -ForegroundColor Gray
        Write-Host ('   Size before: {0:N0} bytes' -f $sizeBefore) -ForegroundColor Gray
        Write-Host ('   Size after:  {0:N0} bytes' -f $sizeAfter) -ForegroundColor Gray
        Write-Host ('   Prompt: ' + $Name + '; a `cli prompt ' + $Name + '` block was added.') -ForegroundColor Gray
    }
    catch {
        Write-Host 'CLI.md was NOT updated.' -ForegroundColor Red
        Write-Host ('Reason: Write failed - {0}' -f $_.Exception.Message) -ForegroundColor Yellow
        Write-Host ('Target: {0}' -f $cliFilePath) -ForegroundColor Gray
        Write-Host ('Size before: {0:N0} bytes' -f $sizeBefore) -ForegroundColor Gray
        Write-Error ('Failed to write to CLI file: ' + $cliFilePath + ' - ' + $_)
        return
    }

    # If Watch is specified, monitor for results
    if ($Watch) {
        Write-Host 'Monitoring CLI.md for results...' -ForegroundColor Cyan
        Watch-CcliResults -CliFilePath $cliFilePath -PromptName $Name
    }
}

<#
.SYNOPSIS
    Archives CLI.md to CLI-history.md and resets CLI.md to the default template.

.DESCRIPTION
    Same behavior as the 'clean-cli' command in the CLI Agent: appends the current
    CLI.md to CLI-history.md with a timestamp, then overwrites CLI.md with the
    default template. Use this when the module is loaded so you can run clean-cli
    from PowerShell without adding a ```cli block to CLI.md.

.PARAMETER ProjectRoot
    Optional. Project root (directory containing FunWasHad.sln). Default: auto-detected.

.EXAMPLE
    clean-cli

.EXAMPLE
    Invoke-CcliClean -ProjectRoot 'E:\github\FunWasHad'
#>
function Invoke-CcliClean {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $false)]
        [string]$ProjectRoot
    )
    if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
        $ProjectRoot = Find-CcliProjectRoot -StartPath $PWD.Path
    }
    $ProjectRoot = [System.IO.Path]::GetFullPath($ProjectRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar, [System.IO.Path]::AltDirectorySeparatorChar))
    if (-not (Test-Path (Join-Path $ProjectRoot 'FunWasHad.sln'))) {
        $alt = Find-CcliProjectRoot -StartPath $PSScriptRoot
        if ($alt -and (Test-Path (Join-Path $alt 'FunWasHad.sln'))) { $ProjectRoot = [System.IO.Path]::GetFullPath($alt) }
    }
    if (-not (Test-Path (Join-Path $ProjectRoot 'FunWasHad.sln'))) {
        Write-Error ('Project root not found (FunWasHad.sln missing). Run from the repository or set -ProjectRoot. Resolved: ' + $ProjectRoot)
        return
    }

    $cliFilePath = Resolve-CliFilePath -ProjectRoot $ProjectRoot
    if (-not $cliFilePath) { return }

    if (-not (Test-Path -LiteralPath $cliFilePath)) {
        Write-Error ('CLI file not found: ' + $cliFilePath)
        return
    }

    $currentContent = Get-Content -LiteralPath $cliFilePath -Raw
    $cliHistoryPath = [System.IO.Path]::GetFullPath((Join-Path $ProjectRoot 'CLI-history.md'))
    $timestamp = Get-Date -Format 'yyyy-MM-dd HH:mm:ss'
    $separator = "`n`n---`n## Archive Entry: $timestamp`n---`n`n"
    $historyEntry = $separator + $currentContent

    $historyHeader = @'
# CLI History

This file contains archived content from CLI.md.

'@
    if (Test-Path -LiteralPath $cliHistoryPath) {
        [System.IO.File]::AppendAllText($cliHistoryPath, $historyEntry)
    } else {
        $utf8NoBom = New-Object System.Text.UTF8Encoding $false
        [System.IO.File]::WriteAllText($cliHistoryPath, $historyHeader + $historyEntry, $utf8NoBom)
    }

    $initialContent = @'
# CLI Agent

This file is monitored by the CLI Agent. Add commands below and the agent will execute them.

## Usage

Add commands in the format:
```cli
help
```

The agent will execute the command and append results below.

## Commands

## Prompts

## Results

_Results will appear here after commands are executed._

---
*Last updated: PLACEHOLDER*
'@
    $initialContent = $initialContent -replace 'PLACEHOLDER', $timestamp
    $utf8 = New-Object System.Text.UTF8Encoding $false
    [System.IO.File]::WriteAllText($cliFilePath, $initialContent, $utf8)

    Write-Host 'CLI file has been archived to CLI-history.md and reset to default.' -ForegroundColor Green
    Write-Host ('   History: ' + $cliHistoryPath) -ForegroundColor Gray
    Write-Host ('   Archived: ' + $timestamp) -ForegroundColor Gray
}

<#
.SYNOPSIS
    Monitors CLI.md for results and displays them in the terminal.

.DESCRIPTION
    Watches the CLI.md file for changes and displays new results in the terminal.
    Continues monitoring until stopped (Ctrl+C).
    CR-PSM-2.3.2: Uses polling (Start-Sleep -Seconds 1). -Timeout limits total runtime. For PowerShell 6+ a FileSystemWatcher-based implementation could reduce overhead on slow or network drives.

.PARAMETER CliFilePath
    Path to the CLI.md file.

.PARAMETER PromptName
    Optional: Name of the prompt to watch for (filters results).

.PARAMETER Timeout
    Maximum time to watch in seconds (default: 300 = 5 minutes).

.EXAMPLE
    Watch-CliResults -CliFilePath '.\CLI.md'

.EXAMPLE
    Watch-CliResults -CliFilePath '.\CLI.md' -PromptName 'code-review'
#>
function Watch-CcliResults {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$CliFilePath,

        [Parameter(Mandatory = $false)]
        [string]$PromptName,

        [Parameter(Mandatory = $false)]
        [int]$Timeout = 300
    )

    if (-not (Test-Path $CliFilePath)) {
        Write-Error ('CLI.md file not found: ' + $CliFilePath)
        return
    }

    # Resolve project root (directory containing FunWasHad.sln) from CLI.md's directory; same rule as CLI Agent
    $cliDir = [System.IO.Path]::GetDirectoryName((Resolve-Path -Path $CliFilePath -ErrorAction Stop).Path)
    $ProjectRoot = Find-CcliProjectRoot -StartPath $cliDir

    Write-Host 'Press Ctrl+C to stop watching' -ForegroundColor Yellow
    Write-Host ''

    # CR-PSM-2.2.3: use string compare instead of GetHashCode() for deterministic change detection
    $lastResultsContent = $null
    $startTime = Get-Date
    $lastWriteTime = (Get-Item $CliFilePath).LastWriteTime

    try {
        # Initial check
        $initialContent = Get-Content $CliFilePath -Raw
        $resultsPattern = '(?s)## Results\s*\n(.*?)(?=\n---|\Z)'
        $resultsMatch = [regex]::Match($initialContent, $resultsPattern)
        if ($resultsMatch.Success) {
            $lastResultsContent = $resultsMatch.Groups[1].Value
        }

        # Poll for changes
        $elapsed = 0
        while ($elapsed -lt $Timeout) {
            Start-Sleep -Seconds 1
            $elapsed = ((Get-Date) - $startTime).TotalSeconds

            # Check if file was modified
            $currentWriteTime = (Get-Item $CliFilePath -ErrorAction SilentlyContinue).LastWriteTime
            if ($currentWriteTime -gt $lastWriteTime) {
                $lastWriteTime = $currentWriteTime
                Start-Sleep -Milliseconds 500  # Debounce

                try {
                    $newContent = Get-Content $CliFilePath -Raw -ErrorAction SilentlyContinue
                    if ($null -eq $newContent) {
                        continue
                    }

                    # Extract results section
                    $resultsPattern = '(?s)## Results\s*\n(.*?)(?=\n---|\Z)'
                    $resultsMatch = [regex]::Match($newContent, $resultsPattern)

                    if ($resultsMatch.Success) {
                        $currentResults = $resultsMatch.Groups[1].Value

                        # Check if results have changed (CR-PSM-2.2.3: direct string compare)
                        if ($lastResultsContent -ne $currentResults) {
                            $lastResultsContent = $currentResults

                            # Filter by prompt name if specified
                            $shouldDisplay = [string]::IsNullOrWhiteSpace($PromptName) -or ($currentResults -match [regex]::Escape($PromptName))

                            if ($shouldDisplay) {
                                Write-Host ''
                                Write-Host ('=' * 80) -ForegroundColor Cyan
                                Write-Host 'NEW RESULTS DETECTED' -ForegroundColor Green
                                Write-Host ('=' * 80) -ForegroundColor Cyan
                                Write-Host ''

                                # Display results (split on LF)
                                $lines = $currentResults -split [char]10
                                $inRelevantSection = $false
                                $sectionStart = 0

                                foreach ($i in 0..($lines.Count - 1)) {
                                    $line = $lines[$i]

                                    # Check if this is a result for our prompt
                                    if ($line -match '### Command:|### Prompt:') {
                                        $inRelevantSection = $true
                                        if (-not [string]::IsNullOrWhiteSpace($PromptName)) {
                                            $inRelevantSection = ($line -match [regex]::Escape($PromptName))
                                        }
                                        if ($inRelevantSection) {
                                            $sectionStart = $i
                                        }
                                    }

                                    if ($inRelevantSection -and $i -ge $sectionStart) {
                                        # Display with appropriate formatting
                                        if ($line -match '^### ') {
                                            Write-Host $line -ForegroundColor Yellow
                                        }
                                        elseif ($line -match '^```') {
                                            Write-Host $line -ForegroundColor Gray
                                        }
                                        elseif ($line -match '^\*\*') {
                                            Write-Host $line -ForegroundColor Cyan
                                        }
                                        else {
                                            Write-Host $line
                                        }
                                    }
                                }

                                Write-Host ''
                                Write-Host ('=' * 80) -ForegroundColor Cyan
                                Write-Host ''
                            }
                        }
                    }
                }
                catch {
                    # Silently ignore errors during file read
                }
            }
        }

        Write-Host ''
        Write-Host 'Watch timeout reached' -ForegroundColor Yellow
        Write-Host ('Timeout: ' + $Timeout + ' seconds') -ForegroundColor Yellow
    }
    catch {
        Write-Error ('Error watching CLI.md: ' + $_.Exception.Message)
    }
}

# Export module members. CR-PSM-2.4.3: derive from CcliCommandManifest.
foreach ($c in $script:CcliCommandManifest) { New-Alias -Name $c.Alias -Value $c.Name -Scope Script -ErrorAction SilentlyContinue }
Export-ModuleMember -Function ($script:CcliCommandManifest | ForEach-Object { $_.Name })
Export-ModuleMember -Alias ($script:CcliCommandManifest | ForEach-Object { $_.Alias })

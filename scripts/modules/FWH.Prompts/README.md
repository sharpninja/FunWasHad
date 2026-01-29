# FWH.Prompts PowerShell Module

A PowerShell module providing templatized prompts for AI interactions with parameterized commands.

## Installation

```powershell
# Import the module
Import-Module .\scripts\modules\FWH.Prompts\FWH.Prompts.psd1

# Or add to your PowerShell profile for permanent access
Add-Content $PROFILE "Import-Module '$PSScriptRoot\modules\FWH.Prompts\FWH.Prompts.psd1'"
```

## Quick Start

All commands are prefixed with `Ccli` and have aliases for backward compatibility:

```powershell
# List all available prompts (using Ccli prefix)
Get-CcliAvailablePrompts
# Or use the alias:
Get-AvailablePrompts

# Get a filled prompt (using Ccli prefix)
Get-CcliPrompt -Name 'code-review' -Parameters @{
    FeatureName = 'User Authentication'
    FilePath = 'src/AuthService.cs'
    Code = Get-Content 'src/AuthService.cs' -Raw
}
# Or use the alias:
Get-Prompt -Name 'code-review' -Parameters @{...}

# Get prompt and copy to clipboard (using Ccli prefix)
Invoke-CcliPrompt -Name 'code-review' -Parameters @{...} -OutputToClipboard
# Or use the alias:
Invoke-Prompt -Name 'code-review' -Parameters @{...} -OutputToClipboard
```

## Available Commands

All commands use the `Ccli` prefix. Aliases are available for backward compatibility.

### Get-CcliAvailablePrompts (Alias: Get-AvailablePrompts)

Lists all available prompt templates with their descriptions and required parameters.

```powershell
Get-CcliAvailablePrompts
Get-CcliAvailablePrompts | Format-Table Name, Description, ParameterCount
# Or use alias:
Get-AvailablePrompts
```

### Get-CcliPrompt (Alias: Get-Prompt)

Gets a filled prompt from a template by providing parameter values.

```powershell
Get-CcliPrompt -Name 'code-review' -Parameters @{
    FeatureName = 'User Authentication'
    FilePath = 'src/AuthService.cs'
    Code = Get-Content 'src/AuthService.cs' -Raw
}
# Or use alias:
Get-Prompt -Name 'code-review' -Parameters @{...}
```

### Invoke-CcliPrompt (Alias: Invoke-Prompt)

Gets a prompt and optionally outputs it, copies to clipboard, or saves to file.

```powershell
# Output to console and copy to clipboard
Invoke-CcliPrompt -Name 'code-review' -Parameters @{...} -OutputToClipboard
# Or use alias:
Invoke-Prompt -Name 'code-review' -Parameters @{...} -OutputToClipboard

# Save to file
Invoke-CcliPrompt -Name 'code-review' -Parameters @{...} -OutputToFile -OutputPath 'prompt.txt'
```

### Get-CcliPromptTemplate (Alias: Get-PromptTemplate)

Gets details about a specific prompt template.

```powershell
Get-CcliPromptTemplate -Name 'code-review'
# Or use alias:
Get-PromptTemplate -Name 'code-review'
```

### New-CcliPromptTemplate (Alias: New-PromptTemplate)

Creates a custom prompt template.

```powershell
New-CcliPromptTemplate -Name 'custom-review' `
    -Description 'Custom code review prompt' `
    -Template 'Review {Code} for {Issues}. Focus on {FocusAreas}.' `
    -Parameters @('Code', 'Issues', 'FocusAreas')
# Or use alias:
New-PromptTemplate -Name 'custom-review' -Description '...' -Template '...' -Parameters @(...)
```

### Remove-CcliPromptTemplate (Alias: Remove-PromptTemplate)

Removes a prompt template.

```powershell
Remove-CcliPromptTemplate -Name 'custom-review'
Remove-CcliPromptTemplate -Name 'custom-review' -Force  # Skip confirmation
# Or use alias:
Remove-PromptTemplate -Name 'custom-review'
```

## Built-in Prompt Templates

### code-review

Request a code review for a specific file or feature.

**Parameters:**
- `FeatureName` - Name of the feature
- `FilePath` - Path to the file
- `Code` - The code to review

**Example:**
```powershell
Get-Prompt -Name 'code-review' -Parameters @{
    FeatureName = 'User Authentication'
    FilePath = 'src/FWH.Mobile/Services/AuthService.cs'
    Code = Get-Content 'src/FWH.Mobile/Services/AuthService.cs' -Raw
}
```

### implement-feature

Request implementation of a feature.

**Parameters:**
- `FeatureName` - Name of the feature
- `Requirements` - Feature requirements
- `Framework` - Target framework
- `Language` - Programming language
- `Patterns` - Design patterns to use

### debug-issue

Request help debugging an issue.

**Parameters:**
- `Problem` - Description of the problem
- `ErrorMessage` - Error message (if any)
- `CodeLocation` - Where the issue occurs
- `StepsToReproduce` - Steps to reproduce
- `ExpectedBehavior` - What should happen
- `ActualBehavior` - What actually happens
- `Framework` - Framework version
- `Version` - Application version
- `OperatingSystem` - OS information

### refactor-code

Request code refactoring.

**Parameters:**
- `FilePath` - Path to the file
- `Code` - Code to refactor
- `RefactoringGoals` - Goals for refactoring
- `MaintainCompatibility` - Whether to maintain backward compatibility
- `PerformanceRequirements` - Performance requirements
- `Patterns` - Patterns to follow

### write-tests

Request unit test generation.

**Parameters:**
- `ClassName` - Name of the class/function
- `FilePath` - Path to the file
- `Code` - Code to test
- `TestFramework` - Test framework to use
- `CoverageLevel` - Desired coverage level
- `FocusAreas` - Areas to focus on

### document-code

Request code documentation.

**Parameters:**
- `FilePath` - Path to the file
- `Code` - Code to document
- `DocumentationStyle` - Documentation style to follow

### optimize-performance

Request performance optimization.

**Parameters:**
- `FilePath` - Path to the file
- `Code` - Code to optimize
- `PerformanceIssues` - Known performance issues
- `TargetResponseTime` - Target response time
- `TargetThroughput` - Target throughput
- `TargetMemoryUsage` - Target memory usage
- `Constraints` - Optimization constraints

### add-feature

Request adding a new feature.

**Parameters:**
- `ProjectName` - Name of the project
- `FeatureName` - Name of the feature
- `Description` - Feature description
- `Requirements` - Feature requirements
- `Framework` - Target framework
- `Database` - Database information
- `ApiEndpoints` - API endpoints needed
- `AcceptanceCriteria` - Acceptance criteria

### fix-bug

Request bug fix.

**Parameters:**
- `BugId` - Bug identifier
- `Title` - Bug title
- `Severity` - Bug severity
- `Location` - Where the bug occurs
- `Description` - Bug description
- `StepsToReproduce` - Steps to reproduce
- `ExpectedBehavior` - Expected behavior
- `ActualBehavior` - Actual behavior
- `Environment` - Environment information

### security-audit

Request security audit.

**Parameters:**
- `FilePath` - Path to the file
- `Code` - Code to audit
- `SecurityFocusAreas` - Security areas to focus on
- `ComplianceRequirements` - Compliance requirements

### document-cli-command

Request documentation for a CLI command.

**Parameters:**
- `CommandName` - Name of the CLI command
- `Description` - Brief description of what the command does
- `Usage` - Command usage syntax
- `Arguments` - Command arguments and their descriptions
- `Options` - Command options/flags and their descriptions
- `Examples` - Example usage scenarios
- `ExitCodes` - Exit codes and their meanings
- `AdditionalContext` - Any additional context or notes

### document-powershell-function

Request documentation for a PowerShell function or cmdlet.

**Parameters:**
- `FunctionName` - Name of the PowerShell function
- `ModuleName` - Module the function belongs to
- `Synopsis` - Brief one-line description
- `Description` - Detailed description of the function
- `Parameters` - List of parameters with types and descriptions
- `Examples` - Example usage scenarios
- `Output` - Description of return value/output
- `Notes` - Additional notes or warnings

### document-api-endpoint

Request documentation for a REST API endpoint.

**Parameters:**
- `Method` - HTTP method (GET, POST, PUT, DELETE, etc.)
- `Path` - Endpoint path
- `Description` - Description of what the endpoint does
- `BaseUrl` - Base URL of the API
- `RequestHeaders` - Required/optional request headers
- `PathParameters` - Path parameter descriptions
- `QueryParameters` - Query parameter descriptions
- `RequestBody` - Request body structure and schema
- `StatusCodes` - HTTP status codes and their meanings
- `ResponseBody` - Response body structure and schema
- `ResponseHeaders` - Response headers
- `Authentication` - Authentication method required
- `RateLimiting` - Rate limiting information
- `Examples` - Example requests and responses

### document-module-command

Request documentation for a module command or function.

**Parameters:**
- `CommandName` - Name of the command
- `ModuleName` - Module the command belongs to
- `Category` - Command category (e.g., "CLI", "PowerShell", "API")
- `Description` - Description of what the command does
- `Syntax` - Command syntax with placeholders
- `Parameters` - Parameter descriptions with types
- `ReturnValue` - Description of return value
- `Examples` - Example usage scenarios
- `RelatedCommands` - Related or similar commands
- `Notes` - Additional notes, warnings, or tips

### document-command-help

Generate help text for a command.

**Parameters:**
- `CommandName` - Name of the command
- `Purpose` - What the command is used for
- `Category` - Command category
- `Usage` - Usage syntax
- `Options` - Available options/flags
- `Arguments` - Command arguments
- `Examples` - Example usage
- `SeeAlso` - Related commands or resources

## Writing Prompts to CLI.md

The module can write prompts directly to `CLI.md` and monitor for results:

### Write-CcliPromptToCli (Alias: Write-PromptToCli)

Writes a prompt to the CLI.md file.

```powershell
# Write prompt to CLI.md
Write-CcliPromptToCli -Name 'code-review' -Parameters @{
    FeatureName = 'User Authentication'
    FilePath = 'src/AuthService.cs'
    Code = Get-Content 'src/AuthService.cs' -Raw
}
# Or use alias:
Write-PromptToCli -Name 'code-review' -Parameters @{...}

# Write prompt and watch for results
Write-CcliPromptToCli -Name 'code-review' -Parameters @{...} -Watch
```

The prompt will be written to the `## Prompts` section in CLI.md. The CLI agent can then process it, or you can manually copy it to an AI service.

### Watch-CcliResults (Alias: Watch-CliResults)

Monitors CLI.md for results and displays them in the terminal.

```powershell
# Watch CLI.md for results (any prompt)
Watch-CcliResults -CliFilePath ".\CLI.md"
# Or use alias:
Watch-CliResults -CliFilePath ".\CLI.md"

# Watch for specific prompt results
Watch-CcliResults -CliFilePath ".\CLI.md" -PromptName "code-review"
```

The watcher will display new results in the terminal as they appear in CLI.md.

## Examples

### Code Review Example

```powershell
Import-Module .\scripts\modules\FWH.Prompts\FWH.Prompts.psd1

$code = Get-Content 'src/FWH.Mobile/Services/AuthService.cs' -Raw

# Write to CLI.md and watch for results
Write-CcliPromptToCli -Name 'code-review' -Parameters @{
    FeatureName = 'User Authentication Service'
    FilePath = 'src/FWH.Mobile/Services/AuthService.cs'
    Code = $code
} -Watch
# Or use alias:
Write-PromptToCli -Name 'code-review' -Parameters @{...} -Watch
```

### Debug Issue Example

```powershell
Invoke-Prompt -Name 'debug-issue' -Parameters @{
    Problem = 'NullReferenceException when loading user profile'
    ErrorMessage = 'Object reference not set to an instance of an object'
    CodeLocation = 'UserService.cs:42'
    StepsToReproduce = @'
1. Login with valid credentials
2. Navigate to profile page
3. Exception is thrown
'@
    ExpectedBehavior = 'Profile page loads successfully'
    ActualBehavior = 'NullReferenceException is thrown'
    Framework = '.NET 9'
    Version = '1.0.0'
    OperatingSystem = 'Windows 11'
} -OutputToClipboard
```

### Custom Template Example

```powershell
# Create a custom template
New-CcliPromptTemplate -Name 'api-design' `
# Or use alias:
New-PromptTemplate -Name 'api-design' `
    -Description 'Request API design review' `
    -Template @'
Design an API endpoint for {FeatureName}:

Requirements:
{Requirements}

Constraints:
- Framework: {Framework}
- Authentication: {Authentication}
- Response Format: {ResponseFormat}

Provide:
1. Endpoint design
2. Request/Response models
3. Error handling
4. Documentation
'@ `
    -Parameters @('FeatureName', 'Requirements', 'Framework', 'Authentication', 'ResponseFormat')

# Use the custom template
Get-CcliPrompt -Name 'api-design' -Parameters @{
# Or use alias:
Get-Prompt -Name 'api-design' -Parameters @{
    FeatureName = 'User Profile'
    Requirements = 'Get user profile by ID'
    Framework = 'ASP.NET Core'
    Authentication = 'API Key'
    ResponseFormat = 'JSON'
}
```

## Template Syntax

Templates use placeholders in the format `{ParameterName}`. When you call `Get-Prompt` or `Invoke-Prompt`, provide a hashtable with values for each parameter.

**Example Template:**
```
Review {Code} for {Issues}. Focus on {FocusAreas}.
```

**Usage:**
```powershell
Get-Prompt -Name 'my-template' -Parameters @{
    Code = 'public class Test { }'
    Issues = 'performance and security'
    FocusAreas = 'memory leaks, SQL injection'
}
```

## Module Structure

```
scripts/modules/FWH.Prompts/
├── FWH.Prompts.psd1          # Module manifest
├── FWH.Prompts.psm1          # Module implementation
├── prompts.md                 # Template storage file
├── FWH.Prompts.Tests.ps1     # Unit tests
├── PROMPT-TEMPLATING.md      # Template system documentation
└── README.md                 # This file
```

## Template System

Templates are stored in `prompts.md` and loaded automatically when the module is imported. See [PROMPT-TEMPLATING.md](PROMPT-TEMPLATING.md) for detailed information on:
- Creating new templates
- Template structure and format
- Parameter system
- Best practices
- Troubleshooting

## Notes

- Templates are stored in memory and persist for the PowerShell session
- Custom templates added with `New-PromptTemplate` are session-specific
- Use `Get-AvailablePrompts` to see all templates including custom ones
- Missing parameters will result in empty strings with a warning
- Unused placeholders in templates will generate warnings

---

**Version:** 1.0.5  
**Last Updated:** 2025-01-27

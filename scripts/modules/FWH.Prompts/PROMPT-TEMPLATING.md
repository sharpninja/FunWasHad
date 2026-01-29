# Prompt Templating System

This document explains how the FWH.Prompts module's templating system works, including how to create, modify, and use prompt templates.

## Overview

The FWH.Prompts module uses a markdown-based template system stored in `prompts.md`. Each template consists of:
- A **name** (used to reference the template)
- A **description** (what the template is used for)
- A **template body** (the prompt text with placeholders)
- A **parameters table** (defines required parameters)

Templates are separated by `---` markers in the markdown file.

## Template File Structure

The `prompts.md` file follows this structure:

```markdown
# Prompt Templates

This file contains all prompt templates...

## template-name

Description of what this template does.

Template body with {ParameterName} placeholders.

{MoreTemplateContent}

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| ParameterName | What this parameter is for | Yes |
| AnotherParam | Another parameter | No |

---

## next-template-name

...
```

## Template Components

### 1. Template Name

The template name is defined by a level 2 markdown header (`##`):

```markdown
## code-review
```

- Must be unique
- Used to reference the template in PowerShell commands
- Should be lowercase with hyphens (kebab-case)
- Examples: `code-review`, `debug-issue`, `implement-feature`

### 2. Description

The description is the first paragraph after the template name:

```markdown
## code-review

Request a code review for a specific file or feature.
```

- Provides context about what the template is used for
- Should be a single, clear sentence
- Appears in `Get-CcliAvailablePrompts` output

### 3. Template Body

The template body contains the actual prompt text with placeholders:

```markdown
Please review the following code for the {FeatureName} feature in {FilePath}:

{Code}

Focus on:
- Code quality and best practices
- Potential bugs or issues
```

**Placeholders:**
- Use `{ParameterName}` syntax
- Parameter names are case-sensitive
- Placeholders are replaced with actual values when generating prompts
- Can appear multiple times in the template

**Example with placeholders:**
```markdown
Implement the following feature: {FeatureName}

Requirements:
{Requirements}

Technical Constraints:
- Framework: {Framework}
- Language: {Language}
```

### 4. Parameters Table

Each template must include a parameters table that defines all placeholders:

```markdown
### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| FeatureName | Name of the feature | Yes | (feature name) |
| Requirements | Feature requirements | Yes | |
| Framework | Target framework | Yes | .NET |
| Language | Programming language | Yes | C# |
```

**Table Structure:**
- **Parameter**: The exact name matching the placeholder (without braces)
- **Description**: What the parameter is used for
- **Required**: Whether the parameter must be provided (Yes/No)
- **Default**: Optional. Value used when the parameter is not provided. Use `-` or leave empty for no default (a warning is shown when missing). Provides hints (e.g. `(path)`) or common values (e.g. `.NET`, `xUnit`).

**Important:**
- Parameter names in the table must exactly match placeholders in the template body
- All placeholders in the template should be listed in the parameters table
- The "Required" column is informational (currently all parameters are treated as required)
- When a parameter is not provided: if it has a non-empty Default, that value is used; otherwise the placeholder is replaced with an empty string and a warning is emitted

## Creating a New Template

### Step 1: Add Template to prompts.md

Add your template to the end of `prompts.md` (before the final `---` if it exists):

```markdown
---

## my-custom-template

Description of what this template does.

Your prompt template text here with {Placeholder1} and {Placeholder2}.

You can include:
- Bullet points
- **Bold text**
- Code blocks
- Multiple paragraphs

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| Placeholder1 | Description of first parameter | Yes | (value) |
| Placeholder2 | Description of second parameter | Yes | |
```

### Step 2: Reload the Module

After adding a template, reload the module:

```powershell
Remove-Module FWH.Prompts -Force
Import-Module .\scripts\modules\FWH.Prompts\FWH.Prompts.psd1
```

Or use:

```powershell
Import-Module .\scripts\modules\FWH.Prompts\FWH.Prompts.psd1 -Force
```

### Step 3: Verify Template

Check that your template loaded correctly:

```powershell
Get-CcliAvailablePrompts | Where-Object { $_.Name -eq 'my-custom-template' }
```

## Using Templates

### Get Available Templates

List all available templates:

```powershell
Get-CcliAvailablePrompts
# Or use alias:
Get-AvailablePrompts
```

### Get Template Details

View a specific template:

```powershell
Get-CcliPromptTemplate -Name 'code-review'
# Or use alias:
Get-PromptTemplate -Name 'code-review'
```

### Generate a Prompt

Fill a template with parameters:

```powershell
$params = @{
    FeatureName = 'User Authentication'
    FilePath = 'src/AuthService.cs'
    Code = Get-Content 'src/AuthService.cs' -Raw
}

$prompt = Get-CcliPrompt -Name 'code-review' -Parameters $params
# Or use alias:
$prompt = Get-Prompt -Name 'code-review' -Parameters $params
```

### Output Options

**Console output:**
```powershell
Invoke-CcliPrompt -Name 'code-review' -Parameters $params
```

**Copy to clipboard:**
```powershell
Invoke-CcliPrompt -Name 'code-review' -Parameters $params -OutputToClipboard
```

**Save to file:**
```powershell
Invoke-CcliPrompt -Name 'code-review' -Parameters $params -OutputToFile -OutputPath 'prompt.txt'
```

**Write to CLI.md:**
```powershell
Write-CcliPromptToCli -Name 'code-review' -Parameters $params
```

## Template Examples

### Example 1: Simple Template

```markdown
## greet-user

Greet a user with a personalized message.

Hello {UserName}, welcome to {ApplicationName}!

We hope you enjoy using our application.

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| UserName | Name of the user | Yes |
| ApplicationName | Name of the application | Yes |
```

**Usage:**
```powershell
Get-CcliPrompt -Name 'greet-user' -Parameters @{
    UserName = 'John'
    ApplicationName = 'FunWasHad'
}
```

**Output:**
```
Hello John, welcome to FunWasHad!

We hope you enjoy using our application.
```

### Example 2: Complex Template with Multiple Placeholders

```markdown
## api-endpoint-design

Design a REST API endpoint.

Design a {Method} endpoint for {ResourceName}:

**Base URL:** {BaseUrl}
**Path:** {Path}
**Authentication:** {AuthMethod}

**Request Body:**
{RequestBody}

**Response Format:** {ResponseFormat}

**Error Handling:**
{ErrorHandling}

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| Method | HTTP method (GET, POST, etc.) | Yes |
| ResourceName | Name of the resource | Yes |
| BaseUrl | Base URL of the API | Yes |
| Path | Endpoint path | Yes |
| AuthMethod | Authentication method | Yes |
| RequestBody | Request body structure | Yes |
| ResponseFormat | Response format (JSON, XML, etc.) | Yes |
| ErrorHandling | Error handling approach | Yes |
```

**Usage:**
```powershell
Get-CcliPrompt -Name 'api-endpoint-design' -Parameters @{
    Method = 'POST'
    ResourceName = 'User'
    BaseUrl = 'https://api.example.com'
    Path = '/api/v1/users'
    AuthMethod = 'Bearer Token'
    RequestBody = '{ "name": "string", "email": "string" }'
    ResponseFormat = 'JSON'
    ErrorHandling = 'Return appropriate HTTP status codes'
}
```

### Example 3: Template with Code Blocks

```markdown
## review-code-snippet

Review the following code snippet.

Please review this code:

```{Language}
{Code}
```

Focus on:
- {FocusArea1}
- {FocusArea2}
- {FocusArea3}

### Parameters

| Parameter | Description | Required |
|-----------|-------------|----------|
| Language | Programming language | Yes |
| Code | Code to review | Yes |
| FocusArea1 | First focus area | Yes |
| FocusArea2 | Second focus area | Yes |
| FocusArea3 | Third focus area | Yes |
```

**Note:** When using code blocks in templates, use triple backticks with the language name as a placeholder if needed, or include them directly in the template body.

## Best Practices

### 1. Template Naming

- Use kebab-case: `code-review`, not `codeReview` or `code_review`
- Be descriptive: `debug-null-reference-exception` is better than `debug1`
- Keep names concise but clear

### 2. Descriptions

- Write clear, one-sentence descriptions
- Explain what the template is used for
- Avoid technical jargon when possible

### 3. Template Body

- Write clear, actionable prompts
- Use proper formatting (markdown is supported)
- Include examples when helpful
- Structure prompts logically (problem → context → request)

### 4. Parameters

- Use descriptive parameter names
- Match parameter names exactly in placeholders and table
- Document what each parameter is for
- Keep parameter count reasonable (5-10 is ideal)

### 5. Placeholder Usage

- Use `{ParameterName}` format (no spaces, case-sensitive)
- Use descriptive names: `{UserEmail}` not `{Email}` or `{e}`
- Avoid special characters in parameter names
- Use PascalCase for parameter names

### 6. Template Organization

- Group related templates together in `prompts.md`
- Use consistent formatting across templates
- Add comments in markdown if needed (using `<!-- comment -->`)

## Parameter Replacement

When you call `Get-CcliPrompt`, the module:

1. Loads the template from `prompts.md`
2. Extracts all placeholders (text in `{...}`)
3. Replaces each placeholder with the corresponding value from your parameters hashtable
4. Warns about missing parameters
5. Warns about unused placeholders

**Example:**
```powershell
# Template has: "Hello {Name}, you have {Count} messages."
# Parameters: @{ Name = 'Alice'; Count = '5' }
# Result: "Hello Alice, you have 5 messages."
```

**Missing Parameters:**
If a parameter is missing, the placeholder is replaced with an empty string and a warning is shown:

```powershell
# Template has: "Hello {Name}"
# Parameters: @{ }  # Missing Name
# Result: "Hello " (with warning)
```

## Dynamic Templates (Runtime)

You can also create templates at runtime using `New-CcliPromptTemplate`:

```powershell
New-CcliPromptTemplate -Name 'runtime-template' `
    -Description 'A template created at runtime' `
    -Template 'This is a {TestParam} template' `
    -Parameters @('TestParam')
```

**Note:** Runtime templates are stored in memory and are lost when the module is reloaded. To persist templates, add them to `prompts.md`.

## Template Validation

The module performs basic validation:

1. **Template exists**: Checks if template name exists
2. **Parameter matching**: Warns if parameters don't match placeholders
3. **Missing parameters**: Warns if required parameters are missing
4. **Unused placeholders**: Warns if placeholders aren't in parameters table

## Troubleshooting

### Template Not Found

**Error:** "Prompt template 'xyz' not found"

**Solution:**
- Check template name spelling (case-sensitive)
- Verify template exists in `prompts.md`
- Reload module: `Import-Module ... -Force`

### Placeholder Not Replaced

**Issue:** Placeholder `{Param}` remains in output

**Solution:**
- Check parameter name matches exactly (case-sensitive)
- Verify parameter is in the parameters hashtable
- Check for typos in placeholder name

### Parameters Table Not Parsed

**Issue:** Template loads but has no parameters

**Solution:**
- Verify table format matches exactly:
  ```markdown
  | Parameter | Description | Required |
  ```
- Check table is under `### Parameters` header
- Ensure table rows use pipe separators correctly

### Template Body Not Extracted

**Issue:** Template body is empty

**Solution:**
- Verify template body is between the description and `### Parameters`
- Check for proper markdown formatting
- Ensure no extra `---` markers within the template

## Advanced Usage

### Multi-line Parameters

Parameters can contain multi-line text:

```powershell
$params = @{
    Code = @'
public class Test
{
    public void Method() { }
}
'@
    Description = 'A test class'
}
```

### Special Characters

To include literal `{` or `}` in your template (not as placeholders), escape them:

```markdown
This is a literal brace: \{
This is a placeholder: {ParameterName}
```

### Nested Templates

You can reference other templates in your prompts by including their names:

```markdown
## combined-review

Use the code-review template for {Code}, then apply security-audit template.

{CodeReviewPrompt}

{SecurityAuditPrompt}
```

(Note: This requires manual composition - the module doesn't automatically compose templates)

## File Location

Templates are stored in:
```
scripts/modules/FWH.Prompts/prompts.md
```

The module automatically loads templates from this file when imported.

## Command Documentation Templates

The module includes specialized templates for documenting commands:

### document-cli-command

Use this template to generate documentation for CLI commands (like those in the CLI agent).

**Example:**
```powershell
Get-CcliPrompt -Name 'document-cli-command' -Parameters @{
    CommandName = 'clean-cli'
    Description = 'Archive CLI.md to CLI-history.md and reset CLI.md'
    Usage = 'clean-cli'
    Arguments = 'None'
    Options = 'None'
    Examples = 'clean-cli'
    ExitCodes = '0 on success, non-zero on error'
    AdditionalContext = 'Preserves command history in CLI-history.md'
}
```

### document-powershell-function

Use this template to generate documentation for PowerShell functions and cmdlets.

**Example:**
```powershell
Get-CcliPrompt -Name 'document-powershell-function' -Parameters @{
    FunctionName = 'Get-CcliPrompt'
    ModuleName = 'FWH.Prompts'
    Synopsis = 'Gets a filled prompt from a template'
    Description = 'Retrieves a prompt template and fills it with provided parameters'
    Parameters = '-Name: Template name, -Parameters: Hashtable of parameter values'
    Examples = 'Get-CcliPrompt -Name code-review -Parameters @{...}'
    Output = 'String containing the filled prompt'
    Notes = 'Template must exist in prompts.md'
}
```

### document-api-endpoint

Use this template to generate documentation for REST API endpoints.

### document-module-command

Use this template to generate documentation for module commands (works for any command type).

### document-command-help

Use this template to generate help text for commands in standard format.

## See Also

- [README.md](README.md) - Module usage and examples
- [FWH.Prompts.Tests.ps1](FWH.Prompts.Tests.ps1) - Unit tests demonstrating template usage

---

**Last Updated:** 2025-01-27  
**Module Version:** 1.0.5

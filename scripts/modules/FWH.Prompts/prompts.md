# Prompt Templates

This file contains all prompt templates for the FWH.Prompts module. Each template is separated by `---` markers.

## shared-context

Context that is prepended to the beginning of every prompt when it is executed. Use this for project-specific guidelines, coding standards, or other information that should inform all AI interactions.

- Project: FunWasHad
- Follow .editorconfig and existing patterns in the codebase.
- Prefer explicit types and meaningful names. Write clear, maintainable code.

---

## code-review

Request a code review for a specific file or feature.

Archive previous code review documents first.

Please review the following code for {FeatureName} in {FilePath}:

Focus on:
- Code quality and best practices
- Potential bugs or issues
- Performance optimizations
- Security concerns
- Test coverage recommendations

Perform the same review for each of these models:
- ChatGPT (latest, most thorough)
- Claude Sonnet (latest, most thorough)
- Grok (latest, most thorough)

Aggregate the results into a single review and note which models each line item comes from.

Provide specific, actionable feedback.

Create an implementation plan.
- Identify tasks that can run in parallel.
- Prepare agents to run each parallel task.
- Create context for each task to aid in implementing the changes.


### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| FeatureName | Name of the feature | No | all features |
| FilePath | Path to the file | No | the entire solution |


---

## implement-feature

Request implementation of a feature.

Implement the following feature: {FeatureName}

Requirements:
{Requirements}

Technical Constraints:
- Framework: {Framework}
- Language: {Language}
- Patterns: {Patterns}

Please provide:
1. Implementation plan
2. Code implementation
3. Unit tests
4. Documentation updates

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| FeatureName | Name of the feature | Yes | (feature name) |
| Requirements | Feature requirements | Yes | |
| Framework | Target framework | Yes | .NET |
| Language | Programming language | Yes | C# |
| Patterns | Design patterns to use | Yes | (e.g. MVVM, Repository) |

---

## debug-issue

Request help debugging an issue.

I'm experiencing the following issue:

**Problem:** {Problem}
**Error Message:** {ErrorMessage}
**Code Location:** {CodeLocation}
**Steps to Reproduce:**
{StepsToReproduce}

**Expected Behavior:**
{ExpectedBehavior}

**Actual Behavior:**
{ActualBehavior}

**Environment:**
- Framework: {Framework}
- Version: {Version}
- OS: {OperatingSystem}

Please help me debug this issue.

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| Problem | Description of the problem | Yes | |
| ErrorMessage | Error message (if any) | Yes | |
| CodeLocation | Where the issue occurs | Yes | (file:line) |
| StepsToReproduce | Steps to reproduce | Yes | |
| ExpectedBehavior | What should happen | Yes | |
| ActualBehavior | What actually happens | Yes | |
| Framework | Framework version | Yes | .NET |
| Version | Application version | Yes | 1.0.0 |
| OperatingSystem | OS information | Yes | (OS and version) |

---

## refactor-code

Request code refactoring.

Please refactor the following code in {FilePath}:

{Code}

Refactoring Goals:
{RefactoringGoals}

Constraints:
- Maintain backward compatibility: {MaintainCompatibility}
- Performance requirements: {PerformanceRequirements}
- Follow patterns: {Patterns}

Provide the refactored code with explanations.

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| FilePath | Path to the file | Yes | (file path) |
| Code | Code to refactor | Yes | |
| RefactoringGoals | Goals for refactoring | Yes | (goals) |
| MaintainCompatibility | Whether to maintain backward compatibility | Yes | Yes |
| PerformanceRequirements | Performance requirements | Yes | (none specified) |
| Patterns | Patterns to follow | Yes | (existing patterns) |

---

## write-tests

Request unit test generation.

Please write comprehensive unit tests for the following code:

**Class/Function:** {ClassName}
**Location:** {FilePath}
**Code:**
{Code}

**Test Requirements:**
- Framework: {TestFramework}
- Coverage: {CoverageLevel}
- Focus Areas: {FocusAreas}

Include:
1. Happy path tests
2. Edge cases
3. Error handling tests
4. Integration tests (if applicable)

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| ClassName | Name of the class/function | Yes | (class or method name) |
| FilePath | Path to the file | Yes | (file path) |
| Code | Code to test | Yes | |
| TestFramework | Test framework to use | Yes | xUnit |
| CoverageLevel | Desired coverage level | Yes | high |
| FocusAreas | Areas to focus on | Yes | (e.g. edge cases, errors) |

---

## document-code

Request code documentation.

Please document the following code:

**Location:** {FilePath}
**Code:**
{Code}

**Documentation Requirements:**
- XML documentation comments
- Usage examples
- Parameter descriptions
- Return value descriptions
- Exception documentation
- See also references

Follow {DocumentationStyle} style.

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| FilePath | Path to the file | Yes | (file path) |
| Code | Code to document | Yes | |
| DocumentationStyle | Documentation style to follow | Yes | Microsoft |

---

## optimize-performance

Request performance optimization.

Please optimize the following code for performance:

**Location:** {FilePath}
**Code:**
{Code}

**Performance Issues:**
{PerformanceIssues}

**Target Metrics:**
- Response Time: {TargetResponseTime}
- Throughput: {TargetThroughput}
- Memory Usage: {TargetMemoryUsage}

**Constraints:**
{Constraints}

Provide optimized code with performance analysis.

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| FilePath | Path to the file | Yes | (file path) |
| Code | Code to optimize | Yes | |
| PerformanceIssues | Known performance issues | Yes | (describe) |
| TargetResponseTime | Target response time | Yes | (target) |
| TargetThroughput | Target throughput | Yes | (target) |
| TargetMemoryUsage | Target memory usage | Yes | (target) |
| Constraints | Optimization constraints | Yes | (any) |

---

## add-feature

Request adding a new feature.

Add the following feature to the {ProjectName} project:

**Feature Name:** {FeatureName}
**Description:** {Description}
**Requirements:**
{Requirements}

**Technical Details:**
- Project: {ProjectName}
- Framework: {Framework}
- Database: {Database}
- API Endpoints: {ApiEndpoints}

**Acceptance Criteria:**
{AcceptanceCriteria}

Please provide implementation plan and code.

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| ProjectName | Name of the project | Yes | FunWasHad |
| FeatureName | Name of the feature | Yes | (feature name) |
| Description | Feature description | Yes | |
| Requirements | Feature requirements | Yes | |
| Framework | Target framework | Yes | .NET |
| Database | Database information | Yes | (e.g. SQLite) |
| ApiEndpoints | API endpoints needed | Yes | (if applicable) |
| AcceptanceCriteria | Acceptance criteria | Yes | |

---

## fix-bug

Request bug fix.

Please fix the following bug:

**Bug ID:** {BugId}
**Title:** {Title}
**Severity:** {Severity}
**Location:** {Location}

**Description:**
{Description}

**Steps to Reproduce:**
{StepsToReproduce}

**Expected Behavior:**
{ExpectedBehavior}

**Actual Behavior:**
{ActualBehavior}

**Environment:**
{Environment}

Provide the fix with explanation and tests.

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| BugId | Bug identifier | Yes | |
| Title | Bug title | Yes | (title) |
| Severity | Bug severity | Yes | Medium |
| Location | Where the bug occurs | Yes | (path or component) |
| Description | Bug description | Yes | |
| StepsToReproduce | Steps to reproduce | Yes | |
| ExpectedBehavior | Expected behavior | Yes | |
| ActualBehavior | Actual behavior | Yes | |
| Environment | Environment information | Yes | (OS, runtime, etc.) |

---

## security-audit

Request security audit.

Please perform a security audit on the following code:

**Location:** {FilePath}
**Code:**
{Code}

**Security Focus Areas:**
{SecurityFocusAreas}

**Compliance Requirements:**
{ComplianceRequirements}

Check for:
- Injection vulnerabilities
- Authentication/Authorization issues
- Data exposure risks
- Cryptographic weaknesses
- Input validation issues

Provide security assessment and recommendations.

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| FilePath | Path to the file | Yes | (file path) |
| Code | Code to audit | Yes | |
| SecurityFocusAreas | Security areas to focus on | Yes | (e.g. injection, auth) |
| ComplianceRequirements | Compliance requirements | Yes | (if any) |

---

## document-cli-command

Request documentation for a CLI command.

Document the following CLI command:

**Command Name:** {CommandName}
**Description:** {Description}
**Usage:** {Usage}
**Arguments:** {Arguments}
**Options:** {Options}
**Examples:** {Examples}
**Exit Codes:** {ExitCodes}

**Additional Context:**
{AdditionalContext}

Please provide comprehensive documentation including:
- Command syntax
- Parameter descriptions
- Usage examples
- Error handling
- Best practices
- Related commands

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| CommandName | Name of the CLI command | Yes | (command) |
| Description | Brief description of what the command does | Yes | (brief description) |
| Usage | Command usage syntax | Yes | (syntax) |
| Arguments | Command arguments and their descriptions | Yes | (args) |
| Options | Command options/flags and their descriptions | Yes | (options) |
| Examples | Example usage scenarios | Yes | (examples) |
| ExitCodes | Exit codes and their meanings | Yes | (exit codes) |
| AdditionalContext | Any additional context or notes | Yes | |

---

## document-powershell-function

Request documentation for a PowerShell function or cmdlet.

Document the following PowerShell function:

**Function Name:** {FunctionName}
**Module:** {ModuleName}
**Synopsis:** {Synopsis}
**Description:** {Description}

**Parameters:**
{Parameters}

**Examples:**
{Examples}

**Output:** {Output}

**Notes:** {Notes}

Please provide comprehensive documentation including:
- Function signature
- Parameter descriptions with types
- Usage examples
- Return value documentation
- Error handling
- Related functions

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| FunctionName | Name of the PowerShell function | Yes | (function name) |
| ModuleName | Module the function belongs to | Yes | (module) |
| Synopsis | Brief one-line description | Yes | (one-line) |
| Description | Detailed description of the function | Yes | (describe) |
| Parameters | List of parameters with types and descriptions | Yes | (list) |
| Examples | Example usage scenarios | Yes | (examples) |
| Output | Description of return value/output | Yes | (return value) |
| Notes | Additional notes or warnings | Yes | |

---

## document-api-endpoint

Request documentation for a REST API endpoint.

Document the following API endpoint:

**Endpoint:** {Method} {Path}
**Description:** {Description}
**Base URL:** {BaseUrl}

**Request:**
- **Headers:** {RequestHeaders}
- **Path Parameters:** {PathParameters}
- **Query Parameters:** {QueryParameters}
- **Request Body:** {RequestBody}

**Response:**
- **Status Codes:** {StatusCodes}
- **Response Body:** {ResponseBody}
- **Response Headers:** {ResponseHeaders}

**Authentication:** {Authentication}
**Rate Limiting:** {RateLimiting}
**Examples:** {Examples}

Please provide comprehensive API documentation including:
- Complete endpoint specification
- Request/response schemas
- Error responses
- Authentication requirements
- Usage examples
- Best practices

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| Method | HTTP method (GET, POST, PUT, DELETE, etc.) | Yes | GET |
| Path | Endpoint path | Yes | (path) |
| Description | Description of what the endpoint does | Yes | (describe) |
| BaseUrl | Base URL of the API | Yes | (base URL) |
| RequestHeaders | Required/optional request headers | Yes | (headers) |
| PathParameters | Path parameter descriptions | Yes | (path params) |
| QueryParameters | Query parameter descriptions | Yes | (query params) |
| RequestBody | Request body structure and schema | Yes | (schema) |
| StatusCodes | HTTP status codes and their meanings | Yes | (status codes) |
| ResponseBody | Response body structure and schema | Yes | (schema) |
| ResponseHeaders | Response headers | Yes | (headers) |
| Authentication | Authentication method required | Yes | (e.g. Bearer) |
| RateLimiting | Rate limiting information | Yes | (if any) |
| Examples | Example requests and responses | Yes | (examples) |

---

## document-module-command

Request documentation for a module command or function.

Document the following module command:

**Command Name:** {CommandName}
**Module:** {ModuleName}
**Category:** {Category}
**Description:** {Description}

**Syntax:**
{Syntax}

**Parameters:**
{Parameters}

**Return Value:** {ReturnValue}

**Examples:**
{Examples}

**Related Commands:** {RelatedCommands}

**Notes:** {Notes}

Please provide comprehensive documentation including:
- Command overview
- Parameter details
- Usage examples
- Return value documentation
- Error handling
- Best practices
- Related commands

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| CommandName | Name of the command | Yes | (command) |
| ModuleName | Module the command belongs to | Yes | (module) |
| Category | Command category (e.g., "CLI", "PowerShell", "API") | Yes | (category) |
| Description | Description of what the command does | Yes | (describe) |
| Syntax | Command syntax with placeholders | Yes | (syntax) |
| Parameters | Parameter descriptions with types | Yes | (params) |
| ReturnValue | Description of return value | Yes | (return value) |
| Examples | Example usage scenarios | Yes | (examples) |
| RelatedCommands | Related or similar commands | Yes | (related) |
| Notes | Additional notes, warnings, or tips | Yes | |

---

## document-command-help

Generate help text for a command.

Generate comprehensive help text for the following command:

**Command:** {CommandName}
**Purpose:** {Purpose}
**Category:** {Category}

**Usage:**
{Usage}

**Options:**
{Options}

**Arguments:**
{Arguments}

**Examples:**
{Examples}

**See Also:** {SeeAlso}

Please generate help text in standard format including:
- Command description
- Usage syntax
- Option/flag descriptions
- Argument descriptions
- Example commands
- See also references
- Error messages (if applicable)

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| CommandName | Name of the command | Yes | (command) |
| Purpose | What the command is used for | Yes | (purpose) |
| Category | Command category | Yes | (category) |
| Usage | Usage syntax | Yes | (syntax) |
| Options | Available options/flags | Yes | (options) |
| Arguments | Command arguments | Yes | (arguments) |
| Examples | Example usage | Yes | (examples) |
| SeeAlso | Related commands or resources | Yes | (related) |

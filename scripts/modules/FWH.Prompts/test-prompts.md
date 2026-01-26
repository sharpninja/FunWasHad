# Test Prompt Templates

Minimal prompts file for Pester tests. Each template is separated by `---` markers.

## shared-context

Test shared context for unit tests.

- Project: FWH.Prompts.Tests

---

## code-review

Request a code review for a specific file or feature.

Please review the following code for the {FeatureName} feature in {FilePath}:

{Code}

Focus on: code quality, potential bugs, performance, security, test coverage. Provide specific, actionable feedback.

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| FeatureName | Name of the feature | Yes | (feature name) |
| FilePath | Path to the file | Yes | (file path) |
| Code | The code to review | Yes | |

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

**Problem:** {Problem}
**Error Message:** {ErrorMessage}
**Code Location:** {CodeLocation}
**Steps to Reproduce:** {StepsToReproduce}
**Expected Behavior:** {ExpectedBehavior}
**Actual Behavior:** {ActualBehavior}
**Environment:** Framework: {Framework}, Version: {Version}, OS: {OperatingSystem}

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

Please refactor the following code in {FilePath}: {Code}
Refactoring Goals: {RefactoringGoals}
Constraints: Maintain backward compatibility: {MaintainCompatibility}; Performance: {PerformanceRequirements}; Patterns: {Patterns}

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

**Class/Function:** {ClassName}
**Location:** {FilePath}
**Code:** {Code}
**Test Requirements:** Framework: {TestFramework}, Coverage: {CoverageLevel}, Focus: {FocusAreas}

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

**Location:** {FilePath}
**Code:** {Code}
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

**Location:** {FilePath}
**Code:** {Code}
**Performance Issues:** {PerformanceIssues}
**Target Metrics:** Response: {TargetResponseTime}, Throughput: {TargetThroughput}, Memory: {TargetMemoryUsage}
**Constraints:** {Constraints}

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

Add the following feature to the {ProjectName} project: **Feature Name:** {FeatureName}, **Description:** {Description}, **Requirements:** {Requirements}
**Technical Details:** Framework: {Framework}, Database: {Database}, API Endpoints: {ApiEndpoints}
**Acceptance Criteria:** {AcceptanceCriteria}

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

**Bug ID:** {BugId}
**Title:** {Title}
**Severity:** {Severity}
**Location:** {Location}
**Description:** {Description}
**Steps to Reproduce:** {StepsToReproduce}
**Expected Behavior:** {ExpectedBehavior}
**Actual Behavior:** {ActualBehavior}
**Environment:** {Environment}

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

**Location:** {FilePath}
**Code:** {Code}
**Security Focus Areas:** {SecurityFocusAreas}
**Compliance Requirements:** {ComplianceRequirements}

### Parameters

| Parameter | Description | Required | Default |
|-----------|-------------|----------|---------|
| FilePath | Path to the file | Yes | (file path) |
| Code | Code to audit | Yes | |
| SecurityFocusAreas | Security areas to focus on | Yes | (e.g. injection, auth) |
| ComplianceRequirements | Compliance requirements | Yes | (if any) |

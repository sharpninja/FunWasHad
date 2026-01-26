# TODO List

This document tracks pending tasks, improvements, and future work for the FunWasHad project.

## MVP-App

### High Priority

- [ ] **MVP-APP-001:** Create a SocialMediaService library for client side operation *(Estimate: 20-25 days, 160-200 hours)*
  - This service will be used to manage defaults for various social media platforms
  - There will be an accompanying SocialMediaApi project that will be used to disseminate templates and defaults to mobile apps
  - The apps won't need the API to share media with social media platforms (client-side only)
  - Design the service interface and implementation for managing social media platform defaults
  - Create the SocialMediaApi project for template and default distribution
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging
- [ ] **MVP-APP-002:** When starting the app, get the current location. If it is in a different tourism market, start a new "MarketArrival" workflow (define in a new puml file) *(Estimate: 12-16 days, 96-128 hours)*
  - On app startup, retrieve the current device location
  - Check if the current location is in a different tourism market than previously stored
  - If tourism market has changed, trigger the MarketArrival workflow
  - Create a new PlantUML (.puml) file to define the MarketArrival workflow structure
  - Integrate workflow execution into app startup sequence
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging

### Medium Priority

- [ ] **MVP-APP-003:** Allow day and night variants to themes *(Estimate: 8-12 days, 64-96 hours)*
  - Add support for light/dark theme variants in BusinessTheme and CityTheme entities
  - Update ThemeService to detect system day/night mode and apply appropriate variant
  - Extend theme API endpoints to return both variants
  - Update mobile app to automatically switch between day/night variants based on system settings
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging
- [ ] **MVP-APP-004:** Always use a dark toolbar and transparent background and no border on the toolbar buttons *(Estimate: 4-5 days, 32-40 hours)*
  - Update MainView toolbar styling to use dark background
  - Set toolbar buttons to have transparent backgrounds
  - Remove borders from toolbar buttons
  - Ensure consistent dark toolbar appearance across all views
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging
- [ ] **MVP-APP-005:** Add a trip planning tool that will be a Blazor website. Users can create trip itineraries and store them in the postgres database and then retrieve them in the app using a QR code displayed in the Blazor website *(Estimate: 40-50 days, 320-400 hours)*
  - Create a new Blazor web application project for trip planning
  - Design database schema for trip itineraries in PostgreSQL
  - Implement itinerary creation, editing, and storage functionality
  - Generate QR codes for each itinerary that links to the app
  - Add QR code scanning capability to the mobile app
  - Implement itinerary retrieval and display in the mobile app
  - Ensure secure access to itineraries via QR code authentication
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging

## MVP-Marketing

### Medium Priority

- [ ] **MVP-MARKETING-001:** Create a Marketing Blazor application for managing the marketing API *(Estimate: 40-50 days, 320-400 hours)*
  - Create a new Blazor web application project for marketing management
  - Implement CRUD operations for businesses, business themes, coupons, menu items, and news items
  - Add management interfaces for cities, city themes, tourism markets, and airports
  - Create forms for creating and editing marketing content
  - Add authentication and authorization for admin access
  - Integrate with the existing Marketing API for data operations
  - Provide dashboard views for marketing analytics and content management
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging
- [ ] **MVP-MARKETING-002:** Setup railway to host the marketing website and add to build pipelines with reusable actions *(Estimate: 8-12 days, 64-96 hours)*
  - Configure Railway deployment for the Marketing Blazor application
  - Set up Railway service and environment variables
  - Create reusable GitHub Actions workflows for building and deploying the marketing website
  - Add build pipeline steps for Blazor application compilation and deployment
  - Configure Railway service discovery and connection strings
  - Set up automated deployments on push to main/develop branches
  - Add health checks and monitoring for the deployed application
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging

## MVP-Support

### High Priority

- [ ] **MVP-SUPPORT-003:** Add code analyzers to all projects, including specialized analyzers for specific project types and libraries used *(Estimate: 4-6 days, 32-48 hours)*
  - Enable .NET analyzers (Microsoft.CodeAnalysis.NetAnalyzers or built-in SDK analyzers) across the solution
  - Add specialized analyzers per project type: ASP.NET Core for APIs, Avalonia for mobile/UI, Blazor if added
  - Add library-specific analyzers (e.g. Entity Framework, HttpClient) where applicable
  - Configure rule sets and severity (error/warning/none) in Directory.Build.props or per project
  - Treat analyzer violations as build errors or warnings per project policy
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging
- [x] **MVP-SUPPORT-007:** Remove FWH.CLI.Agent and its tests *(Estimate: 0.5-1 day, 4-8 hours)*
  - Remove the `src/FWH.CLI.Agent` project and folder
  - Remove the `tests/FWH.CLI.Agent.Tests` project and folder
  - Remove FWH.CLI.Agent and FWH.CLI.Agent.Tests from FunWasHad.sln
  - Remove or update .vscode/launch.json and .vscode/tasks.json entries that reference FWH.CLI.Agent
  - Update or remove documentation and config (e.g. cli-agent.schema.json, CODE-REVIEW-*, cli-agent-config.md) that references FWH.CLI.Agent
  - **Implementation Tasks:**
    - [x] Remove projects from solution and delete src/FWH.CLI.Agent, tests/FWH.CLI.Agent.Tests
    - [x] Clean .vscode launch and tasks
    - [x] Update docs and config that reference FWH.CLI.Agent
    - [x] Final Validation

### Medium Priority

- [ ] **MVP-SUPPORT-001:** Setup railway to host the social media api and add to build pipelines with reusable actions *(Estimate: 8-12 days, 64-96 hours)*
  - Configure Railway deployment for the SocialMediaApi project
  - Set up Railway service and environment variables
  - Create reusable GitHub Actions workflows for building and deploying the Social Media API
  - Add build pipeline steps for API compilation and deployment
  - Configure Railway service discovery and connection strings
  - Set up automated deployments on push to main/develop branches
  - Add health checks and monitoring for the deployed API
- [ ] **MVP-SUPPORT-002:** Create new folder in solution root's parent and move the powershell module and supporting projects and documentation to that folder. Add the new folder to github as `sharpninja/cursor-cli` and push to github *(Estimate: 1-2 days, 8-16 hours)*
  - Create a new folder (e.g. `cursor-cli`) as a sibling of the solution root (e.g. `e:\github\cursor-cli` when root is `e:\github\FunWasHad`)
  - Move FWH.Prompts PowerShell module (`scripts/modules/FWH.Prompts`) and CLI-related scripts (e.g. `Sync-Documentation.ps1`) to the new folder
  - Move FWH.Documentation.Sync and related documentation (e.g. DOCUMENTATION-SYNC-AGENT.md, PROMPT-TEMPLATING.md, prompts.md) to the new folder. *(FWH.CLI.Agent was removed in MVP-SUPPORT-007.)*
  - Remove moved projects from FunWasHad.sln; remove or update .vscode tasks/launch that reference them
  - Initialize the new folder as a git repository, add remote `sharpninja/cursor-cli`, and push to GitHub
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] Create folder structure and move PowerShell module and scripts
    - [ ] Move FWH.Documentation.Sync; create standalone solution or project set *(FWH.CLI.Agent removed in MVP-SUPPORT-007.)*
    - [ ] Update FunWasHad: remove from solution, trim .vscode configs
    - [ ] Git init, add sharpninja/cursor-cli remote, push to GitHub
    - [ ] Final Validation
- [ ] **MVP-SUPPORT-004:** Add a coverage report md file and update after each test run *(Estimate: 1-2 days, 8-16 hours)*
  - Add a coverage report markdown file (e.g. `Coverage-Report.md` or in `docs/`) to the repo
  - Integrate coverage collection into the test run (e.g. `dotnet test` with coverage or CI)
  - Update the coverage report file after each test run with current coverage metrics
- [ ] **MVP-SUPPORT-005:** Add UI to the fwh-cli-agent extension to display a list of prompts defined *(Estimate: 3-5 days, 24-40 hours)*
  - In the extension, discover and show a list of prompts (from prompts.md / FWH.Prompts or from the ## Prompts section in CLI.md)
  - When the user selects a prompt from the list, display a UI for entering values for that prompt’s parameters
  - Add a button to invoke the selected prompt with the user-provided parameters (run in Composer or agent-cli per existing ExecuteMode)
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] Parse prompts.md / CLI.md to build list of prompt names and their parameters
    - [ ] Implement extension UI (webview or tree + input) for prompt list and parameter form
    - [ ] Wire invoke button to existing runInComposer / runWithAgentCli with substituted prompt text
    - [ ] Final Validation
- [ ] **MVP-SUPPORT-006:** Convert solution to new XML solution format *(Estimate: 2-4 days, 16-32 hours)*
  - Migrate FunWasHad.sln from the legacy Visual Studio solution format to the new XML-based solution format
  - Ensure all projects, solution folders, dependencies, and solution items are preserved
  - Verify build, test, and IDE (Visual Studio / VS Code) behavior after conversion
  - Update CI/CD and any tooling that parses or generates the solution file
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] Convert .sln to new XML format; validate project references and configuration mappings
    - [ ] Update .vscode tasks, launch, and scripts that reference the solution
    - [ ] Final Validation

## MVP-Legal

### High Priority

- [ ] **MVP-LEGAL-001:** Create website for hosting legal notices such as EULA, Privacy Policy and Corporate Contact information *(Estimate: 28-35 days, 224-280 hours)*
  - Create a new web application project (Blazor or static site) for legal documentation
  - Design and implement pages for EULA (End User License Agreement)
  - Design and implement pages for Privacy Policy
  - Create corporate contact information page
  - Ensure responsive design for mobile and desktop access
  - Implement versioning system for legal documents to track changes over time
  - Add search functionality for legal documents
  - Configure hosting and deployment pipeline
  - Ensure accessibility compliance (WCAG standards)
  - Add multi-language support if required for international compliance
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging

---

## Code Review Remediation

*From [CODE-REVIEW-AGGREGATED.md](./CODE-REVIEW-AGGREGATED.md). Full implementation plan: [CODE-REVIEW-IMPLEMENTATION-PLAN.md](./CODE-REVIEW-IMPLEMENTATION-PLAN.md).*

### Phase 1: Security & Critical Bugs *(Estimate: 5–7 days)*

- [ ] **CR-P1:** Path validation, CreateInitialCliFile overwrite, command/shell injection, temp file, and critical bugs
  - Validate `CliMdPath`/`PromptsMdPath` and output paths under workspace/project root (EXT, PSM, PUM)
  - Make `CreateInitialCliFile` opt-in: `--init` or `CliAgent:ReinitOnStart`; by default create only if missing (CLI)
  - `ExecutePowerShell`/`ExecuteShellCommand`: use `ArgumentList` or temp file; document trusted use (CLI)
  - `runWithAgentCli`: `unlinkSync` in `finally`/`on('error')`, `0o600` on write, document no secrets (EXT)
  - `Watch-CcliResults`: replace `GetHashCode()` with deterministic hash or direct compare (PSM)
  - `removeCliBlock`: replace by index (end-to-start) to handle duplicate blocks (EXT)
  - `ExecuteRun`: fix double-await, stream cleanup on cancel, wrap `Watcher.Changed` in try/catch (CLI)
  - **Implementation Tasks:**
    - [ ] CR-EXT-1.1.1, CR-EXT-1.1.3, CR-EXT-1.2.5
    - [ ] CR-PSM-2.1.1, CR-PSM-2.1.3, CR-PSM-2.2.3
    - [ ] CR-CLI-3.1.1, CR-CLI-3.1.2, CR-CLI-3.1.4, CR-CLI-3.2.1, CR-CLI-3.2.2, CR-CLI-3.2.3, CR-CLI-3.2.4
    - [ ] CR-PUM-4.1.1

### Phase 2: Bugs & Error Handling *(Estimate: 3–4 days)*

- [ ] **CR-P2:** Debug logging, showOutput reuse, WorkspaceEdit, FWH.Prompts error messages, LiteralPath, OutputToFile, PlantUmlRender behavior
  - Extension: `runPrompt` debug on cancel/read error; `showOutput` reuse existing channel; `onCliMdChange` safe replace or doc (CR-EXT-1.2.1–1.2.4)
  - FWH.Prompts: `Read-CcliPromptsFile` comments; `Get-CcliPrompt` error msg for `-PromptsFile`; `-LiteralPath`; `OutputToFile` sanitize `$Name` (CR-PSM-2.2.1, 2.2.2, 2.2.4, 2.2.5)
  - FWH.CLI.Agent: `ProcessCliFile` regex tests; `_projectRoot` null handling (CR-CLI-3.2.5, 3.2.6)
  - PlantUmlRender: `RenderAll` return value and exit code; `@startuml`/`@enduml`; `outputDir.Create` catch; `-f` invalid format (CR-PUM-4.2.1–4.2.4)
  - **Implementation Tasks:**
    - [ ] CR-EXT-1.2.1, CR-EXT-1.2.2, CR-EXT-1.2.3, CR-EXT-1.2.4
    - [ ] CR-PSM-2.2.1, CR-PSM-2.2.2, CR-PSM-2.2.4, CR-PSM-2.2.5
    - [ ] CR-CLI-3.2.5, CR-CLI-3.2.6
    - [ ] CR-PUM-4.2.1, CR-PUM-4.2.2, CR-PUM-4.2.3, CR-PUM-4.2.4

### Phase 3: Performance *(Estimate: 2–3 days)*

- [ ] **CR-P3:** Caching, polling, and configurable timeouts
  - Extension: cache `cli-agent.json` and paths; optional 1MB cap for `parseCliBlocks` (CR-EXT-1.3.1, 1.3.2)
  - FWH.Prompts: cache `Read-CcliPromptsFile` by path and `LastWriteTime`; `Watch-CcliResults` FileSystemWatcher or doc (CR-PSM-2.3.1, 2.3.2)
  - FWH.CLI.Agent: `CliAgent:RunTimeoutSeconds`, `CliAgent:AgentTimeoutMinutes` (CR-CLI-3.3.2, 3.3.3)
  - PlantUmlRender: `Parallel.ForEachAsync` / `Task.WhenAll` with bounded parallelism (CR-PUM-4.3.1)
  - **Implementation Tasks:**
    - [ ] CR-EXT-1.3.1, CR-EXT-1.3.2
    - [ ] CR-PSM-2.3.1, CR-PSM-2.3.2
    - [ ] CR-CLI-3.3.2, CR-CLI-3.3.3
    - [ ] CR-PUM-4.3.1

### Phase 4: Code Quality & Structure *(Estimate: 4–5 days)*

- [ ] **CR-P4:** Deduplication, helpers, docs, and config schema
  - Extension: `debug()` doc/guard; `runInComposer` warning on throw; `fwhCliAgent.debug` or second channel (CR-EXT-1.4.1–1.4.3)
  - FWH.Prompts: `Resolve-CliFilePath`/`Resolve-PromptsFilePath`; parser refactor; single command manifest; single default CLI.md template; optional 64KB limit for config (CR-PSM-2.1.2, 2.4.1–2.4.4)
  - FWH.CLI.Agent: `CliAgentService`; `RunProcessAsync`; `ParseCommand` doc; `agent`/PATH doc and optional `AgentPath`; `ArgumentList` for dotnet/git (CR-CLI-3.1.3, 3.1.5, 3.4.1–3.4.4)
  - PlantUmlRender: `System.CommandLine`; `RenderOne`; `PlantUmlSettings`/trusted .puml doc (CR-PUM-4.1.2, 4.4.1–4.4.3)
  - Config: `cli-agent.schema.json` and validation or doc; remove or implement `_promptsMdPath` (CR-CFG-5.1.1, 5.1.2)
  - **Implementation Tasks:**
    - [ ] CR-EXT-1.4.1, CR-EXT-1.4.2, CR-EXT-1.4.3
    - [ ] CR-PSM-2.1.2, CR-PSM-2.4.1, CR-PSM-2.4.2, CR-PSM-2.4.3, CR-PSM-2.4.4
    - [ ] CR-CLI-3.1.3, CR-CLI-3.1.5, CR-CLI-3.4.1, CR-CLI-3.4.2, CR-CLI-3.4.3, CR-CLI-3.4.4
    - [ ] CR-PUM-4.1.2, CR-PUM-4.4.1, CR-PUM-4.4.2, CR-PUM-4.4.3
    - [ ] CR-CFG-5.1.1, CR-CFG-5.1.2

### Phase 5: Test Coverage *(Estimate: 5–7 days)*

- [ ] **CR-P5:** Unit, integration, and (where needed) E2E tests
  - Extension: tests for `parseCliBlocks`, `extractPromptFromPromptsSection`, `isPromptCommand`, `removeCliBlock`, `pathsEqual`, `getCliMdPath`, `getExecuteOptions`; mocks for `runWithAgentCli`/`runInComposer` (CR-EXT-1.5.1, 1.5.2)
  - FWH.Prompts: `Invoke-CcliClean`, `Watch-CcliResults`; `Read-CcliPromptsFile` edge cases; `Find-CcliProjectRoot`, `Read-CcliAgentConfig` (CR-PSM-2.5.1, 2.5.2, 2.5.3)
  - FWH.CLI.Agent: test project; `ParseCommand`, `FindProjectRoot`, regex; `ExecutePrompt`/`ExecuteShellCommand` mocks; `CreateInitialCliFile`, `ProcessCliFile`, `ExecuteCleanCli` integration (CR-CLI-3.5.1, 3.5.2, 3.5.3)
  - PlantUmlRender: CLI parsing, `@startuml`/`@enduml`, exit codes; integration with small `.puml` (CR-PUM-4.5.1)
  - **Implementation Tasks:**
    - [ ] CR-EXT-1.5.1, CR-EXT-1.5.2
    - [ ] CR-PSM-2.5.1, CR-PSM-2.5.2, CR-PSM-2.5.3
    - [ ] CR-CLI-3.5.1, CR-CLI-3.5.2, CR-CLI-3.5.3
    - [ ] CR-PUM-4.5.1

---

## Completed Tasks

### MVP-Support (2026-01-25)
- [x] **MVP-SUPPORT-007:** Remove FWH.CLI.Agent and its tests (2026-01-25)
  - Removed `src/FWH.CLI.Agent` and `tests/FWH.CLI.Agent.Tests` from the solution and deleted both folders
  - Removed "CLI Agent" and "CLI Agent (no build)" from `.vscode/launch.json`; removed `kill: cli-agent`, `build: cli-agent`, and `cli-agent` tasks from `.vscode/tasks.json`
  - Updated `cli-agent.schema.json` and `docs/Project/cli-agent-config.md`; marked `ReinitOnStart`, `RunTimeoutSeconds`, `AgentTimeoutMinutes`, `AgentPath` as legacy
  - Updated CODE-REVIEW-*, TODO, Technical-Requirements, Functional-Requirements, Status to note FWH.CLI.Agent removal
  - `extensions/fwh-cli-agent` and `cli-agent.json` retained

### Security & Infrastructure (2025-01-27)
- [x] Resolve Potential SQL Injection in Overpass Query Building (2025-01-27)
  - Added SanitizeCategory method to validate category parameters before use in Overpass queries
  - Only allows alphanumeric characters, hyphens, underscores, and colons (for OSM tag keys)
  - Invalid categories are filtered out with warning logs
  - Prevents injection of Overpass QL syntax like quotes, brackets, semicolons
- [x] Remove Hardcoded Database Credentials in appsettings.json (2025-01-27)
  - Updated ConnectionStrings in both MarketingApi and Location.Api appsettings.json
  - Now uses environment variable substitution with fallback defaults: ${POSTGRES_HOST:localhost}, ${POSTGRES_PASSWORD:postgres}, etc.
  - Credentials can be set via environment variables in production/staging
  - Defaults to localhost/postgres for development convenience
- [x] Eliminate ALL Resource Disposal Issues (2025-01-27)
  - Fixed ActionExecutorErrorHandlingTests: Made class implement IDisposable and track SqliteConnection/ServiceProvider for proper disposal
  - Fixed WorkflowExecutorOptionsTests: Made class implement IDisposable and track SqliteConnection/ServiceProvider for proper disposal
  - All test classes now properly dispose of IDisposable resources

### API Security & Storage (2025-01-23)
- [x] Create API Security for ensuring only genuine builds of the App can call the API (2025-01-23)
  - Implemented API key authentication middleware for both Marketing and Location APIs
  - Added request signing with HMAC-SHA256 for additional security
  - Created ApiAuthenticationService in mobile app to add authentication headers
  - Configured HTTP clients to automatically include authentication headers
  - Added configuration for API keys and secrets in appsettings
  - Authentication can be disabled in development via RequireAuthentication setting
- [x] Add blob storage for staging in Railway (2025-01-23)
  - Created IBlobStorageService interface for abstracting storage
  - Implemented LocalFileBlobStorageService for local filesystem storage
  - Updated FeedbackController to use blob storage service for file uploads
  - Added static file serving for uploaded files
  - Configured persistent volume support in Dockerfile
  - Updated Railway documentation with blob storage configuration
  - Files are stored in /app/uploads with persistent Railway volumes

### Code Quality & Performance (2025-01-23)
- [x] Resolve N+1 Query Problem Potential (2025-01-23)
  - Fixed PlacesViewModel to batch logo fetches and use cached marketing data from database
  - Added batching logic to prevent overwhelming the API with individual requests
- [x] Reduce Generic Exception Handling by handling more known cases. Ensure all async methods have appropriate exception handling and logging (2025-01-23)
  - Replaced generic Exception handlers with specific types: HttpRequestException, TaskCanceledException, OperationCanceledException, DbUpdateException, JsonException
  - Improved exception handling in PlacesViewModel, EfWorkflowRepository, OverpassLocationService, ThemeService, and EfNoteRepository
- [x] Add all missing null checks and ensure nullable annotations are correct and appropriate (2025-01-23)
  - Added null checks in PlacesViewModel constructor and methods
  - Added null checks in EfNoteRepository for method parameters
  - Added missing field declarations for _httpClientFactory and _apiSettings in PlacesViewModel
- [x] Fix Inconsistent Error Handling (2025-01-23)
  - Standardized error handling: database operations throw exceptions, HTTP operations return empty collections/null
  - All error paths now have appropriate logging with specific exception types

### Refactoring (2025-01-27)
- [x] Split multi-type files into single-type files following single-responsibility principle (2025-01-27)
  - Split WorkflowModels.cs, Payloads.cs, Types.cs, ChatEntry.cs, FeedbackModels.cs, BusinessModels.cs
  - Split LocationHandlers.cs and MarketingHandlers.cs into individual handler files
  - Extract TestWorkflowController to separate file
  - All files now follow one type per file pattern with matching file names

### Testing & Documentation (2025-01-27)
- [x] Add XMLDOC to each unit test expressing what is being tested, what data is involved, why the data matters and the expected outcome and the reason it is expected (2025-01-27)
  - Completed: 45+ test files documented with comprehensive XMLDOC comments
  - All major test files completed including MarketingControllerTests.cs
  - All 245 tests passing successfully

### UI & Logging (2025-01-26)
- [x] Better categorize log messages with trace, debug, information, and error throughout the solution (2025-01-26)
- [x] Add button to scroll log viewer to the end (2025-01-26)
- [x] Add dropdown to log viewer to set log level displayed (2025-01-26)
- [x] Update log viewer control to ensure the most recently added entry is visible immediately after being added (2025-01-26)
- [x] Add pause feature to log viewer and use icons instead of text for buttons (2025-01-26)
- [x] Add native map control to the location tracking user control and show device location on the map (2025-01-26)

### Infrastructure & Configuration (2025-01-26)
- [x] Merge all depend-a-bot pull requests (2025-01-26)
- [x] Link to Pages from README (2025-01-26)
- [x] Cleanup docs folder. Remove summaries and plans (2025-01-26)
- [x] Ensure console logging is enabled in the android app (2025-01-26)
- [x] Configure cursor to skip executing $PROFILE on commands (2025-01-26)
- [x] When starting the application, initialize the location tracking with the current location (2025-01-26)

---

*Last updated: 2026-01-25*

---

## Notes

- All code review issues have been resolved
- PostGIS spatial queries implemented with automatic fallback
- Pagination implemented on all list endpoints
- All 245 tests passing
- Codebase is production-ready

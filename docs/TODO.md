# TODO List

This document tracks pending tasks, improvements, and future work for the FunWasHad project.

## High Priority

- [x] Perform code review (2025-01-27)
  - Updated CODE-REVIEW.md to reflect current state
  - 9 of 11 identified issues resolved
  - Remaining: Database query optimization and pagination

## Medium Priority

*(No items currently)*

---

## Completed Tasks

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

*Last updated: 2025-01-27*

# TODO List

Pending tasks and future work for the FunWasHad project.

---

## MVP-App

### High Priority

- [ ] **MVP-APP-001:** Create SocialMediaService library *(160-200 hours)*
  - Manage defaults for social media platforms
  - Create SocialMediaApi for template/default distribution
  - Client-side only sharing (no API required)
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging

- [ ] **MVP-APP-002:** MarketArrival workflow *(96-128 hours)*
  - Get current location on app startup
  - Detect tourism market changes
  - Create MarketArrival.puml workflow definition
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging

### Medium Priority

- [ ] **MVP-APP-004:** Dark toolbar styling *(32-40 hours)*
  - Dark background, transparent buttons, no borders
  - Consistent with platform guidelines (Android/iOS)
  - **Technical Details:**
    - Create `DarkToolbarStyle` in Avalonia themes
    - Transparent button backgrounds with white/light icons
    - Remove border thickness, use subtle shadows for depth
    - Support both light and dark app themes
  - **Implementation Tasks:**
    - [ ] Planning: Design spec for toolbar variants
    - [ ] Create toolbar style definitions in CSS/AXAML
    - [ ] Update `ThemedUserControl` for toolbar awareness
    - [ ] Test on Android and iOS simulators
    - [ ] Final Validation

- [ ] **MVP-APP-005:** Trip planning tool *(320-400 hours)*
  - Blazor website for itinerary creation
  - PostgreSQL storage, QR code retrieval in mobile app
  - **Technical Details:**
    - Blazor Server app with drag-drop itinerary builder
    - Database: `Itineraries`, `ItineraryDays`, `ItineraryItems` tables
    - Generate unique short codes for each itinerary
    - QR codes link to `trip.funwashad.app/{code}`
    - Mobile app scans QR → fetches itinerary → offline cache
  - **Implementation Tasks:**
    - [ ] Planning: Database schema and API design
    - [ ] Create FWH.TripPlanner.Api project
    - [ ] Implement Blazor UI for itinerary builder
    - [ ] QR code generation service (QRCoder library)
    - [ ] Mobile app: QR scanner and itinerary display
    - [ ] Railway deployment configuration
    - [ ] Final Validation

---

## MVP-Marketing

### Medium Priority

- [ ] **MVP-MARKETING-001:** Marketing Blazor admin app *(320-400 hours)*
  - CRUD for businesses, themes, coupons, menu items, news
  - Management for cities, tourism markets, airports
  - Authentication and dashboard views
  - **Technical Details:**
    - Blazor Server with MudBlazor component library
    - Identity Server authentication (admin roles)
    - DataGrid views with inline editing
    - Image upload to blob storage
    - Audit logging for all changes
  - **Implementation Tasks:**
    - [ ] Planning: UI wireframes and data flow
    - [ ] Create FWH.Marketing.Admin project
    - [ ] Implement authentication/authorization
    - [ ] Business CRUD (list, create, edit, delete, image upload)
    - [ ] Theme management (colors, logos, preview)
    - [ ] Coupon management (codes, expiry, usage tracking)
    - [ ] City/Market/Airport reference data management
    - [ ] Dashboard with analytics widgets
    - [ ] Final Validation

- [ ] **MVP-MARKETING-002:** Railway deployment for marketing website *(64-96 hours)*
  - Configure Railway service and CI/CD pipeline
  - Automated deployments and health checks
  - **Technical Details:**
    - Dockerfile for Blazor Server app
    - Railway.toml configuration
    - GitHub Actions workflow for CI/CD
    - Environment variables for connection strings
    - Health check endpoint at `/health`
  - **Implementation Tasks:**
    - [ ] Create Dockerfile for FWH.Marketing.Admin
    - [ ] Configure Railway.toml with health checks
    - [ ] Create GitHub Actions workflow (build, test, deploy)
    - [ ] Set up Railway environment variables
    - [ ] Configure custom domain (admin.funwashad.app)
    - [ ] Test deployment pipeline end-to-end
    - [ ] Final Validation

---

## MVP-Support

### High Priority

- [ ] **MVP-SUPPORT-008:** Update extension to execute prompts via Cursor agent in WSL *(16-24 hours)*
  - Modify fwh-cli-agent extension to invoke `agent` CLI in WSL
  - Pass prompts to Cursor agent for execution
  - Handle response streaming and output display
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] Investigate agent CLI interface and parameters
    - [ ] Update extension to spawn WSL process with agent
    - [ ] Handle input/output streaming
    - [ ] Test and Debug
    - [ ] Final Validation

- [ ] **MVP-SUPPORT-009:** Link shortener website and QR Code generator *(96-128 hours)*
  - Create link shortener website service
  - Automatically generate QR codes for shortened URLs
  - QR code display and download functionality
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging

### Medium Priority

- [ ] **MVP-SUPPORT-006:** Convert solution to XML format *(16-32 hours)*
  - Migrate from legacy .sln format to SDK-style XML
  - Update CI/CD and tooling
  - **Technical Details:**
    - Use `dotnet sln` commands or manual XML conversion
    - SDK-style solution enables better tooling support
    - Ensure all project references preserved
    - Validate build works with `dotnet build`
  - **Implementation Tasks:**
    - [ ] Backup existing FunWasHad.sln
    - [ ] Convert to XML format (slnx or SDK-style)
    - [ ] Verify all 15+ projects load correctly
    - [ ] Update .vscode/tasks.json build commands
    - [ ] Update GitHub Actions workflows
    - [ ] Test full build and test cycle
    - [ ] Final Validation

---

## MVP-Legal

### High Priority

- [ ] **MVP-LEGAL-001:** Legal website *(224-280 hours)*
  - EULA, Privacy Policy, Corporate Contact pages
  - Use MarkdownServer nuget package in a blank asp.net project
  - Documents will be in markdown format.
  - **Implementation Tasks:**
    - [ ] Planning and Documentation
    - [ ] AI Test Generation
    - [ ] AI Implementation
    - [ ] AI Test and Debug
    - [ ] Human Test and Debug
    - [ ] Final Validation
    - [ ] Deployment to Staging

- [ ] **MVP-LEGAL-002:** Legal Tasks
  - Form LLC, obtain EIN, secure licenses
  - **Technical Details:**
    - LLC formation in Texas (or preferred state)
    - Federal EIN via IRS Form SS-4
    - State business license and sales tax permit
    - Local permits as required by city/county
  - **Implementation Tasks:**
    - [ ] Choose LLC state and registered agent
    - [ ] File Articles of Organization
    - [ ] Apply for Federal EIN (IRS.gov)
    - [ ] Register for state sales tax permit
    - [ ] Check local business license requirements
    - [ ] Set up business bank account
    - [ ] Document all legal entity information


---

## Staging & Infrastructure

### Medium Priority

- [ ] **STAGING-001:** Configure API security for Railway
  - Set `ApiSecurity:RequireAuthentication` and `BlobStorage:LocalPath`
  - Document Railway variables and API key provisioning
  - **Technical Details:**
    - Environment variables: `API_KEY`, `DB_CONNECTION_STRING`, `BLOB_PATH`
    - HMAC-SHA256 authentication for API endpoints
    - Local blob storage path: `/app/data/uploads`
  - **Implementation Tasks:**
    - [ ] Add/update appsettings.Staging with env var placeholders
    - [ ] Create RAILWAY-SETUP.md with variable documentation
    - [ ] Generate and store API keys securely
    - [ ] Validate staging flows with auth enabled
    - [ ] Test blob upload/download functionality

- [ ] **STAGING-002:** Test coverage for auth middleware and blob storage
  - Tests for `ApiKeyAuthenticationMiddleware` (both APIs)
  - Cover static file serving from `BlobStorage:LocalPath`
  - **Technical Details:**
    - xUnit tests with WebApplicationFactory
    - Mock API key validation scenarios
    - Test file upload, retrieval, and 404 handling
  - **Implementation Tasks:**
    - [ ] Location API: middleware tests (valid key, invalid key, missing key, config off)
    - [ ] Marketing API: middleware tests and static `/uploads` endpoint tests
    - [ ] Blob storage: upload, retrieve, delete, not found scenarios
    - [ ] Integration tests with actual file system

- [ ] **STAGING-003:** Fix DetectHostIp.ps1 path handling
  - Fix quoting issue with `ProjectDir` trailing quote
  - **Technical Details:**
    - Issue: `$(ProjectDir)` includes trailing backslash causing path errors
    - Solution: Trim or properly escape paths in PowerShell script
  - **Implementation Tasks:**
    - [ ] Inspect FWH.Mobile.Android.csproj Exec task
    - [ ] Fix path quoting in DetectHostIp.ps1
    - [ ] Test Android build on Windows and WSL

### Low Priority

- [ ] **STAGING-004:** Investigate Android device warnings
  - `libdolphin.so` not found, camera open error (non-blocking)
  - **Implementation Tasks:**
    - [ ] Confirm whether libdolphin is required
    - [ ] Reproduce camera error and document

---

## Code Review Remediation

*Reference: [CODE-REVIEW-AGGREGATED.md](../archive/code-review/CODE-REVIEW-AGGREGATED.md)*

### Phase 1: Security & Critical Bugs *(5-7 days)*
- [ ] **CR-P1:** Path validation, temp file cleanup, critical bugs (EXT, PSM, PUM)
  - **Implementation Tasks:**
    - [ ] CR-EXT-1.1.1, CR-EXT-1.1.3, CR-EXT-1.2.5
    - [ ] CR-PSM-2.1.1, CR-PSM-2.1.3, CR-PSM-2.2.3
    - [ ] CR-PUM-4.1.1

### Phase 2: Bugs & Error Handling *(3-4 days)*
- [ ] **CR-P2:** Debug logging, error messages, PlantUmlRender behavior
  - **Implementation Tasks:**
    - [ ] CR-EXT-1.2.1, CR-EXT-1.2.2, CR-EXT-1.2.3, CR-EXT-1.2.4
    - [ ] CR-PSM-2.2.1, CR-PSM-2.2.2, CR-PSM-2.2.4, CR-PSM-2.2.5
    - [ ] CR-PUM-4.2.1, CR-PUM-4.2.2, CR-PUM-4.2.3, CR-PUM-4.2.4

### Phase 3: Performance *(2-3 days)*
- [ ] **CR-P3:** Caching, polling optimization, configurable timeouts
  - **Implementation Tasks:**
    - [ ] CR-EXT-1.3.1, CR-EXT-1.3.2
    - [ ] CR-PSM-2.3.1, CR-PSM-2.3.2
    - [ ] CR-PUM-4.3.1

### Phase 4: Code Quality *(4-5 days)*
- [ ] **CR-P4:** Deduplication, helpers, documentation, config schema
  - **Implementation Tasks:**
    - [ ] CR-EXT-1.4.1, CR-EXT-1.4.2, CR-EXT-1.4.3
    - [ ] CR-PSM-2.1.2, CR-PSM-2.4.1, CR-PSM-2.4.2, CR-PSM-2.4.3, CR-PSM-2.4.4
    - [ ] CR-PUM-4.1.2, CR-PUM-4.4.1, CR-PUM-4.4.2, CR-PUM-4.4.3
    - [ ] CR-CFG-5.1.1

### Phase 5: Test Coverage *(5-7 days)*
- [ ] **CR-P5:** Unit, integration, and E2E tests for EXT, PSM, PUM
  - **Implementation Tasks:**
    - [ ] CR-EXT-1.5.1, CR-EXT-1.5.2
    - [ ] CR-PSM-2.5.1, CR-PSM-2.5.2, CR-PSM-2.5.3
    - [ ] CR-PUM-4.5.1

---

## Completed (Summary)

### 2026-01-27
- **MVP-SUPPORT-003:** Code analyzers enabled (.NET 9.0-all, EF Core analyzers)

### 2026-01-25
- **MVP-SUPPORT-004:** Coverage report with `Update-CoverageReport.ps1`
- **MVP-SUPPORT-005:** Prompts UI in fwh-cli-agent extension
- **MVP-SUPPORT-007:** Removed FWH.CLI.Agent

### 2025-01-27
- SQL injection fix in Overpass queries
- Environment variable substitution for DB credentials
- Resource disposal fixes in tests
- Split multi-type files (single-responsibility)
- XMLDOC added to all 245 tests

### 2025-01-26
- Log viewer improvements (scroll, level filter, pause, icons)
- Native map control with device location
- Dependabot PRs merged, docs cleanup

### 2025-01-23
- API key authentication middleware (HMAC-SHA256)
- Blob storage for Railway (LocalFileBlobStorageService)
- N+1 query fix, specific exception handling, null checks

---

## Notes

- PostGIS spatial queries with automatic fallback
- Pagination on all list endpoints
- All 245 tests passing
- Codebase is production-ready

*Last updated: 2026-01-27*

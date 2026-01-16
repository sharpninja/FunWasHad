# Documentation Guidelines

**Last Updated:** 2025-01-08

---

## Folder Structure

The `docs/` folder is organized into the following subdirectories:

```
docs/
├── README.md                    # Main documentation index (DO NOT MOVE)
├── Technical-Requirements.md   # Core requirements (DO NOT MOVE)
├── Functional-Requirements.md  # Functional spec (DO NOT MOVE)
├── index.md                    # DocFX homepage (DO NOT MOVE)
├── docfx.json                  # DocFX configuration (DO NOT MOVE)
├── toc.yml                     # DocFX navigation (DO NOT MOVE)
├── FWH.Documentation.csproj     # DocFX build project (DO NOT MOVE)
│
├── api/                        # API documentation
├── architecture/               # Architecture and refactoring
├── configuration/              # Configuration guides
├── platform/                   # Platform-specific docs
├── references/                 # Quick references and guides
├── reviews/                    # Code reviews and summaries
├── summaries/                  # Implementation summaries
└── testing/                    # Testing documentation
```

---

## Where to Place New Documentation

### 📡 API Documentation → `api/`
- API endpoint documentation
- API integration guides
- API verification procedures
- API implementation summaries

**Examples:**
- `API-Documentation.md`
- `Mobile_Location_API_Integration_Summary.md`
- `API_Verification_Guide.md`

### 🏛️ Architecture → `architecture/`
- Architecture refactoring summaries
- Design pattern documentation
- Architecture analysis
- Mediator pattern documentation

**Examples:**
- `Mediator_Architecture_Refactoring_Summary.md`
- `SRP_Analysis_And_Refactoring_Plan.md`
- `Orchestrix_Mediator_Summary.md`

### ⚙️ Configuration → `configuration/`
- Setup and configuration guides
- Aspire configuration
- Database configuration
- Port configuration
- Environment setup

**Examples:**
- `Aspire_QuickReference.md`
- `PostgreSQL_LocalStorage_Configuration.md`
- `Aspire_LocationAPI_Port_Configuration.md`

### 📱 Platform-Specific → `platform/`
- Android-specific documentation
- Windows-specific documentation
- iOS-specific documentation
- Platform deployment guides
- CI/CD for platforms

**Examples:**
- `Android_Workflow_Deployment_Summary.md`
- `Windows_Desktop_GPS_Implementation.md`
- `Android_Workload_CI_Enhancement_Summary.md`

### 📚 Quick References → `references/`
- Quick start guides
- Reference materials
- How-to guides
- Tool documentation

**Examples:**
- `Quick_Reference_Next_Steps.md`
- `DocFX-Quick-Start.md`
- `README-DocFX.md`

### 📝 Code Reviews → `reviews/`
- Code review reports
- Review implementation summaries
- Documentation cleanup summaries
- Build configuration summaries
- Setup summaries

**Examples:**
- `Code-Review-Report-YYYY-MM-DD.md`
- `Code-Review-Recommendations-Implementation-Summary.md`
- `Documentation-Cleanup-Summary.md`
- `DocFX-Setup-Summary.md`
- `Swagger-Configuration-Summary.md`

### 📋 Implementation Summaries → `summaries/`
- Feature implementation summaries
- Service implementation details
- Component completion reports
- Technical implementation guides

**Examples:**
- `GPS_Location_Service_Implementation_Summary.md`
- `Location_Tracking_Implementation_Summary.md`
- `Marketing_API_Implementation_Summary.md`
- `Database_Migration_System_Implementation.md`

### 🧪 Testing → `testing/`
- Test implementation summaries
- Test coverage recommendations
- Integration test documentation
- Test analysis reports
- CI/CD testing documentation

**Examples:**
- `TestImplementationSummary.md`
- `TestCoverageRecommendations.md`
- `Integration_Tests_Update_Summary.md`
- `FWH_Common_Imaging_Tests_Analysis.md`

---

## Naming Conventions

### File Naming
- Use descriptive names with underscores
- Include category prefix when appropriate
- Use consistent suffixes:
  - `_Summary.md` - Implementation or feature summaries
  - `_Implementation.md` - Detailed implementation guides
  - `_Guide.md` - How-to guides
  - `_Report.md` - Reports and analysis
  - `_Plan.md` - Planning documents
  - `_Configuration.md` - Configuration guides

**Examples:**
- `Location_Tracking_Implementation_Summary.md`
- `Aspire_QuickReference.md`
- `Code-Review-Report-2025-01-08.md`

### Date Format
- For dated documents: `YYYY-MM-DD` format
- Example: `Code-Review-Report-2025-01-08.md`

---

## Root Level Files (DO NOT MOVE)

These files must remain in the root `docs/` folder:

- ✅ `README.md` - Main documentation index
- ✅ `Technical-Requirements.md` - Core requirements
- ✅ `Functional-Requirements.md` - Functional specification
- ✅ `index.md` - DocFX homepage
- ✅ `docfx.json` - DocFX configuration
- ✅ `toc.yml` - DocFX navigation
- ✅ `FWH.Documentation.csproj` - DocFX build project
- ✅ `DOCUMENTATION-GUIDELINES.md` - This file

---

## Adding New Documentation

### Step 1: Determine Category
Review the folder descriptions above and determine the appropriate subdirectory.

### Step 2: Create File
Create the markdown file in the appropriate subdirectory following naming conventions.

### Step 3: Update Navigation
- Update `docs/toc.yml` if the document should appear in DocFX navigation
- Update `docs/README.md` if the document should appear in the main index

### Step 4: Follow Format
- Include proper markdown formatting
- Add XML documentation references (TR-XXX) when applicable
- Include code examples where helpful
- Add links to related documentation

---

## Prohibited Locations

### ❌ Root Level
**DO NOT** place new markdown files in the root `docs/` folder unless they are:
- Core requirements documents
- Main index files
- DocFX configuration files

**Exception:** Temporary files during development should be moved to appropriate folders before committing.

### ❌ Generated Folders
**DO NOT** place files in:
- `_site/` - Generated by DocFX
- `obj/` - Build artifacts
- `bin/` - Build artifacts
- `api/` - Generated by DocFX (unless it's API documentation markdown)

---

## Maintenance

### Regular Cleanup
- Remove duplicate files
- Move misplaced files to correct folders
- Update README.md when adding new documents
- Update toc.yml when adding to DocFX navigation

### Verification
Before committing:
1. ✅ File is in correct subdirectory
2. ✅ File follows naming conventions
3. ✅ README.md updated (if needed)
4. ✅ toc.yml updated (if needed)
5. ✅ No files in root except allowed files

---

## Quick Reference

| Document Type | Folder | Example |
|--------------|--------|---------|
| API docs | `api/` | `API-Documentation.md` |
| Architecture | `architecture/` | `Mediator_Architecture_Summary.md` |
| Configuration | `configuration/` | `Aspire_QuickReference.md` |
| Platform docs | `platform/` | `Android_Deployment.md` |
| Quick guides | `references/` | `Quick_Start_Guide.md` |
| Code reviews | `reviews/` | `Code-Review-Report.md` |
| Implementation | `summaries/` | `Feature_Implementation_Summary.md` |
| Testing | `testing/` | `Test_Summary.md` |

---

**Questions?** Check `docs/README.md` for the complete documentation index.

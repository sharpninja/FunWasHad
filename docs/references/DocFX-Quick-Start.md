# DocFX Quick Start Guide

## Generate Documentation

### Option 1: PowerShell Script (Recommended)
```powershell
.\scripts\Generate-Documentation.ps1
```

### Option 2: Manual Generation
```bash
cd docs
dotnet tool install -g docfx
docfx docfx.json
```

### Option 3: MSBuild Integration
```bash
dotnet build -p:GenerateDocumentation=true
```

## View Documentation

After generation, open `docs/_site/index.html` in your browser.

## Serve Locally

```powershell
# Generate and serve automatically
.\scripts\Generate-Documentation.ps1 -Serve

# Or manually
cd docs\_site
python -m http.server 8080
# Open http://localhost:8080
```

## What's Included

- ✅ **API Documentation** - Generated from XML comments in all projects
- ✅ **Markdown Files** - All documentation from `docs/` folder structure
- ✅ **Cross-References** - Links between APIs and documentation
- ✅ **Search** - Full-text search across all documentation
- ✅ **Navigation** - Hierarchical table of contents

## Configuration

- **Config File:** `docs/docfx.json`
- **Navigation:** `docs/toc.yml`
- **Homepage:** `docs/index.md`

## Troubleshooting

**DocFX not found:**
```bash
dotnet tool install -g docfx
```

**Missing XML docs:**
Ensure projects have `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in `.csproj` files.

**Build errors:**
Ensure all projects build successfully before generating documentation.

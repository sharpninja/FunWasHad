# DocFX Documentation Generation

This document explains how to generate and view the project documentation using DocFX.

## Quick Start

### Generate Documentation

```powershell
# Using PowerShell script (recommended)
.\scripts\Generate-Documentation.ps1

# Or manually
cd docs
dotnet tool install -g docfx
docfx docfx.json
```

### View Documentation

After generation, open `docs/_site/index.html` in your browser.

### Serve Locally

```powershell
# Generate and serve
.\scripts\Generate-Documentation.ps1 -Serve

# Or manually serve
cd docs\_site
python -m http.server 8080
# Then open http://localhost:8080
```

## What Gets Generated

### API Documentation
- All public APIs from XML comments
- Type information and inheritance
- Cross-references between APIs
- Parameter and return value documentation

### Conceptual Documentation
- All markdown files from `docs/` folder
- Organized by folder structure
- Preserved links and references
- Table of contents navigation

## Configuration

### docfx.json
Main configuration file located at `docs/docfx.json`.

**Key Settings:**
- **Metadata Source:** All `.csproj` files (excluding tests)
- **Content Source:** All markdown files in `docs/` folder
- **Output:** `docs/_site/` folder
- **Templates:** Default + Modern

### toc.yml
Navigation structure for the documentation site.

## Integration

### MSBuild Integration
Documentation can be generated as part of the build:

```bash
dotnet build -p:GenerateDocumentation=true
```

### CI/CD Integration
Add to your CI/CD pipeline:

```yaml
- name: Generate Documentation
  run: |
    dotnet tool install -g docfx
    cd docs
    docfx docfx.json
```

## Publishing

### GitHub Pages
1. Generate documentation
2. Push `_site/` folder to `gh-pages` branch
3. Enable GitHub Pages in repository settings

### Azure Static Web Apps
1. Configure build command: `docfx docfx.json`
2. Set app location: `docs`
3. Set output location: `docs/_site`

## Troubleshooting

### DocFX Not Found
```bash
dotnet tool install -g docfx
```

### Missing XML Documentation
Ensure projects have XML documentation enabled:
```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
```

### Build Errors
Check that all projects build successfully before generating documentation.

## More Information

See [DocFX-Setup-Summary.md](DocFX-Setup-Summary.md) for detailed setup information.

# MVP-LEGAL-001: Legal Website Implementation Plan

**Last Updated:** 2026-01-27

## Overview

Create a website for hosting legal notices including EULA, Privacy Policy, and Corporate Contact information using ASP.NET with the MarkdownServer NuGet package.

**Estimate:** 224-280 hours (28-35 days)

---

## Goals

1. Host legally required documents (EULA, Privacy Policy) for app store compliance
2. Provide corporate contact information for users and legal inquiries
3. Enable easy document updates via markdown files without code changes
4. Support document versioning to track changes over time
5. Ensure accessibility compliance (WCAG 2.1 AA)

---

## Technical Approach

### Stack

| Component | Technology |
|-----------|------------|
| Framework | ASP.NET Core 9.0 (Minimal API) |
| Markdown Rendering | [MarkdownServer 1.4.0](https://www.nuget.org/packages/MarkdownServer) |
| Styling | Custom CSS (lightweight, print-friendly) |
| Hosting | Railway (consistent with other FWH services) |
| CI/CD | GitHub Actions (reusable workflows) |

### Why MarkdownServer?

- **Zero-code content serving** - markdown files automatically become HTML pages
- **YAML front matter** - metadata for title, version, effective date, layout
- **Variable substitution** - use `$(Variable)` in markdown and HTML layouts
- **File includes** - `#include(path/to/file.md)` for reusable content sections
- **Layout templates** - HTML layouts with `MDS-Include` attribute for content injection
- **Built-in syntax highlighting** - for any code examples in documents

### MarkdownServer Dependencies (handled automatically)

- Markdig (markdown parsing)
- YamlDotNet (front matter parsing)
- Newtonsoft.Json
- MarkdownServer.Markdig.SyntaxHighlighting

---

## MarkdownServer Implementation

### Program.cs (Complete)

```csharp
var builder = WebApplication.CreateBuilder(args);

// Add MarkdownServer services
builder.AddMarkdownServer();

var app = builder.Build();

app.UseStaticFiles();

// Enable MarkdownServer middleware - serves .md files as HTML
app.UseMarkdownServer();

app.Run();
```

### Project File

```xml
<!-- FWH.Legal.Web.csproj -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MarkdownServer" />
  </ItemGroup>
</Project>
```

### Directory.Packages.props (add entry)

```xml
<PackageVersion Include="MarkdownServer" Version="1.4.0" />
```

---

## Project Structure

```
src/FWH.Legal.Web/
├── FWH.Legal.Web.csproj
├── Program.cs                    # ~10 lines, see above
├── appsettings.json
├── Properties/
│   └── launchSettings.json
│
├── Shared/                       # Layout templates
│   └── layout.html               # Default HTML layout
│
├── wwwroot/                      # Static assets
│   ├── css/
│   │   └── site.css
│   ├── images/
│   │   └── logo.png
│   └── favicon.ico
│
├── sections/                     # Reusable content includes
│   ├── contact-info.md
│   ├── license-grant.md
│   └── data-retention.md
│
├── index.md                      # → /
├── eula.md                       # → /eula (current version)
├── privacy.md                    # → /privacy (current version)
├── contact.md                    # → /contact
│
├── eula/                         # Version archive
│   ├── history.md                # → /eula/history
│   ├── v1.0.0.md                 # → /eula/v1.0.0
│   └── v1.1.0.md                 # → /eula/v1.1.0
│
├── privacy/                      # Version archive
│   ├── history.md                # → /privacy/history
│   └── v1.0.0.md                 # → /privacy/v1.0.0
│
└── Dockerfile
```

**Key insight:** MarkdownServer maps `.md` files directly to URLs. No routing configuration needed.

---

## Layout Template

```html
<!-- Shared/layout.html -->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>$(Title) - Fun Was Had Legal</title>
    <meta name="description" content="$(Description)">
    <link rel="stylesheet" href="/css/site.css">
    <link rel="icon" href="/favicon.ico">
</head>
<body>
    <a href="#main-content" class="skip-link">Skip to content</a>
    
    <header>
        <nav aria-label="Main navigation">
            <a href="/" class="logo">
                <img src="/images/logo.png" alt="Fun Was Had">
            </a>
            <ul>
                <li><a href="/eula">EULA</a></li>
                <li><a href="/privacy">Privacy Policy</a></li>
                <li><a href="/contact">Contact</a></li>
            </ul>
        </nav>
    </header>
    
    <main id="main-content" MDS-Include="">
        <!-- Markdown content automatically injected here -->
    </main>
    
    <footer>
        <p class="version-info">
            Version $(Version) | Effective: $(EffectiveDate)
        </p>
        <p class="copyright">
            &copy; 2026 Gateway Programming School, Inc.
        </p>
        <p class="links">
            <a href="/eula/history">EULA History</a> |
            <a href="/privacy/history">Privacy History</a>
        </p>
    </footer>
</body>
</html>
```

---

## Document Format

### EULA Example (eula.md)

```markdown
---
Title: End User License Agreement
Description: Terms and conditions for using the Fun Was Had mobile application
Version: 1.2.0
EffectiveDate: February 1, 2026
LastUpdated: January 15, 2026
Layout: Shared/layout.html
---

# End User License Agreement

**Effective Date:** $(EffectiveDate)  
**Version:** $(Version)

## 1. Acceptance of Terms

By downloading, installing, or using the Fun Was Had application ("App"), 
you agree to be bound by the terms and conditions of this End User License 
Agreement ("Agreement").

## 2. License Grant

#include(sections/license-grant.md)

## 3. Intellectual Property

The App and all content, features, and functionality are owned by 
Gateway Programming School, Inc. and are protected by copyright, 
trademark, and other intellectual property laws.

## 4. User Obligations

You agree to:
- Use the App only for lawful purposes
- Not reverse engineer or decompile the App
- Not distribute or sublicense the App

## 5. Disclaimers

THE APP IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND...

## 6. Contact

#include(sections/contact-info.md)

---

[View previous versions](/eula/history)
```

### Privacy Policy Example (privacy.md)

```markdown
---
Title: Privacy Policy
Description: How Fun Was Had collects, uses, and protects your personal information
Version: 1.0.0
EffectiveDate: February 1, 2026
LastUpdated: January 15, 2026
Layout: Shared/layout.html
---

# Privacy Policy

**Last Updated:** $(LastUpdated)

## Information We Collect

### Personal Information
When you use our App, we may collect:
- Name and email address (when you contact us)
- Location data (with your explicit permission)

### Usage Data
We automatically collect:
- App usage patterns and features accessed
- Device type, operating system, and version
- Crash reports and performance data

## How We Use Your Information

#include(sections/data-usage.md)

## Data Retention

#include(sections/data-retention.md)

## Your Rights

You have the right to:
- Access your personal data
- Request correction of inaccurate data
- Request deletion of your data
- Opt out of location tracking

## Contact Us

#include(sections/contact-info.md)

---

[View previous versions](/privacy/history)
```

### Reusable Include Example (sections/contact-info.md)

```markdown
For questions about this document:

- **General:** support@funwashad.app
- **Legal:** legal@funwashad.app
- **Privacy:** privacy@funwashad.app

**Mailing Address:**  
Gateway Programming School, Inc.  
123 Main Street  
Anytown, ST 12345  
United States
```

### Version History Page (eula/history.md)

```markdown
---
Title: EULA Version History
Description: Archive of all End User License Agreement versions
Layout: Shared/layout.html
Version: -
EffectiveDate: -
---

# EULA Version History

| Version | Effective Date | Summary |
|---------|----------------|---------|
| [1.2.0](/eula) | Feb 1, 2026 | Current - Added location data terms |
| [1.1.0](/eula/v1.1.0) | Jan 1, 2026 | Updated liability disclaimers |
| [1.0.0](/eula/v1.0.0) | Dec 1, 2025 | Initial release |

---

[Back to current EULA](/eula)
```

---

## URL Structure

| URL | Source File | Content |
|-----|-------------|---------|
| `/` | `index.md` | Landing page with document links |
| `/eula` | `eula.md` | Current EULA |
| `/eula/history` | `eula/history.md` | List of all EULA versions |
| `/eula/v1.0.0` | `eula/v1.0.0.md` | Specific EULA version |
| `/privacy` | `privacy.md` | Current Privacy Policy |
| `/privacy/history` | `privacy/history.md` | List of all Privacy Policy versions |
| `/privacy/v1.0.0` | `privacy/v1.0.0.md` | Specific Privacy Policy version |
| `/contact` | `contact.md` | Corporate contact information |

---

## Styling (wwwroot/css/site.css)

```css
/* Base styles - accessible and print-friendly */
:root {
    --text-color: #1a1a1a;
    --bg-color: #ffffff;
    --link-color: #0066cc;
    --border-color: #e0e0e0;
    --max-width: 800px;
}

* { box-sizing: border-box; }

body {
    font-family: system-ui, -apple-system, sans-serif;
    font-size: 1rem;
    line-height: 1.7;
    color: var(--text-color);
    background: var(--bg-color);
    margin: 0;
    padding: 0;
}

/* Skip link for accessibility */
.skip-link {
    position: absolute;
    top: -40px;
    left: 0;
    padding: 8px;
    background: var(--link-color);
    color: white;
    z-index: 100;
}
.skip-link:focus { top: 0; }

/* Navigation */
header {
    border-bottom: 1px solid var(--border-color);
    padding: 1rem;
}
nav {
    max-width: var(--max-width);
    margin: 0 auto;
    display: flex;
    justify-content: space-between;
    align-items: center;
}
nav ul {
    list-style: none;
    display: flex;
    gap: 1.5rem;
    margin: 0;
    padding: 0;
}
nav a { text-decoration: none; }
nav a:hover { text-decoration: underline; }

/* Main content */
main {
    max-width: var(--max-width);
    margin: 0 auto;
    padding: 2rem 1rem;
}

/* Typography */
h1 { font-size: 2rem; margin-top: 0; }
h2 { font-size: 1.5rem; margin-top: 2.5rem; }
h3 { font-size: 1.25rem; margin-top: 2rem; }

a { color: var(--link-color); }
a:focus { outline: 2px solid var(--link-color); outline-offset: 2px; }

/* Tables */
table {
    width: 100%;
    border-collapse: collapse;
    margin: 1rem 0;
}
th, td {
    padding: 0.75rem;
    border: 1px solid var(--border-color);
    text-align: left;
}
th { background: #f5f5f5; }

/* Footer */
footer {
    border-top: 1px solid var(--border-color);
    padding: 1.5rem 1rem;
    text-align: center;
    font-size: 0.875rem;
    color: #666;
}

/* Print styles */
@media print {
    header nav ul, footer .links, .skip-link { display: none; }
    main { padding: 0; }
    a { color: inherit; text-decoration: none; }
    a[href]::after { content: " (" attr(href) ")"; font-size: 0.8em; }
}

/* Responsive */
@media (max-width: 600px) {
    nav { flex-direction: column; gap: 1rem; }
    nav ul { flex-wrap: wrap; justify-content: center; }
}
```

---

## Implementation Phases

### Phase 1: Planning and Documentation (16-24 hours)

- [ ] Review app store requirements for EULA and Privacy Policy
- [ ] Research WCAG 2.1 AA accessibility requirements
- [ ] Draft initial content outlines for each document
- [ ] Define versioning strategy and archive structure
- [ ] Create wireframes for page layouts
- [ ] Update Technical-Requirements.md with legal website specs

**Deliverables:**
- Content outlines for EULA, Privacy Policy, Contact
- Wireframes/mockups
- Technical specification document

### Phase 2: AI Test Generation (24-32 hours)

- [ ] Write integration tests for route handling (each .md → URL)
- [ ] Write tests for front matter variable substitution
- [ ] Write tests for `#include()` file resolution
- [ ] Write tests for 404 handling (missing documents)
- [ ] Write accessibility tests (heading structure, contrast, focus)
- [ ] Write tests for layout rendering

**Test Categories:**
```
tests/FWH.Legal.Web.Tests/
├── RouteHandlingTests.cs
├── FrontMatterTests.cs
├── IncludeResolutionTests.cs
├── LayoutRenderingTests.cs
├── AccessibilityTests.cs
└── ErrorHandlingTests.cs
```

### Phase 3: AI Implementation (80-100 hours)

#### 3.1 Project Setup (8-12 hours)
- [ ] Create `FWH.Legal.Web` project with MarkdownServer
- [ ] Add to `FunWasHad.sln`
- [ ] Add `MarkdownServer` to `Directory.Packages.props`
- [ ] Create `Program.cs` (~10 lines)
- [ ] Create `Shared/layout.html` template
- [ ] Create `wwwroot/css/site.css`
- [ ] Configure `appsettings.json` for environments

#### 3.2 Content Structure (16-20 hours)
- [ ] Create `index.md` landing page
- [ ] Create `eula.md` with YAML front matter
- [ ] Create `privacy.md` with YAML front matter
- [ ] Create `contact.md`
- [ ] Create `sections/` directory with reusable includes
- [ ] Create version archive structure (`eula/`, `privacy/`)
- [ ] Create history pages for each document type

#### 3.3 Document Content (40-48 hours)
- [ ] Draft complete EULA content (all required sections)
- [ ] Draft complete Privacy Policy content (GDPR/CCPA compliant)
- [ ] Draft Corporate Contact content
- [ ] Create reusable sections (contact-info, license-grant, etc.)
- [ ] Review and refine all content

#### 3.4 Styling and Polish (16-20 hours)
- [ ] Finalize CSS with FWH branding
- [ ] Ensure responsive design works
- [ ] Add print stylesheet
- [ ] Add favicon and logo
- [ ] Test variable substitution in all documents

### Phase 4: AI Test and Debug (32-40 hours)

- [ ] Run all integration tests, fix failures
- [ ] Verify all routes serve correct content
- [ ] Test `#include()` directives resolve correctly
- [ ] Test `$(Variable)` substitution in markdown and layout
- [ ] Validate HTML output (W3C validator)
- [ ] Run accessibility audit (axe, WAVE, Lighthouse)
- [ ] Fix accessibility issues
- [ ] Test on Chrome, Firefox, Safari, Edge
- [ ] Test on mobile devices (iOS Safari, Android Chrome)

### Phase 5: Human Test and Debug (40-48 hours)

- [ ] Legal review of EULA content
- [ ] Legal review of Privacy Policy content
- [ ] Verify compliance with Apple App Store requirements
- [ ] Verify compliance with Google Play requirements
- [ ] User testing for readability and navigation
- [ ] Verify all internal links work
- [ ] Test print functionality
- [ ] Review SEO (meta tags from front matter)
- [ ] Security review (no sensitive data exposure)
- [ ] Performance testing (page load times)

### Phase 6: Final Validation (16-20 hours)

- [ ] All tests passing
- [ ] Accessibility audit score ≥ 90%
- [ ] Lighthouse performance score ≥ 90%
- [ ] Legal sign-off on all documents
- [ ] Documentation updated
- [ ] Code review completed
- [ ] PR approved and merged

### Phase 7: Deployment to Staging (16-24 hours)

- [ ] Create Dockerfile
- [ ] Configure Railway service
- [ ] Set up environment variables
- [ ] Create GitHub Actions workflow (reuse existing patterns)
- [ ] Deploy to staging environment
- [ ] Verify staging deployment
- [ ] Configure custom domain (legal.funwashad.app)
- [ ] Set up health checks
- [ ] Configure monitoring/alerts

---

## Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["src/FWH.Legal.Web/FWH.Legal.Web.csproj", "FWH.Legal.Web/"]
RUN dotnet restore "FWH.Legal.Web/FWH.Legal.Web.csproj"
COPY src/FWH.Legal.Web/ FWH.Legal.Web/
WORKDIR "/src/FWH.Legal.Web"
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FWH.Legal.Web.dll"]
```

---

## Railway Deployment Plan

### Overview

The Legal Website is a simple static-content server with no database dependencies, making deployment straightforward.

| Component | Value |
|-----------|-------|
| Service Name | `staging-legal-web` (staging) / `legal-web` (production) |
| Domain | `legal.funwashad.app` |
| Port | 8080 |
| Database | None required |
| Storage | None required |

### Prerequisites

- [ ] Railway account at https://railway.app
- [ ] Access to FunWasHad Railway project
- [ ] GitHub repository access
- [ ] `RAILWAY_STAGING_TOKEN` secret configured in GitHub

### Step 1: Create Railway Service

1. Go to Railway project: https://railway.com/project/9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d
2. Click **"+ New"** → **"GitHub Repo"**
3. Select **"sharpninja/FunWasHad"**
4. Rename service to `staging-legal-web`

### Step 2: Configure Environment Variables

Click **"Variables"** tab and add:

```bash
# Core Configuration
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://+:8080
PORT=8080

# Logging
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning
```

### Step 3: Configure Build Settings

Click **"Settings"** → **"Build"**:

| Setting | Value |
|---------|-------|
| Builder | Nixpacks |
| Build Command | `dotnet publish src/FWH.Legal.Web -c Release -o out` |
| Start Command | `dotnet out/FWH.Legal.Web.dll` |
| Root Directory | `/` |

Or use Dockerfile:

| Setting | Value |
|---------|-------|
| Builder | Dockerfile |
| Dockerfile Path | `src/FWH.Legal.Web/Dockerfile` |

### Step 4: Configure Deployment

Click **"Settings"** → **"Deploy"**:

| Setting | Value |
|---------|-------|
| Branch | `develop` (staging) / `main` (production) |
| Auto Deploy | Enabled ✅ |
| Health Check Path | `/health` |
| Health Check Timeout | 30s |

### Step 5: Configure Custom Domain

Click **"Settings"** → **"Networking"**:

1. Click **"Generate Domain"** for Railway subdomain (testing)
2. Click **"Add Custom Domain"**
3. Enter: `legal.funwashad.app`
4. Add DNS records to your domain registrar:
   ```
   Type: CNAME
   Name: legal
   Value: <railway-provided-value>.railway.app
   ```
5. Wait for SSL certificate provisioning (~5 minutes)

### Step 6: Add Health Check Endpoint

Update `Program.cs` to include health check:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.AddMarkdownServer();
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseStaticFiles();
app.UseMarkdownServer();
app.MapHealthChecks("/health");

app.Run();
```

### GitHub Actions Workflow

Add to `.github/workflows/staging.yml` (or create new workflow):

```yaml
  deploy-legal-web:
    name: Deploy Legal Website
    needs: [build-and-test]
    runs-on: ubuntu-latest
    if: github.ref == 'refs/heads/develop'
    
    steps:
      - uses: actions/checkout@v4
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'
      
      - name: Publish Legal Web
        run: |
          dotnet publish src/FWH.Legal.Web/FWH.Legal.Web.csproj \
            -c Release \
            -o ./publish/legal-web
      
      - name: Install Railway CLI
        run: npm install -g @railway/cli
      
      - name: Deploy to Railway
        env:
          RAILWAY_TOKEN: ${{ secrets.RAILWAY_STAGING_TOKEN }}
        run: |
          railway link ${{ secrets.RAILWAY_STAGING_PROJECT_ID }}
          railway service staging-legal-web
          railway up --detach
```

### Environment Variables Reference

| Variable | Staging | Production |
|----------|---------|------------|
| `ASPNETCORE_ENVIRONMENT` | `Staging` | `Production` |
| `ASPNETCORE_URLS` | `http://+:8080` | `http://+:8080` |
| `PORT` | `8080` | `8080` |
| `Logging__LogLevel__Default` | `Information` | `Warning` |

### Verification Checklist

After deployment, verify:

- [ ] Railway service shows green status
- [ ] Health endpoint returns 200: `curl https://legal.funwashad.app/health`
- [ ] Home page loads: `https://legal.funwashad.app/`
- [ ] EULA page loads: `https://legal.funwashad.app/eula`
- [ ] Privacy page loads: `https://legal.funwashad.app/privacy`
- [ ] Contact page loads: `https://legal.funwashad.app/contact`
- [ ] Version history pages load
- [ ] SSL certificate is valid
- [ ] No errors in Railway logs

### Rollback Procedure

If deployment fails:

1. **Railway Dashboard:** Click service → **Deployments** → Select previous deployment → **Rollback**
2. **Or via CLI:**
   ```bash
   railway service staging-legal-web
   railway rollback
   ```

### Cost Estimate

| Resource | Monthly Cost |
|----------|--------------|
| Legal Web Service | ~$5-10 |
| Custom Domain | Included |
| SSL Certificate | Included |
| **Total** | **~$5-10/month** |

*Note: This is a lightweight service with minimal resource usage.*

### Production Deployment

For production, create a separate service:

1. Service name: `legal-web` (no staging prefix)
2. Branch: `main`
3. Domain: `legal.funwashad.app`
4. Environment: `Production`

Consider:
- [ ] Set up monitoring alerts
- [ ] Configure uptime monitoring (e.g., UptimeRobot)
- [ ] Set up error tracking (optional for static content)

---

## Content Requirements

### EULA (End User License Agreement)

Must include:
- [ ] Acceptance of terms
- [ ] License grant and restrictions
- [ ] Intellectual property rights
- [ ] User obligations
- [ ] Disclaimers and limitations of liability
- [ ] Termination conditions
- [ ] Governing law and jurisdiction
- [ ] Contact information for legal inquiries

### Privacy Policy

Must include:
- [ ] Information collected (personal data, usage data, location)
- [ ] How information is used
- [ ] Information sharing and disclosure
- [ ] Data retention periods
- [ ] User rights (access, correction, deletion)
- [ ] Cookie policy (if applicable)
- [ ] Children's privacy (COPPA compliance)
- [ ] International data transfers (GDPR compliance)
- [ ] Security measures
- [ ] Policy update procedures
- [ ] Contact information for privacy inquiries

### Corporate Contact

Must include:
- [ ] Company name and legal entity
- [ ] Physical address
- [ ] Email for general inquiries
- [ ] Email for legal inquiries
- [ ] Email for privacy inquiries
- [ ] Support contact information

---

## Accessibility Requirements (WCAG 2.1 AA)

- [ ] All images have alt text
- [ ] Proper heading hierarchy (h1 → h2 → h3)
- [ ] Sufficient color contrast (4.5:1 for normal text)
- [ ] Keyboard navigation works
- [ ] Focus indicators visible
- [ ] Skip to content link
- [ ] Responsive design (works at 200% zoom)
- [ ] Language attribute set on HTML element
- [ ] Links are distinguishable (not just by color)

---

## Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Legal content not ready | Delays deployment | Start content drafting in Phase 1 |
| MarkdownServer .NET 9 compatibility | May need workaround | Package targets net8.0, should work via rollforward |
| Accessibility failures | App store rejection | Audit early and often |
| Multi-language requirement | Scope increase | Start with English only, design for i18n later |

---

## Success Criteria

1. All three document types accessible via public URLs
2. Document versioning working with archive access
3. All tests passing (≥ 80% coverage)
4. WCAG 2.1 AA compliance verified
5. Legal team sign-off on content
6. Deployed to staging with CI/CD pipeline
7. Page load time < 2 seconds
8. Mobile-responsive design verified
9. `#include()` and `$(Variable)` features working correctly

---

## References

- [MarkdownServer NuGet](https://www.nuget.org/packages/MarkdownServer)
- [MarkdownServer Source](https://github.com/sharpninja/AspNetServices)
- [WCAG 2.1 Guidelines](https://www.w3.org/WAI/WCAG21/quickref/)
- [Apple App Store Review Guidelines](https://developer.apple.com/app-store/review/guidelines/)
- [Google Play Developer Policy](https://play.google.com/about/developer-content-policy/)
- [GDPR Requirements](https://gdpr.eu/)
- [CCPA Requirements](https://oag.ca.gov/privacy/ccpa)

---

*Created: 2026-01-26*  
*Updated: 2026-01-26*  
*Status: Planning*

---

## Appendix: Quick Reference

### Railway Service Configuration

```
Service: staging-legal-web
Domain: legal.funwashad.app
Port: 8080
Health: /health
Branch: develop (staging) / main (production)
```

### Environment Variables (Copy-Paste)

```bash
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://+:8080
PORT=8080
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning
```

### DNS Configuration

```
Type: CNAME
Name: legal
Value: <railway-provided>.railway.app
TTL: 3600
```

### Test Commands

```bash
# Health check
curl -I https://legal.funwashad.app/health

# All pages
curl -I https://legal.funwashad.app/
curl -I https://legal.funwashad.app/eula
curl -I https://legal.funwashad.app/privacy
curl -I https://legal.funwashad.app/contact
```

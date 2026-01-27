# Functional Requirements - FunWasHad Application

**Version:** 1.3  \
**Last Updated:** 2026-01-27  \
**Application:** FunWasHad - Location-Based Activity Tracking & Business Marketing Platform

---

## 1. Overview

FunWasHad is a mobile application that combines location-based activity tracking with business marketing features. The application detects when users arrive at a new address, runs a guided workflow for that location, and surfaces subscribed business marketing content (themes, coupons, menus, and news). Users can also submit feedback to businesses, optionally with media attachments.

---

## 2. Functional Requirements (Business Rules)

App Name (all platforms): #FunWasHad
App Identifier (all platforms): `app.funwashad`

### 2.1 Location Tracking and Address Change

#### FR-LOC-001: Monitor device location
The system SHALL monitor the device’s location while tracking is enabled.

#### FR-LOC-002: Detect arrival before triggering address change
The system SHALL only attempt address-change evaluation after the user is detected as stationary for a minimum period.

#### FR-LOC-003: Raise address-change event
When the detected address differs from the previously recorded address, the system SHALL raise a `NewLocationAddress` event.

#### FR-LOC-004: Resolve and display address for current location
The system SHALL resolve a human-readable address for the current (and startup) location via the Location API reverse-geocoding endpoint (`GET /api/locations/address`), returning business address when available or Overpass/OSM-derived address when no business is present, so that the current location address is displayed even when there is no business.

---

### 2.2 Location-Based Workflow

#### FR-WF-001: Start location workflow on address change
When a `NewLocationAddress` event occurs, the system SHALL start a workflow using the `new-location.puml` definition.

#### FR-WF-002: Persist workflow state per address
The system SHALL persist workflow state locally using an address-based key so a workflow can be resumed later.

#### FR-WF-003: Resume workflow within 24 hours
If a workflow exists for the same address created within the last 24 hours, the system SHALL resume the existing workflow instance; otherwise it SHALL create a new workflow instance.

#### FR-WF-004: Address-based workflow identifier
The workflow identifier SHALL be deterministic for an address (for example `location:{address_hash}`) so that lookups for prior state are stable.

---

### 2.3 Business Discovery and Marketing Content

#### FR-BIZ-001: Find nearby businesses
The system SHALL allow querying for nearby businesses using latitude/longitude and a radius.

#### FR-BIZ-002: Return complete marketing payload
The system SHALL provide an endpoint to retrieve a business’s complete marketing payload, including its theme, active coupons, available menu items, and recent news.

#### FR-BIZ-003: Theme activation
The system SHALL treat a business theme as displayable only when the theme is marked active.

#### FR-BIZ-004: Coupon eligibility
The system SHALL treat a coupon as displayable only when:
- the coupon is marked active, and
- the current time is within the coupon validity window (`ValidFrom` to `ValidUntil`).

#### FR-BIZ-005: Menu item availability
The system SHALL treat a menu item as displayable only when the menu item is marked available.

#### FR-BIZ-006: News recency
The system SHALL allow retrieving a limited number of recent news items for a business.

---

### 2.4 Feedback and Attachments

#### FR-FB-001: Submit feedback
The system SHALL allow users to submit feedback for a business.

#### FR-FB-002: Required feedback fields
When submitting feedback, the system SHALL require:
- `BusinessId`
- `UserId`
- `FeedbackType`
- `Subject`
- `Message`

#### FR-FB-003: Allowed feedback types
The system SHALL accept `FeedbackType` values representing at least: review, complaint, suggestion, and compliment.

#### FR-FB-004: Rating bounds
If a rating is provided, the system SHALL only accept ratings in the range 1–5.

#### FR-FB-005: Public feedback flag
The system SHALL support marking feedback as public or private.

#### FR-FB-006: Moderation for public feedback
Public feedback SHALL be subject to approval before it is eligible for public display.

#### FR-FB-007: Business response
The system SHALL support storing a business response to a feedback submission with a response timestamp.

#### FR-FB-008: Upload feedback attachments
The system SHALL allow users to upload image and video attachments associated with a feedback submission.

#### FR-FB-009: Attachment metadata
For each feedback attachment, the system SHALL store:
- attachment type (image or video)
- file name
- content type
- storage URL
- optional thumbnail URL
- optional file size
- optional duration (for video)

---

## 3. Data Integrity Rules

#### FR-DATA-001: Required business fields
Business records SHALL include a name and address.

#### FR-DATA-002: Cascading deletes
When a business is deleted, the system SHALL delete dependent marketing and feedback data associated with that business.

---

## 4. Planned Features (Future Requirements)

The following features are planned for future implementation. See [TODO.md](./TODO.md) for detailed task breakdowns.

### 4.1 Social Media Integration

#### FR-SOCIAL-001: Social Media Service Library (MVP-APP-001)
The system SHALL provide a client-side service library for managing social media platform defaults and templates.

**Status:** 🔴 Planned  
**Reference:** MVP-APP-001

#### FR-SOCIAL-002: Social Media API (MVP-APP-001)
The system SHALL provide an API for disseminating social media templates and defaults to mobile applications.

**Status:** 🔴 Planned  
**Reference:** MVP-APP-001

### 4.2 Location-Based Workflows

#### FR-WF-005: Market Arrival Workflow (MVP-APP-002)
The system SHALL detect when the user enters a different tourism market and automatically trigger a "MarketArrival" workflow.

**Status:** 🔴 Planned  
**Reference:** MVP-APP-002

- On app startup, retrieve current device location
- Compare current location with previously stored tourism market
- If tourism market has changed, trigger MarketArrival workflow
- Workflow definition in PlantUML (.puml) format

### 4.3 Theme Management

#### FR-THEME-001: Day and Night Theme Variants (MVP-APP-003)
The system SHALL support light and dark theme variants for both business and city themes.

**Status:** 🔴 Planned  
**Reference:** MVP-APP-003

- Support for light/dark variants in BusinessTheme and CityTheme entities
- Automatic detection of system day/night mode
- API endpoints returning both variants
- Mobile app automatic switching based on system settings

#### FR-UI-001: Dark Toolbar Styling (MVP-APP-004)
The system SHALL use a dark toolbar with transparent backgrounds and no borders on toolbar buttons.

**Status:** 🔴 Planned  
**Reference:** MVP-APP-004

### 4.4 Trip Planning

#### FR-TRIP-001: Trip Planning Tool (MVP-APP-005)
The system SHALL provide a Blazor web application for creating trip itineraries that can be retrieved in the mobile app via QR code.

**Status:** 🔴 Planned  
**Reference:** MVP-APP-005

- Blazor website for itinerary creation and editing
- PostgreSQL database schema for trip itineraries
- QR code generation for each itinerary
- QR code scanning capability in mobile app
- Secure access via QR code authentication

### 4.5 Marketing Administration

#### FR-ADMIN-001: Marketing Blazor Application (MVP-MARKETING-001)
The system SHALL provide a Blazor web application for managing marketing content.

**Status:** 🔴 Planned  
**Reference:** MVP-MARKETING-001

- CRUD operations for businesses, themes, coupons, menu items, and news
- Management interfaces for cities, city themes, tourism markets, and airports
- Authentication and authorization for admin access
- Dashboard views for marketing analytics

#### FR-DEPLOY-001: Marketing Website Deployment (MVP-MARKETING-002)
The Marketing Blazor application SHALL be deployed to Railway with automated CI/CD pipelines.

**Status:** 🔴 Planned  
**Reference:** MVP-MARKETING-002

### 4.6 Infrastructure

#### FR-DEPLOY-002: Social Media API Deployment (MVP-SUPPORT-001)
The Social Media API SHALL be deployed to Railway with automated CI/CD pipelines.

**Status:** 🔴 Planned  
**Reference:** MVP-SUPPORT-001

#### FR-DEPLOY-003: Cursor CLI Extraction (MVP-SUPPORT-002)
The PowerShell module (FWH.Prompts), CLI-related scripts, FWH.Documentation.Sync, and their documentation SHALL be moved to a new sibling folder and published as the `sharpninja/cursor-cli` GitHub repository. *(FWH.CLI.Agent was removed from this repo per MVP-SUPPORT-007.)*

**Status:** 🔴 Planned  
**Reference:** MVP-SUPPORT-002

#### FR-CODE-001: Code Analyzers (MVP-SUPPORT-003)
The solution SHALL have code analyzers enabled for all projects, including analyzers specific to project types (e.g. ASP.NET Core, Avalonia) and to key libraries (e.g. Entity Framework, HttpClient).

**Status:** 🔴 Planned  
**Reference:** MVP-SUPPORT-003

### 4.7 Legal Compliance

#### FR-LEGAL-001: Legal Website (MVP-LEGAL-001)
The system SHALL provide a website for hosting legal notices including EULA, Privacy Policy, and Corporate Contact information.

**Status:** 🔴 Planned  
**Reference:** MVP-LEGAL-001

- EULA (End User License Agreement) pages
- Privacy Policy pages
- Corporate contact information page
- Responsive design for mobile and desktop
- Document versioning system
- Search functionality for legal documents
- Accessibility compliance (WCAG standards)
- Multi-language support for international compliance

---

## 5. Change History

This section tracks all changes made to the Functional Requirements document.

### Version 1.3 (2026-01-27)

**Added:**
- FR-LOC-004: Resolve and display address for current location (Location API reverse geocoding; address shown even when no business)

**Changed:**
- FR-CODE-001: Code Analyzers status updated to ✅ Implemented (MVP-SUPPORT-003)
- Version and last updated date set to 1.3 / 2026-01-27

### Version 1.1 (2025-01-27)

**Added:**
- Section 4: Planned Features (Future Requirements)
  - FR-SOCIAL-001, FR-SOCIAL-002: Social Media Service Library and API (MVP-APP-001)
  - FR-WF-005: Market Arrival Workflow (MVP-APP-002)
  - FR-THEME-001: Day and Night Theme Variants (MVP-APP-003)
  - FR-UI-001: Dark Toolbar Styling (MVP-APP-004)
  - FR-TRIP-001: Trip Planning Tool (MVP-APP-005)
  - FR-ADMIN-001: Marketing Blazor Application (MVP-MARKETING-001)
  - FR-DEPLOY-001: Marketing Website Deployment (MVP-MARKETING-002)
  - FR-DEPLOY-002: Social Media API Deployment (MVP-SUPPORT-001)
  - FR-LEGAL-001: Legal Website (MVP-LEGAL-001)

**Changed:**
- Updated version from 1.0 to 1.1
- Updated last updated date to 2025-01-27
- Added references to TODO identifiers for all planned features

### Version 1.2 (2025-01-27)

**Added:**
- FR-DEPLOY-003: Cursor CLI Extraction (MVP-SUPPORT-002)
- FR-CODE-001: Code Analyzers (MVP-SUPPORT-003)

### Version 1.0 (2026-01-13)

**Initial Release:**
- Core functional requirements for location tracking
- Location-based workflow requirements
- Business discovery and marketing content requirements
- Feedback and attachment requirements
- Data integrity rules

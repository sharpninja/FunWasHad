# FunWasHad Documentation

Welcome to the FunWasHad project documentation. This site provides comprehensive documentation for the FunWasHad mobile application and backend services.

## Overview

FunWasHad is a location-based mobile application built with:
- **.NET 9** - Target framework
- **AvaloniaUI** - Cross-platform mobile UI
- **ASP.NET Core** - Backend REST APIs
- **PostgreSQL with PostGIS** - Backend database with spatial query support
- **SQLite** - Mobile local storage
- **.NET Aspire** - Service orchestration

## Recent Updates (2025-01-27)

- ✅ **PostGIS Spatial Queries:** Efficient nearby business queries using spatial GIST indexes
- ✅ **Pagination:** All list endpoints now support pagination with metadata
- ✅ **API Security:** API key authentication with HMAC-SHA256 request signing
- ✅ **Blob Storage:** File upload storage with persistent volumes
- ✅ **Resource Management:** Proper disposal patterns implemented throughout
- ✅ **Test Coverage:** 245+ tests, all passing

## Quick Start

### For New Developers

1. **[Technical Requirements](Project/Technical-Requirements.md)** - Start here to understand the complete technical specification
2. **[API Requirements](Project/Technical-Requirements.md#6-api-requirements)** - REST APIs and endpoints
3. **[Configuration Guides](configuration/Aspire_QuickReference.md)** - Set up your development environment
4. **[Documentation README](README.md)** - Navigation and next steps

### Setting Up Development Environment

1. **[Scripts README](https://github.com/sharpninja/FunWasHad/blob/develop/scripts/README.md)** - Automation scripts
2. **[PostgreSQL Configuration](configuration/PostgreSQL_LocalStorage_Configuration.md)** - Database setup
3. **[Aspire Quick Reference](configuration/Aspire_QuickReference.md)** - Service orchestration

## Documentation Structure

### 📋 Requirements
- [Technical Requirements](Project/Technical-Requirements.md) - Complete technical specification (100% implemented)
- [Functional Requirements](Project/Functional-Requirements.md) - Functional requirements specification

### 📡 API & Backend
- [API Requirements](Project/Technical-Requirements.md#6-api-requirements) - REST API reference
- [API Security](API-SECURITY.md) - API authentication and security
- [Blob Storage](BLOB-STORAGE.md) - File upload storage

### 🔄 Workflows
- [Workflows Index](workflows/index.md) - Location-triggered workflows
- [Fun Was Had](workflows/workflow.md) - Main workflow (get nearby businesses, photo, record fun)
- [New Location Detected](workflows/new-location.md) - Welcome flow for new locations

### ⚙️ Configuration
- [Aspire Quick Reference](configuration/Aspire_QuickReference.md) - Aspire orchestration
- [PostgreSQL Configuration](configuration/PostgreSQL_LocalStorage_Configuration.md) - Database setup

### 🧪 Testing
- [Test Remediation Summary](testing/Test-Remediation-Summary.md) - Test fixes and coverage

### 📝 Code Reviews
- [Code Review Report (2025-01-27)](reviews/Code-Review-Report-2025-01-27.md) - Implementation summary

## Key Features

### Location Tracking
- Real-time GPS tracking with platform-specific implementations
- Movement state detection (Stationary, Walking, Riding)
- Activity tracking with comprehensive statistics
- Speed calculation and unit conversions

### Workflow Engine
- PlantUML-based workflow definitions
- Location-triggered workflows
- Address-keyed workflow state management
- Automatic workflow resumption

### Backend APIs
- Location API for device tracking
- Marketing API for business data
- Feedback API with attachment support
- Complete REST API documentation

### Data Persistence
- PostgreSQL for backend (with Docker volumes)
- SQLite for mobile (offline-first)
- Automatic database migrations
- UTC timestamp handling

## Architecture

The application follows a clean architecture pattern:

- **Mobile Client** - AvaloniaUI with offline-first SQLite
- **Backend APIs** - ASP.NET Core REST APIs
- **Shared Libraries** - Common functionality (Location, Chat, Workflow, Imaging)
- **Data Layer** - Entity Framework Core with PostgreSQL/SQLite
- **Orchestration** - .NET Aspire for local development

## Requirements Compliance

✅ **100% Complete** - All 81 technical requirements implemented

- Architecture & Orchestration: 100%
- Solution Components: 100%
- Data Storage: 100%
- Location Tracking: 100%
- API Requirements: 100%
- Quality & Testing: 100%

See [Technical Requirements](Project/Technical-Requirements.md) for complete details.

## Getting Help

- **Documentation Issues:** Check the [README](README.md) for navigation
- **API Questions:** See [API Requirements](Project/Technical-Requirements.md#6-api-requirements)
- **Configuration:** Review [Configuration Guides](configuration/Aspire_QuickReference.md)
- **Code Reviews:** See [Code Reviews](reviews/Code-Review-Report-2025-01-27.md)

---

**Last Updated:** 2025-01-08
**Project:** FunWasHad
**Framework:** .NET 9

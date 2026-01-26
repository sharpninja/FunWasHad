# Copilot Instructions

## Project Context
- **Project:** FunWasHad
- **Target Framework:** .NET 9
- **Architecture:** Mobile (AvaloniaUI) + Backend (ASP.NET Core) with offline-first design
- Follow `.editorconfig` and existing patterns in the codebase.
- Prefer explicit types and meaningful names. Write clear, maintainable code.

## General Guidelines
- Use .NET 9 as the target framework for all projects.
- Public APIs must include XML documentation comments that reference requirement IDs (e.g., `/// <summary>TR-XXX: Description</summary>`).
- xUnit tests are required for all public methods.
- Every public feature must have appropriate Unit Tests and be part of an integration test suite.
- Incorporate all Technical Requirements (`./docs/Project/Technical-Requirements.md`) into the implementation.
- Keep Technical (`./docs/Project/Technical-Requirements.md`) and Functional (`./docs/Project/Functional-Requirements.md`) requirements up-to-date.

## Architecture Guidelines

### Solution Structure
- Multi-project solution separating mobile UI, shared libraries, and backend APIs.
- Mobile client: offline-first with local SQLite persistence using AvaloniaUI.
- Backend: REST APIs using ASP.NET Core.
- Shared libraries: Cross-cutting logic in `FWH.Common.*` projects (Location, Chat, Workflow, Imaging).
- Orchestration: Backend runnable locally via .NET Aspire with containerized dependencies (Docker).

### Design Patterns
- Use platform-agnostic libraries where available. Only create platform-wrappers when necessary.
- Implement Gang of Four MVC pattern using the `GPS.SimpleMVC` library.
- Use Entity Framework Core for data access:
  - PostgreSQL for backend persistence
  - SQLite for mobile persistence
- PlantUML Activity Diagrams define workflows. No hard-coded state transitions.

### Code Organization
- **Separation of Concerns:**
  - API controllers handle HTTP concerns only.
  - Domain/data models represent persisted entities.
  - Data access is encapsulated in DbContexts and repositories.
  - Workflow logic is encapsulated in workflow services/action handlers.
- **Project Structure:**
  - `FWH.MarketingApi`: Controllers, Models, Data folders
  - `FWH.Mobile`: Services, ViewModels, platform-specific implementations
  - `FWH.Mobile.Data`: EF/SQLite persistence and repository abstractions
  - `FWH.Common.*`: Shared functionality across projects

## Code Style

### EditorConfig Compliance
- Follow `.editorconfig` settings:
  - 4-space indentation for C# files
  - File-scoped namespaces (`csharp_style_namespace_declarations = file_scoped`)
  - Primary constructors preferred (`csharp_style_prefer_primary_constructors = true`)
  - Expression-bodied properties, accessors, indexers, and lambdas
  - Pascal case for types and non-field members
  - Interfaces begin with `I` prefix
  - System.* using directives appear first (`dotnet_sort_system_directives_first = true`)
  - Avoid `this.` qualification unless necessary

### Banned Libraries
- Use `FWH.Orchestrix.Mediator` instead of `MediatR`.
- Use `NSubstitute` instead of `Moq`.
- Use `EF Core` instead of `Dapper`.

## Testing Requirements
- xUnit framework for all unit tests.
- All public methods must have unit tests.
- Integration tests required for all public features.
- Use `NSubstitute` for mocking.

## Documentation Requirements
- XML documentation comments required for all public APIs.
- Reference requirement IDs in documentation (e.g., `TR-XXX`).
- Follow Microsoft documentation style.
- Keep Technical and Functional requirements documents updated.

## Agent Integration Context

### CLI Agent
- The project uses `CLI.md` for agent command execution.
- Prompts are defined in `scripts/modules/FWH.Prompts/prompts.md`.
- The FWH CLI Agent extension monitors `CLI.md` and executes prompts.
- Configuration is in `cli-agent.json`.

### Shared Context for Prompts
When generating prompts or agent instructions, include:
- Project: FunWasHad
- Follow .editorconfig and existing patterns in the codebase.
- Prefer explicit types and meaningful names. Write clear, maintainable code.

## Workflow Engine
- Workflows are defined using PlantUML Activity Diagrams.
- No hard-coded state transitions.
- Workflow logic in `FWH.Common.Workflow`.
- Action handlers implement `IWorkflowActionHandler`.

## Data Access
- Use Entity Framework Core exclusively.
- Automatic database migrations required.
- PostgreSQL for backend services.
- SQLite for mobile client.
- Repository pattern for data access abstraction.

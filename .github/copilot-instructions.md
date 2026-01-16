# Copilot Instructions

## General Guidelines
- Use .NET 9 as the target framework for all projects.
- Public APIs must include XML documentation comments that reference requirement IDs.
- xUnit tests are required for all public methods.
- Incorporate all Technical Requirements (./docs/technical-requirements.md) into the implementation.
- Keep Technical (./docs/technical-requirements.md) and Functional (./docs/functional-requirements.md) requirements up-to-date.

## Code Style
- Avoid using banned libraries: 
  - Use `Orchestrix.Mediator` instead of `MediatR`.
  - Use `NSubstitute` instead of `Moq`.
  - Use `EF Core` instead of `Dapper`.

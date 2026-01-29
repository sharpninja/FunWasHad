# Staging Workflow Status

This document is automatically generated and updated by the monitoring workflow.
It shows the status of the most recent staging.yml workflow run on the develop branch.

## Latest Run Information

- **Run Number**: #121
- **Run ID**: 21491952639
- **Status**: failure
- **Branch**: develop
- **Commit**: [`0cf821b`](https://github.com/sharpninja/FunWasHad/commit/0cf821b50e6b641dd34c707d8732a6c3e377480f)
- **Started**: 2026-01-29T19:31:20Z
- **Completed**: 2026-01-29T19:35:54Z
- **Run URL**: [View on GitHub](https://github.com/sharpninja/FunWasHad/actions/runs/21491952639)

---

## Job Status Summary

| Job Name | Status | Duration | Conclusion |
|----------|--------|----------|------------|
| Get Configuration | completed | 4s | success |
| Detect Changes | completed | 4s | success |
| Build and Test / Build and Test | completed | 172s | failure |
| Deploy to Railway / Deploy to Railway Staging | completed | 81s | success |
| Build Mobile Android | completed | 0s | skipped |
| Build Legal Web Docker Image | completed | 0s | skipped |
| Build Location API Docker Image | completed | 0s | skipped |
| Build Marketing API Docker Image | completed | 0s | skipped |
| Notify Deployment Status / Notify Deployment Status | completed | 3s | success |
| Create Android Release | completed | 0s | skipped |
| Reset force-build-all Flag | completed | 0s | skipped |

---

## Detailed Job Information

### Get Configuration

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61916518494`](https://github.com/sharpninja/FunWasHad/actions/runs/21491952639/job/61916518494)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Set Configuration Values | completed | success |
| Complete job | completed | success |


### Detect Changes

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61916518516`](https://github.com/sharpninja/FunWasHad/actions/runs/21491952639/job/61916518516)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Checkout repository | completed | success |
| Detect file changes | completed | success |
| Create no-build check run | completed | skipped |
| Post Checkout repository | completed | success |
| Complete job | completed | success |


### Build and Test / Build and Test

- **Status**: completed
- **Conclusion**: failure
- **Job ID**: [`61916531630`](https://github.com/sharpninja/FunWasHad/actions/runs/21491952639/job/61916531630)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Checkout repository | completed | success |
| Setup .NET 9 | completed | success |
| Cache .NET environment | completed | success |
| Restore dependencies | completed | failure |
| Build solution | completed | skipped |
| Run tests | completed | skipped |
| Restore dotnet tools | completed | skipped |
| Update coverage report | completed | skipped |
| Upload coverage report | completed | skipped |
| Publish Location API | completed | skipped |
| Publish Marketing API | completed | skipped |
| Upload Location API artifacts | completed | skipped |
| Upload Marketing API artifacts | completed | skipped |
| Publish Legal Web (MarkdownServer) | completed | skipped |
| Upload Legal Web artifacts | completed | skipped |
| Post Cache .NET environment | completed | success |
| Post Setup .NET 9 | completed | skipped |
| Post Checkout repository | completed | success |
| Complete job | completed | success |


### Deploy to Railway / Deploy to Railway Staging

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61916838638`](https://github.com/sharpninja/FunWasHad/actions/runs/21491952639/job/61916838638)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Initialize containers | completed | success |
| Checkout repository | completed | success |
| Get image tags | completed | success |
| Verify previous Docker builds succeeded | completed | success |
| Deploy Location API to Railway | completed | success |
| Deploy Marketing API to Railway | completed | success |
| Deploy Legal Web to Railway | completed | success |
| Wait for deployments | completed | success |
| Health check Location API | completed | success |
| Post Checkout repository | completed | success |
| Stop containers | completed | success |
| Complete job | completed | success |


### Build Mobile Android

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61916838707`](https://github.com/sharpninja/FunWasHad/actions/runs/21491952639/job/61916838707)


### Build Legal Web Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61916838749`](https://github.com/sharpninja/FunWasHad/actions/runs/21491952639/job/61916838749)


### Build Location API Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61916838958`](https://github.com/sharpninja/FunWasHad/actions/runs/21491952639/job/61916838958)


### Build Marketing API Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61916839009`](https://github.com/sharpninja/FunWasHad/actions/runs/21491952639/job/61916839009)


### Notify Deployment Status / Notify Deployment Status

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61916984663`](https://github.com/sharpninja/FunWasHad/actions/runs/21491952639/job/61916984663)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Check for skipped deployments | completed | success |
| Deployment Success | completed | success |
| Deployment Skipped | completed | skipped |
| Deployment Failed | completed | skipped |
| Complete job | completed | success |


### Create Android Release

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61916984792`](https://github.com/sharpninja/FunWasHad/actions/runs/21491952639/job/61916984792)


### Reset force-build-all Flag

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61916994595`](https://github.com/sharpninja/FunWasHad/actions/runs/21491952639/job/61916994595)


---

## Build and Deployment Errors

### Failed Job: Build and Test / Build and Test

<details>
<summary>View Error Log Excerpt</summary>

```
2026-01-29T19:34:20.6054348Z D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj : error NU1605: Warning As Error: Detected package downgrade: KubernetesClient from 18.0.5 to 17.0.14. Reference the package directly from the project to select a different version.  [D:\a\FunWasHad\FunWasHad\FunWasHad.sln]
2026-01-29T19:34:20.6057097Z D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj : error NU1605:  FWH.AppHost -> Aspire.Hosting.AppHost 13.1.0 -> KubernetesClient (>= 18.0.5)  [D:\a\FunWasHad\FunWasHad\FunWasHad.sln]
2026-01-29T19:34:20.6058992Z D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj : error NU1605:  FWH.AppHost -> KubernetesClient (>= 17.0.14) [D:\a\FunWasHad\FunWasHad\FunWasHad.sln]
2026-01-29T19:34:20.6116827Z   Failed to restore D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj (in 19.57 sec).
2026-01-29T19:34:20.7631329Z ##[error]Process completed with exit code 1.
```
</details>

---

## Analyzer Warnings

✅ No analyzer warnings detected in this run.

---


*Last updated: 2026-01-29 19:36:05 UTC*

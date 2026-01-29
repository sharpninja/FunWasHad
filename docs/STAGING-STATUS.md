# Staging Workflow Status

This document is automatically generated and updated by the monitoring workflow.
It shows the status of the most recent staging.yml workflow run on the develop branch.

## Latest Run Information

- **Run Number**: #122
- **Run ID**: 21491953612
- **Status**: failure
- **Branch**: develop
- **Commit**: [`0cf821b`](https://github.com/sharpninja/FunWasHad/commit/0cf821b50e6b641dd34c707d8732a6c3e377480f)
- **Started**: 2026-01-29T19:31:22Z
- **Completed**: 2026-01-29T19:34:43Z
- **Run URL**: [View on GitHub](https://github.com/sharpninja/FunWasHad/actions/runs/21491953612)

---

## Job Status Summary

| Job Name | Status | Duration | Conclusion |
|----------|--------|----------|------------|
| Detect Changes | completed | 7s | success |
| Get Configuration | completed | 4s | success |
| Build and Test / Build and Test | completed | 180s | failure |
| Build Legal Web Docker Image | completed | -1s | skipped |
| Build Marketing API Docker Image | completed | -1s | skipped |
| Build Mobile Android | completed | -1s | skipped |
| Build Location API Docker Image | completed | -1s | skipped |
| Deploy to Railway / Deploy to Railway Staging | completed | 0s | skipped |
| Notify Deployment Status / Notify Deployment Status | completed | 3s | success |
| Create Android Release | completed | 0s | skipped |
| Reset force-build-all Flag | completed | 0s | skipped |

---

## Detailed Job Information

### Detect Changes

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61916521159`](https://github.com/sharpninja/FunWasHad/actions/runs/21491953612/job/61916521159)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Checkout repository | completed | success |
| Detect file changes | completed | success |
| Create no-build check run | completed | skipped |
| Post Checkout repository | completed | success |
| Complete job | completed | success |


### Get Configuration

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61916521180`](https://github.com/sharpninja/FunWasHad/actions/runs/21491953612/job/61916521180)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Set Configuration Values | completed | success |
| Complete job | completed | success |


### Build and Test / Build and Test

- **Status**: completed
- **Conclusion**: failure
- **Job ID**: [`61916537301`](https://github.com/sharpninja/FunWasHad/actions/runs/21491953612/job/61916537301)

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


### Build Legal Web Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61916862236`](https://github.com/sharpninja/FunWasHad/actions/runs/21491953612/job/61916862236)


### Build Marketing API Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61916862288`](https://github.com/sharpninja/FunWasHad/actions/runs/21491953612/job/61916862288)


### Build Mobile Android

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61916862387`](https://github.com/sharpninja/FunWasHad/actions/runs/21491953612/job/61916862387)


### Build Location API Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61916862415`](https://github.com/sharpninja/FunWasHad/actions/runs/21491953612/job/61916862415)


### Deploy to Railway / Deploy to Railway Staging

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61916862743`](https://github.com/sharpninja/FunWasHad/actions/runs/21491953612/job/61916862743)


### Notify Deployment Status / Notify Deployment Status

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61916863086`](https://github.com/sharpninja/FunWasHad/actions/runs/21491953612/job/61916863086)

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
- **Job ID**: [`61916863312`](https://github.com/sharpninja/FunWasHad/actions/runs/21491953612/job/61916863312)


### Reset force-build-all Flag

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61916871623`](https://github.com/sharpninja/FunWasHad/actions/runs/21491953612/job/61916871623)


---

## Build and Deployment Errors

### Failed Job: Build and Test / Build and Test

<details>
<summary>View Error Log Excerpt</summary>

```
2026-01-29T19:34:32.8908889Z D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj : error NU1605: Warning As Error: Detected package downgrade: KubernetesClient from 18.0.5 to 17.0.14. Reference the package directly from the project to select a different version.  [D:\a\FunWasHad\FunWasHad\FunWasHad.sln]
2026-01-29T19:34:32.8912044Z D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj : error NU1605:  FWH.AppHost -> Aspire.Hosting.AppHost 13.1.0 -> KubernetesClient (>= 18.0.5)  [D:\a\FunWasHad\FunWasHad\FunWasHad.sln]
2026-01-29T19:34:32.8913535Z D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj : error NU1605:  FWH.AppHost -> KubernetesClient (>= 17.0.14) [D:\a\FunWasHad\FunWasHad\FunWasHad.sln]
2026-01-29T19:34:32.9081244Z   Failed to restore D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj (in 38.6 sec).
2026-01-29T19:34:33.2261711Z ##[error]Process completed with exit code 1.
```
</details>

---

## Analyzer Warnings

✅ No analyzer warnings detected in this run.

---


*Last updated: 2026-01-29 19:34:55 UTC*

# Staging Workflow Status

This document is automatically generated and updated by the monitoring workflow.
It shows the status of the most recent staging.yml workflow run on the develop branch.

## Latest Run Information

- **Run Number**: #125
- **Run ID**: 21492747841
- **Status**: failure
- **Branch**: develop
- **Commit**: [`38d50b3`](https://github.com/sharpninja/FunWasHad/commit/38d50b30c64a566f933de5edc3159ff13a60100f)
- **Started**: 2026-01-29T19:58:18Z
- **Completed**: 2026-01-29T20:04:45Z
- **Run URL**: [View on GitHub](https://github.com/sharpninja/FunWasHad/actions/runs/21492747841)

---

## Job Status Summary

| Job Name | Status | Duration | Conclusion |
|----------|--------|----------|------------|
| Detect Changes | completed | 7s | success |
| Get Configuration | completed | 3s | success |
| Build and Test / Build and Test | completed | 199s | failure |
| Build Legal Web Docker Image | completed | 0s | skipped |
| Build Mobile Android | completed | 0s | skipped |
| Deploy to Railway / Deploy to Railway Staging | completed | 72s | success |
| Build Location API Docker Image | completed | 0s | skipped |
| Build Marketing API Docker Image | completed | 0s | skipped |
| Notify Deployment Status / Notify Deployment Status | completed | 3s | success |
| Create Android Release | completed | 0s | skipped |
| Reset force-build-all Flag | completed | 0s | skipped |

---

## Detailed Job Information

### Detect Changes

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61919514830`](https://github.com/sharpninja/FunWasHad/actions/runs/21492747841/job/61919514830)

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
- **Job ID**: [`61919514835`](https://github.com/sharpninja/FunWasHad/actions/runs/21492747841/job/61919514835)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Set Configuration Values | completed | success |
| Complete job | completed | success |


### Build and Test / Build and Test

- **Status**: completed
- **Conclusion**: failure
- **Job ID**: [`61919530659`](https://github.com/sharpninja/FunWasHad/actions/runs/21492747841/job/61919530659)

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
- **Job ID**: [`61919888135`](https://github.com/sharpninja/FunWasHad/actions/runs/21492747841/job/61919888135)


### Build Mobile Android

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61919888142`](https://github.com/sharpninja/FunWasHad/actions/runs/21492747841/job/61919888142)


### Deploy to Railway / Deploy to Railway Staging

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61919888160`](https://github.com/sharpninja/FunWasHad/actions/runs/21492747841/job/61919888160)

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


### Build Location API Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61919888236`](https://github.com/sharpninja/FunWasHad/actions/runs/21492747841/job/61919888236)


### Build Marketing API Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61919888335`](https://github.com/sharpninja/FunWasHad/actions/runs/21492747841/job/61919888335)


### Notify Deployment Status / Notify Deployment Status

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61920023906`](https://github.com/sharpninja/FunWasHad/actions/runs/21492747841/job/61920023906)

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
- **Job ID**: [`61920024049`](https://github.com/sharpninja/FunWasHad/actions/runs/21492747841/job/61920024049)


### Reset force-build-all Flag

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61920036503`](https://github.com/sharpninja/FunWasHad/actions/runs/21492747841/job/61920036503)


---

## Build and Deployment Errors

### Failed Job: Build and Test / Build and Test

<details>
<summary>View Error Log Excerpt</summary>

```
2026-01-29T20:03:17.3159571Z D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj : error NU1605: Warning As Error: Detected package downgrade: KubernetesClient from 18.0.5 to 17.0.14. Reference the package directly from the project to select a different version.  [D:\a\FunWasHad\FunWasHad\FunWasHad.sln]
2026-01-29T20:03:17.3162014Z D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj : error NU1605:  FWH.AppHost -> Aspire.Hosting.AppHost 13.1.0 -> KubernetesClient (>= 18.0.5)  [D:\a\FunWasHad\FunWasHad\FunWasHad.sln]
2026-01-29T20:03:17.3163353Z D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj : error NU1605:  FWH.AppHost -> KubernetesClient (>= 17.0.14) [D:\a\FunWasHad\FunWasHad\FunWasHad.sln]
2026-01-29T20:03:17.3217766Z   Failed to restore D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj (in 18.96 sec).
2026-01-29T20:03:17.4850332Z ##[error]Process completed with exit code 1.
```
</details>

---

## Analyzer Warnings

✅ No analyzer warnings detected in this run.

---


*Last updated: 2026-01-29 20:05:00 UTC*

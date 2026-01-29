# Staging Workflow Status

This document is automatically generated and updated by the monitoring workflow.
It shows the status of the most recent staging.yml workflow run on the develop branch.

## Latest Run Information

- **Run Number**: #126
- **Run ID**: 21492749294
- **Status**: failure
- **Branch**: develop
- **Commit**: [`38d50b3`](https://github.com/sharpninja/FunWasHad/commit/38d50b30c64a566f933de5edc3159ff13a60100f)
- **Started**: 2026-01-29T19:58:20Z
- **Completed**: 2026-01-29T20:02:23Z
- **Run URL**: [View on GitHub](https://github.com/sharpninja/FunWasHad/actions/runs/21492749294)

---

## Job Status Summary

| Job Name | Status | Duration | Conclusion |
|----------|--------|----------|------------|
| Get Configuration | completed | 3s | success |
| Detect Changes | completed | 5s | success |
| Build and Test / Build and Test | completed | 224s | failure |
| Build Marketing API Docker Image | completed | 0s | skipped |
| Build Legal Web Docker Image | completed | 0s | skipped |
| Build Mobile Android | completed | 0s | skipped |
| Build Location API Docker Image | completed | 0s | skipped |
| Deploy to Railway / Deploy to Railway Staging | completed | -1s | skipped |
| Notify Deployment Status / Notify Deployment Status | completed | 3s | success |
| Create Android Release | completed | 0s | skipped |
| Reset force-build-all Flag | completed | 0s | skipped |

---

## Detailed Job Information

### Get Configuration

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61919357705`](https://github.com/sharpninja/FunWasHad/actions/runs/21492749294/job/61919357705)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Set Configuration Values | completed | success |
| Complete job | completed | success |


### Detect Changes

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61919357712`](https://github.com/sharpninja/FunWasHad/actions/runs/21492749294/job/61919357712)

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
- **Job ID**: [`61919371961`](https://github.com/sharpninja/FunWasHad/actions/runs/21492749294/job/61919371961)

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


### Build Marketing API Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61919770571`](https://github.com/sharpninja/FunWasHad/actions/runs/21492749294/job/61919770571)


### Build Legal Web Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61919770598`](https://github.com/sharpninja/FunWasHad/actions/runs/21492749294/job/61919770598)


### Build Mobile Android

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61919770685`](https://github.com/sharpninja/FunWasHad/actions/runs/21492749294/job/61919770685)


### Build Location API Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61919770750`](https://github.com/sharpninja/FunWasHad/actions/runs/21492749294/job/61919770750)


### Deploy to Railway / Deploy to Railway Staging

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61919770993`](https://github.com/sharpninja/FunWasHad/actions/runs/21492749294/job/61919770993)


### Notify Deployment Status / Notify Deployment Status

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61919771272`](https://github.com/sharpninja/FunWasHad/actions/runs/21492749294/job/61919771272)

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
- **Job ID**: [`61919771462`](https://github.com/sharpninja/FunWasHad/actions/runs/21492749294/job/61919771462)


### Reset force-build-all Flag

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61919781488`](https://github.com/sharpninja/FunWasHad/actions/runs/21492749294/job/61919781488)


---

## Build and Deployment Errors

### Failed Job: Build and Test / Build and Test

<details>
<summary>View Error Log Excerpt</summary>

```
2026-01-29T20:02:11.9846651Z D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj : error NU1605: Warning As Error: Detected package downgrade: KubernetesClient from 18.0.5 to 17.0.14. Reference the package directly from the project to select a different version.  [D:\a\FunWasHad\FunWasHad\FunWasHad.sln]
2026-01-29T20:02:11.9849007Z D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj : error NU1605:  FWH.AppHost -> Aspire.Hosting.AppHost 13.1.0 -> KubernetesClient (>= 18.0.5)  [D:\a\FunWasHad\FunWasHad\FunWasHad.sln]
2026-01-29T20:02:11.9850724Z D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj : error NU1605:  FWH.AppHost -> KubernetesClient (>= 17.0.14) [D:\a\FunWasHad\FunWasHad\FunWasHad.sln]
2026-01-29T20:02:11.9911518Z   Failed to restore D:\a\FunWasHad\FunWasHad\src\FWH.AppHost\FWH.AppHost.csproj (in 24.97 sec).
2026-01-29T20:02:12.1468677Z ##[error]Process completed with exit code 1.
```
</details>

---

## Analyzer Warnings

✅ No analyzer warnings detected in this run.

---


*Last updated: 2026-01-29 20:02:44 UTC*

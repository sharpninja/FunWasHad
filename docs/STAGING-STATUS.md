# Staging Workflow Status

This document is automatically generated and updated by the monitoring workflow.
It shows the status of the most recent staging.yml workflow run on the develop branch.

## Latest Run Information

- **Run Number**: #112
- **Run ID**: 21450355592
- **Status**: success
- **Branch**: develop
- **Commit**: [`f531713`](https://github.com/sharpninja/FunWasHad/commit/f5317134637c89dee8dcb2da0609b0774a597078)
- **Started**: 2026-01-28T18:20:24Z
- **Completed**: 2026-01-28T18:32:57Z
- **Run URL**: [View on GitHub](https://github.com/sharpninja/FunWasHad/actions/runs/21450355592)

---

## Job Status Summary

| Job Name | Status | Duration | Conclusion |
|----------|--------|----------|------------|
| Get Configuration | completed | 3s | success |
| Detect Changes | completed | 7s | success |
| Build and Test / Build and Test | completed | 269s | success |
| Build Location API Docker Image / Build location-api Docker Image | completed | 39s | success |
| Build Mobile Android / Build Mobile Android | completed | 461s | success |
| Build Legal Web Docker Image | completed | 0s | skipped |
| Build Marketing API Docker Image | completed | 0s | skipped |
| Deploy to Railway / Deploy to Railway Staging | completed | 69s | success |
| Notify Deployment Status / Notify Deployment Status | completed | 2s | success |
| Create Android Release | completed | 0s | skipped |

---

## Detailed Job Information

### Get Configuration

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61777225202`](https://github.com/sharpninja/FunWasHad/actions/runs/21450355592/job/61777225202)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Set Configuration Values | completed | success |
| Complete job | completed | success |


### Detect Changes

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61777225224`](https://github.com/sharpninja/FunWasHad/actions/runs/21450355592/job/61777225224)

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
- **Conclusion**: success
- **Job ID**: [`61777242703`](https://github.com/sharpninja/FunWasHad/actions/runs/21450355592/job/61777242703)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Checkout repository | completed | success |
| Setup .NET 9 | completed | success |
| Cache .NET environment | completed | success |
| Restore dependencies | completed | success |
| Build solution | completed | success |
| Run tests | completed | success |
| Restore dotnet tools | completed | success |
| Update coverage report | completed | success |
| Upload coverage report | completed | success |
| Publish Location API | completed | success |
| Publish Marketing API | completed | success |
| Upload Location API artifacts | completed | success |
| Upload Marketing API artifacts | completed | success |
| Publish Legal Web (MarkdownServer) | completed | success |
| Upload Legal Web artifacts | completed | success |
| Post Cache .NET environment | completed | success |
| Post Setup .NET 9 | completed | success |
| Post Checkout repository | completed | success |
| Complete job | completed | success |


### Build Location API Docker Image / Build location-api Docker Image

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61777754886`](https://github.com/sharpninja/FunWasHad/actions/runs/21450355592/job/61777754886)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Checkout repository | completed | success |
| Download location-api artifacts | completed | success |
| Set up Docker Buildx | completed | success |
| Log in to GitHub Container Registry | completed | success |
| Extract metadata for Docker | completed | success |
| Build and push Docker image | completed | success |
| Post Build and push Docker image | completed | success |
| Post Log in to GitHub Container Registry | completed | success |
| Post Set up Docker Buildx | completed | success |
| Post Checkout repository | completed | success |
| Complete job | completed | success |


### Build Mobile Android / Build Mobile Android

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61777754896`](https://github.com/sharpninja/FunWasHad/actions/runs/21450355592/job/61777754896)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Checkout repository | completed | success |
| Setup .NET 9 | completed | success |
| Setup Java 17 | completed | success |
| Cache .NET environment | completed | success |
| Cache .NET workloads | completed | success |
| Install .NET MAUI Android workload | completed | success |
| Restore dependencies | completed | success |
| Build Android app | completed | success |
| Publish Android APK | completed | success |
| Find APK file | completed | success |
| Upload APK artifact | completed | success |
| Post Cache .NET workloads | completed | success |
| Post Cache .NET environment | completed | success |
| Post Setup Java 17 | completed | success |
| Post Setup .NET 9 | completed | success |
| Post Checkout repository | completed | success |
| Complete job | completed | success |


### Build Legal Web Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61777755162`](https://github.com/sharpninja/FunWasHad/actions/runs/21450355592/job/61777755162)


### Build Marketing API Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61777755172`](https://github.com/sharpninja/FunWasHad/actions/runs/21450355592/job/61777755172)


### Deploy to Railway / Deploy to Railway Staging

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61777834520`](https://github.com/sharpninja/FunWasHad/actions/runs/21450355592/job/61777834520)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Initialize containers | completed | success |
| Checkout repository | completed | success |
| Get image tags | completed | success |
| Verify previous Docker builds succeeded | completed | success |
| Deploy Location API to Railway | completed | success |
| Deploy Marketing API to Railway | completed | skipped |
| Deploy Legal Web to Railway | completed | skipped |
| Wait for deployments | completed | success |
| Health check Location API | completed | success |
| Post Checkout repository | completed | success |
| Stop containers | completed | success |
| Complete job | completed | success |


### Notify Deployment Status / Notify Deployment Status

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61778613249`](https://github.com/sharpninja/FunWasHad/actions/runs/21450355592/job/61778613249)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Check for skipped deployments | completed | success |
| Deployment Success | completed | skipped |
| Deployment Skipped | completed | success |
| Deployment Failed | completed | skipped |
| Complete job | completed | success |


### Create Android Release

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61778613304`](https://github.com/sharpninja/FunWasHad/actions/runs/21450355592/job/61778613304)


---

## Build and Deployment Errors

✅ No build or deployment errors detected in this run.

---

## Analyzer Warnings

| Warning Code | Count | Description |
|--------------|-------|-------------|
| CA1848 | 1174 | For improved performance, use the LoggerMessage delegates instead of calling 'Lo |
| CA1031 | 314 | Modify 'GetAppliedMigrationsAsync' to catch a more specific allowed exception ty |
| CA1056 | 72 | Change the type of property 'CityMarketingInfoEntity.LogoUrl' from 'string' to ' |
| CA1002 | 48 | Change 'List<NodeEntity>' in 'WorkflowDefinitionEntity.Nodes' to use 'Collection |
| CA2007 | 46 | Consider calling ConfigureAwait on the awaited task (https://learn.microsoft.com |
| CA1305 | 38 | The behavior of 'int.ToString()' could vary based on the current user's locale s |
| CA1812 | 36 | 'UploadFeedbackAttachmentHandler.AttachmentCreatedDto' is an internal class that |
| CA1860 | 34 | Prefer comparing 'Count' to 0 rather than using 'Any()', both for clarity and fo |
| CA1003 | 32 | Change the event 'ChoiceSelected' to replace the type 'System.EventHandler<FWH.C |
| CA1849 | 30 | 'WebApplication.Run(string?)' synchronously blocks. Await 'WebApplication.RunAsy |
| CS8604 | 28 | Possible null reference argument for parameter 'nodeId' in 'void WorkflowControl |
| CA1852 | 28 | Type 'AttachmentCreatedDto' can be sealed because it has no subtypes in its cont |
| CA1819 | 28 | Properties should not return arrays (https://learn.microsoft.com/dotnet/fundamen |
| CA1708 | 28 | Names of 'Members' and 'FWH.Common.Chat.ViewModels.ChoicePayload.Prompt, FWH.Com |
| CA1051 | 28 | Do not declare visible instance fields (https://learn.microsoft.com/dotnet/funda |
| CA1826 | 24 | Do not use Enumerable methods on indexable collections. Instead use the collecti |
| CA1308 | 22 | In method 'ParseArgs', replace the call to 'ToLowerInvariant' with 'ToUpperInvar |
| CA2227 | 20 | Change 'Nodes' to be read-only by removing the property setter (https://learn.mi |
| CA2000 | 20 | Call System.IDisposable.Dispose on object created by 'new SKSvg()' before all re |
| CA1822 | 20 | Member 'ResolveChoiceValue' does not access instance data and can be marked as s |
| CA1054 | 18 | Change the type of parameter 'storageUrl' of method 'IBlobStorageService.DeleteA |
| CA1861 | 16 | Prefer 'static readonly' fields over constant array arguments if the called meth |
| CA1845 | 14 | Use span-based 'string.Concat' and 'AsSpan' instead of 'Substring' (https://lear |
| CS8618 | 12 | Non-nullable property 'Platform' must contain a non-null value when exiting cons |
| CA1311 | 12 | Specify a culture or use an invariant version to avoid implicit dependency on cu |
| CA1310 | 12 | The behavior of 'string.StartsWith(string)' could vary based on the current user |
| CA1304 | 12 | The behavior of 'string.ToLower()' could vary based on the current user's locale |
| CA1001 | 12 | Type 'RateLimitedLocationService' owns disposable field(s) '_rateLimiter' but is |
| CA1859 | 10 | Change type of variable 'parameters' from 'System.Collections.Generic.IDictionar |
| CA1866 | 8 | Use 'string.StartsWith(char)' instead of 'string.StartsWith(string)' when you ha |
| CA1515 | 6 | Because an application's API isn't typically referenced from outside the assembl |
| CA2254 | 4 | The logging message template should not vary between calls to 'LoggerExtensions. |
| CA2100 | 4 | Review if the query string passed to 'NpgsqlCommand.NpgsqlCommand(string? cmdTex |
| CA1847 | 4 | Use 'string.Contains(char)' instead of 'string.Contains(string)' when searching  |
| CA1510 | 4 | Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exc |
| CS0168 | 2 | The variable 'ex' is declared but never used [D:\a\FunWasHad\FunWasHad\tests\FWH |
| CA1016 | 2 | Mark assemblies with assembly version (https://learn.microsoft.com/dotnet/fundam |

---


*Last updated: 2026-01-28 18:33:30 UTC*

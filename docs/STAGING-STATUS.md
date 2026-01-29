# Staging Workflow Status

This document is automatically generated and updated by the monitoring workflow.
It shows the status of the most recent staging.yml workflow run on the develop branch.

## Latest Run Information

- **Run Number**: #139
- **Run ID**: 21495087304
- **Status**: success
- **Branch**: develop
- **Commit**: [`d2e1404`](https://github.com/sharpninja/FunWasHad/commit/d2e1404378f387c5e20c7879308a17198c90b170)
- **Started**: 2026-01-29T21:19:11Z
- **Completed**: 2026-01-29T21:32:14Z
- **Run URL**: [View on GitHub](https://github.com/sharpninja/FunWasHad/actions/runs/21495087304)

---

## Job Status Summary

| Job Name | Status | Duration | Conclusion |
|----------|--------|----------|------------|
| Detect Changes | completed | 10s | success |
| Get Configuration | completed | 3s | success |
| Build and Test / Build and Test | completed | 317s | success |
| Build Legal Web Docker Image / Build legal-web Docker Image | completed | 22s | success |
| Build Location API Docker Image / Build location-api Docker Image | completed | 30s | success |
| Build Mobile Android / Build Mobile Android | completed | 420s | success |
| Build Marketing API Docker Image / Build marketing-api Docker Image | completed | 21s | success |
| Deploy to Railway / Deploy to Railway Staging | completed | 77s | success |
| Create Android Release / Create Android Release Candidate | completed | 18s | success |
| Notify Deployment Status / Notify Deployment Status | completed | 2s | success |
| Reset force-build-all Flag | completed | -1s | skipped |

---

## Detailed Job Information

### Detect Changes

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61927700450`](https://github.com/sharpninja/FunWasHad/actions/runs/21495087304/job/61927700450)

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
- **Job ID**: [`61927700481`](https://github.com/sharpninja/FunWasHad/actions/runs/21495087304/job/61927700481)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Set Configuration Values | completed | success |
| Complete job | completed | success |


### Build and Test / Build and Test

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61927721030`](https://github.com/sharpninja/FunWasHad/actions/runs/21495087304/job/61927721030)

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


### Build Legal Web Docker Image / Build legal-web Docker Image

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61928254402`](https://github.com/sharpninja/FunWasHad/actions/runs/21495087304/job/61928254402)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Checkout repository | completed | success |
| Download legal-web artifacts | completed | success |
| Set up Docker Buildx | completed | success |
| Log in to GitHub Container Registry | completed | success |
| Extract metadata for Docker | completed | success |
| Build and push Docker image | completed | success |
| Post Build and push Docker image | completed | success |
| Post Log in to GitHub Container Registry | completed | success |
| Post Set up Docker Buildx | completed | success |
| Post Checkout repository | completed | success |
| Complete job | completed | success |


### Build Location API Docker Image / Build location-api Docker Image

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61928254408`](https://github.com/sharpninja/FunWasHad/actions/runs/21495087304/job/61928254408)

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
- **Job ID**: [`61928254411`](https://github.com/sharpninja/FunWasHad/actions/runs/21495087304/job/61928254411)

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


### Build Marketing API Docker Image / Build marketing-api Docker Image

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61928254412`](https://github.com/sharpninja/FunWasHad/actions/runs/21495087304/job/61928254412)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Checkout repository | completed | success |
| Download marketing-api artifacts | completed | success |
| Set up Docker Buildx | completed | success |
| Log in to GitHub Container Registry | completed | success |
| Extract metadata for Docker | completed | success |
| Build and push Docker image | completed | success |
| Post Build and push Docker image | completed | success |
| Post Log in to GitHub Container Registry | completed | success |
| Post Set up Docker Buildx | completed | success |
| Post Checkout repository | completed | success |
| Complete job | completed | success |


### Deploy to Railway / Deploy to Railway Staging

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61928310283`](https://github.com/sharpninja/FunWasHad/actions/runs/21495087304/job/61928310283)

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


### Create Android Release / Create Android Release Candidate

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61928945748`](https://github.com/sharpninja/FunWasHad/actions/runs/21495087304/job/61928945748)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Checkout repository | completed | success |
| Download APK artifact | completed | success |
| Generate release candidate tag | completed | success |
| Find APK file | completed | success |
| Create GitHub Release Candidate | completed | success |
| Post Checkout repository | completed | success |
| Complete job | completed | success |


### Notify Deployment Status / Notify Deployment Status

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61928979070`](https://github.com/sharpninja/FunWasHad/actions/runs/21495087304/job/61928979070)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Check for skipped deployments | completed | success |
| Deployment Success | completed | success |
| Deployment Skipped | completed | skipped |
| Deployment Failed | completed | skipped |
| Complete job | completed | success |


### Reset force-build-all Flag

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61928986989`](https://github.com/sharpninja/FunWasHad/actions/runs/21495087304/job/61928986989)


---

## Build and Deployment Errors

✅ No build or deployment errors detected in this run.

---

## Analyzer Warnings

| Warning Code | Count | Description |
|--------------|-------|-------------|
| CA1848 | 1222 | For improved performance, use the LoggerMessage delegates instead of calling 'Lo |
| CA1031 | 334 | Modify 'HandleAsync' to catch a more specific allowed exception type, or rethrow |
| CA1056 | 72 | Change the type of property 'UploadFeedbackAttachmentResponse.StorageUrl' from ' |
| CA2007 | 50 | Consider calling ConfigureAwait on the awaited task (https://learn.microsoft.com |
| CA1002 | 48 | Change 'List<CouponDto>' in 'GetBusinessCouponsResponse.Coupons' to use 'Collect |
| CA1305 | 38 | The behavior of 'int.ToString()' could vary based on the current user's locale s |
| CS8604 | 36 | Possible null reference argument for parameter 'nodeId' in 'void WorkflowControl |
| CA1812 | 36 | 'UploadFeedbackAttachmentHandler.AttachmentCreatedDto' is an internal class that |
| CA1003 | 36 | Change the event 'ChoiceSelected' to replace the type 'System.EventHandler<FWH.C |
| CA1860 | 34 | Prefer comparing 'Count' to 0 rather than using 'Any()', both for clarity and fo |
| CA1849 | 30 | 'WebApplication.Run(string?)' synchronously blocks. Await 'WebApplication.RunAsy |
| CA1852 | 28 | Type 'AttachmentCreatedDto' can be sealed because it has no subtypes in its cont |
| CA1819 | 28 | Properties should not return arrays (https://learn.microsoft.com/dotnet/fundamen |
| CA1708 | 28 | Names of 'Members' and 'FWH.Common.Chat.ViewModels.ChoicePayload.Prompt, FWH.Com |
| CA1051 | 28 | Do not declare visible instance fields (https://learn.microsoft.com/dotnet/funda |
| CA1826 | 24 | Do not use Enumerable methods on indexable collections. Instead use the collecti |
| CA1308 | 22 | In method 'ParseArgs', replace the call to 'ToLowerInvariant' with 'ToUpperInvar |
| CA2227 | 20 | Change 'Transitions' to be read-only by removing the property setter (https://le |
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
| CS0108 | 4 | 'ConfirmationDialog.Title' hides inherited member 'Window.Title'. Use the new ke |
| CA2254 | 4 | The logging message template should not vary between calls to 'LoggerExtensions. |
| CA2234 | 4 | Modify 'LocationApiHeartbeatService.CheckApiAvailabilityAsync(CancellationToken) |
| CA2213 | 4 | 'LocationApiHeartbeatService' contains field '_httpClient' that is of IDisposabl |
| CA2208 | 4 | Method .ctor passes 'Value' as the paramName argument to a ArgumentNullException |
| CA2100 | 4 | Review if the query string passed to 'NpgsqlCommand.NpgsqlCommand(string? cmdTex |
| CA1847 | 4 | Use 'string.Contains(char)' instead of 'string.Contains(string)' when searching  |
| CA1805 | 4 | Member '_isProgrammaticViewportUpdate' is explicitly initialized to its default  |
| CA1510 | 4 | Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exc |
| CA1062 | 4 | In externally visible method 'LocationApiHeartbeatService.LocationApiHeartbeatSe |
| CS0618 | 2 | 'PostgreSqlBuilder.PostgreSqlBuilder()' is obsolete: 'This parameterless constru |
| CS0168 | 2 | The variable 'ex' is declared but never used [D:\a\FunWasHad\FunWasHad\tests\FWH |
| CA1016 | 2 | Mark assemblies with assembly version (https://learn.microsoft.com/dotnet/fundam |

---


*Last updated: 2026-01-29 21:32:42 UTC*

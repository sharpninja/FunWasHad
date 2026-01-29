# Staging Workflow Status

This document is automatically generated and updated by the monitoring workflow.
It shows the status of the most recent staging.yml workflow run on the develop branch.

## Latest Run Information

- **Run Number**: #119
- **Run ID**: 21487171394
- **Status**: success
- **Branch**: develop
- **Commit**: [`b38d2ae`](https://github.com/sharpninja/FunWasHad/commit/b38d2ae5c4bc19905471d51e35345125afd787f1)
- **Started**: 2026-01-29T16:56:54Z
- **Completed**: 2026-01-29T17:04:34Z
- **Run URL**: [View on GitHub](https://github.com/sharpninja/FunWasHad/actions/runs/21487171394)

---

## Job Status Summary

| Job Name | Status | Duration | Conclusion |
|----------|--------|----------|------------|
| Detect Changes | completed | 5s | success |
| Get Configuration | completed | 4s | success |
| Build and Test / Build and Test | completed | 264s | success |
| Build Marketing API Docker Image / Build marketing-api Docker Image | completed | 72s | success |
| Build Location API Docker Image / Build location-api Docker Image | completed | 101s | success |
| Build Legal Web Docker Image | completed | 0s | skipped |
| Build Mobile Android | completed | -1s | skipped |
| Deploy to Railway / Deploy to Railway Staging | completed | 71s | success |
| Create Android Release | completed | -1s | skipped |
| Notify Deployment Status / Notify Deployment Status | completed | 3s | success |
| Reset force-build-all Flag | completed | 0s | skipped |

---

## Detailed Job Information

### Detect Changes

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61899521626`](https://github.com/sharpninja/FunWasHad/actions/runs/21487171394/job/61899521626)

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
- **Job ID**: [`61899521649`](https://github.com/sharpninja/FunWasHad/actions/runs/21487171394/job/61899521649)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Set Configuration Values | completed | success |
| Complete job | completed | success |


### Build and Test / Build and Test

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61899537103`](https://github.com/sharpninja/FunWasHad/actions/runs/21487171394/job/61899537103)

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


### Build Marketing API Docker Image / Build marketing-api Docker Image

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61900072669`](https://github.com/sharpninja/FunWasHad/actions/runs/21487171394/job/61900072669)

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


### Build Location API Docker Image / Build location-api Docker Image

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61900072696`](https://github.com/sharpninja/FunWasHad/actions/runs/21487171394/job/61900072696)

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


### Build Legal Web Docker Image

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61900073011`](https://github.com/sharpninja/FunWasHad/actions/runs/21487171394/job/61900073011)


### Build Mobile Android

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61900073549`](https://github.com/sharpninja/FunWasHad/actions/runs/21487171394/job/61900073549)


### Deploy to Railway / Deploy to Railway Staging

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61900276558`](https://github.com/sharpninja/FunWasHad/actions/runs/21487171394/job/61900276558)

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
| Deploy Legal Web to Railway | completed | skipped |
| Wait for deployments | completed | success |
| Health check Location API | completed | success |
| Post Checkout repository | completed | success |
| Stop containers | completed | success |
| Complete job | completed | success |


### Create Android Release

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61900423726`](https://github.com/sharpninja/FunWasHad/actions/runs/21487171394/job/61900423726)


### Notify Deployment Status / Notify Deployment Status

- **Status**: completed
- **Conclusion**: success
- **Job ID**: [`61900423839`](https://github.com/sharpninja/FunWasHad/actions/runs/21487171394/job/61900423839)

#### Steps:

| Step | Status | Conclusion |
|------|--------|------------|
| Set up job | completed | success |
| Check for skipped deployments | completed | success |
| Deployment Success | completed | skipped |
| Deployment Skipped | completed | success |
| Deployment Failed | completed | skipped |
| Complete job | completed | success |


### Reset force-build-all Flag

- **Status**: completed
- **Conclusion**: skipped
- **Job ID**: [`61900434829`](https://github.com/sharpninja/FunWasHad/actions/runs/21487171394/job/61900434829)


---

## Build and Deployment Errors

✅ No build or deployment errors detected in this run.

---

## Analyzer Warnings

| Warning Code | Count | Description |
|--------------|-------|-------------|
| CA1848 | 674 | For improved performance, use the LoggerMessage delegates instead of calling 'Lo |
| CA1031 | 172 | Modify 'GetAppliedMigrationsAsync' to catch a more specific allowed exception ty |
| CA2007 | 48 | Consider calling ConfigureAwait on the awaited task (https://learn.microsoft.com |
| CA1056 | 36 | Change the type of property 'CityMarketingInfoEntity.LogoUrl' from 'string' to ' |
| CA1849 | 28 | 'WebApplication.Run(string?)' synchronously blocks. Await 'WebApplication.RunAsy |
| CA1002 | 24 | Change 'List<NodeEntity>' in 'WorkflowDefinitionEntity.Nodes' to use 'Collection |
| CA1305 | 20 | The behavior of 'int.ToString()' could vary based on the current user's locale s |
| CS8604 | 18 | Possible null reference argument for parameter 'nodeId' in 'void WorkflowControl |
| CA1860 | 18 | Prefer comparing 'Count' to 0 rather than using 'Any()', both for clarity and fo |
| CA1812 | 18 | 'SubmitFeedbackHandler.FeedbackCreatedDto' is an internal class that is apparent |
| CA1003 | 18 | Change the event 'ChoiceSelected' to replace the type 'System.EventHandler<FWH.C |
| CA1852 | 14 | Type 'FeedbackCreatedDto' can be sealed because it has no subtypes in its contai |
| CA1819 | 14 | Properties should not return arrays (https://learn.microsoft.com/dotnet/fundamen |
| CA1708 | 14 | Names of 'Members' and 'FWH.Common.Chat.ViewModels.ChoicePayload.Prompt, FWH.Com |
| CA1051 | 14 | Do not declare visible instance fields (https://learn.microsoft.com/dotnet/funda |
| CA1826 | 12 | Do not use Enumerable methods on indexable collections. Instead use the collecti |
| CA1308 | 12 | In method 'ParseArgs', replace the call to 'ToLowerInvariant' with 'ToUpperInvar |
| CA1054 | 12 | Change the type of parameter 'storageUrl' of method 'IBlobStorageService.GetAsyn |
| CA2227 | 10 | Change 'Nodes' to be read-only by removing the property setter (https://learn.mi |
| CA2000 | 10 | Call System.IDisposable.Dispose on object created by 'new SKSvg()' before all re |
| CA1845 | 10 | Use span-based 'string.Concat' and 'AsSpan' instead of 'Substring' (https://lear |
| CA1822 | 10 | Member 'ResolveChoiceValue' does not access instance data and can be marked as s |
| CA1311 | 10 | Specify a culture or use an invariant version to avoid implicit dependency on cu |
| CA1304 | 10 | The behavior of 'string.ToLower()' could vary based on the current user's locale |
| CA1861 | 8 | Prefer 'static readonly' fields over constant array arguments if the called meth |
| CS8618 | 6 | Non-nullable property 'Platform' must contain a non-null value when exiting cons |
| CA1515 | 6 | Because an application's API isn't typically referenced from outside the assembl |
| CA1310 | 6 | The behavior of 'string.StartsWith(string)' could vary based on the current user |
| CA1001 | 6 | Type 'RateLimitedLocationService' owns disposable field(s) '_rateLimiter' but is |
| CA2100 | 4 | Review if the query string passed to 'NpgsqlCommand.NpgsqlCommand(string? cmdTex |
| CA1866 | 4 | Use 'string.StartsWith(char)' instead of 'string.StartsWith(string)' when you ha |
| CA1859 | 4 | Change type of variable 'parameters' from 'System.Collections.Generic.IDictionar |
| CA1847 | 4 | Use 'string.Contains(char)' instead of 'string.Contains(string)' when searching  |
| CS0168 | 2 | The variable 'ex' is declared but never used [D:\a\FunWasHad\FunWasHad\tests\FWH |
| CS0108 | 2 | 'ConfirmationDialog.Title' hides inherited member 'Window.Title'. Use the new ke |
| CA2254 | 2 | The logging message template should not vary between calls to 'LoggerExtensions. |
| CA2234 | 2 | Modify 'LocationApiHeartbeatService.CheckApiAvailabilityAsync(CancellationToken) |
| CA2213 | 2 | 'LocationApiHeartbeatService' contains field '_httpClient' that is of IDisposabl |
| CA2208 | 2 | Method .ctor passes 'Value' as the paramName argument to a ArgumentNullException |
| CA1805 | 2 | Member '_isProgrammaticViewportUpdate' is explicitly initialized to its default  |
| CA1510 | 2 | Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exc |
| CA1062 | 2 | In externally visible method 'LocationApiHeartbeatService.LocationApiHeartbeatSe |
| CA1016 | 2 | Mark assemblies with assembly version (https://learn.microsoft.com/dotnet/fundam |

---


*Last updated: 2026-01-29 17:04:54 UTC*

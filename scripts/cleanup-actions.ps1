<#
.SYNOPSIS
  Delete failed, cancelled, no-build tagged, and excess successful GitHub Actions workflow runs in a repository, keeping only the most recent failed run per workflow.

.DESCRIPTION
  Uses the GitHub CLI (`gh`) to enumerate workflow runs for a repository, finds runs whose conclusion is "failure" or "cancelled",
  or runs tagged with "no-build" check run, and deletes them according to the following rules:
  - Failed runs: Keeps the most recent failed run per workflow (grouped by workflow_id), deletes all others.
  - Cancelled runs: Deletes ALL cancelled runs (none are kept).
  - No-build runs: Deletes ALL runs tagged with "no-build" check run (none are kept).
  - Successful runs (when -KeepOnlyThree is specified): Keeps only the first 3 most recent successful runs per workflow, deletes all others.
  By default the script prompts before performing deletions. Use -Force to skip the prompt, or -WhatIf to simulate.

.PARAMETER Repo
  The repository in owner/repo format. If omitted the script will attempt to detect the current repository via `gh repo view`.

.PARAMETER Force
  When specified, do not prompt before deleting; proceed with deletions.

.PARAMETER WhatIf
  When specified, do not perform deletions — only show what would be deleted.

.PARAMETER CleanupDockerImages
  When specified, also clean up old Docker images from GHCR, keeping the most recent image for each API.

.PARAMETER KeepOnlyThree
  When specified, also deletes successful runs beyond the first three most recent per workflow.

.EXAMPLE
  # Simulate cleanup for owner/FunWasHad
  .\cleanup-actions.ps1 -Repo "owner/FunWasHad" -WhatIf

.EXAMPLE
  # Actually delete (after confirmation)
  .\cleanup-actions.ps1 -Repo "owner/FunWasHad"

.EXAMPLE
  # Force delete without interactive confirmation
  .\cleanup-actions.ps1 -Repo "owner/FunWasHad" -Force

.EXAMPLE
  # Clean up both workflow runs and Docker images
  .\cleanup-actions.ps1 -Repo "owner/FunWasHad" -CleanupDockerImages

.EXAMPLE
  # Keep only the first 3 successful runs per workflow, delete the rest
  .\cleanup-actions.ps1 -Repo "owner/FunWasHad" -KeepOnlyThree

.NOTES
  - Requires `gh` CLI installed and authenticated with a token that has repo/actions and packages permissions.
  - For Docker image cleanup, token needs `read:packages` and `delete:packages` scopes.
  - Deletions are irreversible. Use -WhatIf first to verify.
#>

param(
    [string]$Repo = '',
    [switch]$Force,
    [switch]$WhatIf,
    [switch]$CleanupDockerImages,
    [switch]$KeepOnlyThree
)

function Get-RepoFullName
{
    param([string]$candidate)

    if ($candidate -and $candidate.Trim() -ne '')
    {
        return $candidate.Trim()
    }

    # Try to detect current repo via gh
    try
    {
        $repo = gh repo view --json nameWithOwner -q .nameWithOwner 2>$null
        if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($repo))
        {
            throw 'gh repo view failed or returned empty.'
        }
        return $repo.Trim()
    }
    catch
    {
        Write-Error "Repository not provided and could not determine current repo via 'gh repo view'. Provide -Repo 'owner/repo'."
        exit 1
    }
}

$repoFullName = Get-RepoFullName -candidate $Repo
Write-Host "Target repository: $repoFullName"

# Fetch all workflow runs (paginated). We request per_page=100 as a hint.
Write-Host 'Fetching workflow runs (this may take a moment)...'
try
{
    $base64Lines = gh api --paginate "repos/$repoFullName/actions/runs?per_page=100" -q '.workflow_runs[] | @base64' 2>$null
    if ($LASTEXITCODE -ne 0)
    {
        throw 'gh api call failed. Ensure gh is installed, authenticated, and you have access to the repository.'
    }
}
catch
{
    Write-Error $_
    exit 1
}

if (-not $base64Lines)
{
    Write-Host 'No workflow runs found.'
    # Continue to Docker cleanup if requested, even if no workflow runs
    if (-not $CleanupDockerImages)
    {
        exit 0
    }
    Write-Host 'Proceeding to Docker image cleanup...'
    Write-Host ""
}

# Decode base64 lines and convert to objects
$runs = @()
foreach ($line in $base64Lines)
{
    try
    {
        $json = [System.Text.Encoding]::UTF8.GetString([Convert]::FromBase64String($line))
        $obj = $json | ConvertFrom-Json
        $runs += $obj
    }
    catch
    {
        Write-Warning 'Skipping a run because parsing failed.'
    }
}

# Filter failed, cancelled, and successful runs separately
# Only process completed runs to avoid in-progress runs
$completedRuns = $runs | Where-Object { $_.status -eq 'completed' }
$failedRuns = $completedRuns | Where-Object { $_.conclusion -eq 'failure' }
$cancelledRuns = $completedRuns | Where-Object { $_.conclusion -eq 'cancelled' }
$successfulRuns = if ($KeepOnlyThree) { $completedRuns | Where-Object { $_.conclusion -eq 'success' } } else { @() }

Write-Host "Found $($runs.Count) total workflow run(s)" -ForegroundColor Cyan
Write-Host "  - Completed runs: $($completedRuns.Count)" -ForegroundColor Cyan
if ($failedRuns.Count -gt 0) {
    Write-Host "  - Failed runs: $($failedRuns.Count)" -ForegroundColor Red
}
if ($cancelledRuns.Count -gt 0) {
    Write-Host "  - Cancelled runs: $($cancelledRuns.Count)" -ForegroundColor Yellow
}
if ($successfulRuns.Count -gt 0) {
    Write-Host "  - Successful runs: $($successfulRuns.Count)" -ForegroundColor Green
}
Write-Host ""

# Find runs tagged with "no-build" check run
Write-Host 'Checking for runs tagged with "no-build"...' -ForegroundColor Cyan
$noBuildRuns = @()
foreach ($run in $runs)
{
    try
    {
        $checkRunsJson = gh api "repos/$repoFullName/actions/runs/$($run.id)/check-runs" 2>$null
        if ($LASTEXITCODE -eq 0 -and $checkRunsJson)
        {
            $checkRuns = $checkRunsJson | ConvertFrom-Json
            $hasNoBuildTag = $checkRuns.check_runs | Where-Object { $_.name -eq 'no-build' }
            if ($hasNoBuildTag)
            {
                $noBuildRuns += $run
            }
        }
    }
    catch
    {
        # Silently skip if we can't check check runs (e.g., insufficient permissions or API error)
        # This is not critical - we'll just miss some no-build runs
    }
}

$hasRunsToProcess = ($failedRuns -and $failedRuns.Count -gt 0) -or ($cancelledRuns -and $cancelledRuns.Count -gt 0) -or ($noBuildRuns -and $noBuildRuns.Count -gt 0) -or ($successfulRuns -and $successfulRuns.Count -gt 0)

if (-not $hasRunsToProcess)
{
    $message = 'No failed, cancelled, or no-build tagged workflow runs to process.'
    if ($KeepOnlyThree)
    {
        $message += ' No successful runs to process.'
    }
    Write-Host $message
    # Continue to Docker cleanup if requested, even if no runs to process
    if (-not $CleanupDockerImages)
    {
        exit 0
    }
    Write-Host 'Proceeding to Docker image cleanup...'
    Write-Host ""
}

# Prepare deletion list
$toDelete = @()

# For failed runs: keep most recent per workflow, delete the rest
if ($failedRuns -and $failedRuns.Count -gt 0)
{
    Write-Host "Processing failed runs by workflow..." -ForegroundColor Cyan
    $grouped = $failedRuns | Group-Object -Property workflow_id
    foreach ($grp in $grouped)
    {
        $sorted = $grp.Group | Sort-Object { [DateTime]$_.created_at } -Descending
        $mostRecent = $sorted[0]
        Write-Host "  Workflow: $($mostRecent.name) (ID: $($grp.Name)) - $($sorted.Count) failed run(s)" -ForegroundColor Gray
        Write-Host "    Keeping: Run ID $($mostRecent.id) (created: $($mostRecent.created_at))" -ForegroundColor Green

        # Keep the first (most recent failed run), delete the rest
        $candidates = $sorted | Select-Object -Skip 1
        if ($candidates.Count -gt 0)
        {
            Write-Host "    Deleting: $($candidates.Count) older failed run(s)" -ForegroundColor Yellow
            foreach ($r in $candidates)
            {
                $toDelete += [PSCustomObject]@{
                    id          = $r.id
                    workflow_id = $r.workflow_id
                    name        = $r.name
                    head_branch = $r.head_branch
                    event       = $r.event
                    conclusion  = $r.conclusion
                    created_at  = $r.created_at
                    url         = $r.html_url
                }
            }
        }
        else
        {
            Write-Host "    No older runs to delete (only 1 failed run for this workflow)" -ForegroundColor Gray
        }
    }
    Write-Host ""
}

# For cancelled runs: delete ALL of them (don't keep any)
if ($cancelledRuns -and $cancelledRuns.Count -gt 0)
{
    Write-Host "Processing cancelled runs (deleting ALL cancelled runs)..." -ForegroundColor Cyan
    $groupedCancelled = $cancelledRuns | Group-Object -Property workflow_id
    foreach ($grp in $groupedCancelled)
    {
        $workflowName = ($grp.Group | Select-Object -First 1).name
        Write-Host "  Workflow: $workflowName (ID: $($grp.Name)) - $($grp.Count) cancelled run(s) to delete" -ForegroundColor Gray
        foreach ($r in $grp.Group)
        {
            Write-Host "    Deleting: Run ID $($r.id) (created: $($r.created_at), branch: $($r.head_branch))" -ForegroundColor Yellow
            $toDelete += [PSCustomObject]@{
                id          = $r.id
                workflow_id = $r.workflow_id
                name        = $r.name
                head_branch = $r.head_branch
                event       = $r.event
                conclusion  = $r.conclusion
                created_at  = $r.created_at
                url         = $r.html_url
                tag         = 'cancelled'
            }
        }
    }
    Write-Host ""
}

# For no-build runs: delete ALL of them (don't keep any)
if ($noBuildRuns -and $noBuildRuns.Count -gt 0)
{
    Write-Host "Found $($noBuildRuns.Count) run(s) tagged with 'no-build'" -ForegroundColor Cyan
    foreach ($r in $noBuildRuns)
    {
        $toDelete += [PSCustomObject]@{
            id          = $r.id
            workflow_id = $r.workflow_id
            name        = $r.name
            head_branch = $r.head_branch
            event       = $r.event
            conclusion  = $r.conclusion
            created_at  = $r.created_at
            url         = $r.html_url
            tag         = 'no-build'
        }
    }
}

# For successful runs: keep first 3 per workflow, delete the rest (only if KeepOnlyThree is specified)
if ($KeepOnlyThree -and $successfulRuns -and $successfulRuns.Count -gt 0)
{
    Write-Host "Processing successful runs (keeping only first 3 per workflow)..." -ForegroundColor Green
    $grouped = $successfulRuns | Group-Object -Property workflow_id
    foreach ($grp in $grouped)
    {
        $sorted = $grp.Group | Sort-Object { [DateTime]$_.created_at } -Descending
        # Keep the first 3 (most recent successful runs), delete the rest
        $candidates = $sorted | Select-Object -Skip 3
        foreach ($r in $candidates)
        {
            $toDelete += [PSCustomObject]@{
                id          = $r.id
                workflow_id = $r.workflow_id
                name        = $r.name
                head_branch = $r.head_branch
                event       = $r.event
                conclusion  = $r.conclusion
                created_at  = $r.created_at
                url         = $r.html_url
                tag         = 'success'
            }
        }
    }
}

if (-not $toDelete -or $toDelete.Count -eq 0)
{
    $message = "Nothing to delete — most recent failed run per workflow is preserved, and no cancelled or no-build runs found."
    if ($KeepOnlyThree)
    {
        $message += " No excess successful runs found (all workflows have 3 or fewer successful runs)."
    }
    Write-Host $message
    # Continue to Docker cleanup if requested, even if no runs to delete
    if (-not $CleanupDockerImages)
    {
        exit 0
    }
    Write-Host 'Proceeding to Docker image cleanup...'
    Write-Host ""
}

# Count by type for reporting
$failedCount = ($toDelete | Where-Object { $_.conclusion -eq 'failure' }).Count
$cancelledCount = ($toDelete | Where-Object { $_.tag -eq 'cancelled' }).Count
$noBuildCount = ($toDelete | Where-Object { $_.tag -eq 'no-build' }).Count
$successCount = ($toDelete | Where-Object { $_.tag -eq 'success' }).Count

Write-Host "Found $($toDelete.Count) run(s) to delete:"
if ($failedCount -gt 0) {
    Write-Host "  - Failed runs: $failedCount (keeping most recent per workflow)" -ForegroundColor Red
}
if ($cancelledCount -gt 0) {
    Write-Host "  - Cancelled runs: $cancelledCount (deleting ALL cancelled runs)" -ForegroundColor Yellow
}
if ($noBuildCount -gt 0) {
    Write-Host "  - No-build runs: $noBuildCount (deleting ALL no-build tagged runs)" -ForegroundColor Cyan
}
if ($successCount -gt 0) {
    Write-Host "  - Successful runs: $successCount (keeping only first 3 per workflow)" -ForegroundColor Green
}
Write-Host ''

foreach ($item in $toDelete)
{
    if ($item.tag -eq 'no-build')
    {
        $conclusionColor = 'Cyan'
        $status = 'no-build'
    }
    elseif ($item.tag -eq 'success')
    {
        $conclusionColor = 'Green'
        $status = 'success'
    }
    elseif ($item.conclusion -eq 'failure')
    {
        $conclusionColor = 'Red'
        $status = 'failure'
    }
    else
    {
        $conclusionColor = 'Yellow'
        $status = 'cancelled'
    }
    Write-Host ('Run ID: {0}  Workflow: {1}  Name: {2}  Branch: {3}  Status: {4}  Created: {5}' -f $item.id, $item.workflow_id, $item.name, $item.head_branch, $status, $item.created_at) -ForegroundColor $conclusionColor
    Write-Host "  URL: $($item.url)"
    Write-Host ''
}

if (-not $WhatIf)
{
    if (-not $Force)
    {
        $ans = Read-Host "Proceed to delete these $($toDelete.Count) runs? Type 'yes' to confirm"
        if ($ans.Trim().ToLower() -ne 'yes')
        {
            Write-Host 'Workflow run deletion aborted by user.'
            # Continue to Docker cleanup if requested, even if user aborted workflow cleanup
            if (-not $CleanupDockerImages)
            {
                exit 0
            }
            Write-Host 'Proceeding to Docker image cleanup...'
            Write-Host ""
        }
    }

    # Perform deletions
    $errors = @()
    foreach ($item in $toDelete)
    {
        Write-Host "Deleting run ID $($item.id) ..."
        try
        {
            gh api -X DELETE "repos/$repoFullName/actions/runs/$($item.id)" 2>$null
            if ($LASTEXITCODE -ne 0)
            {
                throw "gh returned exit code $LASTEXITCODE"
            }
            Write-Host '  Deleted.'
        }
        catch
        {
            Write-Warning "  Failed to delete run $($item.id): $_"
            $errors += $item
        }
    }

    if ($errors.Count -gt 0)
    {
        Write-Warning "$($errors.Count) run(s) failed to delete. See warnings above."
        exit 2
    }

    Write-Host "Done. Deleted $($toDelete.Count) run(s)."
}
else
{
    Write-Host 'WhatIf specified — no workflow run deletions will be performed.'
}

Write-Host ""

# Cleanup Docker images from GHCR if requested
if ($CleanupDockerImages)
{
    Write-Host "══════════════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host "Cleaning up Docker images from GHCR" -ForegroundColor Yellow
    Write-Host "══════════════════════════════════════════════════════════════" -ForegroundColor Yellow
    Write-Host ""

    # Extract owner from repo name
    $owner = $repoFullName.Split('/')[0]
    $apiImages = @('fwh-location-api', 'fwh-marketing-api')
    $imagesToDelete = @()

    foreach ($imageName in $apiImages)
    {
        Write-Host "Processing package: $imageName" -ForegroundColor Cyan

        try
        {
            # Get all versions of the package
            $versionsJson = gh api --paginate "users/$owner/packages/container/$imageName/versions" 2>$null
            if ($LASTEXITCODE -ne 0)
            {
                Write-Warning "Failed to fetch versions for $imageName. Skipping."
                continue
            }

            if (-not $versionsJson)
            {
                Write-Host "  No versions found for $imageName" -ForegroundColor Gray
                continue
            }

            $versions = $versionsJson | ConvertFrom-Json

            if ($versions.Count -le 1)
            {
                Write-Host "  Only $($versions.Count) version(s) found for $imageName - nothing to delete" -ForegroundColor Gray
                continue
            }

            # Sort by updated_at (most recent first)
            $sortedVersions = $versions | Sort-Object { [DateTime]$_.updated_at } -Descending

            # Keep the most recent, mark the rest for deletion
            $mostRecent = $sortedVersions[0]
            $toDeleteForImage = $sortedVersions | Select-Object -Skip 1

            Write-Host "  Keeping most recent version: ID $($mostRecent.id) (updated: $($mostRecent.updated_at))" -ForegroundColor Green

            foreach ($version in $toDeleteForImage)
            {
                $tags = if ($version.metadata.container.tags) { $version.metadata.container.tags -join ', ' } else { '(untagged)' }
                $imagesToDelete += [PSCustomObject]@{
                    PackageName = $imageName
                    VersionId   = $version.id
                    Tags        = $tags
                    UpdatedAt   = $version.updated_at
                }
            }
        }
        catch
        {
            Write-Warning "Error processing $imageName : $_"
        }
    }

    if ($imagesToDelete.Count -eq 0)
    {
        Write-Host "No Docker images to delete - most recent image for each API is preserved." -ForegroundColor Green
    }
    else
    {
        Write-Host "Found $($imagesToDelete.Count) Docker image version(s) to delete (keeping most recent per API)." -ForegroundColor Yellow
        Write-Host ""

        foreach ($img in $imagesToDelete)
        {
            Write-Host "  Package: $($img.PackageName)  Version ID: $($img.VersionId)  Tags: $($img.Tags)  Updated: $($img.UpdatedAt)" -ForegroundColor Gray
        }
        Write-Host ""

        if (-not $WhatIf)
        {
            if (-not $Force)
            {
                $ans = Read-Host "Proceed to delete these $($imagesToDelete.Count) Docker image versions? Type 'yes' to confirm"
                if ($ans.Trim().ToLower() -ne 'yes')
                {
                    Write-Host "Docker image cleanup aborted by user." -ForegroundColor Yellow
                }
                else
                {
                    # Perform deletions
                    $imageErrors = @()
                    foreach ($img in $imagesToDelete)
                    {
                        Write-Host "Deleting $($img.PackageName) version $($img.VersionId) ..." -ForegroundColor Cyan
                        try
                        {
                            gh api -X DELETE "users/$owner/packages/container/$($img.PackageName)/versions/$($img.VersionId)" 2>$null
                            if ($LASTEXITCODE -ne 0)
                            {
                                throw "gh returned exit code $LASTEXITCODE"
                            }
                            Write-Host "  Deleted." -ForegroundColor Green
                        }
                        catch
                        {
                            Write-Warning "  Failed to delete version $($img.VersionId): $_"
                            $imageErrors += $img
                        }
                    }

                    if ($imageErrors.Count -gt 0)
                    {
                        Write-Warning "$($imageErrors.Count) Docker image version(s) failed to delete. See warnings above."
                    }
                    else
                    {
                        Write-Host "Done. Deleted $($imagesToDelete.Count) Docker image version(s)." -ForegroundColor Green
                    }
                }
            }
            else
            {
                # Force mode - delete without confirmation
                $imageErrors = @()
                foreach ($img in $imagesToDelete)
                {
                    Write-Host "Deleting $($img.PackageName) version $($img.VersionId) ..." -ForegroundColor Cyan
                    try
                    {
                        gh api -X DELETE "users/$owner/packages/container/$($img.PackageName)/versions/$($img.VersionId)" 2>$null
                        if ($LASTEXITCODE -ne 0)
                        {
                            throw "gh returned exit code $LASTEXITCODE"
                        }
                        Write-Host "  Deleted." -ForegroundColor Green
                    }
                    catch
                    {
                        Write-Warning "  Failed to delete version $($img.VersionId): $_"
                        $imageErrors += $img
                    }
                }

                if ($imageErrors.Count -gt 0)
                {
                    Write-Warning "$($imageErrors.Count) Docker image version(s) failed to delete. See warnings above."
                }
                else
                {
                    Write-Host "Done. Deleted $($imagesToDelete.Count) Docker image version(s)." -ForegroundColor Green
                }
            }
        }
        else
        {
            Write-Host "WhatIf specified — no Docker image deletions will be performed." -ForegroundColor Yellow
        }
    }
}

exit 0

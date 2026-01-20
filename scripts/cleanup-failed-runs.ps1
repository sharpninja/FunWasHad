<#
.SYNOPSIS
  Delete failed and cancelled GitHub Actions workflow runs in a repository while keeping the most recent run per workflow.

.DESCRIPTION
  Uses the GitHub CLI (`gh`) to enumerate workflow runs for a repository, finds runs whose conclusion is "failure" or "cancelled",
  and deletes all but the most recent failed/cancelled run for each workflow (grouped by workflow_id).
  By default the script prompts before performing deletions. Use -Force to skip the prompt, or -WhatIf to simulate.

.PARAMETER Repo
  The repository in owner/repo format. If omitted the script will attempt to detect the current repository via `gh repo view`.

.PARAMETER Force
  When specified, do not prompt before deleting; proceed with deletions.

.PARAMETER WhatIf
  When specified, do not perform deletions — only show what would be deleted.

.PARAMETER CleanupDockerImages
  When specified, also clean up old Docker images from GHCR, keeping the most recent image for each API.

.EXAMPLE
  # Simulate cleanup for owner/FunWasHad
  .\cleanup-failed-runs.ps1 -Repo "owner/FunWasHad" -WhatIf

.EXAMPLE
  # Actually delete (after confirmation)
  .\cleanup-failed-runs.ps1 -Repo "owner/FunWasHad"

.EXAMPLE
  # Force delete without interactive confirmation
  .\cleanup-failed-runs.ps1 -Repo "owner/FunWasHad" -Force

.EXAMPLE
  # Clean up both workflow runs and Docker images
  .\cleanup-failed-runs.ps1 -Repo "owner/FunWasHad" -CleanupDockerImages

.NOTES
  - Requires `gh` CLI installed and authenticated with a token that has repo/actions and packages permissions.
  - For Docker image cleanup, token needs `read:packages` and `delete:packages` scopes.
  - Deletions are irreversible. Use -WhatIf first to verify.
#>

param(
    [string]$Repo = '',
    [switch]$Force,
    [switch]$WhatIf,
    [switch]$CleanupDockerImages
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

# Filter failed and cancelled runs
$failedRuns = $runs | Where-Object { $_.conclusion -eq 'failure' -or $_.conclusion -eq 'cancelled' }
$hasFailedRuns = $failedRuns -and $failedRuns.Count -gt 0

if (-not $hasFailedRuns)
{
    Write-Host 'No failed or cancelled workflow runs to process.'
    # Continue to Docker cleanup if requested, even if no failed/cancelled runs
    if (-not $CleanupDockerImages)
    {
        exit 0
    }
    Write-Host 'Proceeding to Docker image cleanup...'
    Write-Host ""
}

# Group by workflow_id and prepare deletion list (keep most recent per workflow)
$toDelete = @()
$grouped = $failedRuns | Group-Object -Property workflow_id
foreach ($grp in $grouped)
{
    $sorted = $grp.Group | Sort-Object { [DateTime]$_.created_at } -Descending
    # Keep the first (most recent failed/cancelled run), delete the rest
    $candidates = $sorted | Select-Object -Skip 1
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

if (-not $toDelete -or $toDelete.Count -eq 0)
{
    Write-Host "Nothing to delete — each workflow's most recent failed/cancelled run is preserved."
    # Continue to Docker cleanup if requested, even if no runs to delete
    if (-not $CleanupDockerImages)
    {
        exit 0
    }
    Write-Host 'Proceeding to Docker image cleanup...'
    Write-Host ""
}

# Count by conclusion type for reporting
$failedCount = ($toDelete | Where-Object { $_.conclusion -eq 'failure' }).Count
$cancelledCount = ($toDelete | Where-Object { $_.conclusion -eq 'cancelled' }).Count

Write-Host "Found $($toDelete.Count) run(s) to delete (keeping the most recent failed/cancelled run per workflow)."
if ($failedCount -gt 0) {
    Write-Host "  - Failed runs: $failedCount" -ForegroundColor Red
}
if ($cancelledCount -gt 0) {
    Write-Host "  - Cancelled runs: $cancelledCount" -ForegroundColor Yellow
}
Write-Host ''

foreach ($item in $toDelete)
{
    $conclusionColor = if ($item.conclusion -eq 'failure') { 'Red' } else { 'Yellow' }
    Write-Host ('Run ID: {0}  Workflow: {1}  Name: {2}  Branch: {3}  Status: {4}  Created: {5}' -f $item.id, $item.workflow_id, $item.name, $item.head_branch, $item.conclusion, $item.created_at) -ForegroundColor $conclusionColor
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

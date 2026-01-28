# Where is the Generated Report?

## Report Location

The staging workflow status report is located at:

```
docs/STAGING-STATUS.md
```

You can view it:
- **In the repository**: Navigate to `docs/STAGING-STATUS.md` on the develop branch
- **Direct link**: [docs/STAGING-STATUS.md](../docs/STAGING-STATUS.md)
- **From docs index**: It's linked in [docs/README.md](../docs/README.md) under "Quick Links"

## Current Status

⚠️ **Important**: The monitoring workflow is currently in the feature branch `copilot/monitor-staging-action-output` and has **not yet been merged to the develop branch**.

### What This Means

The automated status report will **not be generated** until:

1. ✅ This PR is merged to the develop branch
2. ✅ The monitoring workflow file (`.github/workflows/staging-monitor.yml`) is on develop
3. ✅ A staging.yml workflow completes on develop branch
4. ✅ The monitoring workflow automatically triggers
5. ✅ The status document is generated and committed

### Timeline

**After this PR is merged:**
- The very next staging.yml workflow run on develop will trigger the monitoring workflow
- Within a few minutes of completion, `docs/STAGING-STATUS.md` will be updated
- The update will be committed back to develop with message: `chore: update staging workflow status [skip ci]`

## How to Know When the Report is Ready

### Method 1: Check the File Directly
Once merged, navigate to `docs/STAGING-STATUS.md` on the develop branch. If it shows:
```markdown
Status document will be updated after the next staging.yml workflow run completes.
```
Then the monitoring workflow hasn't run yet.

### Method 2: Check GitHub Actions
1. Go to the [Actions tab](https://github.com/sharpninja/FunWasHad/actions)
2. Look for "Monitor Staging Workflow" in the list of workflows
3. Check if it has run and completed successfully

### Method 3: Check Recent Commits
Look for a commit on develop with message:
```
chore: update staging workflow status [skip ci]
```

### Method 4: Use the GitHub API
```bash
# Check if the workflow file exists on develop
curl -s https://api.github.com/repos/sharpninja/FunWasHad/contents/.github/workflows/staging-monitor.yml?ref=develop

# Check if the status document has been updated
curl -s https://api.github.com/repos/sharpninja/FunWasHad/contents/docs/STAGING-STATUS.md?ref=develop | jq -r '.content' | base64 -d
```

## What the Report Contains

Once generated, the status document includes:

### 1. Latest Run Information
- Run number, ID, and direct link
- Branch and commit details
- Timestamps (start and completion)
- Overall status/conclusion

### 2. Job Status Summary Table
| Job Name | Status | Duration | Conclusion |
|----------|--------|----------|------------|
| Example | completed | 45s | success |

### 3. Detailed Job Breakdown
For each job:
- Overall status and conclusion
- Step-by-step breakdown
- Links to view full logs

### 4. Build and Deployment Errors
- Collapsible sections with error excerpts from failed jobs
- Common error patterns highlighted
- Links to full error logs

### 5. Analyzer Warnings Table
| Warning Code | Count | Description |
|--------------|-------|-------------|
| CS1234 | 15 | Example warning description |
| CA5678 | 8 | Another warning description |

## Viewing the Report

### On GitHub
1. Navigate to the repository
2. Switch to the `develop` branch
3. Open `docs/STAGING-STATUS.md`

### Via Raw URL
```
https://raw.githubusercontent.com/sharpninja/FunWasHad/develop/docs/STAGING-STATUS.md
```

### Local Clone
```bash
git clone https://github.com/sharpninja/FunWasHad.git
cd FunWasHad
git checkout develop
cat docs/STAGING-STATUS.md
```

## FAQ

### Q: Why isn't there a report yet?
**A:** The monitoring workflow needs to be merged to the develop branch first. It's currently in a feature branch.

### Q: How often is the report updated?
**A:** Automatically after every staging.yml workflow run that completes on the develop branch.

### Q: Can I trigger a manual update?
**A:** Not directly, but you can trigger a staging.yml run by pushing to develop, which will then trigger the monitoring workflow.

### Q: What if the monitoring workflow fails?
**A:** Check the Actions tab for the "Monitor Staging Workflow" run. The workflow includes error handling and will show detailed error messages.

### Q: Can I view historical reports?
**A:** Yes! Each update is committed to git, so you can view the file's history:
```bash
git log -p -- docs/STAGING-STATUS.md
```

Or on GitHub: https://github.com/sharpninja/FunWasHad/commits/develop/docs/STAGING-STATUS.md

## Testing the Workflow

After this PR is merged, you can test by:

1. Making any small change to the develop branch
2. Pushing the change (this triggers staging.yml)
3. Waiting for staging.yml to complete
4. Checking that monitoring workflow runs
5. Verifying `docs/STAGING-STATUS.md` is updated

## Need Help?

- **Full documentation**: See `docs/deployment/Staging_Workflow_Monitoring_Implementation.md`
- **Workflow file**: See `.github/workflows/staging-monitor.yml`
- **Implementation details**: See the PR description for `copilot/monitor-staging-action-output`

---

**Last Updated**: 2026-01-28  
**Status**: Pending merge to develop branch  
**Expected Available**: After PR merge + next staging run

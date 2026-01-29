# Deploy to Staging - Fix Summary

## 🐛 Issues Identified

### Primary Issue
The `Deploy to Staging` workflow was using `railway up --detach`, which is designed to deploy from source code, not from pre-built Docker images. This command attempts to build and deploy from the repository source, which conflicts with the workflow's design of building Docker images and pushing them to GitHub Container Registry (GHCR).

### Root Cause
- The workflow builds Docker images and pushes them to GHCR
- Railway services need to be configured to pull Docker images from GHCR
- The `railway up` command was trying to build from source instead of using the pre-built images

## ✅ Fixes Applied

### 1. Updated Railway Deployment Commands
**Changed from:**
```yaml
railway service staging-location-api
railway up --detach
```

**Changed to:**
```yaml
railway redeploy --service staging-location-api --yes
```

The `railway redeploy` command triggers Railway to pull the latest Docker image from the configured registry (GHCR) and deploy it.

### 2. Improved Authentication Flow
- Consolidated Railway authentication into a single step
- Added proper token-based login: `railway login --token $RAILWAY_TOKEN`
- Linked to project before deployment: `railway link ${{ secrets.RAILWAY_STAGING_PROJECT_ID }}`

### 3. Added Image Tag Tracking
- Created a step to generate and track Docker image tags
- Outputs image paths for debugging and verification
- Ensures consistent tag usage: `staging-latest`

### 4. Enhanced Error Handling
- Added comprehensive error messages with troubleshooting steps
- Included setup instructions in error output
- Provided direct links to Railway dashboard
- Better logging for deployment status

### 5. Improved Health Checks
- Enhanced health check steps with better URL retrieval
- Added retry logic and timeout handling
- More informative error messages
- Non-blocking health checks (continue-on-error: true)

## 📋 Railway Service Configuration Requirements

**IMPORTANT:** Railway services must be pre-configured in the Railway dashboard before the workflow can deploy:

1. **Service Source Configuration:**
   - Go to Railway dashboard: https://railway.com/project/{PROJECT_ID}
   - Navigate to each service (`staging-location-api`, `staging-marketing-api`)
   - Go to **Settings → Source**
   - Select **"Docker Image"** (not "GitHub Repo")
   - Enter the Docker image path:
     - Location API: `ghcr.io/{OWNER}/fwh-location-api:staging-latest`
     - Marketing API: `ghcr.io/{OWNER}/fwh-marketing-api:staging-latest`

2. **Registry Credentials:**
   - In service settings, configure registry credentials for `ghcr.io`
   - Use a GitHub Personal Access Token (PAT) with `read:packages` scope
   - Railway will use these credentials to pull private images from GHCR

3. **Environment Variables:**
   - Ensure all required environment variables are set in Railway
   - See `docs/deployment/railway-staging-setup.md` for complete list

## 🔄 Workflow Flow (After Fix)

1. **Build and Test** (`build_and_test` job)
   - Builds both APIs on Windows
   - Runs tests
   - Publishes artifacts

2. **Build Docker Images** (`docker_location_api_staging`, `docker_marketing_api_staging`)
   - Downloads published artifacts
   - Builds Docker images using `Dockerfile.artifacts`
   - Pushes images to GHCR with tags: `staging-latest`, branch name, SHA

3. **Deploy to Railway** (`deploy_railway_staging`)
   - Authenticates with Railway
   - Triggers redeploy for each service
   - Railway pulls latest images from GHCR
   - Waits for deployments to complete
   - Runs health checks

4. **Notify Status** (`notify_deployment`)
   - Reports deployment success or failure

## 🚨 Troubleshooting

### If Deployment Fails

1. **Check Railway Service Configuration:**
   - Verify services exist: `staging-location-api`, `staging-marketing-api`
   - Confirm source is set to "Docker Image" (not "GitHub Repo")
   - Verify Docker image paths match: `ghcr.io/{OWNER}/fwh-*-api:staging-latest`

2. **Check Registry Credentials:**
   - Ensure GHCR credentials are configured in Railway service settings
   - Token must have `read:packages` scope
   - For private repos, Railway needs access to pull images

3. **Verify Docker Images:**
   - Check GHCR to ensure images were pushed successfully
   - Verify image tags match what's configured in Railway
   - Images should be tagged with `staging-latest`

4. **Check Railway CLI:**
   - Verify `RAILWAY_STAGING_TOKEN` secret is set in GitHub
   - Token must have deployment permissions
   - Ensure `RAILWAY_STAGING_PROJECT_ID` secret is correct

### Common Errors

**Error: "Service not found"**
- Solution: Create services in Railway dashboard first
- Or verify service names match exactly: `staging-location-api`, `staging-marketing-api`

**Error: "Docker image not found"**
- Solution: Verify images were pushed to GHCR in previous jobs
- Check image tags match Railway configuration

**Error: "Authentication failed"**
- Solution: Verify `RAILWAY_STAGING_TOKEN` is valid and not expired
- Check token has deployment permissions

**Error: "Health check failed"**
- Solution: This is non-blocking (continue-on-error: true)
- Check Railway logs for service startup issues
- Verify environment variables are set correctly
- Services may need a few minutes to start

## 📚 Related Documentation

- **Railway Setup Guide:** `docs/deployment/railway-staging-setup.md`
- **Workflow File:** `.github/workflows/staging.yml`
- **Railway Dashboard:** https://railway.com/project/{PROJECT_ID}

## ✅ Verification Checklist

Before the workflow can succeed, ensure:

- [ ] Railway services `staging-location-api` and `staging-marketing-api` exist
- [ ] Services are configured with "Docker Image" source
- [ ] Docker image paths are set correctly in Railway
- [ ] GHCR registry credentials are configured in Railway
- [ ] GitHub secrets `RAILWAY_STAGING_TOKEN` and `RAILWAY_STAGING_PROJECT_ID` are set
- [ ] Environment variables are configured in Railway services
- [ ] PostgreSQL database is provisioned and linked

## 🎯 Next Steps

1. Configure Railway services as described above
2. Push to `develop` or `staging` branch to trigger deployment
3. Monitor deployment in GitHub Actions: https://github.com/{OWNER}/FunWasHad/actions
4. Check Railway dashboard for deployment status
5. Verify health endpoints after deployment completes

---

**Fixed Date:** 2025-01-19
**Workflow File:** `.github/workflows/staging.yml`
**Status:** ✅ Ready for testing

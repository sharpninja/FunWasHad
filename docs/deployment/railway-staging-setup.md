# 🚂 Railway Staging Environment Setup

Complete guide for configuring Railway staging environment for FunWasHad APIs.

## 📋 Prerequisites

- ✅ Railway account at https://railway.app
- ✅ Railway CLI installed: `npm install -g @railway/cli`
- ✅ GitHub repository access
- ✅ Railway staging project ID: `9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d`

---

## 🔐 Step 1: Configure GitHub Secrets

**Location:** https://github.com/sharpninja/FunWasHad/settings/secrets/actions

### Required Secrets:

#### 1. `RAILWAY_STAGING_TOKEN`
**How to get:**
1. Go to https://railway.app/account/tokens
2. Click **"Create Token"**
3. Name: `GitHub Actions Staging`
4. Copy the token (shown only once!)

**Add to GitHub:**
1. Go to repository **Settings** → **Secrets and variables** → **Actions**
2. Click **"New repository secret"**
3. Name: `RAILWAY_STAGING_TOKEN`
4. Value: [paste your Railway token]
5. Click **"Add secret"**

#### 2. `RAILWAY_STAGING_PROJECT_ID`
**Value:** `9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d`

**Add to GitHub:**
1. Click **"New repository secret"**
2. Name: `RAILWAY_STAGING_PROJECT_ID`
3. Value: `9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d`
4. Click **"Add secret"**

---

## 🗄️ Step 2: Create PostgreSQL Database

**Railway Project:** https://railway.com/project/9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d

1. Click **"+ New"**
2. Select **"Database"**
3. Select **"PostgreSQL"**

**⚠️ PostGIS Extension:**
The Marketing API uses PostGIS for efficient spatial queries. Railway's PostgreSQL service includes PostGIS by default. The migration will automatically enable PostGIS when available. If PostGIS is not available (e.g., in test environments), the API will gracefully fall back to bounding box queries.
3. Choose **"Add PostgreSQL"**
4. Wait for provisioning (~30 seconds)
5. **Note the service name** (usually `Postgres` or `postgres`)

---

## 🎯 Step 3: Configure Location API Service

### Create Service:
1. Click **"+ New"**
2. Select **"GitHub Repo"**
3. Choose **"sharpninja/FunWasHad"**
4. Click on the new service

### Rename Service:
1. Click **"Settings"**
2. Change **Service name** to: `staging-location-api`

### Set Environment Variables:
Click **"Variables"** tab and add:

```bash
# Core Configuration
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://+:8080
PORT=8080

# Database Connection (replace Postgres with your actual service name)
ConnectionStrings__funwashad=${{Postgres.DATABASE_URL}}

# Location Service Settings
LocationService__DefaultRadiusMeters=1000
LocationService__MaxRadiusMeters=10000
LocationService__MinRadiusMeters=100
LocationService__TimeoutSeconds=30
LocationService__UserAgent=FunWasHad-Staging/1.0
LocationService__OverpassApiUrl=https://overpass-api.de/api/interpreter

# Logging
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning
Logging__LogLevel__Microsoft.EntityFrameworkCore=Information
```

**⚠️ Important:** Replace `Postgres` with your actual PostgreSQL service name!

### Configure Deployment:
1. Click **"Settings"** → **"Deploy"**
2. **Source Repo:** Already set to FunWasHad
3. **Branch:** `develop`
4. **Root Directory:** Leave empty
5. **Build Command:** Leave default
6. **Auto Deploy:** Enable ✅

### Generate Public URL:
1. Click **"Settings"** → **"Networking"**
2. Click **"Generate Domain"**
3. Copy the URL (e.g., `staging-location-api-production-xxxx.up.railway.app`)

---

## 🎯 Step 4: Configure Marketing API Service

### Create Service:
1. Click **"+ New"**
2. Select **"GitHub Repo"**
3. Choose **"sharpninja/FunWasHad"** again
4. Click on the new service

### Rename Service:
1. Click **"Settings"**
2. Change **Service name** to: `staging-marketing-api`

### Set Environment Variables:
Click **"Variables"** tab and add:

```bash
# Core Configuration
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://+:8080
PORT=8080

# Database Connection (replace Postgres with your actual service name)
ConnectionStrings__marketing=${{Postgres.DATABASE_URL}}

# Blob Storage Configuration
BlobStorage__Provider=LocalFile
BlobStorage__LocalPath=/app/uploads
BlobStorage__BaseUrl=/uploads

# Logging
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning
Logging__LogLevel__Microsoft.EntityFrameworkCore=Information
```

**⚠️ Important:** Replace `Postgres` with your actual PostgreSQL service name!

### Configure Persistent Storage (for file uploads):
1. Click **"Settings"** → **"Volumes"**
2. Click **"Add Volume"**
3. **Mount Path:** `/app/uploads`
4. **Volume Name:** `marketing-api-uploads` (or any name you prefer)
5. Click **"Add"**

This ensures uploaded files (feedback attachments) persist across deployments.

### Configure Deployment:
1. Click **"Settings"** → **"Deploy"**
2. **Branch:** `develop`
3. **Auto Deploy:** Enable ✅

### Generate Public URL:
1. Click **"Settings"** → **"Networking"**
2. Click **"Generate Domain"**
3. Copy the URL (e.g., `staging-marketing-api-production-xxxx.up.railway.app`)

---

## 🚀 Step 5: Deploy via GitHub Actions

### Automatic Deployment:
Simply push to the `develop` branch:

```bash
git checkout develop
git push origin develop
```

**GitHub Actions will:**
1. ✅ Build and test on Windows
2. ✅ Publish both APIs
3. ✅ Build Docker images
4. ✅ Push images to GHCR
5. ✅ Deploy to Railway
6. ✅ Run health checks

**Monitor:** https://github.com/sharpninja/FunWasHad/actions

### Manual Deployment (Railway CLI):

```bash
# Install Railway CLI
npm install -g @railway/cli

# Login to Railway
railway login

# Link to staging project
railway link 9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d

# Deploy Location API
railway service staging-location-api
railway up --detach

# Deploy Marketing API
railway service staging-marketing-api
railway up --detach
```

---

## 🧪 Step 6: Verify Deployment

### Check Service Status:
**Railway Dashboard:**
https://railway.com/project/9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d

**Look for:**
- ✅ Green status on both services
- ✅ Latest deployment succeeded
- ✅ No error logs

### Test Health Endpoints:

```bash
# Get your URLs from Railway dashboard first!

# Location API
curl https://staging-location-api-production-xxxx.up.railway.app/health

# Marketing API
curl https://staging-marketing-api-production-xxxx.up.railway.app/health

# Expected response:
# {"status":"Healthy"}
```

### Test API Functionality:

```bash
# Location API - Nearby businesses
curl "https://staging-location-api-production-xxxx.up.railway.app/api/locations/nearby?latitude=40.7128&longitude=-74.0060&radiusMeters=1000"

# Marketing API - Health (if no data yet)
curl "https://staging-marketing-api-production-xxxx.up.railway.app/health"
```

---

## 🔧 Troubleshooting

### Services Won't Start

**Check Deployment Logs:**
1. Railway dashboard → Service → **Deployments**
2. Click latest deployment
3. View logs

**Common Issues:**
- ❌ Missing environment variables
- ❌ Database connection string wrong
- ❌ Port not set to 8080
- ❌ Service name mismatch in variables

**Solution:**
```bash
# Verify all required variables are set
# Check PostgreSQL service name matches in ConnectionStrings
# Ensure PORT=8080 is set
```

### Database Connection Errors

**Error:** `Could not connect to database`

**Check:**
1. PostgreSQL service is running
2. `DATABASE_URL` exists in Postgres service variables
3. Connection string reference is correct:
   ```
   ConnectionStrings__funwashad=${{Postgres.DATABASE_URL}}
   ```
4. PostgreSQL service name matches (might be `postgres`, `Postgres`, or custom)

**Get correct service name:**
1. Click on PostgreSQL service
2. Look at top left for exact name
3. Use that exact name in references

### Health Endpoint Returns 404

**Possible causes:**
- Service not fully deployed
- Wrong port configuration
- Health endpoint not registered

**Solution:**
```bash
# Check ASPNETCORE_URLS
ASPNETCORE_URLS=http://+:8080

# Check PORT
PORT=8080

# Verify in Program.cs:
app.MapHealthChecks("/health");
```

### GitHub Actions Deployment Fails

**Error:** `Railway token invalid` or `Project not found`

**Check GitHub Secrets:**
1. Go to: https://github.com/sharpninja/FunWasHad/settings/secrets/actions
2. Verify both secrets exist:
   - `RAILWAY_STAGING_TOKEN`
   - `RAILWAY_STAGING_PROJECT_ID`
3. Re-create Railway token if expired
4. Update GitHub secret with new token

### Railway CLI Issues

**Error:** `Not logged in`

```bash
# Re-login
railway login

# Verify
railway whoami
```

**Error:** `Project not linked`

```bash
# Link to project
railway link 9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d

# Verify
railway status
```

---

## 📊 Environment Variables Reference

### Location API Required Variables

| Variable | Example Value | Description |
|----------|---------------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Staging` | Environment name |
| `ASPNETCORE_URLS` | `http://+:8080` | Listening address |
| `PORT` | `8080` | Railway port |
| `ConnectionStrings__funwashad` | `${{Postgres.DATABASE_URL}}` | Database connection |
| `LocationService__DefaultRadiusMeters` | `1000` | Default search radius |
| `LocationService__MaxRadiusMeters` | `10000` | Maximum search radius |
| `LocationService__OverpassApiUrl` | `https://overpass-api.de/api/interpreter` | Overpass API endpoint |
| `LocationService__UserAgent` | `FunWasHad-Staging/1.0` | HTTP user agent |

### Marketing API Required Variables

| Variable | Example Value | Description |
|----------|---------------|-------------|
| `ASPNETCORE_ENVIRONMENT` | `Staging` | Environment name |
| `ASPNETCORE_URLS` | `http://+:8080` | Listening address |
| `PORT` | `8080` | Railway port |
| `ConnectionStrings__marketing` | `${{Postgres.DATABASE_URL}}` | Database connection |
| `BlobStorage__Provider` | `LocalFile` | Storage provider type |
| `BlobStorage__LocalPath` | `/app/uploads` | Local storage path |
| `BlobStorage__BaseUrl` | `/uploads` | Base URL for serving files |

**Note:** For blob storage, you should also configure a persistent volume in Railway:
- **Mount Path:** `/app/uploads`
- This ensures uploaded files persist across deployments

### PostgreSQL Auto-Generated Variables

Railway automatically provides these from your PostgreSQL service:

- `${{Postgres.DATABASE_URL}}` - Complete connection string
- `${{Postgres.PGHOST}}` - Host
- `${{Postgres.PGPORT}}` - Port (5432)
- `${{Postgres.PGUSER}}` - Username
- `${{Postgres.PGPASSWORD}}` - Password
- `${{Postgres.PGDATABASE}}` - Database name

---

## ✅ Setup Checklist

### Railway Configuration
- [ ] PostgreSQL database created
- [ ] PostgreSQL service name noted
- [ ] Location API service created as `staging-location-api`
- [ ] Location API environment variables set (all 8+ variables)
- [ ] Location API domain generated
- [ ] Marketing API service created as `staging-marketing-api`
- [ ] Marketing API environment variables set (all 4+ variables)
- [ ] Marketing API domain generated
- [ ] Auto-deploy enabled on both services

### GitHub Configuration
- [ ] `RAILWAY_STAGING_TOKEN` secret added
- [ ] `RAILWAY_STAGING_PROJECT_ID` secret added
- [ ] Secrets verified in repository settings

### Verification
- [ ] Pushed to `develop` branch
- [ ] GitHub Actions workflow completed successfully
- [ ] Both services showing green in Railway
- [ ] Location API health endpoint returns 200
- [ ] Marketing API health endpoint returns 200
- [ ] No errors in Railway service logs

---

## 💰 Cost Estimate

**Railway Pricing:**
- **Hobby Plan:** $5/month includes $5 usage
- **Developer Plan:** $20/month includes $10 usage + better limits

**Expected Usage:**
- PostgreSQL: ~$5-10/month
- Location API: ~$5-10/month
- Marketing API: ~$5-10/month

**Total: ~$15-30/month**

**Recommendation:** Start with Hobby plan, upgrade to Developer if needed.

---

## 🎯 Quick Copy-Paste Sections

### For Location API Variables:

```
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://+:8080
PORT=8080
ConnectionStrings__funwashad=${{Postgres.DATABASE_URL}}
LocationService__DefaultRadiusMeters=1000
LocationService__MaxRadiusMeters=10000
LocationService__MinRadiusMeters=100
LocationService__TimeoutSeconds=30
LocationService__UserAgent=FunWasHad-Staging/1.0
LocationService__OverpassApiUrl=https://overpass-api.de/api/interpreter
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning
Logging__LogLevel__Microsoft.EntityFrameworkCore=Information
```

### For Marketing API Variables:

```
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://+:8080
PORT=8080
ConnectionStrings__marketing=${{Postgres.DATABASE_URL}}
BlobStorage__Provider=LocalFile
BlobStorage__LocalPath=/app/uploads
BlobStorage__BaseUrl=/uploads
Logging__LogLevel__Default=Information
Logging__LogLevel__Microsoft.AspNetCore=Warning
Logging__LogLevel__Microsoft.EntityFrameworkCore=Information
```

---

## 📚 Additional Resources

- **Railway Documentation:** https://docs.railway.app/
- **Railway Environment Variables:** https://docs.railway.app/develop/variables
- **Railway CLI Reference:** https://docs.railway.app/develop/cli
- **Your Staging Project:** https://railway.com/project/9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d
- **GitHub Actions Workflow:** [.github/workflows/staging.yml](../../.github/workflows/staging.yml)
- **Staging Quick Start:** [Railway-Setup-Quick-Start.md](./Railway-Setup-Quick-Start.md)

---

## 🎉 Success Criteria

Your staging environment is ready when:

1. ✅ Both GitHub secrets are set
2. ✅ All Railway services are running (green status)
3. ✅ Health endpoints return `{"status":"Healthy"}`
4. ✅ GitHub Actions workflow completes without errors
5. ✅ Services auto-deploy when pushing to `develop`
6. ✅ No errors in Railway service logs

---

## 🚀 Next Steps

Once setup is complete:

1. **Test the deployment:**
   ```bash
   git checkout develop
   echo "# Test deployment" >> README.md
   git commit -am "test: trigger staging deployment"
   git push origin develop
   ```

2. **Monitor the deployment:**
   - GitHub Actions: https://github.com/sharpninja/FunWasHad/actions
   - Railway Dashboard: https://railway.com/project/9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d

3. **Verify everything works:**
   - Check service logs for errors
   - Test health endpoints
   - Test API functionality

4. **Update documentation** with your actual Railway URLs

---

## 🆘 Need Help?

**Common Resources:**
- **Railway Discord:** https://discord.gg/railway
- **GitHub Discussions:** https://github.com/sharpninja/FunWasHad/discussions
- **Railway Status:** https://railway.app/status

**Troubleshooting Steps:**
1. Check Railway service logs
2. Verify environment variables
3. Test database connectivity
4. Review GitHub Actions logs
5. Check Railway service status page

---

**Setup Complete!** Your staging environment should now automatically deploy on every push to `develop`! 🎉

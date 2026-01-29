# 🚂 Railway Staging Setup - Quick Start

Quick reference for setting up Railway staging environment for FunWasHad.

## 📚 Complete Guide

**Full documentation:** [railway-staging-setup.md](./railway-staging-setup.md)

---

## ⚡ Quick Setup (15 minutes)

### 1. GitHub Secrets (2 min)
https://github.com/sharpninja/FunWasHad/settings/secrets/actions

Add these secrets:
- `RAILWAY_STAGING_TOKEN` - Get from https://railway.app/account/tokens
- `RAILWAY_STAGING_PROJECT_ID` - Value: `9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d`

### 2. Railway Services (10 min)
https://railway.com/project/9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d

**Create:**
1. PostgreSQL database
2. Location API service (name: `staging-location-api`)
3. Marketing API service (name: `staging-marketing-api`)

**Configure each API with environment variables** - see full guide for variables.

### 3. Deploy (2 min)
```bash
git push origin develop
```

Watch: https://github.com/sharpninja/FunWasHad/actions

---

## 🎯 Essential Variables

### Location API:
```bash
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://+:8080
PORT=8080
ConnectionStrings__funwashad=${{Postgres.DATABASE_URL}}
LocationService__DefaultRadiusMeters=1000
LocationService__MaxRadiusMeters=10000
LocationService__OverpassApiUrl=https://overpass-api.de/api/interpreter
LocationService__UserAgent=FunWasHad-Staging/1.0
```

### Marketing API:
```bash
ASPNETCORE_ENVIRONMENT=Staging
ASPNETCORE_URLS=http://+:8080
PORT=8080
ConnectionStrings__marketing=${{Postgres.DATABASE_URL}}
```

---

## ✅ Verification

Test health endpoints:
```bash
curl https://funwashad-location-api-staging.up.railway.app/health
curl https://funwashad-marketing-api-staging.up.railway.app/health
```

Expected: `{"status":"Healthy"}`

---

## 📖 Documentation

- **Complete Setup Guide:** [railway-staging-setup.md](./railway-staging-setup.md)
- **GitHub Actions Workflow:** [.github/workflows/staging.yml](../../.github/workflows/staging.yml)

---

## 🆘 Troubleshooting

**Services won't start?**
- Check Railway logs in service dashboard
- Verify all environment variables are set
- Ensure PostgreSQL service name matches in ConnectionStrings

**GitHub Actions fails?**
- Verify both secrets are set
- Check Railway token hasn't expired
- Review workflow logs

**Full troubleshooting:** See [railway-staging-setup.md](./railway-staging-setup.md#troubleshooting)

---

**Railway Project:** https://railway.com/project/9dce0bf4-23a8-4e8b-bdda-7ce048c0c73d
**GitHub Actions:** https://github.com/sharpninja/FunWasHad/actions

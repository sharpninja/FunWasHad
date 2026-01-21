# Docker Deployment Guide

This guide explains how to build, run, and deploy the FunWasHad API services using Docker.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Building Images](#building-images)
- [Running Services](#running-services)
- [CI/CD Pipeline](#cicd-pipeline)
- [Production Deployment](#production-deployment)
- [Troubleshooting](#troubleshooting)

## Prerequisites

- Docker 24.0+ with BuildKit enabled
- Docker Compose 2.20+
- .NET 9 SDK (for local development)
- 4GB+ RAM available for containers

## Quick Start

### Start all services with Docker Compose

```bash
# Start all services (databases + APIs)
docker-compose up -d

# View logs
docker-compose logs -f

# Stop all services
docker-compose down

# Stop and remove volumes (clean state)
docker-compose down -v
```

### Access the APIs

- **Location API**: http://localhost:4747
  - Swagger UI: http://localhost:4747/swagger
  - Health check: http://localhost:4747/health

- **Marketing API**: http://localhost:4749
  - Swagger UI: http://localhost:4749/swagger
  - Health check: http://localhost:4749/health

## Building Images

### Build Individual API Images

#### Location API

```bash
# Build from solution root
docker build -f src/FWH.Location.Api/Dockerfile -t fwh-location-api:latest .

# Build with specific tag
docker build -f src/FWH.Location.Api/Dockerfile -t fwh-location-api:v1.0.0 .

# Build for production
docker build -f src/FWH.Location.Api/Dockerfile \
  --build-arg BUILD_CONFIGURATION=Release \
  -t fwh-location-api:latest .
```

#### Marketing API

```bash
# Build from solution root
docker build -f src/FWH.MarketingApi/Dockerfile -t fwh-marketing-api:latest .

# Build with specific tag
docker build -f src/FWH.MarketingApi/Dockerfile -t fwh-marketing-api:v1.0.0 .
```

### Multi-platform Builds

Build images for both AMD64 and ARM64 architectures:

```bash
# Create buildx builder (one-time setup)
docker buildx create --name multiplatform --use

# Build multi-platform image for Location API
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -f src/FWH.Location.Api/Dockerfile \
  -t fwh-location-api:latest \
  --push \
  .

# Build multi-platform image for Marketing API
docker buildx build \
  --platform linux/amd64,linux/arm64 \
  -f src/FWH.MarketingApi/Dockerfile \
  -t fwh-marketing-api:latest \
  --push \
  .
```

## Running Services

### Run Individual Containers

#### Location API (without Docker Compose)

```bash
# Start PostgreSQL for Location API
docker run -d \
  --name fwh-postgres-location \
  -e POSTGRES_DB=funwashad \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5432:5432 \
  postgres:16-alpine

# Start Location API
docker run -d \
  --name fwh-location-api \
  -p 4747:8080 \
  -e ConnectionStrings__funwashad="Host=postgres-location;Port=5432;Database=funwashad;Username=postgres;Password=postgres" \
  --link fwh-postgres-location:postgres-location \
  fwh-location-api:latest
```

#### Marketing API (without Docker Compose)

```bash
# Start PostgreSQL for Marketing API
docker run -d \
  --name fwh-postgres-marketing \
  -e POSTGRES_DB=marketing \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  -p 5433:5432 \
  postgres:16-alpine

# Start Marketing API
docker run -d \
  --name fwh-marketing-api \
  -p 4749:8080 \
  -e ConnectionStrings__marketing="Host=postgres-marketing;Port=5432;Database=marketing;Username=postgres;Password=postgres" \
  --link fwh-postgres-marketing:postgres-marketing \
  fwh-marketing-api:latest
```

### Docker Compose Commands

```bash
# Start services in detached mode
docker-compose up -d

# Start specific service
docker-compose up -d location-api

# View logs for all services
docker-compose logs -f

# View logs for specific service
docker-compose logs -f location-api

# Restart services
docker-compose restart

# Stop services (keep containers)
docker-compose stop

# Stop and remove containers
docker-compose down

# Stop, remove containers and volumes
docker-compose down -v

# Rebuild images
docker-compose build

# Rebuild and start
docker-compose up -d --build

# Scale services (if needed)
docker-compose up -d --scale location-api=3
```

## CI/CD Pipeline

### GitHub Actions Workflow

The CI pipeline automatically:

1. **Builds and tests** the solution on every push/PR
2. **Builds Docker images** for both APIs
3. **Pushes images** to GitHub Container Registry (GHCR) on main branch
4. **Tags images** with:
   - Branch name (e.g., `main`)
   - Git SHA (e.g., `main-abc1234`)
   - Semantic version tags (e.g., `v1.0.0`, `1.0`, `1`)
   - `latest` for main branch

### Pulling Images from GHCR

```bash
# Login to GitHub Container Registry
echo $GITHUB_TOKEN | docker login ghcr.io -u USERNAME --password-stdin

# Pull Location API
docker pull ghcr.io/OWNER/fwh-location-api:latest

# Pull Marketing API
docker pull ghcr.io/OWNER/fwh-marketing-api:latest

# Pull specific version
docker pull ghcr.io/OWNER/fwh-location-api:v1.0.0
```

### Manual Image Push

```bash
# Tag image for GHCR
docker tag fwh-location-api:latest ghcr.io/OWNER/fwh-location-api:latest

# Push to GHCR
docker push ghcr.io/OWNER/fwh-location-api:latest
```

## Production Deployment

### Production Configuration

Create a `docker-compose.prod.yml` file:

```yaml
version: '3.8'

services:
  location-api:
    image: ghcr.io/OWNER/fwh-location-api:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:8443;http://+:8080
      - ConnectionStrings__funwashad=${LOCATION_DB_CONNECTION}
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/certificate.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
    volumes:
      - ./certs:/https:ro
    restart: always
    deploy:
      replicas: 3
      resources:
        limits:
          cpus: '1'
          memory: 512M

  marketing-api:
    image: ghcr.io/OWNER/fwh-marketing-api:latest
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=https://+:8443;http://+:8080
      - ConnectionStrings__marketing=${MARKETING_DB_CONNECTION}
      - ASPNETCORE_Kestrel__Certificates__Default__Path=/https/certificate.pfx
      - ASPNETCORE_Kestrel__Certificates__Default__Password=${CERT_PASSWORD}
    volumes:
      - ./certs:/https:ro
    restart: always
    deploy:
      replicas: 3
      resources:
        limits:
          cpus: '1'
          memory: 512M
```

### Deploy to Production

```bash
# Pull latest images
docker-compose -f docker-compose.prod.yml pull

# Start production services
docker-compose -f docker-compose.prod.yml up -d

# Monitor logs
docker-compose -f docker-compose.prod.yml logs -f
```

### Health Checks

Both APIs include health check endpoints:

```bash
# Check Location API health
curl http://localhost:4747/health

# Check Marketing API health
curl http://localhost:4749/health

# Docker health check status
docker ps --filter "name=fwh-location-api" --format "{{.Status}}"
```

## Environment Variables

### Location API

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | `Production` |
| `ASPNETCORE_URLS` | URLs to listen on | `http://+:8080` |
| `ConnectionStrings__funwashad` | PostgreSQL connection string | Required |
| `LocationService__DefaultRadiusMeters` | Default search radius | `1000` |
| `LocationService__MaxRadiusMeters` | Maximum search radius | `10000` |
| `LocationService__OverpassApiUrl` | Overpass API URL | `https://overpass-api.de/api/interpreter` |

### Marketing API

| Variable | Description | Default |
|----------|-------------|---------|
| `ASPNETCORE_ENVIRONMENT` | Environment (Development/Production) | `Production` |
| `ASPNETCORE_URLS` | URLs to listen on | `http://+:8080` |
| `ConnectionStrings__marketing` | PostgreSQL connection string | Required |

## Dockerfile Features

Both Dockerfiles include:

- ✅ **Multi-stage build** - Optimized image size
- ✅ **Non-root user** - Enhanced security
- ✅ **Health checks** - Container health monitoring
- ✅ **.NET 9** - Latest framework
- ✅ **Build caching** - Faster builds
- ✅ **Multi-platform** - AMD64 and ARM64 support

## Troubleshooting

### Container won't start

```bash
# Check container logs
docker logs fwh-location-api

# Check container status
docker ps -a

# Inspect container
docker inspect fwh-location-api
```

### Database connection issues

```bash
# Check if PostgreSQL is running
docker ps --filter "name=postgres"

# Check PostgreSQL logs
docker logs fwh-postgres-location

# Test connection from host
psql -h localhost -p 5432 -U postgres -d funwashad
```

### Port conflicts

If ports are already in use, modify the port mappings in `docker-compose.yml`:

```yaml
services:
  location-api:
    ports:
      - "5747:8080"  # Changed from 4747
```

### Image build failures

```bash
# Clear Docker build cache
docker builder prune -a

# Build with no cache
docker build --no-cache -f src/FWH.Location.Api/Dockerfile .

# Check available disk space
docker system df
```

### Performance issues

```bash
# Check container resource usage
docker stats

# Limit container resources
docker run -d --memory="512m" --cpus="1" fwh-location-api:latest
```

## Best Practices

1. **Use specific tags** - Avoid `latest` in production
2. **Set resource limits** - Prevent resource exhaustion
3. **Enable health checks** - Monitor container health
4. **Use secrets management** - Don't hardcode passwords
5. **Regular updates** - Keep base images updated
6. **Multi-stage builds** - Reduce image size
7. **Non-root users** - Enhance security
8. **Log aggregation** - Centralize logs for monitoring

## References

- [.NET Docker Best Practices](https://docs.microsoft.com/en-us/dotnet/core/docker/build-container)
- [Docker Compose Documentation](https://docs.docker.com/compose/)
- [GitHub Container Registry](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-container-registry)
- [ASP.NET Core in Docker](https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/docker/)

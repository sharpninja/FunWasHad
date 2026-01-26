# Docker Quick Reference

Quick commands for working with Docker in the FunWasHad project.

## 🚀 Quick Start

```bash
# Start everything
docker-compose up -d

# View logs
docker-compose logs -f

# Stop everything
docker-compose down
```

## 📦 Build Commands

```bash
# Build all services
docker-compose build

# Build specific service
docker-compose build location-api

# Build from Dockerfile
docker build -f src/FWH.Location.Api/Dockerfile -t fwh-location-api .
docker build -f src/FWH.MarketingApi/Dockerfile -t fwh-marketing-api .
```

## 🏃 Run Commands

```bash
# Start all services
docker-compose up -d

# Start specific service
docker-compose up -d location-api postgres-location

# Restart services
docker-compose restart

# Stop services
docker-compose stop

# Remove containers
docker-compose down

# Remove containers and volumes
docker-compose down -v
```

## 📊 Monitoring

```bash
# View logs (all services)
docker-compose logs -f

# View logs (specific service)
docker-compose logs -f location-api

# Container status
docker-compose ps

# Resource usage
docker stats

# Health check
curl http://localhost:4747/health
curl http://localhost:4749/health
```

## 🔧 Debugging

```bash
# Execute shell in container
docker exec -it fwh-location-api /bin/sh

# View container details
docker inspect fwh-location-api

# View container logs
docker logs fwh-location-api

# Follow logs
docker logs -f fwh-location-api
```

## 🗄️ Database

```bash
# Connect to Location database
docker exec -it fwh-postgres-location psql -U postgres -d funwashad

# Connect to Marketing database
docker exec -it fwh-postgres-marketing psql -U postgres -d marketing

# Backup database
docker exec fwh-postgres-location pg_dump -U postgres funwashad > backup.sql

# Restore database
docker exec -i fwh-postgres-location psql -U postgres funwashad < backup.sql
```

## 🧹 Cleanup

```bash
# Stop and remove containers
docker-compose down

# Remove volumes
docker-compose down -v

# Remove images
docker rmi fwh-location-api fwh-marketing-api

# Clean up system
docker system prune -a

# Remove unused volumes
docker volume prune
```

## 🌐 Access URLs

- **Location API**: http://localhost:4747
  - Swagger: http://localhost:4747/swagger
  - Health: http://localhost:4747/health

- **Marketing API**: http://localhost:4749
  - Swagger: http://localhost:4749/swagger
  - Health: http://localhost:4749/health

- **PostgreSQL (Location)**: localhost:5432
- **PostgreSQL (Marketing)**: localhost:5433

## 🏷️ Image Tags

```bash
# Pull from GitHub Container Registry
docker pull ghcr.io/OWNER/fwh-location-api:latest
docker pull ghcr.io/OWNER/fwh-marketing-api:latest

# Tag for registry
docker tag fwh-location-api:latest ghcr.io/OWNER/fwh-location-api:v1.0.0

# Push to registry
docker push ghcr.io/OWNER/fwh-location-api:v1.0.0
```

## 🔐 Security

```bash
# Run as non-root (already configured in Dockerfiles)
# Scan image for vulnerabilities
docker scan fwh-location-api:latest

# Update base images
docker pull mcr.microsoft.com/dotnet/aspnet:9.0
docker pull mcr.microsoft.com/dotnet/sdk:9.0
```

## ⚙️ Configuration

### Development

```bash
# Use override file (automatic)
docker-compose up -d

# Specify environment
docker-compose --env-file .env.dev up -d
```

### Production

```bash
# Use production compose file
docker-compose -f docker-compose.yml -f docker-compose.prod.yml up -d

# Specify environment
docker-compose --env-file .env.prod up -d
```

## 📚 More Information

See [docker-guide.md](./docker-guide.md) for detailed documentation.

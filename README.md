# Fun Was Had

A mobile-first application for tracking activities, discovering nearby businesses, and managing personal notes with integrated workflow capabilities.

## 🚀 Quick Start

### Using Docker (Recommended)

```bash
# Start all services (APIs + Databases)
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

**Access Points:**
- Location API: http://localhost:4747/swagger
- Marketing API: http://localhost:4749/swagger

See [DOCKER.md](DOCKER.md) for detailed Docker commands and [docs/deployment/docker-guide.md](docs/deployment/docker-guide.md) for complete deployment guide.

### Local Development

```bash
# Restore dependencies
dotnet restore

# Build solution
dotnet build

# Run tests
dotnet test

# Run specific API
cd src/FWH.Location.Api
dotnet run
```

## 📋 Project Structure

```
FunWasHad/
├── src/
│   ├── FWH.Location.Api/          # Location & business discovery API
│   ├── FWH.MarketingApi/          # Marketing & feedback API
│   ├── FWH.Mobile/                # Cross-platform mobile app (Avalonia)
│   │   ├── FWH.Mobile/            # Shared mobile code
│   │   ├── FWH.Mobile.Android/    # Android-specific
│   │   ├── FWH.Mobile.Desktop/    # Desktop (Windows/Linux/macOS)
│   │   └── FWH.Mobile.iOS/        # iOS-specific
│   ├── FWH.Mobile.Data/           # Mobile local database
│   ├── FWH.Common.Location/       # Location services
│   ├── FWH.Common.Chat/           # Chat & notification services
│   ├── FWH.Common.Workflow/       # Workflow engine
│   ├── FWH.Common.Imaging/        # Image processing
│   └── FWH.ServiceDefaults/       # Aspire service defaults
├── tests/                         # Unit and integration tests
├── docs/                          # Documentation
├── scripts/                       # Setup and utility scripts
└── docker-compose.yml             # Docker orchestration
```

## 🛠️ Technology Stack

- **.NET 9** - Latest framework
- **Avalonia** - Cross-platform UI framework
- **Entity Framework Core** - ORM with PostgreSQL & SQLite
- **Aspire** - Cloud-native orchestration
- **Docker** - Containerization
- **GitHub Actions** - CI/CD pipeline
- **xUnit** - Testing framework

## 📱 Platforms

- ✅ Android (API 21+)
- ✅ Windows Desktop
- ✅ Linux Desktop
- ✅ macOS Desktop
- 🚧 iOS (In Progress)

## 🔑 Key Features

### Location API
- GPS-based business discovery using Overpass API
- Radius-based search (100m - 10km)
- PostgreSQL with PostGIS for spatial queries
- RESTful API with Swagger documentation

### Marketing API
- Business information management
- Customer feedback & ratings
- Photo attachments
- PostgreSQL persistence

### Mobile App
- Cross-platform UI with Avalonia
- Local SQLite database for offline storage
- Real-time location tracking
- Workflow-driven UI
- Camera integration
- Activity tracking

## 🐳 Docker Deployment

### Quick Commands

```bash
# Build images
docker-compose build

# Start services
docker-compose up -d

# View logs
docker-compose logs -f

# Stop services
docker-compose down
```

### CI/CD Pipeline

The project uses two main GitHub Actions workflows:

**Main Actions** (`.github/workflows/main.yml`):
- Triggers on pushes to `main`, `release/**` branches, and version tags (`v*.*.*`)
- Builds and tests with `Release` configuration
- Creates Docker images for both APIs
- Pushes images to GitHub Container Registry
- Supports multi-platform builds (AMD64/ARM64)
- Creates Android releases for version tags

**Staging Actions** (`.github/workflows/staging.yml`):
- Triggers on pushes to `develop` and `staging` branches
- Builds and tests with `Staging` configuration
- Creates Docker images and deploys to Railway staging environment
- Creates Android release candidates for testing

**Image Tagging:**
- `latest` - Latest main branch build
- `v1.0.0` - Semantic version tags
- `main-abc1234` - Git SHA for traceability
- `staging-rc-*` - Staging release candidates

## 📚 Documentation

- **[Technical Requirements](docs/Technical-Requirements.md)** - Complete technical specifications
- **[API Documentation](docs/api/API-Documentation.md)** - RESTful API reference
- **[Docker Guide](docs/deployment/docker-guide.md)** - Deployment and operations
- **[Docker Quick Reference](DOCKER.md)** - Common Docker commands
- **[Database Initialization](docs/mobile/database-initialization.md)** - SQLite setup for mobile
- **[Testing Guide](docs/testing/)** - Testing strategy and practices

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run specific test project
dotnet test tests/FWH.Location.Api.Tests/
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

### Code Quality

- Follow .NET 9 conventions
- Write unit tests for new features
- Ensure all tests pass before submitting PR
- Use meaningful commit messages

## 📄 License

This project is private and proprietary.

## 🔗 Links

- **Repository**: https://github.com/sharpninja/FunWasHad
- **Issues**: https://github.com/sharpninja/FunWasHad/issues
- **Documentation**: [docs/README.md](docs/README.md)
- **Documentation Site**: https://sharpninja.github.io/FunWasHad

## 📞 Support

For questions or issues, please open an issue on GitHub.

---

**Built with ❤️ using .NET 9 and Avalonia**

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy global configuration files
COPY ["Directory.Packages.props", "."]
COPY ["global.json", "."]
COPY ["NuGet.config", "."]

# Copy ALL project files first (this ensures complete dependency graph)
COPY ["src/FWH.Location.Api/FWH.Location.Api.csproj", "src/FWH.Location.Api/"]
COPY ["src/FWH.ServiceDefaults/FWH.ServiceDefaults.csproj", "src/FWH.ServiceDefaults/"]
COPY ["src/FWH.Common.Location/FWH.Common.Location.csproj", "src/FWH.Common.Location/"]
COPY ["src/FWH.Common.Chat/FWH.Common.Chat.csproj", "src/FWH.Common.Chat/"]
COPY ["src/FWH.Common.Workflow/FWH.Common.Workflow.csproj", "src/FWH.Common.Workflow/"]
COPY ["src/FWH.Common.Imaging/FWH.Common.Imaging.csproj", "src/FWH.Common.Imaging/"]
COPY ["src/FWH.Mobile.Data/FWH.Mobile.Data.csproj", "src/FWH.Mobile.Data/"]
COPY ["src/FWH.Orchestrix.Contracts/FWH.Orchestrix.Contracts.csproj", "src/FWH.Orchestrix.Contracts/"]
COPY ["src/FWH.Orchestrix.Mediator.Remote/FWH.Orchestrix.Mediator.Remote.csproj", "src/FWH.Orchestrix.Mediator.Remote/"]

# Restore dependencies with explicit timeout
RUN dotnet restore "src/FWH.Location.Api/FWH.Location.Api.csproj" \
    --configfile NuGet.config \
    --verbosity normal

# Copy all source code
COPY src/ src/

# Build the application
WORKDIR "/src/src/FWH.Location.Api"
RUN dotnet build "FWH.Location.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FWH.Location.Api.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

# Create non-root user for security
RUN addgroup --system --gid 1001 appuser \
    && adduser --system --uid 1001 --ingroup appuser appuser

WORKDIR /app

# Copy published files
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

# Expose ports
EXPOSE 8080
EXPOSE 8081

# Configure ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "FWH.Location.Api.dll"]


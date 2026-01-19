# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy global configuration files
COPY ["Directory.Packages.props", "."]
COPY ["global.json", "."]

# Copy all project files for restore
COPY ["src/FWH.MarketingApi/FWH.MarketingApi.csproj", "src/FWH.MarketingApi/"]
COPY ["src/FWH.ServiceDefaults/FWH.ServiceDefaults.csproj", "src/FWH.ServiceDefaults/"]

# Restore dependencies (simple, no retry)
RUN dotnet restore "src/FWH.MarketingApi/FWH.MarketingApi.csproj" --verbosity normal

# Copy all source code
COPY src/ src/

# Build the application
WORKDIR "/src/src/FWH.MarketingApi"
RUN dotnet build "FWH.MarketingApi.csproj" -c $BUILD_CONFIGURATION -o /app/build --no-restore

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FWH.MarketingApi.csproj" \
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

ENTRYPOINT ["dotnet", "FWH.MarketingApi.dll"]


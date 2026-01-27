# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy global configuration files (required for central package management)
COPY ["Directory.Packages.props", "."]
COPY ["global.json", "."]
COPY ["NuGet.config", "."]

# Copy project file (FWH.Legal.Web has no project references, only MarkdownServer package)
COPY ["src/FWH.Legal.Web/FWH.Legal.Web.csproj", "src/FWH.Legal.Web/"]

# Restore dependencies
RUN dotnet restore "src/FWH.Legal.Web/FWH.Legal.Web.csproj" \
    --configfile NuGet.config \
    --verbosity minimal

# Copy all source and wwwroot content
COPY ["src/FWH.Legal.Web/", "src/FWH.Legal.Web/"]

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
WORKDIR "/src/src/FWH.Legal.Web"
RUN dotnet publish "FWH.Legal.Web.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final

RUN addgroup --system --gid 1001 appuser \
    && adduser --system --uid 1001 --ingroup appuser appuser

WORKDIR /app

COPY --from=publish /app/publish .

RUN chown -R appuser:appuser /app

USER appuser

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true

HEALTHCHECK --interval=30s --timeout=3s --start-period=10s --retries=3 \
    CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "FWH.Legal.Web.dll"]

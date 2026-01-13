# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["FWH.MarketingApi/FWH.MarketingApi.csproj", "FWH.MarketingApi/"]
COPY ["FWH.ServiceDefaults/FWH.ServiceDefaults.csproj", "FWH.ServiceDefaults/"]
COPY ["Directory.Packages.props", "./"]

# Restore dependencies
RUN dotnet restore "FWH.MarketingApi/FWH.MarketingApi.csproj"

# Copy source code
COPY FWH.MarketingApi/ FWH.MarketingApi/
COPY FWH.ServiceDefaults/ FWH.ServiceDefaults/

# Build
WORKDIR /src/FWH.MarketingApi
RUN dotnet build "FWH.MarketingApi.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "FWH.MarketingApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FWH.MarketingApi.dll"]

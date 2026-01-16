# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Directory.Packages.props ./
COPY global.json ./
COPY src/FWH.Location.Api/*.csproj src/FWH.Location.Api/
COPY src/FWH.Common.Location/*.csproj src/FWH.Common.Location/
COPY src/FWH.Common.Chat/*.csproj src/FWH.Common.Chat/
COPY src/FWH.Common.Workflow/*.csproj src/FWH.Common.Workflow/
COPY src/FWH.Common.Imaging/*.csproj src/FWH.Common.Imaging/
COPY src/FWH.Mobile.Data/*.csproj src/FWH.Mobile.Data/
RUN dotnet restore src/FWH.Location.Api/FWH.Location.Api.csproj

COPY . .
RUN dotnet publish src/FWH.Location.Api/FWH.Location.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FWH.Location.Api.dll"]

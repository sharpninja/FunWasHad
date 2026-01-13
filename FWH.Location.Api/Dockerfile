# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ../Directory.Packages.props ./
COPY ../global.json ./
COPY FWH.Location.Api/*.csproj FWH.Location.Api/
COPY FWH.Common.Location/*.csproj FWH.Common.Location/
COPY FWH.Common.Chat/*.csproj FWH.Common.Chat/
COPY FWH.Common.Workflow/*.csproj FWH.Common.Workflow/
COPY FWH.Common.Imaging/*.csproj FWH.Common.Imaging/
COPY FWH.Mobile.Data/*.csproj FWH.Mobile.Data/
RUN dotnet restore FWH.Location.Api/FWH.Location.Api.csproj

COPY . .
RUN dotnet publish FWH.Location.Api/FWH.Location.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FWH.Location.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS base
WORKDIR /app
EXPOSE 8080

LABEL "com.centurylinklabs.watchtower.enable"="true"

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Restore in its own layer so it's cached until the project file changes
COPY MatrixClient.csproj .
RUN dotnet restore MatrixClient.csproj
COPY . .
RUN dotnet publish MatrixClient.csproj -c Release -o /app/publish --no-restore

FROM base AS final
# Chiseled images ship without tzdata; the app needs it to resolve its configured TimeZone
COPY --from=build /usr/share/zoneinfo /usr/share/zoneinfo
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MatrixClient.dll"]

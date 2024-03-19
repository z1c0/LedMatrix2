FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy-chiseled AS base
WORKDIR /app
EXPOSE 8080

LABEL "com.centurylinklabs.watchtower.enable"="true"

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore MatrixClient.csproj
RUN dotnet build -c Release -o /app MatrixClient.csproj

FROM build AS publish
RUN dotnet publish -c Release -o /app MatrixClient.csproj

FROM base AS final
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "MatrixClient.dll"]

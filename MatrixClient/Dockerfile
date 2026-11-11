# SDK image tag. Docker/BuildKit honour `FROM --platform=$BUILDPLATFORM` below
# and pick the build host's architecture from the multi-arch tag. Kaniko
# ignores FROM --platform and resolves every base image for --custom-platform
# (the arm64 target), so the Woodpecker pipeline passes an amd64-only tag.
ARG SDK_TAG=10.0

FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS base
WORKDIR /app
EXPOSE 8080

LABEL "com.centurylinklabs.watchtower.enable"="true"

# The SDK runs on the build host's own architecture (amd64 on k3s-forge) and
# cross-compiles for the target (TARGETARCH, arm64 for the Pis), so nothing
# runs under emulation. The final stage is the target platform's image and
# only copies files.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:$SDK_TAG AS build
ARG TARGETARCH
WORKDIR /src
# Restore in its own layer so the package download is cached until the
# project file changes. publish then re-restores for the target architecture
# (a quick, offline step): `restore -a` doesn't record the RID, so
# `publish -a --no-restore` fails with NETSDK1047.
COPY MatrixClient.csproj .
RUN dotnet restore MatrixClient.csproj
COPY . .
RUN dotnet publish MatrixClient.csproj -c Release -a $TARGETARCH -o /app/publish

FROM base AS final
# Chiseled images ship without tzdata; the app needs it to resolve its configured TimeZone
COPY --from=build /usr/share/zoneinfo /usr/share/zoneinfo
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MatrixClient.dll"]

# syntax=docker/dockerfile:1

# ---------------------------------------------------------------------------
# Stage 1: build & publish
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Restore configuration layers first so they are cached independently of code.
COPY Directory.Build.props Directory.Packages.props ./
COPY NuGet.config .editorconfig stylecop.json ./
COPY src/Directory.Build.props src/Directory.Build.props
COPY tests/Directory.Build.props tests/Directory.Build.props
COPY LastHour.Backend.sln ./
COPY src/ src/
COPY tests/ tests/

# --locked-mode fails the build if committed packages.lock.json files drift,
# keeping image restores deterministic.
RUN dotnet restore LastHour.Backend.sln --locked-mode

RUN dotnet publish src/Api/LastHour.Api/LastHour.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /out

# ---------------------------------------------------------------------------
# Stage 2: minimal runtime image, non-root user
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# curl is used by the docker-compose health check.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Writable log directory for the Serilog File sink.
RUN mkdir -p /app/logs \
    && chown -R app:app /app

COPY --from=build /out ./

USER app
ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080

ENTRYPOINT ["dotnet", "LastHour.Api.dll"]

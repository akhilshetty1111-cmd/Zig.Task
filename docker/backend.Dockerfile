# Build context is backend/ (see docker-compose.yml) - that's the directory
# containing global.json, Directory.Build.props and Directory.Packages.props,
# which MSBuild needs to find by walking up from each project.

# ---- Stage 1: build -------------------------------------------------------
# The SDK image (~800MB) has the compiler; it never ships in the final image.
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy only the project files first, restore, THEN copy the rest of the
# source. Docker caches each instruction as a layer - as long as no .csproj
# changed, this restore layer is reused on every rebuild instead of
# re-downloading every NuGet package each time you edit a .cs file.
COPY global.json Directory.Build.props Directory.Packages.props ZigZag.sln ./
COPY src/ZigZag.API/ZigZag.API.csproj src/ZigZag.API/
COPY src/ZigZag.Application/ZigZag.Application.csproj src/ZigZag.Application/
COPY src/ZigZag.Domain/ZigZag.Domain.csproj src/ZigZag.Domain/
COPY src/ZigZag.Infrastructure/ZigZag.Infrastructure.csproj src/ZigZag.Infrastructure/
RUN dotnet restore src/ZigZag.API/ZigZag.API.csproj

COPY src/ ./src/
RUN dotnet publish src/ZigZag.API/ZigZag.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ---- Stage 2: runtime -------------------------------------------------------
# The ASP.NET runtime image (~200MB) has no compiler, no SDK - just what's
# needed to execute an already-built app. This is the image that actually
# ships and runs.
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# curl is not in the base image; added only so the HEALTHCHECK below can call
# the API's own /health endpoint. --no-install-recommends + cleaning the apt
# cache keeps this from bloating the image much.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# The base image ships a built-in unprivileged "app" user - running as that
# instead of root is a real security improvement for near-zero effort.
USER app

COPY --from=build --chown=app:app /app/publish .

# Containers don't terminate TLS themselves in this setup (a reverse proxy or
# Azure App Service does that in front, in later phases) - Kestrel just
# listens on plain HTTP inside the container network.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "ZigZag.API.dll"]

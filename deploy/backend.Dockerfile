FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend/Hop.Api/Hop.Api.csproj backend/Hop.Api/
RUN dotnet restore backend/Hop.Api/Hop.Api.csproj

COPY backend/Hop.Api/ backend/Hop.Api/
COPY frontend/src/assets/logo/hospital-logo.png backend/Hop.Api/assets/logo/hospital-logo.png
RUN dotnet publish backend/Hop.Api/Hop.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && groupadd --system hop \
    && useradd --system --gid hop --create-home --home-dir /home/hop hop \
    && mkdir -p /app/storage \
    && chown -R hop:hop /app /home/hop
COPY --from=build --chown=hop:hop /app/publish .
USER hop
EXPOSE 8080
ENTRYPOINT ["dotnet", "Hop.Api.dll"]

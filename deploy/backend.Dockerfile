FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY backend/Hop.Api/Hop.Api.csproj backend/Hop.Api/
RUN dotnet restore backend/Hop.Api/Hop.Api.csproj

COPY backend/Hop.Api/ backend/Hop.Api/
RUN dotnet publish backend/Hop.Api/Hop.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Hop.Api.dll"]

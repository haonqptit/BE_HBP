# Build context is the repository root: docker build -f src/HBP.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY src/HBP.Domain/HBP.Domain.csproj src/HBP.Domain/
COPY src/HBP.Application/HBP.Application.csproj src/HBP.Application/
COPY src/HBP.Infrastructure/HBP.Infrastructure.csproj src/HBP.Infrastructure/
COPY src/HBP.Api/HBP.Api.csproj src/HBP.Api/
RUN dotnet restore src/HBP.Api/HBP.Api.csproj

COPY src/ src/
RUN dotnet publish src/HBP.Api/HBP.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Media lives on a mounted volume; create it up front so the non-root user owns it.
RUN mkdir -p /data/media && chown -R app:app /data/media
COPY --from=build --chown=app:app /app/publish ./

USER app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Media__StorageRoot=/data/media

ENTRYPOINT ["dotnet", "HBP.Api.dll"]

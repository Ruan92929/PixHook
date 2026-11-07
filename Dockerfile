# ---- build ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY Directory.Build.props global.json ./
COPY src/PixHook.Domain/PixHook.Domain.csproj src/PixHook.Domain/
COPY src/PixHook.Application/PixHook.Application.csproj src/PixHook.Application/
COPY src/PixHook.Infrastructure/PixHook.Infrastructure.csproj src/PixHook.Infrastructure/
COPY src/PixHook.Api/PixHook.Api.csproj src/PixHook.Api/
RUN dotnet restore src/PixHook.Api/PixHook.Api.csproj
COPY src/ src/
RUN dotnet publish src/PixHook.Api/PixHook.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
RUN mkdir -p /data /app/logs && chown -R app:app /data /app
USER app
COPY --from=build --chown=app:app /app/publish .
ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__PixHook="Data Source=/data/pixhook.db" \
    Database__MigrateOnStartup=true \
    Swagger__Enabled=true
EXPOSE 8080
VOLUME ["/data"]
ENTRYPOINT ["dotnet", "PixHook.Api.dll"]

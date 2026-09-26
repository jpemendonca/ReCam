FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/Recam.Server/Recam.Server.csproj src/Recam.Server/
COPY src/Recam.Web/Recam.Web.csproj src/Recam.Web/
RUN dotnet restore src/Recam.Server/Recam.Server.csproj
COPY src/ src/
RUN dotnet publish src/Recam.Server/Recam.Server.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS= \
    RECAM_DATA_DIR=/data
# A named volume copies this ownership on first use, so the non-root user can write to it.
# MediaMTX runs as the same user and writes the recordings.
RUN mkdir /data /recordings && chown "$APP_UID" /data /recordings
USER $APP_UID
EXPOSE 8443
HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=3 \
    CMD ["dotnet", "Recam.Server.dll", "healthcheck"]
ENTRYPOINT ["dotnet", "Recam.Server.dll"]

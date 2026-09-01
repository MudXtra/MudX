# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0@sha256:a4556ed033fa96f984bb7a8d348851cb2d36b1281dd2420070045f664fbb5f94 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD bash -c 'exec 3<>/dev/tcp/127.0.0.1/8080; printf "GET /healthz HTTP/1.1\r\nHost: localhost\r\nConnection: close\r\n\r\n" >&3; grep -q "200 OK" <&3'


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0@sha256:e1ffd2a92ae84c1291bc1b6887501f8af98e6331e7af6d4c8d37168c5e87a64c AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MudX.Docs.Hybrid/MudX.Docs.Hybrid/MudX.Docs.Hybrid.csproj", "MudX.Docs.Hybrid/MudX.Docs.Hybrid/"]
COPY ["MudX.Docs.Hybrid/MudX.Docs.Hybrid.Client/MudX.Docs.Hybrid.Client.csproj", "MudX.Docs.Hybrid/MudX.Docs.Hybrid.Client/"]
COPY ["MudX.Docs/MudX.Docs.csproj", "MudX.Docs/"]
COPY ["MudX/MudX.csproj", "MudX/"]
RUN dotnet restore "./MudX.Docs.Hybrid/MudX.Docs.Hybrid/MudX.Docs.Hybrid.csproj"
COPY . .
WORKDIR "/src/MudX.Docs.Hybrid/MudX.Docs.Hybrid"
RUN dotnet clean "./MudX.Docs.Hybrid.csproj" -c $BUILD_CONFIGURATION
RUN dotnet build "./MudX.Docs.Hybrid.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:BuildInParallel=false

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG SOURCE_DATE_EPOCH=0
RUN dotnet publish "./MudX.Docs.Hybrid.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false && \
    release_timestamp=$(date --utc --date="@$SOURCE_DATE_EPOCH" '+%a, %d %b %Y %H:%M:%S GMT') && \
    find /app/publish -name '*.staticwebassets.endpoints.json' -exec \
        sed -i -E "s|(\"Name\":\"Last-Modified\",\"Value\":\")[^\"]*\"|\1$release_timestamp\"|g" {} +

# Copy generated static assets to final publish folder
#RUN cp -r /src/MudX/wwwroot /app/publish/MudX/wwwroot && \
#    cp -r /src/MudX.Docs/wwwroot /app/publish/MudX.Docs/wwwroot

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MudX.Docs.Hybrid.dll"]

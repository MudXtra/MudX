# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["MudX.Docs.Hybrid/MudX.Docs.Hybrid/MudX.Docs.Hybrid.csproj", "MudX.Docs.Hybrid/MudX.Docs.Hybrid/"]
COPY ["MudX.Docs.Hybrid/MudX.Docs.Hybrid.Client/MudX.Docs.Hybrid.Client.csproj", "MudX.Docs.Hybrid/MudX.Docs.Hybrid.Client/"]
COPY ["MudX.Docs/MudX.Docs.csproj", "MudX.Docs/"]
COPY ["MudX/MudX.csproj", "MudX/"]
COPY ["MudX.DependsOn/MudX.DependsOn.csproj", "MudX.DependsOn/"]
RUN dotnet restore "./MudX.Docs.Hybrid/MudX.Docs.Hybrid/MudX.Docs.Hybrid.csproj"
COPY . .
WORKDIR "/src/MudX.Docs.Hybrid/MudX.Docs.Hybrid"
RUN dotnet clean "./MudX.Docs.Hybrid.csproj" -c $BUILD_CONFIGURATION
RUN dotnet build "./MudX.Docs.Hybrid.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:BuildInParallel=false

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
RUN dotnet publish "./MudX.Docs.Hybrid.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Copy generated static assets to final publish folder
#RUN cp -r /src/MudX/wwwroot /app/publish/MudX/wwwroot && \
#    cp -r /src/MudX.Docs/wwwroot /app/publish/MudX.Docs/wwwroot

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MudX.Docs.Hybrid.dll"]

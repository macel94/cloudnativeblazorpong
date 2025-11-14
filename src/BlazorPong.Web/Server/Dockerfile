#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BlazorPong.Web/Server/BlazorPong.Web.Server.csproj", "BlazorPong.Web/Server/"]
COPY ["BlazorPong.Backend.Defaults/BlazorPong.Backend.Defaults.csproj", "BlazorPong.Backend.Defaults/"]
COPY ["BlazorPong.Web/Client/BlazorPong.Web.Client.csproj", "BlazorPong.Web/Client/"]
COPY ["BlazorPong.Web.Components/BlazorPong.Web.Components.csproj", "BlazorPong.Web.Components/"]
COPY ["BlazorPong.Web/Shared/BlazorPong.Web.Shared.csproj", "BlazorPong.Web/Shared/"]
RUN dotnet restore "./BlazorPong.Web/Server/BlazorPong.Web.Server.csproj"
COPY . .
WORKDIR "/src/BlazorPong.Web/Server"
RUN dotnet build "./BlazorPong.Web.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BlazorPong.Web.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
# Install curl as root, then revert to non-root
USER root
RUN apt-get update \
 && apt-get install -y --no-install-recommends curl \
 && rm -rf /var/lib/apt/lists/*

# Switch back to the built-in non-root user for security
USER app

WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlazorPong.Web.Server.dll"]
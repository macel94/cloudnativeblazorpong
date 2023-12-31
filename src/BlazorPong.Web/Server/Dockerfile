# Using .NET Chiseled image as the base
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base
USER app
WORKDIR /app
EXPOSE 8080

# Building the app using the standard .NET SDK image
FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BlazorPong.Web/Server/BlazorPong.Web.Server.csproj", "BlazorPong.Web/Server/"]
COPY ["BlazorPong.Web/Client/BlazorPong.Web.Client.csproj", "BlazorPong.Web/Client/"]
COPY ["BlazorPong.Components/BlazorPong.Components.csproj", "BlazorPong.Components/"]
COPY ["BlazorPong.Web/Shared/BlazorPong.Web.Shared.csproj", "BlazorPong.Web/Shared/"]
RUN dotnet restore "./BlazorPong.Web/Server/BlazorPong.Web.Server.csproj"
COPY . .
WORKDIR "/src/BlazorPong.Web/Server"
RUN dotnet build "./BlazorPong.Web.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BlazorPong.Web.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage, using the Chiseled base image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlazorPong.Web.Server.dll"]

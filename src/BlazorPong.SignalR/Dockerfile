#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base
USER app
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["BlazorPong.SignalR/BlazorPong.SignalR.csproj", "BlazorPong.SignalR/"]
COPY ["BlazorPong.Backend.Defaults/BlazorPong.Backend.Defaults.csproj", "BlazorPong.Backend.Defaults/"]
COPY ["BlazorPong.Web/Shared/BlazorPong.Web.Shared.csproj", "BlazorPong.Web/Shared/"]
RUN dotnet restore "./BlazorPong.SignalR/BlazorPong.SignalR.csproj"
COPY . .
WORKDIR "/src/BlazorPong.SignalR"
RUN dotnet build "./BlazorPong.SignalR.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./BlazorPong.SignalR.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BlazorPong.SignalR.dll"]
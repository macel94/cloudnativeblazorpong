# GitHub Copilot Instructions for cloudnativeblazorpong

## Project Overview

This project is a cloud-native Blazor Pong game. It appears to be a multi-project .NET solution, likely using Blazor WebAssembly for the frontend, a .NET backend (possibly SignalR for real-time communication), and potentially Azure SQL for the database. The project also includes Docker configuration for containerization and observability components (Grafana, Prometheus, Loki, Tempo).

## Key Technologies

*   **Languages**: C#
*   **Frameworks/Libraries**: .NET, Blazor (WebAssembly and Server), ASP.NET Core, SignalR, Entity Framework Core
*   **Database**: Likely Azure SQL (based on `Blazorpong.DB.AzureSql.sqlproj`)
*   **Containerization**: Docker, Docker Compose
*   **Observability**: Grafana, Prometheus, Loki, OpenTelemetry (otel-collector-config.yaml), Tempo
*   **Build System**: .NET SDK (MSBuild)

## Important Directories and Files

*   `src/BlazorPong.sln`: The main solution file for the .NET projects.
*   `src/BlazorPong.Web/`: Contains the Blazor frontend (Client) and backend (Server) projects.
    *   `src/BlazorPong.Web/Client/Pages/`: Contains the Blazor pages (routable components).
    *   `src/BlazorPong.Web/Client/Program.cs`: Entry point for the Blazor WebAssembly client.
    *   `src/BlazorPong.Web/Server/Program.cs`: Entry point for the Blazor Web backend.
*   `src/BlazorPong.Web.Components/`: Contains reusable Blazor UI components, like `PongComponent.razor`.
*   `src/BlazorPong.SignalR/`: Likely handles real-time communication for the game using SignalR.
    *   `src/BlazorPong.SignalR/Hubs/GameHub.cs`: The SignalR hub for game logic.
    *   `src/BlazorPong.SignalR/Rooms/`: Contains logic for managing game rooms.
*   `src/BlazorPong.Backend.Defaults/`: Potentially shared backend configurations or defaults.
*   `src/Blazorpong.DB.AzureSql/`: Contains the database project for Azure SQL.
    *   `src/Blazorpong.DB.AzureSql/Tables/`: SQL table definitions.
*   `src/docker-compose.yml`: Defines the services, networks, and volumes for the Dockerized application.
*   `src/Observability/`: Contains configurations for observability tools.
    *   `otel-collector-config.yaml`: OpenTelemetry Collector configuration.
    *   `prometheus.yaml`: Prometheus configuration.
    *   `grafana-datasources.yaml`: Grafana datasource configurations.

## Coding Conventions and Best Practices

*   Follow standard C# and .NET coding conventions.
*   Utilize Dependency Injection, especially in ASP.NET Core and Blazor projects.
*   When working with Blazor components:
    *   Separate C# logic into `@code` blocks or code-behind files (`.razor.cs`).
    *   Use `@inject` for services.
    *   Follow component lifecycle methods.
*   For SignalR:
    *   Hub methods should be asynchronous (`async Task`).
    *   Clearly define client-callable methods and server-callable methods.
*   For Entity Framework Core:
    *   Define entities in the `EFCore` folder (e.g., `Client.cs`, `Room.cs`).
    *   The `PongDbContext.cs` is the database context.
*   When adding new projects, ensure they are added to the `BlazorPong.sln` solution file.
*   Keep Dockerfiles optimized for build speed and image size.

## How to Interact with Copilot

*   **Be specific**: When asking for code, mention the specific project (e.g., "in `BlazorPong.Web.Client`", "for the `GameHub`").
*   **Provide context**: If working on a specific component or service, mention its name or purpose.
*   **Refer to existing patterns**: If you want code that follows a pattern already in the project, point Copilot to an example. For instance, "generate a new Blazor page similar to `Index.razor` for displaying game scores."
*   **Database interactions**: When asking for database-related code, specify if it should use Entity Framework Core and the `PongDbContext`.
*   **SignalR communication**: For features involving real-time updates, mention that SignalR is used and refer to `GameHub.cs`.

## Build and Run

*   The solution can likely be built using `dotnet build src/BlazorPong.sln`.
*   The application can be run using Docker Compose: `docker compose up -f src/docker-compose.yml`.
*   Individual projects might have their own launch profiles (see `Properties/launchSettings.json` files).
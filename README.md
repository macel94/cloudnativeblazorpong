# Cloud Native Blazor Pong

Cloud Native Blazor Pong is a revival of the classic [BlazorPong](https://github.com/macel94/BlazorPong) with enhanced features including authentication, multi-room support, automatic infrastructure scalability, and a highscores page with statistical data on player performance.

# Architecture\Flow diagram

```mermaid
sequenceDiagram
    participant BlazorClient1 as Player 1 (Blazor WASM)
    participant BlazorClient2 as Player 2 (Blazor WASM)
    participant SignalRHub as SignalR Hub
    participant BackgroundService as Background Service
    participant NETBackend as .NET Backend

    Note over BlazorClient1, BlazorClient2: Players connect to the website at /pong
    BlazorClient1->>SignalRHub: Connects to SignalR
    BlazorClient2->>SignalRHub: Connects to SignalR
    SignalRHub->>NETBackend: Notify of new connections
    NETBackend->>BackgroundService: Initialize game session
    BackgroundService->>SignalRHub: Send game state updates
    loop Game Loop
        BlazorClient1->>SignalRHub: Send bar movements
        BlazorClient2->>SignalRHub: Send bar movements
        SignalRHub->>BackgroundService: Relay player actions
        BackgroundService->>SignalRHub: Update ball position
        SignalRHub->>BlazorClient1: Update game state
        SignalRHub->>BlazorClient2: Update game state
    end
```

# Prerequisites

This will be removed and dev containers will be used instead.
Anyway, to use locally you need to have the following installed:
https://redis.io/docs/install/install-redis/install-redis-on-windows/
https://github.com/Azure/azure-signalr/blob/dev/docs/emulator.md
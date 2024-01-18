using BlazorPong.SignalR.EFCore;
using BlazorPong.SignalR.Rooms;
using BlazorPong.Web.Shared;
using BlazorPong.Web.Shared.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BlazorPong.SignalR.Hubs;

//TODO Create interface and use method names with nameof on the client side
public class GameHub(IRoomsManager roomGamesManager, PongDbContext pongDbContext, RedisRoomStateCache roomsDictionary, ILogger<GameHub> logger) : Hub<IBlazorPongClient>, IGameHub
{
    public async Task UpdateGameObjectPosition(Guid roomId, GameObject clientGameObject)
    {
        clientGameObject.LastUpdatedBy = Context.ConnectionId;
        await roomGamesManager.UpdateGameObjectPositionOnServer(roomId, clientGameObject);
    }

    public async Task SetPlayerIsReady(Guid roomId)
    {
        await roomGamesManager.SetPlayerIsReadyAsync(roomId, Context.ConnectionId);
    }

    public async Task<Dictionary<string, GameObject>> GetGameObjects(Guid roomId)
    {
        var roomState = await roomsDictionary.UnsafeGetRoomStateAsync(roomId) ?? throw new InvalidOperationException($"Room with id {roomId} not found");
        if (roomState.GameObjectsDictionary == null || roomState.GameObjectsDictionary.Count != 3)
        {
            // Aggiungo solo i mancanti se sono qui
            roomState = await roomGamesManager.InitializeGameObjectsOnServer(roomId, false);
        }

        return roomState.GameObjectsDictionary!;
    }

    public override Task OnConnectedAsync()
    {
        // TODO Save in db the current signalR server name and connection id so that it
        // can be displayed later even if the server restarts or changes
        // possibly notifying it to the client

        // https://github.com/microsoft/dotnet-podcasts/blob/main/src/Services/ListenTogether/ListenTogether.Hub/Hubs/ListenTogetherHub.cs
        return base.OnConnectedAsync();
    }

    public async Task<Roles> OpenRoom(Guid roomId, string userName)
    {
        // Create this room in the db using dbContext
        pongDbContext.Rooms.Add(new Room { Id = roomId });
        await pongDbContext.SaveChangesAsync(Context.ConnectionAborted);
        // Da capire se necessario
        //await Clients.Caller.SendAsync(RoomOpenedMethod);
        logger.LogInformation($"Room {roomId} opened");

        // Wait for a server to pick up the room so that we find the room in the dictionary of states when we join
        await Task.Delay(GameConstants.RoomCheckDelayInMs);

        return await JoinRoom(roomId, userName);
    }

    public async Task<Roles> JoinRoom(Guid roomId, string userName)
    {
        var room = await pongDbContext.Rooms.Include(x => x.Clients).FirstOrDefaultAsync(x => x.Id == roomId, Context.ConnectionAborted) ?? throw new InvalidOperationException($"Room with id {roomId} not found");
        // This will need to change to be based on nickname or something else, not on connection id
        Roles role;
        var roomstate = await roomsDictionary.UnsafeGetRoomStateAsync(roomId);
        switch (room.Clients.Count)
        {
            case 0:
                role = Roles.Player1;
                break;
            case 1:
                role = Roles.Player2;
                break;
            default:
                role = Roles.Spectator;
                break;
        }
        await roomGamesManager.SetPlayerConnectionIdAsync(roomstate, role, Context.ConnectionId);

        room.Clients.Add(new EFCore.Client { ConnectionId = Context.ConnectionId, Role = (byte)role, Username = userName, RoomId = roomId });

        await pongDbContext.SaveChangesAsync();
        var roomIdAsString = roomId.ToString();
        await Groups.AddToGroupAsync(Context.ConnectionId, roomIdAsString, Context.ConnectionAborted);
        // TODO - This will be implemented as a notification with the name of whoever connected
        //await Clients.Group(roomIdAsString).SendAsync(UpdateRoomMethod, room);

        logger.LogInformation($"Room {roomId} joined by {userName} as {role}");
        return role;
    }

    public async Task LeaveRoom(Guid roomId)
    {
        var room = await pongDbContext.Rooms.Include(x => x.Clients).FirstOrDefaultAsync(x => x.Id == roomId, Context.ConnectionAborted) ?? throw new InvalidOperationException($"Room with id {roomId} not found");
        await LeaveRoomAsync(room);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Search the db for the room with the connection id and remove the client
        var room = await pongDbContext.Rooms.Include(x => x.Clients).FirstOrDefaultAsync(x => x.Clients.Any(x => x.ConnectionId == Context.ConnectionId));
        if (room == null)
        {
            return;
        }
        await LeaveRoomAsync(room);
        await base.OnDisconnectedAsync(exception);
    }

    private async Task LeaveRoomAsync(Room room)
    {
        var client = room.Clients.Single(x => x.ConnectionId == Context.ConnectionId);
        room.Clients.Remove(client);
        var roomState = await roomsDictionary.UnsafeGetRoomStateAsync(room.Id);

        if (client.Role == (byte)Roles.Player1)
        {
            if (roomState.MustPlayGame)
            {
                await roomGamesManager.Player1Disconnected(room.Id);
            }
        }
        else if (client.Role == (byte)Roles.Player2)
        {
            if (roomState.MustPlayGame)
            {
                await roomGamesManager.Player2Disconnected(room.Id);
            }
        }

        await pongDbContext.SaveChangesAsync(Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.Id.ToString(), Context.ConnectionAborted);
        // TODO - Understand if we need to send the room update to the clients
        //await Clients.Group(roomId.ToString()).SendAsync(UpdateRoomMethod, room, cancellationToken);
    }
}

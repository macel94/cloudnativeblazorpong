using System.Threading;
using BlazorPong.Web.Server.EFCore;
using BlazorPong.Web.Shared;
using BlazorPong.Web.Shared.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BlazorPong.Web.Server.Rooms.Games.Hubs;

//TODO Create interface and use method names with nameof on the client side
public class GameHub(RoomsManager roomGamesManager, PongDbContext pongDbContext, ILogger<GameHub> logger) : Hub<IBlazorPongClient>
{
    public void UpdateGameObjectPosition(Guid roomId, GameObject clientGameObject)
    {
        clientGameObject = clientGameObject with { LastUpdatedBy = Context.ConnectionId };
        roomGamesManager.UpdateGameObjectPositionOnServer(roomId, clientGameObject);
    }
    public void SetPlayerIsReady(Guid roomId)
    {
        var roomstate = roomGamesManager.RoomsDictionary[roomId];
        if (roomstate.Player1ConnectionId == Context.ConnectionId)
        {
            roomstate.Player1Ready = true;
        }
        else if (roomstate.Player2ConnectionId == Context.ConnectionId)
        {
            roomstate.Player2Ready = true;
        }
    }

    public Dictionary<string, GameObject> GetGameObjects(Guid roomId)
    {
        var roomState = (roomGamesManager.RoomsDictionary?[roomId]) ?? throw new InvalidOperationException($"Room with id {roomId} not found");
        if (roomState.GameObjectsDictionary == null || roomState.GameObjectsDictionary.Count != 3)
        {
            // Aggiungo solo i mancanti se sono qui
            roomGamesManager.InitializeGameObjectsOnServer(roomId, false);
        }

        return roomGamesManager.RoomsDictionary?[roomId]!.GameObjectsDictionary!;
    }

    public override Task OnConnectedAsync()
    {
        // TODO Save in db the current signalR server name and connection id so that it
        // can be displayed later even if the server restarts or changes
        // possibly notifying it to the client

        // https://github.com/microsoft/dotnet-podcasts/blob/main/src/Services/ListenTogether/ListenTogether.Hub/Hubs/ListenTogetherHub.cs
        return base.OnConnectedAsync();
    }

    public async Task<Role> OpenRoom(Guid roomId, string userName)
    {
        // Create this room in the db using dbContext
        pongDbContext.Rooms.Add(new Room { Id = roomId });
        await pongDbContext.SaveChangesAsync(Context.ConnectionAborted);
        // Da capire se necessario
        //await Clients.Caller.SendAsync(RoomOpenedMethod);
        logger.LogInformation($"Room {roomId} opened");

        // Wait for a server to pick up the room so that we find the room in the dictionary of states when we join
        await Task.Delay(GameConstants.RoomCheckDelayBetweenTicksInMs);

        return await JoinRoom(roomId, userName);
    }

    public async Task<Role> JoinRoom(Guid roomId, string userName)
    {
        var room = await pongDbContext.Rooms.Include(x => x.Clients).FirstOrDefaultAsync(x => x.Id == roomId, Context.ConnectionAborted) ?? throw new InvalidOperationException($"Room with id {roomId} not found");
        // This will need to change to be based on nickname or something else, not on connection id
        Role result;
        var roomstate = roomGamesManager.RoomsDictionary[roomId];
        switch (room.Clients.Count)
        {
            case 0:
                result = Role.Player1;
                roomstate.Player1ConnectionId = Context.ConnectionId;
                break;
            case 1:
                result = Role.Player2;
                roomstate.Player2ConnectionId = Context.ConnectionId;
                break;
            default:
                result = Role.Spectator;
                break;
        }

        room.Clients.Add(new EFCore.Client { ConnectionId = Context.ConnectionId, Role = (byte)result, Username = userName, RoomId = roomId });

        await pongDbContext.SaveChangesAsync();
        var roomIdAsString = roomId.ToString();
        await Groups.AddToGroupAsync(Context.ConnectionId, roomIdAsString, Context.ConnectionAborted);
        // TODO - This will be implemented as a notification with the name of whoever connected
        //await Clients.Group(roomIdAsString).SendAsync(UpdateRoomMethod, room);

        logger.LogInformation($"Room {roomId} joined by {userName} as {result}");
        return result;
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
        var roomState = roomGamesManager.RoomsDictionary[room.Id];

        if (client.Role == (byte)Role.Player1)
        {
            if (roomState.MustPlayGame)
            {
                roomGamesManager.Player1Disconnected(room.Id);
            }
        }
        else if (client.Role == (byte)Role.Player2)
        {
            if (roomState.MustPlayGame)
            {
                roomGamesManager.Player2Disconnected(room.Id);
            }
        }

        await pongDbContext.SaveChangesAsync(Context.ConnectionAborted);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, room.Id.ToString(), Context.ConnectionAborted);
        // TODO - Understand if we need to send the room update to the clients
        //await Clients.Group(roomId.ToString()).SendAsync(UpdateRoomMethod, room, cancellationToken);
    }
}

using BlazorPong.Web.Server.EFCore;
using BlazorPong.Web.Shared;
using BlazorPong.Web.Shared.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace BlazorPong.Web.Server.Rooms.Games.Hubs;

//TODO Create interface and use method names with nameof on the client side
public class GameHub(RoomsManager roomGamesManager, PongDbContext pongDbContext) : Hub<IBlazorPongClient>
{
    public void UpdateGameObjectPosition(Guid roomId, GameObject clientGameObject)
    {
        clientGameObject = clientGameObject with { LastUpdatedBy = Context.ConnectionId };
        roomGamesManager.UpdateGameObjectPositionOnServer(roomId, clientGameObject);
    }

    public async Task<Role> GetClientType(Guid roomId)
    {
        var room = await pongDbContext.Rooms.Include(x => x.Clients).FirstOrDefaultAsync(x => x.Id == roomId) ?? throw new InvalidOperationException($"Room with id {roomId} not found");
        // This will need to change to be based on nickname or something else, not on connection id
        Role role;
        var roomstate = roomGamesManager.RoomsDictionary[roomId];
        switch (room.Clients.Count)
        {
            case 0:
                role = Role.Player1;
                roomstate.Player1ConnectionId = Context.ConnectionId;
                break;
            case 1:
                role = Role.Player2;
                roomstate.Player2ConnectionId = Context.ConnectionId;
                break;
            default:
                role = Role.Spectator;
                break;
        }

        room.Clients.Add(new EFCore.Client{ Id = Context.ConnectionId, Role = role, RoomId = roomId });

        await pongDbContext.SaveChangesAsync();

        return role;
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

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Search the db for the room with the connection id and remove the client
        var room = await pongDbContext.Rooms.Include(x => x.Clients).FirstOrDefaultAsync(x => x.Clients.Any(x => x.Id == Context.ConnectionId));
        if(room == null)
        {
            return;
        }

        var client = room.Clients.Single(x => x.Id == Context.ConnectionId);
        var roomState = roomGamesManager.RoomsDictionary[room.Id];

        if (client.Role == Role.Player1)
        {
            if (roomState.MustPlayGame)
            {
                roomGamesManager.Player1Disconnected(room.Id);
            }
        }
        else if (client.Role == Role.Player2)
        {
            if (roomState.MustPlayGame)
            {
                roomGamesManager.Player2Disconnected(room.Id);
            }
        }

        await base.OnDisconnectedAsync(exception);
    }
}

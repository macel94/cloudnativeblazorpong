using BlazorPong.Web.Server.EFCore;
using BlazorPong.Web.Server.Rooms.Games;
using BlazorPong.Web.Shared;
using BlazorPong.Web.Shared.Clock;
using Microsoft.EntityFrameworkCore;

namespace BlazorPong.Web.Server.Rooms;

public class RoomsManager(IBallManager ballManager,
                          PongDbContext pongDbContext,
                          RedisRoomStateCache roomsDictionary,
                          ILogger<RoomsManager> logger,
                          ISystemClock systemClock) : IRoomsManager
{
    public async Task<RoomState> InitializeGameObjectsOnServer(Guid roomId, bool forceInitialization)
    {
        Dictionary<string, GameObject> tempInitGameObjects = new()
        {
            {
                GameConstants.Player1RoleAsString,
                new(Id: GameConstants.Player1RoleAsString,
                    Width : 2,
                    Height : 9)
                {
                    Left=10,
                    Top=50,
                }
            },
            {
                GameConstants.Player2RoleAsString,
                new(Id : GameConstants.Player2RoleAsString,
                    Width: 2,
                    Height: 9)
                {
                    Left=90,
                    Top=50
                }
            },
            {
                GameConstants.BallRoleAsString,
                new(Id : GameConstants.BallRoleAsString,
                    Width: 1.5,
                    Height: 3)
                {
                    Left=50,
                    Top=50,
                    Angle = Random.Shared.Next(1, 5) switch
                    {
                        1 => 45,
                        2 => 135,
                        3 => 225,
                        4 => 315,
                        _ => throw new InvalidOperationException("Random number is not between 1 and 4")
                    }
                }
            }
        };
        var roomState = await roomsDictionary.UnsafeGetRoomStateAsync(roomId);

        if (!forceInitialization)
        {
            foreach (var tempInitPair in tempInitGameObjects)
            {
                if (!roomState.GameObjectsDictionary.ContainsKey(tempInitPair.Key))
                {
                    roomState.GameObjectsDictionary.Add(tempInitPair.Key, tempInitPair.Value);
                }
            }
        }
        else
        {
            roomState.GameObjectsDictionary = tempInitGameObjects!;
        }

        await roomsDictionary.SetRoomStateAsync(roomId, roomState);
        return roomState;
    }

    public async Task UpdateGameObjectPositionOnServer(Guid roomId, GameObject clientUpdatedObject)
    {
        var roomState = await roomsDictionary.UnsafeGetRoomStateAsync(roomId);

        var gameObject = roomState.GameObjectsDictionary[clientUpdatedObject.Id];

        if (gameObject != null)
        {
            gameObject.Left = clientUpdatedObject.Left;
            gameObject.Top = clientUpdatedObject.Top;
            gameObject.LastUpdatedBy = clientUpdatedObject.LastUpdatedBy;
            gameObject.LastUpdate = clientUpdatedObject.LastUpdate;
            gameObject.LastTimeServerReceivedUpdate = systemClock.UtcNow.ToUnixTimeMilliseconds();
            gameObject.LastSinglaRServerReceivedUpdateName = Environment.MachineName;

            roomState.GameObjectsDictionary[clientUpdatedObject.Id] = gameObject;
            await roomsDictionary.SetRoomStateAsync(roomId, roomState);
        }
    }

    public async Task<int> AddPlayer1Point(Guid roomId)
    {
        var roomState = await roomsDictionary.UnsafeGetRoomStateAsync(roomId);

        roomState.Player1Points++;
        await roomsDictionary.SetRoomStateAsync(roomId, roomState);

        roomState = await InitializeGameObjectsOnServer(roomId, true);

        if (roomState.Player1Points == 3)
        {
            roomState.GameMustReset = true;
            await roomsDictionary.SetRoomStateAsync(roomId, roomState);
        }

        return roomState.Player1Points;
    }

    public async Task Player1Disconnected(Guid roomId)
    {
        var roomState = await InitializeGameObjectsOnServer(roomId, true);
        roomState.Player2Points = 3;
        roomState.GameMustReset = true;
        roomState.Player1ConnectionId = null;
        await roomsDictionary.SetRoomStateAsync(roomId, roomState);
    }

    public async Task<int> AddPlayer2Point(Guid roomId)
    {
        var roomState = await roomsDictionary.UnsafeGetRoomStateAsync(roomId);

        roomState.Player2Points++;
        await roomsDictionary.SetRoomStateAsync(roomId, roomState);

        roomState = await InitializeGameObjectsOnServer(roomId, true);

        if (roomState.Player2Points == 3)
        {
            roomState.GameMustReset = true;
            await roomsDictionary.SetRoomStateAsync(roomId, roomState);
        }
        return roomState.Player2Points;
    }

    public async Task Player2Disconnected(Guid roomId)
    {
        var roomState = await InitializeGameObjectsOnServer(roomId, true);
        roomState.Player1Points = 3;
        roomState.GameMustReset = true;
        roomState.Player2ConnectionId = null;
        await roomsDictionary.SetRoomStateAsync(roomId, roomState);
    }

    public async Task<string> GetGameOverMessage(Guid roomId)
    {
        var result = string.Empty;
        var roomState = await roomsDictionary.UnsafeGetRoomStateAsync(roomId);

        // Dato che prendo il messaggio, riporto i punti a 0 come anche lo stato di player ready
        if (roomState.Player1Points == 3)
        {
            result = "Player1 won!";
        }
        else if (roomState.Player2Points == 3)
        {
            result = "Player2 won!";
        }

        roomState.Player1Points = 0;
        roomState.Player2Points = 0;
        roomState.Player2Ready = false;
        roomState.Player1Ready = false;
        roomState.GameMustReset = false;
        await roomsDictionary.SetRoomStateAsync(roomId, roomState);

        return result;
    }

    public async Task<string> UpdateBallPosition(Guid roomId)
    {
        var roomState = await roomsDictionary.UnsafeGetRoomStateAsync(roomId);

        var ball = roomState.GameObjectsDictionary[GameConstants.BallRoleAsString];
        var res = ballManager!.Update(ref ball!);

        // Verify collisions between player1 and ball
        if (ballManager.VerifyObjectsCollision(ball!, roomState.GameObjectsDictionary[GameConstants.Player1RoleAsString]!))
        {
            ballManager.OnPlayer1Hit(ref ball);
        }
        else if (ballManager.VerifyObjectsCollision(ball!, roomState.GameObjectsDictionary[GameConstants.Player2RoleAsString]!))
        {
            ballManager.OnPlayer2Hit(ref ball);
        }
        roomState.GameObjectsDictionary[GameConstants.BallRoleAsString] = ball;
        await roomsDictionary.SetRoomStateAsync(roomId, roomState);

        return res;
    }

    public async Task TryLockRoomAsync()
    {
        var room = await TryGetRoomWithoutServerAssignedAsync();
        if (room != null)
        {
            await AssignCurrentServerToRoom(room);
        }

        // TODO - Get empty rooms and either clear them or logically delete them and keep them for stats
        //var roomsToDelete = await GetRoomsWithoutPlayersByServer(Environment.MachineName);
        //foreach (var roomToDelete in roomsToDelete)
        //{
        //    await DeleteRoom(roomToDelete);
        //}
    }

    public async Task UnlockRoomAsync(Guid key)
    {
        var room = await pongDbContext.Rooms.FirstOrDefaultAsync(x => x.Id == key);
        if (room != null)
        {
            room.ServerName = null;
            await pongDbContext.SaveChangesAsync();

            var roomState = await roomsDictionary.UnsafeGetRoomStateAsync(room.Id);
            roomState.ServerName = null;
            await roomsDictionary.SetRoomStateAsync(room.Id, roomState);
            logger.LogInformation("Room unlocked by server: {MachineName}", Environment.MachineName);
        }
        else
        {
            logger.LogWarning("Room {RoomId} not found when trying to unlock it", key);
        }
    }

    private async Task AssignCurrentServerToRoom(Room room)
    {
        // Update the room entity with the current server name
        room.ServerName = Environment.MachineName;
        // save asynchronously, only if no one else has updated it in the meantime
        await pongDbContext.SaveChangesAsync();
        logger.LogInformation("Room locked for server: {MachineName}", Environment.MachineName);

        // Add the room the the current in-memory dictionary now that's locked
        await roomsDictionary.SetRoomStateAsync(room.Id, new()
        {
            RoomId = room.Id,
            ServerName = Environment.MachineName
        });

        return;
    }

    private Task<Room?> TryGetRoomWithoutServerAssignedAsync()
    {
        return pongDbContext.Rooms.FirstOrDefaultAsync(x => x.ServerName == null);
    }

    public async Task SetPlayerIsReadyAsync(Guid roomId, string connectionId)
    {
        var roomstate = await roomsDictionary.UnsafeGetRoomStateAsync(roomId);
        if (roomstate.Player1ConnectionId == connectionId)
        {
            roomstate.Player1Ready = true;
        }
        else if (roomstate.Player2ConnectionId == connectionId)
        {
            roomstate.Player2Ready = true;
        }

        await roomsDictionary.SetRoomStateAsync(roomId, roomstate);
    }

    public async Task SetPlayerConnectionIdAsync(RoomState roomstate, Roles role, string connectionId)
    {
        if (role == Roles.Spectator)
        {
            return;
        }

        if (role == Roles.Player1)
        {
            roomstate.Player1ConnectionId = connectionId;
        }
        else if (role == Roles.Player2)
        {
            roomstate.Player2ConnectionId = connectionId;
        }

        await roomsDictionary.SetRoomStateAsync(roomstate.RoomId, roomstate);
    }
}

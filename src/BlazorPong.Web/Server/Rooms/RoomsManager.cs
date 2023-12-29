using System.Collections.Concurrent;
using BlazorPong.Web.Server.EFCore;
using BlazorPong.Web.Server.Rooms.Games;
using BlazorPong.Web.Shared;
using Microsoft.EntityFrameworkCore;

namespace BlazorPong.Web.Server.Rooms;

public class RoomsManager(BallManager ballManager, PongDbContext pongDbContext, ILogger<RoomsManager> logger)
{
    // This is going to be shared between servers so it won't be in memory, and it will be read and written by multiple threads so it must be thread safe
    public ConcurrentDictionary<Guid, RoomState> RoomsDictionary = [];

    public void InitializeGameObjectsOnServer(Guid roomId, bool forceInitialization)
    {
        Dictionary<string, GameObject> tempInitGameObjects = new()
        {
            {
                "player1",
                new(Id: "player1",
                    LastUpdatedBy: string.Empty,
                    Width : 2,
                    Height : 9)
                {
                    Left=10,
                    Top=50
                }
            },
            {
                "player2",
                new(Id : "player2",
                    LastUpdatedBy: string.Empty,
                    Width: 2,
                    Height: 9)
                {
                    Left=90,
                    Top=50
                }
            },
            {
                "ball",
                new(Id : "ball",
                    LastUpdatedBy: string.Empty,
                    Width: 1.5,
                    Height: 3)
                {
                    Left=50,
                    Top=50
                }
            }
        };

        if (!forceInitialization)
        {
            var roomState = RoomsDictionary[roomId];

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
            RoomsDictionary[roomId].GameObjectsDictionary = tempInitGameObjects!;
        }
    }

    public void UpdateGameObjectPositionOnServer(Guid roomId, GameObject clientUpdatedObject)
    {
        var roomState = RoomsDictionary[roomId];

        var gameObject = roomState.GameObjectsDictionary[clientUpdatedObject.Id];

        if (gameObject != null)
        {
            gameObject = gameObject with
            {
                Left = clientUpdatedObject.Left,
                Top = clientUpdatedObject.Top,
                LastUpdatedBy = clientUpdatedObject.LastUpdatedBy,
                LastUpdateTicks = clientUpdatedObject.LastUpdateTicks,
                LastTickServerReceivedUpdate = DateTimeOffset.UtcNow.Ticks
            };

            roomState.GameObjectsDictionary[clientUpdatedObject.Id] = gameObject;
        }
    }

    public int AddPlayer1Point(Guid roomId)
    {
        var roomState = RoomsDictionary[roomId];

        roomState.Player1Points++;
        InitializeGameObjectsOnServer(roomId, true);
        // Understand if this is redundant or can just be avoided
        roomState = RoomsDictionary[roomId];

        if (roomState.Player1Points == 3)
        {
            roomState.GameMustReset = true;
        }
        return roomState.Player1Points;
    }

    public void Player1Disconnected(Guid roomId)
    {
        InitializeGameObjectsOnServer(roomId, true);
        var roomState = RoomsDictionary[roomId];
        roomState.Player2Points = 3;
        roomState.GameMustReset = true;
        roomState.Player1ConnectionId = null;
    }

    public int AddPlayer2Point(Guid roomId)
    {
        var roomState = RoomsDictionary[roomId];

        roomState.Player2Points++;
        InitializeGameObjectsOnServer(roomId, true);
        // Understand if this is redundant or can just be avoided
        roomState = RoomsDictionary[roomId];

        if (roomState.Player2Points == 3)
        {
            roomState.GameMustReset = true;
        }
        return roomState.Player2Points;
    }

    public void Player2Disconnected(Guid roomId)
    {
        InitializeGameObjectsOnServer(roomId, true);
        var roomState = RoomsDictionary[roomId];
        roomState.Player1Points = 3;
        roomState.GameMustReset = true;
        roomState.Player2ConnectionId = null;
    }

    public string GetGameOverMessage(Guid roomId)
    {
        var result = string.Empty;
        var roomState = RoomsDictionary[roomId];

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

        return result;
    }

    internal string UpdateBallPosition(Guid roomId)
    {
        var roomState = RoomsDictionary[roomId];

        var ball = roomState.GameObjectsDictionary["ball"];
        var res = ballManager!.Update(ref ball!);
        // TODO - Understand if this is necessary, ignored during refactoring
        roomState.GameObjectsDictionary["ball"] = ball;

        // Verify collisions between player1 and ball
        if (ballManager.VerifyObjectsCollision(ball!, roomState.GameObjectsDictionary["player1"]!))
        {
            ballManager.OnPlayer1Hit();
        }
        else if (ballManager.VerifyObjectsCollision(ball!, roomState.GameObjectsDictionary["player2"]!))
        {
            ballManager.OnPlayer2Hit();
        }

        return res;
    }

    internal async Task TryLockRoomAsync()
    {
        //TODO Implement a service and don't directly use the cache here

        var room = await TryGetRoomWithoutServerAssignedAsync();
        if (room != null)
        {
            await AssignCurrentServerToRoom(room);
        }

        // Get empty rooms and clear them
        //var roomsToDelete = await GetRoomsWithoutPlayersByServer(Environment.MachineName);
        //foreach (var roomToDelete in roomsToDelete)
        //{
        //    await DeleteRoom(roomToDelete);
        //}
    }

    private async Task AssignCurrentServerToRoom(Room room)
    {
        // Update the room entity with the current server name
        room.ServerName = Environment.MachineName;
        // save asynchronously, only if no one else has updated it in the meantime
        await pongDbContext.SaveChangesAsync();
        logger.LogInformation("Room locked for server: {MachineName}", Environment.MachineName);

        // Add the room the the current in-memory dictionary now that's locked
        RoomsDictionary.TryAdd(room.Id, new()
        {
            RoomId = room.Id
        });

        return;
    }

    private Task<Room?> TryGetRoomWithoutServerAssignedAsync()
    {
        return pongDbContext.Rooms.FirstOrDefaultAsync(x => x.ServerName == null);
    }
}

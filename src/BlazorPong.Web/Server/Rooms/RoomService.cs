using BlazorPong.Web.Server.Rooms.Games;

namespace BlazorPong.Web.Server.Rooms;

public class RoomService(RoomsManager roomGameManager, ILogger<RoomService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                logger.LogInformation("RoomService is running");
                await roomGameManager.TryLockRoomAsync();
                await Task.Delay(GameConstants.RoomCheckDelayInMs, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RoomGamesService");
            }
        }
    }
}

using BlazorPong.Web.Shared;

namespace BlazorPong.Web.Server.Rooms;

public class RoomService(IRoomsManager roomGameManager, ILogger<RoomService> logger)
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
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in RoomGamesService");
            }
            await Task.Delay(GameConstants.RoomCheckDelayInMs, cancellationToken);
        }
    }
}

namespace BlazorPong.Web.Shared;

public record GameObject(double Left, double Top, string Id, string LastUpdatedBy, int Width, int Height, long? LastUpdateTicks = 0)
{
    public bool Draggable { get; set; }
    public long LastSentUpdate = 0;
    public bool NeedsToSendUpdate => LastUpdateTicks > LastSentUpdate;
    public string LeftPx => $"{(int)Left}px";

    public string TopPx => $"{(int)Top}px";
}

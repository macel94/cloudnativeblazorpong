namespace BlazorPong.Web.Shared;

public record GameObject(string Id, string LastUpdatedBy, int Width, int Height, long? LastUpdateTicks = 0)
{
    public double Left { get; set; }
    public double Top { get; set; }
    public bool Draggable { get; set; }
    public long LastTickClientKnowsServerReceivedUpdate = 0;
    public long LastTickServerReceivedUpdate = 0;
    public bool WasUpdated => LastUpdateTicks > LastTickClientKnowsServerReceivedUpdate;
    public string ToStyle()
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        return $@"
top: {Top.ToString("0.00000", culture)}%;
left: {Left.ToString("0.00000", culture)}%;";
    }
}

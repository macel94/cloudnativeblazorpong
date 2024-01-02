namespace BlazorPong.Web.Shared;

public record GameObject(string Id, string LastUpdatedBy, double Width, double Height, long? LastUpdateTicks = 0)
{
    public double Left { get; set; }
    public double Top { get; set; }
    public bool Draggable { get; set; }
    public long LastTickClientKnowsServerReceivedUpdate { get; set; }
    public long LastTickConnectedServerReceivedUpdate { get; set; }
    public string? LastSinglaRServerReceivedUpdateName { get; set; }
    public bool WasUpdated => LastUpdateTicks > LastTickClientKnowsServerReceivedUpdate;
    public int Angle { get; set; }

    public string ToStyle()
    {
        var culture = System.Globalization.CultureInfo.InvariantCulture;

        return $@"
top: {Top.ToString("0.00000", culture)}%;
left: {Left.ToString("0.00000", culture)}%;
height: {Height.ToString("0.00000", culture)}%; 
width: {Width.ToString("0.00000", culture)}%; 
";
    }
}

namespace BlazorPong.Web.Shared;

public record GameObject(string Id, double Width, double Height)
{
    public double Left { get; set; }
    public double Top { get; set; }
    public long LastTickClientKnowsServerReceivedUpdate { get; set; }
    public long LastTimeServerReceivedUpdate { get; set; }
    public string? LastSinglaRServerReceivedUpdateName { get; set; }
    public long LastUpdate { get; set; }
    public string? LastUpdatedBy { get; set; }
    public bool WasUpdated => LastUpdate > LastTickClientKnowsServerReceivedUpdate;
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

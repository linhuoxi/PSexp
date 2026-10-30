namespace PsExporter.Models;

public sealed record WindowPlacementState
{
    public int X { get; init; }
    public int Y { get; init; }
    public int WorkX { get; init; }
    public int WorkY { get; init; }
    public int WorkWidth { get; init; }
    public int WorkHeight { get; init; }
    public double Scaling { get; init; } = 1;
    public bool IsDocked { get; init; }
    public string Edge { get; init; } = "None";
}

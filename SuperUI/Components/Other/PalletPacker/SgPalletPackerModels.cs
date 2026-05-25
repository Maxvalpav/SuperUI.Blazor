namespace SuperUI.Components;

public sealed class SgBox
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = string.Empty;
    public double Width { get; set; }
    public double Height { get; set; }
    public double Depth { get; set; }
    public double Weight { get; set; }
    public string Color { get; set; } = "#4a9eff";
    public int Quantity { get; set; } = 1;
}

public sealed class SgPallet
{
    public double Width { get; set; } = 120;
    public double Height { get; set; } = 144;
    public double Depth { get; set; } = 80;
    public double MaxWeight { get; set; } = 1000;
}

public sealed class SgPackedBox
{
    public SgBox Box { get; set; } = new();
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
    public int Step { get; set; }
}

public sealed class SgPackingResult
{
    public List<SgPackedBox> PackedBoxes { get; set; } = new();
    public List<SgBox> UnpackedBoxes { get; set; } = new();
    public double Utilization { get; set; }
    public double TotalWeight { get; set; }
    public TimeSpan ComputeTime { get; set; }
}

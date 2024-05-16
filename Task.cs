public class Task
{
    public double MinDist { get; set; }
    public double Budget { get; set; }
    public double[,] Locations { get; set; } = null!;
    public double[,] Costs { get; set; } = null!;
    public double[,] Powers { get; set; } = null!;

    public double ExpectedTotalPower { get; set; }
} 
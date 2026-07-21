namespace CharityRabbit.Models;

public class DoGooderModel
{
    public string? UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int EventCount { get; set; }
    public int CarrotsEarned { get; set; }  // Renamed from HoursVolunteered - 1 carrot per hour!
}

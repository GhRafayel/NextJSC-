
namespace Backend.Models;

public class Translations : ITimestamped
{
    public int      Id          { get; set; }
    public string   Key         { get; set; } = "";
    public string   Values      { get; set; } = "{}";
    public DateTime  CreatedAt  { get; set; } = DateTime.UtcNow;
    public DateTime  UpdatedAt  { get; set; } = DateTime.UtcNow;

}   
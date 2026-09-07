
namespace Backend.Models;

public class Translations
{
    public int      Id          { get; set; }
    public string   Key         { get; set; } = "";
    public string   Values      { get; set; } = "{}";
    public DateTime UpdatedAT   { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt   { get; set; } = DateTime.UtcNow;

}
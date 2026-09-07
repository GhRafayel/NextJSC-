using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public static class TranslationsSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "Translations");

        foreach ( string path in Directory.GetFiles(dir, "*.json"))
        {
            string key = Path.GetFileNameWithoutExtension(path);
            string values = await File.ReadAllTextAsync(path);
            Translations? row = await db.Translations.FirstOrDefaultAsync(t => t.Key == key);
            if (row is null)
                db.Translations.Add(new Translations {Key = key, Values = values});
            else
                row.Values = values;
        }

        await db.SaveChangesAsync();
    }
}
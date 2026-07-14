using System.Text.Json;
using CommunityPlatform.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.Infrastructure.Persistence.SeedData;

public static class LocationSeeder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>Tablo boşsa 81 il + 973 ilçeyi tek seferde yükler. İdempotent — sonraki
    /// başlatmalarda Cities dolu olduğu için hemen döner.</summary>
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Cities.AnyAsync())
            return;

        var cities = JsonSerializer.Deserialize<List<CitySeedDto>>(TurkeyLocationSeedData.Json, JsonOptions)
            ?? throw new InvalidOperationException("Lokasyon seed verisi çözümlenemedi.");

        foreach (var c in cities)
        {
            var city = new City { Name = c.Name, PlateCode = c.PlateCode };
            foreach (var districtName in c.Districts)
            {
                city.Districts.Add(new District { Name = districtName, CityId = city.Id });
            }
            db.Cities.Add(city);
        }

        await db.SaveChangesAsync();
    }

    private class CitySeedDto
    {
        public string PlateCode { get; set; } = null!;
        public string Name { get; set; } = null!;
        public List<string> Districts { get; set; } = [];
    }
}

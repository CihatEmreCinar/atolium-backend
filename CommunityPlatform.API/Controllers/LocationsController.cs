using CommunityPlatform.Application.DTOs.Locations;
using CommunityPlatform.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CommunityPlatform.API.Controllers;

[ApiController]
[Route("api/v1/locations")]
[AllowAnonymous]
public class LocationsController(AppDbContext db) : ControllerBase
{
    // Kayıt/profil ekranlarındaki İl dropdown'ı için — plaka koduna göre sıralı.
    [HttpGet("cities")]
    public async Task<IActionResult> GetCities()
    {
        var cities = await db.Cities
            .OrderBy(c => c.PlateCode)
            .Select(c => new CityResponse { Id = c.Id, Name = c.Name, PlateCode = c.PlateCode })
            .ToListAsync();

        return Ok(cities);
    }

    // İl seçilince İlçe dropdown'ını doldurmak için.
    [HttpGet("cities/{cityId:guid}/districts")]
    public async Task<IActionResult> GetDistricts(Guid cityId)
    {
        var exists = await db.Cities.AnyAsync(c => c.Id == cityId);
        if (!exists)
            return NotFound(new { message = "İl bulunamadı." });

        var districts = await db.Districts
            .Where(d => d.CityId == cityId)
            .OrderBy(d => d.Name)
            .Select(d => new DistrictResponse { Id = d.Id, CityId = d.CityId, Name = d.Name })
            .ToListAsync();

        return Ok(districts);
    }
}

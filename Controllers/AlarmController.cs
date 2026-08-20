using KurTakipApi.Models;
using KurTakipApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KurTakipApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AlarmController : ControllerBase
{
    private readonly KurDbContext _dbContext;
    private readonly ILogger<AlarmController> _logger;

    public AlarmController(KurDbContext dbContext, ILogger<AlarmController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>Tüm alarmları listeler</summary>
    [HttpGet]
    public async Task<IActionResult> Listele()
    {
        var alarmlar = await _dbContext.KurAlarmlari
            .OrderByDescending(a => a.OlusturmaTarihi)
            .ToListAsync();
        return Ok(alarmlar);
    }

    /// <summary>Yeni alarm oluşturur</summary>
    [HttpPost]
    public async Task<IActionResult> Olustur([FromBody] AlarmOlusturRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Sembol))
            return BadRequest("Sembol boş olamaz.");

        if (request.EsikDeger <= 0)
            return BadRequest("Eşik değeri 0'dan büyük olmalıdır.");

        var alarm = new KurAlarm
        {
            Sembol = request.Sembol.ToUpper().Trim(),
            EsikDeger = request.EsikDeger,
            Yon = request.Yon,
            Aktif = true,
            OlusturmaTarihi = DateTime.Now,
            Aciklama = request.Aciklama
        };

        _dbContext.KurAlarmlari.Add(alarm);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Yeni alarm oluşturuldu: {Sembol} {Yon} {Esik}", alarm.Sembol, alarm.Yon, alarm.EsikDeger);
        return CreatedAtAction(nameof(GetById), new { id = alarm.Id }, alarm);
    }

    /// <summary>Tek alarm getirir</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var alarm = await _dbContext.KurAlarmlari.FindAsync(id);
        if (alarm == null) return NotFound($"ID {id} ile alarm bulunamadı.");
        return Ok(alarm);
    }

    /// <summary>Alarmı günceller (eşik, yön, aktiflik, açıklama)</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Guncelle(int id, [FromBody] AlarmGuncelleRequest request)
    {
        var alarm = await _dbContext.KurAlarmlari.FindAsync(id);
        if (alarm == null) return NotFound($"ID {id} ile alarm bulunamadı.");

        if (request.EsikDeger.HasValue) alarm.EsikDeger = request.EsikDeger.Value;
        if (request.Yon.HasValue) alarm.Yon = request.Yon.Value;
        if (request.Aktif.HasValue) alarm.Aktif = request.Aktif.Value;
        if (request.Aciklama != null) alarm.Aciklama = request.Aciklama;

        await _dbContext.SaveChangesAsync();
        return Ok(alarm);
    }

    /// <summary>Alarmı siler</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Sil(int id)
    {
        var alarm = await _dbContext.KurAlarmlari.FindAsync(id);
        if (alarm == null) return NotFound($"ID {id} ile alarm bulunamadı.");

        _dbContext.KurAlarmlari.Remove(alarm);
        await _dbContext.SaveChangesAsync();

        _logger.LogInformation("Alarm silindi: ID={Id}, Sembol={Sembol}", id, alarm.Sembol);
        return NoContent();
    }

    /// <summary>Alarmı aktif/pasif yapar</summary>
    [HttpPatch("{id:int}/toggle")]
    public async Task<IActionResult> Toggle(int id)
    {
        var alarm = await _dbContext.KurAlarmlari.FindAsync(id);
        if (alarm == null) return NotFound($"ID {id} ile alarm bulunamadı.");

        alarm.Aktif = !alarm.Aktif;
        await _dbContext.SaveChangesAsync();

        return Ok(new { alarm.Id, alarm.Aktif, Mesaj = alarm.Aktif ? "Alarm aktifleştirildi." : "Alarm pasifleştirildi." });
    }
}

// DTO sınıfları
public record AlarmOlusturRequest(
    string Sembol,
    decimal EsikDeger,
    AlarmYonu Yon,
    string? Aciklama = null
);

public record AlarmGuncelleRequest(
    decimal? EsikDeger = null,
    AlarmYonu? Yon = null,
    bool? Aktif = null,
    string? Aciklama = null
);

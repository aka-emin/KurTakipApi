using KurTakipApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace KurTakipApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class KurController : ControllerBase
{
    private readonly IKurService _kurService;
    private readonly KurTakipBackgroundService _backgroundService;

    public KurController(IKurService kurService, KurTakipBackgroundService backgroundService)
    {
        _kurService = kurService;
        _backgroundService = backgroundService;
    }

    /// <summary>
    /// Anlık Döviz ve Kripto kurlarını canlı olarak getirir.
    /// </summary>
    [HttpGet("anlik")]
    public async Task<IActionResult> AnlikKurlariGetir()
    {
        var kurlar = await _kurService.AnlikKurlariGetirAsync();
        return Ok(kurlar);
    }

    /// <summary>
    /// Veritabanındaki geçmiş kur kayıtlarını getirir (Grafik çizmek için).
    /// </summary>
    [HttpGet("gecmis")]
    public async Task<IActionResult> GecmisKurlariGetir([FromQuery] string? sembol = null, [FromQuery] int limit = 200)
    {
        var gecmis = await _kurService.GecmisKurlariGetirAsync(sembol, limit);
        return Ok(gecmis);
    }

    /// <summary>
    /// Biriken tüm verilerin kaydedildiği kur_gecmis.csv dosyasını indirir veya görüntüler.
    /// </summary>
    [HttpGet("csv")]
    public async Task<IActionResult> CsvDosyasiIndir()
    {
        var csvYolu = _kurService.CsvDosyaYolunuGetir();
        if (!System.IO.File.Exists(csvYolu))
            return NotFound("CSV dosyası henüz oluşturulmadı.");

        var bytes = await System.IO.File.ReadAllBytesAsync(csvYolu);
        return File(bytes, "text/csv", "kur_gecmis.csv");
    }

    /// <summary>
    /// 10 dakikalık otomatik takip servisinin anlık durumunu getirir.
    /// </summary>
    [HttpGet("durum")]
    public IActionResult ServisDurumuGetir()
    {
        var durum = _backgroundService.DurumGetir();
        return Ok(durum);
    }

    /// <summary>
    /// 10 dakikalık otomatik veritabanı & CSV kayıt sürecini BAŞLATIR.
    /// </summary>
    [HttpPost("baslat")]
    public IActionResult OtomatikTakibiBaslat()
    {
        _backgroundService.Baslat();
        return Ok(new { Mesaj = "10 dakikalık otomatik kur takibi başlatıldı.", Durum = _backgroundService.DurumGetir() });
    }

    /// <summary>
    /// 10 dakikalık otomatik veritabanı & CSV kayıt sürecini DURDURUR.
    /// </summary>
    [HttpPost("durdur")]
    public IActionResult OtomatikTakibiDurdur()
    {
        _backgroundService.Durdur();
        return Ok(new { Mesaj = "Otomatik kur takibi durduruldu.", Durum = _backgroundService.DurumGetir() });
    }

    /// <summary>
    /// 10 dakikalık süreyi beklemeden hemen anlık bir çekim yapıp DB ve CSV'ye kaydeder.
    /// </summary>
    [HttpPost("tetikle")]
    public async Task<IActionResult> ManuelTetikle()
    {
        var eklenenAdet = await _backgroundService.CalistirVeKaydetAsync();
        return Ok(new { Mesaj = $"{eklenenAdet} adet yeni kur verisi çekildi, DB ve CSV dosyasına eklendi.", Durum = _backgroundService.DurumGetir() });
    }
}

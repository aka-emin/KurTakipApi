using KurTakipApi.Models;
using System.Text.Json;

namespace KurTakipApi.Services;

public class FonService : IFonService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FonService> _logger;

    // Takip edilecek fon kodları ve açıklamaları
    private static readonly Dictionary<string, (string Ad, string Kategori)> TakipEdilecekFonlar = new()
    {
        { "AK2",  ("Ak Portföy Para Piyasası",            "Para Piyasası") },
        { "IEF",  ("İş Portföy Para Piyasası",            "Para Piyasası") },
        { "YAS",  ("Yapı Kredi Portföy Para Piyasası",    "Para Piyasası") },
        { "GAF",  ("Garanti Portföy Para Piyasası",       "Para Piyasası") },
        { "TTE",  ("TEB Portföy Para Piyasası",           "Para Piyasası") },
        { "TPF",  ("Tacirler Portföy Para Piyasası",      "Para Piyasası") },
        { "MAC",  ("Ak Portföy Hisse Senedi",             "Hisse Senedi")  },
        { "IAH",  ("İş Portföy Hisse Senedi",             "Hisse Senedi")  },
    };

    public FonService(HttpClient httpClient, ILogger<FonService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<FonKayit>> AnlikFonlariGetirAsync()
    {
        var sonuc = new List<FonKayit>();

        foreach (var (fonKodu, (fonAdi, kategori)) in TakipEdilecekFonlar)
        {
            try
            {
                var fon = await FonVeriCekAsync(fonKodu, fonAdi, kategori);
                if (fon != null)
                    sonuc.Add(fon);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "{FonKodu} fonu çekilirken hata oluştu.", fonKodu);
            }
        }

        return sonuc;
    }

    private async Task<FonKayit?> FonVeriCekAsync(string fonKodu, string fonAdi, string kategori)
    {
        // TEFAS API — fon günlük değer bilgisi
        // Endpoint: https://www.tefas.gov.tr/api/DB/BindHistoryInfo
        var bugun = DateTime.Now.ToString("dd.MM.yyyy");
        var url = $"https://www.tefas.gov.tr/api/DB/BindHistoryInfo?fontip=YAT&sfonkod={fonKodu}&bastarih={bugun}&bittarih={bugun}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        request.Headers.Add("Referer", "https://www.tefas.gov.tr/");
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");

        var response = await _httpClient.SendAsync(request);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("TEFAS API {FonKodu} için HTTP {StatusCode} döndürdü.", fonKodu, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // TEFAS yanıt yapısı: { "data": [ { "FIYAT": 1.234, "GUNLUK_GETIRI": 0.12, ... } ] }
        if (!root.TryGetProperty("data", out var data))
            return null;

        if (data.ValueKind != JsonValueKind.Array || data.GetArrayLength() == 0)
            return null;

        var ilkEleman = data[0];

        decimal birimPayDegeri = 0;
        decimal? gunlukDegisim = null;

        if (ilkEleman.TryGetProperty("FIYAT", out var fiyatEl))
            birimPayDegeri = fiyatEl.GetDecimal();

        if (ilkEleman.TryGetProperty("GUNLUK_GETIRI", out var degisimEl))
            gunlukDegisim = degisimEl.GetDecimal();

        if (birimPayDegeri == 0) return null;

        return new FonKayit
        {
            FonKodu = fonKodu,
            FonAdi = fonAdi,
            Kategori = kategori,
            BirimPayDegeri = birimPayDegeri,
            GunlukDegisim = gunlukDegisim,
            Tarih = DateTime.Now,
            Kaynak = "TEFAS"
        };
    }
}

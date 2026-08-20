using KurTakipApi.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace KurTakipApi.Services;

public class KurService : IKurService
{
    private readonly HttpClient _httpClient;
    private readonly KurDbContext _dbContext;
    private readonly ILogger<KurService> _logger;
    private static readonly SemaphoreSlim _csvLock = new SemaphoreSlim(1, 1);
    private readonly string _csvFilePath;

    public KurService(HttpClient httpClient, KurDbContext dbContext, ILogger<KurService> logger, IWebHostEnvironment env)
    {
        _httpClient = httpClient;
        _dbContext = dbContext;
        _logger = logger;
        
        // CSV dosya yolu: Proje ana dizininde kur_gecmis.csv
        _csvFilePath = Path.Combine(env.ContentRootPath, "kur_gecmis.csv");
        
        // CSV dosyası yoksa başlık satırı ile oluştur
        CsvDosyasiniHazirla();
    }

    public async Task<List<KurKayit>> AnlikKurlariGetirAsync()
    {
        var sonuc = new List<KurKayit>();
        var simdi = DateTime.Now;

        // 1. Döviz Kurlarını Çek (Frankfurter API)
        try
        {
            // USD -> TRY
            var usdJson = await _httpClient.GetStringAsync("https://api.frankfurter.app/latest?from=USD&to=TRY");
            using var usdDoc = JsonDocument.Parse(usdJson);
            if (usdDoc.RootElement.GetProperty("rates").TryGetProperty("TRY", out var usdTryVal))
            {
                sonuc.Add(new KurKayit
                {
                    Sembol = "USD/TRY",
                    Kategori = "Döviz",
                    Fiyat = usdTryVal.GetDecimal(),
                    Tarih = simdi,
                    Kaynak = "Frankfurter"
                });
            }

            // EUR -> TRY
            var eurJson = await _httpClient.GetStringAsync("https://api.frankfurter.app/latest?from=EUR&to=TRY");
            using var eurDoc = JsonDocument.Parse(eurJson);
            if (eurDoc.RootElement.GetProperty("rates").TryGetProperty("TRY", out var eurTryVal))
            {
                sonuc.Add(new KurKayit
                {
                    Sembol = "EUR/TRY",
                    Kategori = "Döviz",
                    Fiyat = eurTryVal.GetDecimal(),
                    Tarih = simdi,
                    Kaynak = "Frankfurter"
                });
            }

            // GBP -> TRY
            var gbpJson = await _httpClient.GetStringAsync("https://api.frankfurter.app/latest?from=GBP&to=TRY");
            using var gbpDoc = JsonDocument.Parse(gbpJson);
            if (gbpDoc.RootElement.GetProperty("rates").TryGetProperty("TRY", out var gbpTryVal))
            {
                sonuc.Add(new KurKayit
                {
                    Sembol = "GBP/TRY",
                    Kategori = "Döviz",
                    Fiyat = gbpTryVal.GetDecimal(),
                    Tarih = simdi,
                    Kaynak = "Frankfurter"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Döviz kurları çekilirken hata oluştu.");
        }

        // 2. Kripto Kurlarını Çek (CoinGecko API)
        try
        {
            // CoinGecko API istek başlıkları gerektirebilir (User-Agent)
            var request = new HttpRequestMessage(HttpMethod.Get, 
                "https://api.coingecko.com/api/v3/simple/price?ids=bitcoin,ethereum,solana&vs_currencies=usd,try");
            request.Headers.Add("User-Agent", "KurTakipApi/1.0");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var cryptoJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(cryptoJson);
                var root = doc.RootElement;

                // Bitcoin
                if (root.TryGetProperty("bitcoin", out var btc))
                {
                    if (btc.TryGetProperty("usd", out var btcUsd))
                        sonuc.Add(new KurKayit { Sembol = "BTC/USDT", Kategori = "Kripto", Fiyat = btcUsd.GetDecimal(), Tarih = simdi, Kaynak = "CoinGecko" });
                }

                // Ethereum
                if (root.TryGetProperty("ethereum", out var eth))
                {
                    if (eth.TryGetProperty("usd", out var ethUsd))
                        sonuc.Add(new KurKayit { Sembol = "ETH/USDT", Kategori = "Kripto", Fiyat = ethUsd.GetDecimal(), Tarih = simdi, Kaynak = "CoinGecko" });
                }

                // Solana
                if (root.TryGetProperty("solana", out var sol))
                {
                    if (sol.TryGetProperty("usd", out var solUsd))
                        sonuc.Add(new KurKayit { Sembol = "SOL/USDT", Kategori = "Kripto", Fiyat = solUsd.GetDecimal(), Tarih = simdi, Kaynak = "CoinGecko" });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kripto kurları çekilirken hata oluştu.");
        }

        return sonuc;
    }

    public async Task<List<KurKayit>> KurlariKaydetVeAktarAsync()
    {
        // 1. Anlık kurları dış servislerden al
        var kurlar = await AnlikKurlariGetirAsync();
        if (kurlar.Count == 0)
        {
            _logger.LogWarning("Hiç kur verisi alınamadı, kayıt yapılmadı.");
            return kurlar;
        }

        // 2. Veritabanına Ekle
        _dbContext.KurKayitlari.AddRange(kurlar);
        await _dbContext.SaveChangesAsync();

        // 3. CSV Dosyasına Ekle
        await CsvyeEkaleAsync(kurlar);

        _logger.LogInformation("{Count} adet kur kaydı veritabanına ve CSV dosyasına başarıyla yazıldı.", kurlar.Count);

        return kurlar;
    }

    public async Task<List<KurKayit>> GecmisKurlariGetirAsync(string? sembol = null, int limit = 200)
    {
        var query = _dbContext.KurKayitlari.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(sembol))
        {
            query = query.Where(k => k.Sembol.ToLower() == sembol.ToLower());
        }

        return await query
            .OrderByDescending(k => k.Tarih)
            .Take(limit)
            .OrderBy(k => k.Tarih) // Grafikler için kronolojik sıra
            .ToListAsync();
    }

    public string CsvDosyaYolunuGetir() => _csvFilePath;

    public async Task<string> CsvIceriginiGetirAsync()
    {
        await _csvLock.WaitAsync();
        try
        {
            if (!File.Exists(_csvFilePath))
                return string.Empty;

            return await File.ReadAllTextAsync(_csvFilePath, Encoding.UTF8);
        }
        finally
        {
            _csvLock.Release();
        }
    }

    private void CsvDosyasiniHazirla()
    {
        if (!File.Exists(_csvFilePath))
        {
            var header = "Id,Tarih,Kategori,Sembol,Fiyat,Kaynak" + Environment.NewLine;
            File.WriteAllText(_csvFilePath, header, Encoding.UTF8);
        }
    }

    private async Task CsvyeEkaleAsync(List<KurKayit> kayitlar)
    {
        await _csvLock.WaitAsync();
        try
        {
            var sb = new StringBuilder();
            foreach (var item in kayitlar)
            {
                // CSV Uyumlu format
                var satir = $"{item.Id},{item.Tarih:yyyy-MM-dd HH:mm:ss},{item.Kategori},{item.Sembol},{item.Fiyat.ToString(CultureInfo.InvariantCulture)},{item.Kaynak}";
                sb.AppendLine(satir);
            }

            await File.AppendAllTextAsync(_csvFilePath, sb.ToString(), Encoding.UTF8);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CSV dosyasına yazılırken hata oluştu.");
        }
        finally
        {
            _csvLock.Release();
        }
    }
}
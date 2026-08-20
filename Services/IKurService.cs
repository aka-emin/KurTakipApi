using KurTakipApi.Models;

namespace KurTakipApi.Services;

// Kur çekme işlemiyle ilgili sözleşme
public interface IKurService
{
    // Dış API'lerden (Frankfurter & CoinGecko) anlık kurları getirir
    
    Task<List<KurKayit>> AnlikKurlariGetirAsync();

    // Anlık kurları çeker, Veritabanına kaydeder ve CSV dosyasına ekler
    Task<List<KurKayit>> KurlariKaydetVeAktarAsync();

    // Veritabanındaki geçmiş kayıtları getirir (Fiyat grafiği için)
    Task<List<KurKayit>> GecmisKurlariGetirAsync(string? sembol = null, int limit = 200);

    // CSV dosyasının yolunu döner
    string CsvDosyaYolunuGetir();

    // CSV dosya içeriğini metin olarak döner
    Task<string> CsvIceriginiGetirAsync();
}
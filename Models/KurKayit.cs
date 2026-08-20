namespace KurTakipApi.Models;

public class KurKayit
{
    public int Id { get; set; }
    
    // Sembol (Örn: "USD/TRY", "EUR/TRY", "BTC/USDT", "ETH/USDT")
    public string Sembol { get; set; } = string.Empty;
    
    // Kategori ("Döviz" veya "Kripto")
    public string Kategori { get; set; } = string.Empty;
    
    // Anlık Fiyat
    public decimal Fiyat { get; set; }
    
    // Kayıt Tarihi (UTC / Local)
    public DateTime Tarih { get; set; } = DateTime.Now;
    
    // Verinin Alındığı Kaynak ("Frankfurter", "CoinGecko", vb.)
    public string Kaynak { get; set; } = string.Empty;
}

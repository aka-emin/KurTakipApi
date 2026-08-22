namespace KurTakipApi.Models;

public class FonKayit
{
    /// <summary>
    /// Fon kodu (Örn: "AK2", "IEF", "YAS")
    /// </summary>
    public string FonKodu { get; set; } = string.Empty;

    /// <summary>
    /// Fonun tam adı
    /// </summary>
    public string FonAdi { get; set; } = string.Empty;

    /// <summary>
    /// Fon kategorisi (Para Piyasası, Hisse Senedi, vb.)
    /// </summary>
    public string Kategori { get; set; } = string.Empty;

    /// <summary>
    /// Net Aktif Değer (Birim Pay Değeri) - TL cinsinden
    /// </summary>
    public decimal BirimPayDegeri { get; set; }

    /// <summary>
    /// Günlük değişim yüzdesi
    /// </summary>
    public decimal? GunlukDegisim { get; set; }

    /// <summary>
    /// Verinin alındığı tarih
    /// </summary>
    public DateTime Tarih { get; set; } = DateTime.Now;

    /// <summary>
    /// Veri kaynağı
    /// </summary>
    public string Kaynak { get; set; } = "TEFAS";
}

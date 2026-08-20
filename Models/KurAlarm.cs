namespace KurTakipApi.Models;

public enum AlarmYonu
{
    /// <summary>Fiyat belirtilen eşiğin ÜSTÜNE çıkınca bildirim gönder</summary>
    UstundeIse,
    /// <summary>Fiyat belirtilen eşiğin ALTINA düşünce bildirim gönder</summary>
    AltindaIse
}

public class KurAlarm
{
    public int Id { get; set; }

    /// <summary>Takip edilecek sembol (örn: "USD/TRY", "BTC/USDT")</summary>
    public string Sembol { get; set; } = string.Empty;

    /// <summary>Tetikleme eşik değeri</summary>
    public decimal EsikDeger { get; set; }

    /// <summary>Alarmın tetiklenme yönü: eşiğin üstünde mi altında mı?</summary>
    public AlarmYonu Yon { get; set; }

    /// <summary>Alarm aktif mi?</summary>
    public bool Aktif { get; set; } = true;

    /// <summary>Alarmın oluşturulma tarihi</summary>
    public DateTime OlusturmaTarihi { get; set; } = DateTime.Now;

    /// <summary>Son bildirim gönderilme tarihi (tekrar kontrolü için)</summary>
    public DateTime? SonTetiklemeTarihi { get; set; }

    /// <summary>İsteğe bağlı açıklama</summary>
    public string? Aciklama { get; set; }
}

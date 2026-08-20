using KurTakipApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KurTakipApi.Services;

public class AlarmKontrolService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IEmailBildirimService _emailService;
    private readonly ILogger<AlarmKontrolService> _logger;

    public AlarmKontrolService(
        IServiceScopeFactory scopeFactory,
        IEmailBildirimService emailService,
        ILogger<AlarmKontrolService> logger)
    {
        _scopeFactory = scopeFactory;
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Verilen kur kayıtlarını aktif alarmlarla karşılaştırır.
    /// Eşik aşıldıysa e-posta gönderir ve alarmın son tetikleme zamanını günceller.
    /// </summary>
    public async Task KurlariKontrolEtAsync(List<KurKayit> guncelKurlar)
    {
        if (guncelKurlar.Count == 0) return;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<KurDbContext>();

        // Aktif alarmları veritabanından çek
        var aktifAlarmlar = await dbContext.KurAlarmlari
            .Where(a => a.Aktif)
            .ToListAsync();

        if (aktifAlarmlar.Count == 0) return;

        _logger.LogDebug("{AlarmSayisi} aktif alarm kontrol ediliyor...", aktifAlarmlar.Count);

        foreach (var alarm in aktifAlarmlar)
        {
            // Bu alarma ait güncel kur verisini bul
            var kurKayit = guncelKurlar.FirstOrDefault(k =>
                string.Equals(k.Sembol, alarm.Sembol, StringComparison.OrdinalIgnoreCase));

            if (kurKayit == null)
            {
                _logger.LogDebug("Alarm sembolü '{Sembol}' için güncel kur verisi bulunamadı.", alarm.Sembol);
                continue;
            }

            bool tetiklendi = alarm.Yon switch
            {
                AlarmYonu.UstundeIse => kurKayit.Fiyat >= alarm.EsikDeger,
                AlarmYonu.AltindaIse => kurKayit.Fiyat <= alarm.EsikDeger,
                _ => false
            };

            if (!tetiklendi) continue;

            _logger.LogInformation(
                "⚠️ Alarm tetiklendi! Sembol: {Sembol}, Fiyat: {Fiyat}, Eşik: {Esik}, Yön: {Yon}",
                alarm.Sembol, kurKayit.Fiyat, alarm.EsikDeger, alarm.Yon);

            // E-posta gönder
            await _emailService.BildirimGonderAsync(
                alarm.Sembol,
                kurKayit.Fiyat,
                alarm.EsikDeger,
                alarm.Yon,
                alarm.Aciklama);

            // Son tetikleme zamanını güncelle
            alarm.SonTetiklemeTarihi = DateTime.Now;
        }

        // Güncellenen son tetikleme zamanlarını kaydet
        await dbContext.SaveChangesAsync();
    }
}

namespace KurTakipApi.Services;

public class KurTakipBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KurTakipBackgroundService> _logger;
    private readonly AlarmKontrolService _alarmKontrol;

    public bool IsRunning { get; private set; } = true;
    public int IntervalMinutes { get; set; } = 10;
    public DateTime? LastRunTime { get; private set; }
    public DateTime? NextRunTime { get; private set; }
    public int RunCount { get; private set; } = 0;
    public string? LastError { get; private set; }

    public KurTakipBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<KurTakipBackgroundService> logger,
        AlarmKontrolService alarmKontrol)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _alarmKontrol = alarmKontrol;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kur Takip Arka Plan Servisi Başlatıldı. Periyot: {Interval} dakika.", IntervalMinutes);

        // Uygulama başlar başlamaz ilk çekimi yap
        await CalistirVeKaydetAsync();

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var beklemeSuresi = TimeSpan.FromMinutes(IntervalMinutes);
                NextRunTime = DateTime.Now.Add(beklemeSuresi);

                await Task.Delay(beklemeSuresi, stoppingToken);

                if (IsRunning)
                {
                    await CalistirVeKaydetAsync();
                }
                else
                {
                    _logger.LogInformation("Kur Takip Servisi şu an durdurulmuş durumda. Çekim atlandı.");
                }
            }
            catch (TaskCanceledException)
            {
                // Uygulama kapatılırken iptal tetiklenir
                break;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _logger.LogError(ex, "Arka plan servisi döngüsünde hata oluştu.");
            }
        }

        _logger.LogInformation("Kur Takip Arka Plan Servisi Durduruldu.");
    }

    public async Task<int> CalistirVeKaydetAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var kurService = scope.ServiceProvider.GetRequiredService<IKurService>();

            var kayitlar = await kurService.KurlariKaydetVeAktarAsync();
            LastRunTime = DateTime.Now;
            NextRunTime = LastRunTime.Value.AddMinutes(IntervalMinutes);
            RunCount++;
            LastError = null;

            _logger.LogInformation("10 dk'lık Periyodik Çekim Yapıldı. Çekilen kayit sayısı: {Count}. Toplam Çekim Sayısı: {RunCount}", kayitlar.Count, RunCount);

            // Alarm kontrolü: yeni kurları aktif alarmlarla karşılaştır
            await _alarmKontrol.KurlariKontrolEtAsync(kayitlar);

            return kayitlar.Count;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            _logger.LogError(ex, "Periyodik kur verisi çekilirken ve kaydedilirken hata oluştu.");
            throw;
        }
    }

    public void Baslat()
    {
        IsRunning = true;
        NextRunTime = DateTime.Now.AddMinutes(IntervalMinutes);
        _logger.LogInformation("Kur Takip Periyodik Servis Kullanıcı Tarafından BAŞLATILDI.");
    }

    public void Durdur()
    {
        IsRunning = false;
        NextRunTime = null;
        _logger.LogInformation("Kur Takip Periyodik Servis Kullanıcı Tarafından DURDURULDU.");
    }

    public object DurumGetir()
    {
        return new
        {
            IsRunning,
            IntervalMinutes,
            LastRunTime,
            NextRunTime,
            RunCount,
            LastError
        };
    }
}

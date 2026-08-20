using KurTakipApi.Models;

namespace KurTakipApi.Services;

public interface IEmailBildirimService
{
    /// <summary>
    /// Kur alarmı tetiklendiğinde e-posta bildirimi gönderir.
    /// </summary>
    Task BildirimGonderAsync(string sembol, decimal fiyat, decimal esikDeger, AlarmYonu yon, string? aciklama = null);
}

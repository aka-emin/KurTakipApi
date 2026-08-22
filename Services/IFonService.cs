using KurTakipApi.Models;

namespace KurTakipApi.Services;

public interface IFonService
{
    /// <summary>
    /// TEFAS'tan seçili yatırım fonlarının güncel verilerini çeker.
    /// </summary>
    Task<List<FonKayit>> AnlikFonlariGetirAsync();
}

using KurTakipApi.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace KurTakipApi.Services;

public class EmailBildirimService : IEmailBildirimService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailBildirimService> _logger;

    public EmailBildirimService(IConfiguration config, ILogger<EmailBildirimService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task BildirimGonderAsync(string sembol, decimal fiyat, decimal esikDeger, AlarmYonu yon, string? aciklama = null)
    {
        var ayarlar = _config.GetSection("EmailAyarlari");
        var smtpHost = ayarlar["SmtpHost"] ?? "smtp.gmail.com";
        var smtpPort = int.Parse(ayarlar["SmtpPort"] ?? "587");
        var kullaniciAdi = ayarlar["KullaniciAdi"] ?? "";
        var sifre = ayarlar["Sifre"] ?? "";
        var gondericiAd = ayarlar["GondericiAd"] ?? "Kur Takip";

        // Birden fazla alıcı: virgülle ayır
        var aliciListesi = (ayarlar["AliciEmailler"] ?? ayarlar["AliciEmail"] ?? kullaniciAdi)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(e => e.Contains('@'))
            .ToList();

        if (string.IsNullOrWhiteSpace(kullaniciAdi) || string.IsNullOrWhiteSpace(sifre))
        {
            _logger.LogWarning("E-posta ayarları eksik. appsettings.json içindeki 'EmailAyarlari' bölümünü doldurun.");
            return;
        }

        if (aliciListesi.Count == 0)
        {
            _logger.LogWarning("Alıcı e-posta adresi tanımlanmamış. 'AliciEmailler' alanını doldurun.");
            return;
        }

        var yonMetin = yon == AlarmYonu.UstundeIse ? "üstüne çıktı ↑" : "altına düştü ↓";
        var simdi = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");

        try
        {
            var mesaj = new MimeMessage();
            mesaj.From.Add(new MailboxAddress(gondericiAd, kullaniciAdi));
            // Tüm alıcıları ekle
            foreach (var adres in aliciListesi)
                mesaj.To.Add(MailboxAddress.Parse(adres));
            mesaj.Subject = $"🔔 Kur Alarmı: {sembol} {yonMetin}";

            // HTML e-posta gövdesi
            var htmlGovde = $@"
<!DOCTYPE html>
<html lang=""tr"">
<head><meta charset=""UTF-8""></head>
<body style=""font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px;"">
  <div style=""max-width: 500px; margin: auto; background: #ffffff; border-radius: 10px; 
               padding: 30px; box-shadow: 0 2px 8px rgba(0,0,0,0.1);"">
    <h2 style=""color: #333; margin-top: 0;"">🔔 Kur Alarmı Tetiklendi</h2>
    <table style=""width: 100%; border-collapse: collapse;"">
      <tr>
        <td style=""padding: 8px 0; color: #666; width: 140px;"">Sembol</td>
        <td style=""padding: 8px 0; font-weight: bold; font-size: 18px;"">{sembol}</td>
      </tr>
      <tr style=""background: #f9f9f9;"">
        <td style=""padding: 8px; color: #666;"">Güncel Fiyat</td>
        <td style=""padding: 8px; font-weight: bold; color: {(yon == AlarmYonu.UstundeIse ? "#e74c3c" : "#27ae60")}; font-size: 20px;"">
          {fiyat:N4}
        </td>
      </tr>
      <tr>
        <td style=""padding: 8px 0; color: #666;"">Eşik Değer</td>
        <td style=""padding: 8px 0;"">{esikDeger:N4}</td>
      </tr>
      <tr style=""background: #f9f9f9;"">
        <td style=""padding: 8px; color: #666;"">Durum</td>
        <td style=""padding: 8px; font-weight: bold;"">{yonMetin}</td>
      </tr>
      {(string.IsNullOrWhiteSpace(aciklama) ? "" : $@"
      <tr>
        <td style=""padding: 8px 0; color: #666;"">Açıklama</td>
        <td style=""padding: 8px 0;"">{aciklama}</td>
      </tr>")}
      <tr>
        <td style=""padding: 8px 0; color: #666;"">Tarih / Saat</td>
        <td style=""padding: 8px 0; color: #999; font-size: 13px;"">{simdi}</td>
      </tr>
    </table>
    <hr style=""border: none; border-top: 1px solid #eee; margin: 20px 0;"">
    <p style=""color: #999; font-size: 12px; margin: 0;"">Bu bildirim KurTakipApi tarafından otomatik olarak gönderilmiştir.</p>
  </div>
</body>
</html>";

            var body = new BodyBuilder { HtmlBody = htmlGovde };
            mesaj.Body = body.ToMessageBody();

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(kullaniciAdi, sifre);
            await smtp.SendAsync(mesaj);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation(
                "✅ E-posta bildirimi gönderildi: {Sembol} fiyatı {Fiyat} ile eşik {Esik} değerinin {Yon}.",
                sembol, fiyat, esikDeger, yonMetin);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ E-posta gönderilirken hata oluştu. Alıcılar: {Alicilar}, Sembol: {Sembol}", string.Join(", ", aliciListesi), sembol);
        }
    }
}

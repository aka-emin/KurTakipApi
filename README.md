# 💱 Kur Takip API

> Döviz ve kripto kurlarını gerçek zamanlı takip eden, eşik aşıldığında e-posta bildirimi gönderen ASP.NET Core Web API.

---

## 🚀 Özellikler

- 📈 **Anlık kur verisi** — Frankfurter API üzerinden döviz ve kripto kurlarını canlı çeker
- 🗄️ **Geçmiş kayıt** — Her 10 dakikada bir kurları SQLite veritabanına ve CSV dosyasına kaydeder
- 🔔 **Alarm sistemi** — Belirli bir kur belirlenen eşiği aştığında veya altına düştüğünde tetiklenir
- 📧 **E-posta bildirimi** — Alarm tetiklendiğinde Gmail SMTP üzerinden birden fazla alıcıya mail gönderir
- 📊 **CSV dışa aktarma** — Birikmiş tüm verileri `kur_gecmis.csv` olarak indirme imkânı
- 🐳 **Docker desteği** — Railway veya herhangi bir container ortamına kolayca deploy edilir
- 🌐 **Swagger / OpenAPI** — Development ortamında API dokümantasyonu otomatik açılır

---

## 🛠️ Teknolojiler

| Katman | Teknoloji |
|---|---|
| Framework | ASP.NET Core (.NET 10) |
| Veritabanı | SQLite + Entity Framework Core |
| E-posta | MailKit (Gmail SMTP) |
| Kur Verisi | [Frankfurter API](https://www.frankfurter.app/) |
| Konteyner | Docker |
| API Dokümantasyon | OpenAPI / Swagger |

---

## 📁 Proje Yapısı

```
KurTakipApi/
├── Controllers/
│   ├── KurController.cs       # Kur verisi endpoint'leri
│   └── AlarmController.cs     # Alarm CRUD endpoint'leri
├── Models/
│   ├── KurAlarm.cs            # Alarm veri modeli
│   ├── KurKayit.cs            # Geçmiş kur kaydı modeli
│   ├── KurDbContext.cs        # EF Core veritabanı bağlamı
│   └── FrankfurterYanit.cs    # API yanıt modeli
├── Services/
│   ├── KurService.cs              # Frankfurter API entegrasyonu
│   ├── KurTakipBackgroundService.cs  # 10 dk'lık arka plan servisi
│   ├── AlarmKontrolService.cs     # Alarm tetikleme mantığı
│   ├── EmailBildirimService.cs    # Gmail SMTP bildirim servisi
│   └── IKurService.cs / IEmailBildirimService.cs
├── appsettings.json           # Uygulama yapılandırması
├── Dockerfile                 # Docker imaj tanımı
└── Program.cs                 # Uygulama başlangıç noktası
```

---

## ⚙️ Kurulum ve Çalıştırma

### Gereksinimler

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Bir Gmail hesabı + [App Password](https://myaccount.google.com/apppasswords)

### 1. Klonla

```bash
git clone https://github.com/aka-emin/KurTakipApi.git
cd KurTakipApi
```

### 2. E-posta Ayarlarını Yap

`appsettings.json` dosyasındaki `EmailAyarlari` bölümünü düzenle:

```json
"EmailAyarlari": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "KullaniciAdi": "senin@gmail.com",
  "Sifre": "xxxx xxxx xxxx xxxx",
  "GondericiAd": "Kur Takip Alarm",
  "AliciEmailler": "alici1@gmail.com,alici2@hotmail.com"
}
```

> **Not:** `Sifre` alanına Gmail şifrenizi değil, [Google App Password](https://myaccount.google.com/apppasswords) oluşturarak aldığınız 16 haneli şifreyi yazın.
> `AliciEmailler` alanına virgülle ayırarak birden fazla alıcı ekleyebilirsiniz.

### 3. Çalıştır

```bash
dotnet run
```

Uygulama `http://localhost:5154` adresinde başlar.
Swagger UI: `http://localhost:5154/openapi`

---

## 📡 API Endpoint'leri

### Kur Endpoint'leri — `/api/kur`

| Metod | Endpoint | Açıklama |
|---|---|---|
| `GET` | `/api/kur/anlik` | Anlık döviz/kripto kurlarını getirir |
| `GET` | `/api/kur/gecmis` | Geçmiş kur kayıtlarını getirir (grafik için) |
| `GET` | `/api/kur/csv` | Tüm geçmişi CSV olarak indirir |
| `GET` | `/api/kur/durum` | Arka plan servisinin durumunu getirir |
| `POST` | `/api/kur/baslat` | 10 dakikalık otomatik takibi başlatır |
| `POST` | `/api/kur/durdur` | Otomatik takibi durdurur |
| `POST` | `/api/kur/tetikle` | 10 dakikayı beklemeden anlık kayıt yapar |

### Alarm Endpoint'leri — `/api/alarm`

| Metod | Endpoint | Açıklama |
|---|---|---|
| `GET` | `/api/alarm` | Tüm alarmları listeler |
| `GET` | `/api/alarm/{id}` | Belirli bir alarmı getirir |
| `POST` | `/api/alarm` | Yeni alarm oluşturur |
| `PUT` | `/api/alarm/{id}` | Alarmı günceller |
| `DELETE` | `/api/alarm/{id}` | Alarmı siler |
| `PATCH` | `/api/alarm/{id}/toggle` | Alarmı aktif/pasif yapar |

### Alarm Oluşturma — Örnek İstek

```json
POST /api/alarm
{
  "sembol": "USD",
  "esikDeger": 38.50,
  "yon": 0,
  "aciklama": "Dolar 38.50'yi geçerse bildir"
}
```

> `yon` değerleri: `0` = Üstünde ise bildir ↑, `1` = Altında ise bildir ↓

---

## 🔄 Sistem Akışı

```
Her 10 Dakikada Bir
      │
      ▼
Frankfurter API → Kur Verisi Çekimi
      │
      ├──► SQLite DB'ye Kaydet (KurKayit tablosu)
      ├──► kur_gecmis.csv'ye Ekle
      │
      └──► Aktif Alarmları Kontrol Et
                │
                └──► Eşik Aşıldıysa → Gmail SMTP → 📧 Alıcılara Mail
```

---

## 🐳 Docker ile Çalıştırma

### Build & Run

```bash
docker build -t kurtakip-api .
docker run -p 8080:8080 \
  -e EmailAyarlari__KullaniciAdi=senin@gmail.com \
  -e EmailAyarlari__Sifre="xxxx xxxx xxxx xxxx" \
  -e EmailAyarlari__AliciEmailler=alici@gmail.com \
  kurtakip-api
```

### Railway'e Deploy

1. GitHub reposunu Railway'e bağla
2. `Dockerfile` otomatik algılanır
3. Environment variable olarak e-posta ayarlarını ekle
4. Deploy!

---

## 📊 Veri Modelleri

### KurAlarm

| Alan | Tip | Açıklama |
|---|---|---|
| `Id` | int | Birincil anahtar |
| `Sembol` | string | Döviz kodu (USD, EUR, BTC...) |
| `EsikDeger` | decimal | Tetiklenme eşiği |
| `Yon` | enum | UstundeIse / AltindaIse |
| `Aktif` | bool | Alarm aktif mi? |
| `OlusturmaTarihi` | DateTime | Oluşturulma zamanı |
| `Aciklama` | string? | İsteğe bağlı not |

### KurKayit

| Alan | Tip | Açıklama |
|---|---|---|
| `Id` | int | Birincil anahtar |
| `Sembol` | string | Döviz kodu |
| `Deger` | decimal | Kur değeri |
| `Tarih` | DateTime | Kayıt zamanı |

---

## 📄 Lisans

Bu proje MIT lisansı ile lisanslanmıştır.

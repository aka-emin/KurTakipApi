using KurTakipApi.Models;
using KurTakipApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Controllers & OpenAPI/Swagger
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 2. SQLite Veritabanı Tanımlaması
builder.Services.AddDbContext<KurDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=kurtakip.db"));

// 3. KurServis & HttpClient Tanımlaması
builder.Services.AddHttpClient<IKurService, KurService>();

// 3b. FonServis & HttpClient Tanımlaması (TEFAS)
builder.Services.AddHttpClient<IFonService, FonService>();

// 4. Bildirim & Alarm Servisleri
builder.Services.AddSingleton<IEmailBildirimService, EmailBildirimService>();
builder.Services.AddSingleton<AlarmKontrolService>();

// 5. Periyodik 10 Dakikalık Arka Plan Servisi Tanımlaması
builder.Services.AddSingleton<KurTakipBackgroundService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<KurTakipBackgroundService>());

var app = builder.Build();

// 5. Veritabanını Otomatik Oluştur
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<KurDbContext>();
    db.Database.EnsureCreated();
}

// 6. HTTP Pipeline Ayarları
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Web Arayüzü İçin Statik Dosya Desteği (wwwroot/index.html)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();

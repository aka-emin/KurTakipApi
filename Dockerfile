# Build aşaması
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Proje dosyasını kopyala ve bağımlılıkları yükle
COPY *.csproj ./
RUN dotnet restore

# Kaynak kodları kopyala ve yayınla
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Çalıştırma aşaması (daha küçük imaj)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Yayınlanan dosyaları kopyala
COPY --from=build /app/publish .

# Railway PORT değişkenini kullan
ENV ASPNETCORE_URLS=http://+:${PORT:-8080}
ENV ASPNETCORE_ENVIRONMENT=Production

# SQLite için data klasörü oluştur
RUN mkdir -p /app/data

EXPOSE 8080

ENTRYPOINT ["dotnet", "KurTakipApi.dll"]

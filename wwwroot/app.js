let chartInstance = null;
let countdownTimer = null;
let nextRunTimeDate = null;

document.addEventListener('DOMContentLoaded', async () => {
    await verileriYukle();
    await alarmListesiniYukle();
    
    // Her 15 saniyede bir verileri ve durumu otomatik tazele
    setInterval(verileriYukle, 15000);
    setInterval(alarmListesiniYukle, 30000);

    // Sayaç güncellemesi (Her 1 saniyede bir)
    setInterval(updateCountdown, 1000);
});

async function verileriYukle() {
    await Promise.all([
        anlikKurlariYukle(),
        servisDurumunuYukle(),
        grafikGuncelle(),
        gecmisTabloGuncelle()
    ]);
}

// 1. Anlık Kur Kartları
async function anlikKurlariYukle() {
    try {
        const res = await fetch('/api/kur/anlik');
        if (!res.ok) return;
        const kurlar = await res.json();

        const grid = document.getElementById('ratesGrid');
        grid.innerHTML = '';

        kurlar.forEach(item => {
            const isDoviz = item.kategori === 'Döviz';
            const birim = item.sembol.includes('TRY') ? '₺' : '$';

            const card = document.createElement('div');
            card.className = 'rate-card';
            card.innerHTML = `
                <div class="rate-header">
                    <span class="rate-symbol">${item.sembol}</span>
                    <span class="rate-category ${isDoviz ? 'doviz' : 'kripto'}">${item.kategori}</span>
                </div>
                <div class="rate-price">${birim} ${item.fiyat.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })}</div>
                <div class="rate-meta">
                    <span>${item.kaynak}</span>
                    <span>${new Date(item.tarih).toLocaleTimeString('tr-TR')}</span>
                </div>
            `;
            grid.appendChild(card);
        });
    } catch (err) {
        console.error('Anlık kurlar yüklenirken hata:', err);
    }
}

// 2. Servis Durumu ve Sayaç
async function servisDurumunuYukle() {
    try {
        const res = await fetch('/api/kur/durum');
        if (!res.ok) return;
        const durum = await res.json();

        const badge = document.getElementById('statusBadge');
        const statusText = document.getElementById('statusText');
        const isRunningEl = document.getElementById('infoIsRunning');
        const lastRunEl = document.getElementById('infoLastRun');
        const runCountEl = document.getElementById('infoRunCount');

        if (durum.isRunning) {
            badge.className = 'status-badge active';
            statusText.innerText = '10 Dk Otomatik Takip: ÇALIŞIYOR';
            isRunningEl.innerText = '🟢 Aktif (10 Dk Periyot)';
            isRunningEl.style.color = '#10b981';
        } else {
            badge.className = 'status-badge inactive';
            statusText.innerText = 'Otomatik Takip: DURDURULDU';
            isRunningEl.innerText = '🔴 Durduruldu';
            isRunningEl.style.color = '#f43f5e';
        }

        lastRunEl.innerText = durum.lastRunTime ? new Date(durum.lastRunTime).toLocaleTimeString('tr-TR') : 'Henüz Yapılmadı';
        runCountEl.innerText = durum.runCount || 0;

        if (durum.nextRunTime && durum.isRunning) {
            nextRunTimeDate = new Date(durum.nextRunTime);
        } else {
            nextRunTimeDate = null;
            document.getElementById('infoCountdown').innerText = 'Durduruldu';
        }
    } catch (err) {
        console.error('Servis durumu yüklenirken hata:', err);
    }
}

function updateCountdown() {
    const el = document.getElementById('infoCountdown');
    if (!nextRunTimeDate) {
        el.innerText = 'Durduruldu';
        return;
    }

    const now = new Date();
    const diff = nextRunTimeDate - now;

    if (diff <= 0) {
        el.innerText = 'Veri çekiliyor...';
        return;
    }

    const minutes = Math.floor(diff / 60000);
    const seconds = Math.floor((diff % 60000) / 1000);

    el.innerText = `${minutes.toString().padStart(2, '0')}:${seconds.toString().padStart(2, '0')}`;
}

// 3. Chart.js Fiyat Grafiği
async function grafikGuncelle() {
    const sembol = document.getElementById('chartSymbolFilter').value;
    try {
        const res = await fetch(`/api/kur/gecmis?sembol=${encodeURIComponent(sembol)}&limit=50`);
        if (!res.ok) return;
        const veriler = await res.json();

        const etiketler = veriler.map(v => new Date(v.tarih).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' }));
        const fiyatlar = veriler.map(v => v.fiyat);

        const ctx = document.getElementById('kurChart').getContext('2d');

        if (chartInstance) {
            chartInstance.destroy();
        }

        const isCrypto = sembol.includes('USDT');
        const color = isCrypto ? '#8b5cf6' : '#3b82f6';
        const bgGradient = ctx.createLinearGradient(0, 0, 0, 300);
        bgGradient.addColorStop(0, isCrypto ? 'rgba(139, 92, 246, 0.35)' : 'rgba(59, 130, 246, 0.35)');
        bgGradient.addColorStop(1, 'rgba(15, 23, 42, 0)');

        chartInstance = new Chart(ctx, {
            type: 'line',
            data: {
                labels: etiketler,
                datasets: [{
                    label: `${sembol} Fiyatı`,
                    data: fiyatlar,
                    borderColor: color,
                    backgroundColor: bgGradient,
                    fill: true,
                    tension: 0.3,
                    pointRadius: 4,
                    pointHoverRadius: 6,
                    borderWidth: 2
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: true, labels: { color: '#94a3b8' } }
                },
                scales: {
                    x: {
                        grid: { color: 'rgba(255, 255, 255, 0.05)' },
                        ticks: { color: '#94a3b8' }
                    },
                    y: {
                        grid: { color: 'rgba(255, 255, 255, 0.05)' },
                        ticks: { color: '#94a3b8' }
                    }
                }
            }
        });
    } catch (err) {
        console.error('Grafik verisi yüklenirken hata:', err);
    }
}

// 4. Geçmiş Tablo
async function gecmisTabloGuncelle() {
    const sembol = document.getElementById('tableFilter').value;
    try {
        const url = sembol ? `/api/kur/gecmis?sembol=${encodeURIComponent(sembol)}&limit=50` : '/api/kur/gecmis?limit=50';
        const res = await fetch(url);
        if (!res.ok) return;
        const veriler = await res.json();

        const tbody = document.getElementById('gecmisTableBody');
        if (veriler.length === 0) {
            tbody.innerHTML = '<tr><td colspan="6" style="text-align: center;">Henüz kayıt bulunamadı.</td></tr>';
            return;
        }

        tbody.innerHTML = veriler.reverse().map(item => `
            <tr>
                <td>#${item.id}</td>
                <td>${new Date(item.tarih).toLocaleString('tr-TR')}</td>
                <td><span class="rate-category ${item.kategori === 'Döviz' ? 'doviz' : 'kripto'}">${item.kategori}</span></td>
                <td><strong>${item.sembol}</strong></td>
                <td>${item.fiyat.toLocaleString('tr-TR', { minimumFractionDigits: 2, maximumFractionDigits: 4 })}</td>
                <td>${item.kaynak}</td>
            </tr>
        `).join('');
    } catch (err) {
        console.error('Tablo yüklenirken hata:', err);
    }
}

// 5. Kontrol Aksiyonları
async function servisBaslat() {
    await fetch('/api/kur/baslat', { method: 'POST' });
    await verileriYukle();
}

async function servisDurdur() {
    await fetch('/api/kur/durdur', { method: 'POST' });
    await verileriYukle();
}

async function manuelTetikle() {
    const btn = event.target;
    btn.innerText = '⌛ Çekiliyor...';
    btn.disabled = true;
    try {
        await fetch('/api/kur/tetikle', { method: 'POST' });
        await verileriYukle();
    } finally {
        btn.innerText = '⚡ Şimdi Çek & Kaydet';
        btn.disabled = false;
    }
}

function csvIndir() {
    window.open('/api/kur/csv', '_blank');
}

// ──────────────────────────────────────────
// 🔔 ALARM FONKSİYONLARI
// ──────────────────────────────────────────

async function alarmListesiniYukle() {
    const liste = document.getElementById('alarmList');
    try {
        const res = await fetch('/api/alarm');
        if (!res.ok) {
            liste.innerHTML = `<div class="alarm-empty">⚠️ Alarm listesi yüklenemedi (HTTP ${res.status})</div>`;
            return;
        }
        const alarmlar = await res.json();

        document.getElementById('alarmCount').textContent = `${alarmlar.length} Alarm`;

        if (alarmlar.length === 0) {
            liste.innerHTML = `<div class="alarm-empty">Henüz alarm oluşturulmadı. Yukarıdaki formu kullanarak ekleyin.</div>`;
            return;
        }

        liste.innerHTML = alarmlar.map(a => {
            const yon = a.yon === 0 ? '↑ Üstüne çıkınca' : '↓ Altına düşünce';
            const yonClass = a.yon === 0 ? 'yon-ust' : 'yon-alt';
            const aktifClass = a.aktif ? 'aktif' : 'pasif';
            const sonTetik = a.sonTetiklemeTarihi
                ? `Son bildirim: ${new Date(a.sonTetiklemeTarihi).toLocaleString('tr-TR')}`
                : 'Henüz tetiklenmedi';

            return `
            <div class="alarm-item ${aktifClass}">
                <div class="alarm-item-left">
                    <span class="alarm-sembol">${a.sembol}</span>
                    <span class="alarm-yon ${yonClass}">${yon}</span>
                    <span class="alarm-esik">${a.esikDeger.toLocaleString('tr-TR', { maximumFractionDigits: 4 })}</span>
                    ${a.aciklama ? `<span class="alarm-aciklama">"${a.aciklama}"</span>` : ''}
                </div>
                <div class="alarm-item-right">
                    <span class="alarm-son-tetik">${sonTetik}</span>
                    <button class="btn btn-sm ${a.aktif ? 'btn-warning' : 'btn-success'}" onclick="alarmToggle(${a.id})">
                        ${a.aktif ? '⏸ Pasifleştir' : '▶ Aktifleştir'}
                    </button>
                    <button class="btn btn-sm btn-danger" onclick="alarmSil(${a.id})">🗑 Sil</button>
                </div>
            </div>`;
        }).join('');
    } catch (err) {
        console.error('Alarm listesi yüklenirken hata:', err);
        liste.innerHTML = `<div class="alarm-empty">⚠️ Sunucuya bağlanılamadı.</div>`;
    }
}

async function alarmEkle() {
    const sembol = document.getElementById('alarmSembol').value;
    const esik = parseFloat(document.getElementById('alarmEsik').value);
    const yon = parseInt(document.getElementById('alarmYon').value);
    const aciklama = document.getElementById('alarmAciklama').value.trim();

    if (!esik || esik <= 0) {
        alert('Lütfen geçerli bir eşik değeri girin.');
        return;
    }

    const body = { sembol, esikDeger: esik, yon };
    if (aciklama) body.aciklama = aciklama;

    try {
        const res = await fetch('/api/alarm', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(body)
        });

        if (res.ok) {
            document.getElementById('alarmEsik').value = '';
            document.getElementById('alarmAciklama').value = '';
            await alarmListesiniYukle();
        } else {
            const hata = await res.text();
            alert('Hata: ' + hata);
        }
    } catch (err) {
        console.error('Alarm eklenirken hata:', err);
    }
}

async function alarmSil(id) {
    if (!confirm('Bu alarmı silmek istiyor musunuz?')) return;
    try {
        await fetch(`/api/alarm/${id}`, { method: 'DELETE' });
        await alarmListesiniYukle();
    } catch (err) {
        console.error('Alarm silinirken hata:', err);
    }
}

async function alarmToggle(id) {
    try {
        await fetch(`/api/alarm/${id}/toggle`, { method: 'PATCH' });
        await alarmListesiniYukle();
    } catch (err) {
        console.error('Alarm toggle hatası:', err);
    }
}


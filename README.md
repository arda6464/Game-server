# 🎮 High-Performance C# Game Server Infrastructure ⚡

> **.NET 10** tabanlı, ultra-optimize edilmiş, modüler ve kurumsal düzeyde özelliklerle donatılmış çok oyunculu oyun sunucu platformu.

Bu proje, sadece bir oyun sunucusu değil; yüksek trafikli oyunlar için tasarlanmış, düşük gecikme süreli (low-latency) ve kapsamlı yönetim araçlarına sahip bir mühendislik altyapısıdır.

---

## 🏗️ Derinlemesine Teknik Mimari

### 📡 Ağ ve Veri Katmanı (The Core)
*   **Layer 7 TcpProxy Multiplexer**: Sunucu, tek bir port üzerinden (örn: 5000) veri paketlerini gerçek zamanlı olarak koklar. Eğer paket bir HTTP başlığı (GET, POST vb.) içeriyorsa **Admin Dashboard API**'sine, binary bir protokol içeriyorsa **Game Socket**'ine milisaniyeler içinde yönlendirilir.
*   **Zero-Allocation ByteBuffer**: `ArrayPool<byte>.Shared` havuzunu kullanarak Garbage Collector (GC) üzerindeki yükü minimize eder. Yüksek eş zamanlılıkta bellek şişmelerini önler.
*   **ZigZag VarInt Encoding**: Tamsayı verileri değerlerine göre paketlenir. Örn: 1-127 arası sayılar sadece 1 byte yer kaplar. Bu teknikle bant genişliği kullanımı %70'e varan oranlarda optimize edilir.
*   **Reflection-Based Packet Handling**: `[PacketHandler]` niteliği (attribute) ile donatılmış sınıflar sunucu tarafından otomatik keşfedilir. Kod karmaşasına girmeden, yeni paketleri sisteme saniyeler içinde takıp çıkarabilirsiniz (Plug-and-Play).

### ⚔️ Savaş Motoru ve Senkronizasyon (Battle Engine)
*   **Hybrid Sync Protocol**: Pozisyon ve input verileri için yüksek hızlı **UDP**, ölüm, hasar ve eşya toplama gibi "asla kaybolmaması gereken" olaylar için güvenilir **TCP** kanalı senkronize çalışır.
*   **Tick-Based Physics**: Sunucu bazlı 20Hz (veya daha yüksek) tick döngüsü. Mermi (Bullet) ömür döngüsü, hızı, menzili ve hasar yarıçapı tamamen sunucu tarafında doğrulanır.
*   **Snapshot Broadcasting**: Tüm oyuncuların durumu (position/rotation) periyodik olarak serialize edilerek UDP üzerinden istemcilere "snapshot" olarak basılır.

---

## ✨ Kapsamlı Özellik Listesi (Full Feature Set)

### 👤 Gelişmiş Hesap ve Sosyal Sistemler
- **SQLite Database Integration**: JSON sisteminden, yüksek performanslı ve ilişkisel SQLite veritabanı mimarisine geçiş.
- **Advanced Account Cache**: Lru-ish mantığıyla çalışan, en çok erişilen hesap verilerini RAM'de tutan ultra-hızlı önbellek sistemi.
- **Quest Engine 2.0**: 
  - Günlük (Daily), Sezonluk (Seasonal) ve Premium görev katmanları.
  - Dinamik hedeflere (Kill count, Win count vb.) dayalı otomatik ilerleme takibi.
  - Sunucu taraflı otomatik yenileme (Refresh) mekanizması.
- **Klan (Club) Yönetimi**: Hiyerarşik roller (Lider, Üye vb.), kulüp içi gerçek zamanlı sohbet ve klan istatistikleri.
- **Friendship 2.0**: Durum bildirimi (Online/Offline), arkadaşlık istekleri ve "En İyi Arkadaşlar" listesi.

### 🔐 Moderasyon ve Güvenlik Altyapısı
- **Multi-Layered Ban System**:
  - **Perma & Süreli Ban**: SQL tabanlı, süresi dolunca otomatik açılan yasaklama sistemi.
  - **IP & DeviceID Ban**: Sadece hesap değil, cihaz ve ağ tabanlı tam engelleme.
  - **Ban History**: Her oyuncu için tutulan kapsamlı disiplin geçmişi.
- **Context-Aware Reporting**: Bir oyuncu raporlandığında, sunucu o andaki sohbet akışından (Son 10-15 mesaj) "bağlamı" otomatik yakalayarak adminlere sunar.
- **Küfür Filtresi**: `bannedword.json` üzerinden özelleştirilebilir, regex tabanlı sohbet koruması.

### 🛠️ Bakım ve Operasyon (DevOps)
- **Maintenance Automation**: Sunucuyu kapatmadan önce "Panic Mode" ile tüm oyunculara canlı bildirim gönderilir ve güvenli tahliye işlemi başlatılır.
- **Traffic Monitor**: Anlık paket trafiğini (Gelen/Giden, MB/s) paket türüne göre analiz eden dashboard entegresi.
- **Notification Policy Manager**: Kullanıcı tercihlerine ve cooldown (bekleme süresi) kurallarına göre akıllı bildirim (Toast, Popup, Push) dağıtımı.
- **Dynamic Config (Hot-Reload)**: `dynamic_config.json` izleme mekanizmasıyla, sunucuyu yeniden başlatmaya (reboot) gerek kalmadan ayarların anında uygulanması.
- **Automated Client Error Management**: Oyun istemcisinde (Unity vb.) oluşan hataların sunucuya gizli TCP paketleriyle bildirilip, `ClientErrorManager` aracılığıyla yönetim panelinde (Admin Dashboard) gerçek zamanlı loglanması.

### 🎮 Yeni Oyun ve Sosyal Sistemleri
- **Mağaza & Ekonomi (Shop System)**: Ürün satış, Market API ve "Mail Sistemi" entegre dijital pazar yeri.
- **Leaderboard Engine**: Oyuncular arası puanlama ve sıralama tabloları.
- **Team & Player Presence**: Oyuncuların durumlarının (Lobi, Oyunda, Çevrimdışı vb.) anlık durumu, "Hazır (Ready)" onayı verme ve takım içi senkronizasyon yetenekleri.

---

## 🤖 Discord Command Center & Bot Bridge

Proje, oyun ile Discord arasında çift yönlü bir köprü (bridge) kurar:
- **🎫 Ticket (Destek) Sistemi**: Oyuncu oyun içinden talep açar, bu talep Discord'da özel bir kanala dönüşür. Yetkililer Discord'dan yazar, oyuncu cevabı oyun içinde "Inbox" olarak alır.
- **🛡️ Moderasyon Komutları**: `/ban`, `/unban`, `/addrole` komutlarıyla Discord üzerinden direkt oyun veritabanına müdahale.
- **📊 Canlı Monitoring**: `/serverstats` ve `/traffic` komutları ile CPU, RAM ve anlık oyuncu dağılımı takibi.
- **🔔 Notification Hub**: `/sendallnotification` ile tüm oyunculara saniyeler içinde duyuru gönderme (In-game Popup).

---

## 📅 Roadmap (Yol Haritası)

- [x] **V1.0**: TCP/UDP Temel Altyapısı & ByteBuffer.
- [x] **V2.0**: SQLite Geçişi & Quest Engine.
- [x] **V2.5**: Discord Bilet Sistemi & Admin API - Single Port Layer 7 Routing.
- [x] **V3.0**: Market API, Mağaza (Shop), Mail ve Liderlik (Leaderboard) Katmanı.
- [x] **V3.1**: Friends & Sosyal Etkileşim Refactor, Team System, Player Presence.
- [x] **V3.5**: Dinamik Config (Hot-Reload) & Automated Client Error Reporting (Hata loglarının admin paneline entegrasyonu).
- [ ] **V4.0**: Horizontal Scaling (Redis Cluster Entegrasyonu).
- [ ] **V4.5**: Anti-Cheat Katmanı (Server-Side Havok Physics Simulation).

---

## 🚀 Başlarken

- **SDK**: .NET 10.0
- **Veritabanı**: SQLite 3
- **Platform**: Cross-platform (Windows, Linux, Docker)

1. `config.json` dosyasını düzenleyin.
2. `dotnet build -c Release`
3. `dotnet run`

---

## 🧠 Geliştirici
**Arda Sürücü**
> *"Basitlik karmaşıklığın çözülmüş halidir. Performans ise bu çözümün kalbidir."*

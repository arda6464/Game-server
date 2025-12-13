

# 🎮 Game Server

> ⚡ C# ile geliştirilen, çok oyunculu oyunlar için hafif, ölçeklenebilir ve modüler bir sunucu altyapısı.




---

# 🚀 Özellikler

⚙️ Ağ ve Veri Sistemi

TCP tabanlı, yüksek performanslı bağlantı mimarisi

ByteBuffer ile güvenli okuma/yazma işlemleri

Asenkron istemci bağlantıları (multi-thread destekli)

Paket kayıplarına karşı hata toleranslı yapı



---

 ## 👤 Hesap Sistemi

- Giriş / kayıt mekanizması

- Kalıcı AccountCache yönetimi

- Avatar, kullanıcı adı ve kimlik yönetimi

- Otomatik ID üretimi (örnek: 0FU8YO95)

- Hesap verileri diske JSON formatında kaydedilir




---

## 🧑‍🤝‍🧑 Kulüp (Clan) Sistemi

Üye ekleme / çıkarma

Yetki (role) yükseltme / düşürme sistemi

ClubCache ile bellek içi senkronizasyon

Otomatik JSON kaydı (ClubManager.Save())

Gerçek zamanlı güncelleme: üyeler online/offline olduğunda kulüp güncellenir



---

## 🤝 Arkadaşlık Sistemi (Geliştirme Aşamasında)

İstek gönderme / kabul etme / reddetme

Gerçek zamanlı durum bildirimi (online / offline)


## 🏪 Market sistemi

İtem satın alma


---

## 🎫 Destek Sistemi (Geliştirme Aşamasında)

destek oluşturma / Mesajlaşma 

---

## 🤖 Discord Bot Entegresi
  Destek açan oyuncuların mesajlarına yanıt verme


---
## 📦 Packet Sistemi

Tüm veri paketleri PacketHandler üzerinden yönetilir

Yeni komut tipleri kolayca eklenebilir

ByteBuffer tabanlı güvenli okuma / yazma

Hata durumlarında otomatik disconnect mekanizması



---

🔐 Oturum (Session) Yönetimi

Her oyuncuya özel Session nesnesi

Bağlantı, kopma ve veri işleme kontrolü

Thread-safe yapı ve otomatik temizleme

IP / Device-ID bazlı güvenlik kontrolü



---

🧱 Güvenlik

Her istemciden gelen veriler PacketID ile doğrulanır

Saldırı veya sahte veri durumunda oturum sonlandırılır

(Planlanan) Device ID doğrulama sistemi

(Planlanan) Sunucu taraflı “anti-tampering” mekanizması



---

🧩 Modüler Mimari

Her alt sistem (Account, Session, Club, Friend vs.) kendi yöneticisinde

Handlers/ dizininde tüm olaylar ayrı sınıflarla yönetilir

Yeni özellikler kolayca eklenebilir ve test edilebilir



---

🧾 Loglama Sistemi

Gerçek zamanlı konsol logları

Dosyaya otomatik kayıt

Log kategorileri: genel, error, network



---

🛠️ Geliştirme Notları

Dil: C# (.NET 10.0)

Bağımlılıklar: Newtonsoft.Json, System.Net.Sockets

Derleme: Visual Studio Code / Rider

Test Ortamı: Lokal TCP istemci simülasyonu



---

📅 Yol Haritası

✅ Hesap sistemi

✅ Kulüp sistemi

⚙️ Arkadaşlık sistemi

🔒 Güvenlik / DeviceID doğrulama

💬 Gerçek zamanlı sohbet

🎫 Destek Sistemi

🫂 Kayıt/Giriş Sistemi

📧 Eposta ile doğrulama

🏪 Market sistemi

🤖 Discord Sistemi

🌐 Maç sistemi (PvP sunucuları)



---

🧠 Geliştirici

Arda Sürücü

> “Basitlik, hız ve güven. Hepsi tek bir sunucuda.”




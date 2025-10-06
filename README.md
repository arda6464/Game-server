# 🎮 Game Server

> C# ile yazılmış, çok oyunculu oyunlar için hafif, ölçeklenebilir ve genişletilebilir bir sunucu altyapısı.

---

## 🚀 Özellikler

- ⚙️ **TCP Tabanlı Ağ Sistemi**  
  Verimli veri aktarımı için özel `ByteBuffer` yapısı.  
  Asenkron istemci bağlantılarını destekler.

- 👤 **Hesap Sistemi**  
  - Giriş / kayıt  
  - `AccountCache` üzerinde kalıcı veri tutma  
  - Avatar, kullanıcı adı ve kimlik yönetimi

- 🤝 **Arkadaşlık Sistemi (Geliştirme Aşamasında)**  
  - Arkadaşlık isteği gönderme / alma  
  - Kabul etme / reddetme  
  - Gerçek zamanlı çevrim içi / çevrim dışı durumu  
  - (Planlanan) arkadaş mesajlaşma desteği

- 📦 **Packet (Veri Paketi) Sistemi**  
  - Merkezî `PacketHandler` yönetimi  
  - Kolayca yeni paket türleri eklenebilir  
  - Güvenli `Read` / `Write` işlemleri `ByteBuffer` aracılığıyla yapılır

- 🔐 **Oturum (Session) Yönetimi**  
  - Her oyuncu için ayrı `Session` nesnesi  
  - Bağlantı, kopma ve veri işleme kontrolleri  
  - Thread-safe yapı ve otomatik temizleme sistemi

- 🧩 **Modüler Mimari**  
  - Her sistem kendi yöneticisinde (`AccountManager`, `SessionManager` vs.)  
  - Yeni özellikler kolayca eklenebilir  
  - `Handlers/` klasöründe tüm sunucu olayları yönetilir

- 🧾 **Loglama Sistemi**  
  - Gerçek zamanlı konsol logları  
  - Dosyaya kayıt özelliği  
  - Kategori bazlı loglar (`genellog`, `erorlog`)

---



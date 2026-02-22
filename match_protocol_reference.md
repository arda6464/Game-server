# Maç İçi Protokol Referansı — TCP vs UDP

## Temel Kural

> **"Kaçırılırsa oyun bozulur mu?"**
> - Evet → **TCP** (güvenilir, sıralı, yavaş)
> - Hayır → **UDP** (hızlı, kayıp olabilir)

---

## 📋 Olay Tablosu

| Olay | Protokol | Neden |
|---|---|---|
| **Oyuncu pozisyonu / hareketi** | UDP | Kaçırılsa bile bir sonraki frame gelir |
| **Oyuncu rotasyonu** | UDP | Anlık, eski değer zaten geçersiz |
| **Joystick input (PlayerInputPacket)** | UDP | Sürekli akıyor, bir kayıp önemli değil |
| **Mermi spawn (PlayerShootPacket)** | TCP | Kaçırılırsa client merminin varlığından habersiz kalır |
| **Mermi pozisyon güncellemesi** | UDP | Client zaten kendi simüle eder, sadece düzeltme |
| **Mermi yok olma (BulletDestroy)** | TCP | Kaçırılırsa client'ta hayalet mermi kalır |
| **Hasar / Health güncelleme** | TCP | Yanlış HP göstermek oyunu bozar |
| **Ölüm bildirimi (PlayerDeath)** | TCP | Bir kez olur, kesinlikle ulaşmalı |
| **Kill bildirimi (KillFeed)** | TCP | UI için kritik, kaçırılmamalı |
| **Maç başlangıcı** | TCP | Tüm oyuncular senkron başlamalı |
| **Maç bitişi / sonuç** | TCP | Kesinlikle ulaşmalı |
| **Oyuncu bağlantı kesildi** | TCP | Diğer oyuncular bilmeli |
| **Spawn pozisyonu** | TCP | Oyuncu nerede doğacağını bilmeli |
| **Yeniden doğma (Respawn)** | TCP | Kritik durum değişikliği |
| **Skor / istatistik güncelleme** | TCP | Doğru skor gösterilmeli |
| **Snapshot (tüm oyuncu pozisyonları)** | UDP | Periyodik, kaçırılsa bir sonraki gelir |

---

## 🏗️ Mevcut Projeye Göre Özet

```
UDP kanalı  → PlayerInputPacket, PlayerMovePacket, BroadcastSnapshot
TCP kanalı  → PlayerShootPacket, PlayerHealthUpdatePacket, PlayerDeathPacket,
               MatchFoundPacket, MatchEndPacket
```

---

## ⚡ Özel Durumlar

### Mermi Spawn — Neden TCP?
Mermi client'ta spawn olmazsa, hasar animasyonu çalışmaz ve oyuncu
"nereden vurulduğunu" göremez. Görsel tutarlılık için TCP şart.

### Health — Neden TCP?
UDP ile gönderilirse bir paket kaybolduğunda oyuncu
100 HP'de sanır ama aslında 50 HP'de olur. Sonraki paket gelene kadar
yanlış bilgi görür.

### Pozisyon — Neden UDP?
20Hz (50ms) ile sürekli gönderiliyor. Bir paket kaybolsa bile
50ms sonra doğru pozisyon gelir. TCP'nin overhead'i burada gereksiz.

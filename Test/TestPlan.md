# Stres Testi Planı

Sunucunun thread-safety ve genel stabilitesini test etmek için bir stres testi istemcisi oluşturulacaktır.

## Hedefler
- Birden fazla eşzamanlı bağlantıyı simüle etme.
- Rastgele aralıklarla kritik paketler (Arkadaşlık, Kulüp, Takım) gönderme.
- Sunucun "Collection was modified" veya benzeri concurrency hataları verip vermediğini izleme.

## Adımlar
1. `Test` klasörü içinde yeni bir .NET Console uygulaması başlat.
2. `ByteBuffer.cs` dosyasını test projesine entegre et.
3. `StressTester.cs` ile çoklu istemci mantığını kur.
4. Testi çalıştır ve sunucu loglarını takip et.

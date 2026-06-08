# Microsoft Yaz Okulu Sunum Notları

## Proje Adı

Darklove Local AI Module

## Tek Cümlelik Tanım

Türkçe duygusal metinleri cihaz üzerinde, açıklanabilir kurallarla analiz eden
ve riskli ifadelerde güvenli destek yönlendirmesi yapan .NET 10 Web API.

## Problem

Kullanıcılar motivasyon veya duygusal destek uygulamalarına hassas metinler
yazabilir. Metnin zorunlu olarak bir bulut servisine gönderilmesi gizlilik
endişesi doğurur. Ayrıca sonucu açıklanamayan bir model, eğitim projesinin ilk
aşamasında test ve hata analizi yapmayı zorlaştırır.

## Çözüm

- Metin yerel API içinde işlenir.
- Açıklanabilir kural tabanlı analiz kullanılır.
- Sonuçta duygu, skorlar ve eşleşen ifadeler gösterilir.
- Riskli metin normal motivasyon akışından ayrılır.
- API testlerle ve Swagger UI ile doğrulanır.

## Dürüst Teknik Konumlandırma

Bu sürüm gerçek bir yapay zekâ veya makine öğrenmesi modeli değildir. Sağlam ve
ölçülebilir bir temel MVP'dir. Gelecek fazda Microsoft Foundry Local
entegrasyonunun karşılaştırma zemini olarak kullanılacaktır.

## Kullanılan Teknolojiler

- C# ve .NET 10
- ASP.NET Core Minimal API
- OpenAPI ve Swagger UI
- xUnit ve WebApplicationFactory
- GitHub Actions

## 6 Dakikalık Sunum Akışı

1. Problemi ve gizlilik gerekçesini anlat.
2. Mimari diyagramı göster.
3. Swagger UI'ı aç.
4. Normal üzüntü örneğini gönder; skorları ve anahtar ifadeleri göster.
5. `Sinir sistemi hakkında okuyorum` örneğinin neden anger olmadığını göster.
6. Eşit sadness ve anger örneğinde `mixed` sonucunu göster.
7. Kriz örneğinde güvenli mesajı ve 112 yönlendirmesini göster.
8. `dotnet test` sonucunda 20 testin geçtiğini göster.
9. Sınırlamaları ve Foundry Local sonraki adımını anlat.

## Önerilen Demo Metinleri

### Sadness

`Bugün kendimi yalnız ve yorgun hissediyorum.`

Beklenen: `sadness`, skor `2`, güven `0.9`.

### Mixed

`Hem yalnızım hem de sinirliyim.`

Beklenen: `mixed`.

### Yanlış Pozitif Kontrolü

`Sinir sistemi hakkında bir makale okuyorum.`

Beklenen: `neutral`.

### Kriz Güvenliği

`Artık yaşamak istemiyorum.`

Beklenen: `riskLevel=high`, profesyonel destek ve 112 yönlendirmesi.

## Jüriden Gelebilecek Sorular

### Neden Minimal API?

API yüzeyi küçük olduğu için daha az altyapı koduyla açık ve test edilebilir
endpointler oluşturmayı sağlar.

### Neden doğrudan yapay zekâ modeli kullanılmadı?

Önce veri akışı, güvenlik, test, açıklanabilirlik ve API sözleşmesi kuruldu.
Model entegrasyonu sonraki fazda aynı servis arayüzü üzerinden eklenebilir.

### Confidence gerçek olasılık mı?

Hayır. Kural sayısı ve en yakın rakip skor arasındaki farka dayanan, belgelenmiş
sezgisel bir değerdir.

### Proje tıbbi tavsiye veriyor mu?

Hayır. Teşhis koymaz ve profesyonel desteğin yerine geçmez. Riskli ifadelerde
yalnızca güvenli yardım yönlendirmesi yapar.

### Veri nerede saklanıyor?

Bu MVP kullanıcı metnini veritabanına yazmaz ve loglamaz. Metin yalnızca istek
süresince bellekte işlenir.

# Microsoft Yaz Okulu Sunum Notları

## Proje Adı

Darklove Local AI Module

## Tek Cümlelik Tanım

Türkçe duygusal metinleri açık ağırlıklı yerel model ve güvenli kural tabanlı
fallback ile analiz eden .NET 10 Web API.

## Problem

Kullanıcılar motivasyon veya duygusal destek uygulamalarına hassas metinler
yazabilir. Metnin zorunlu olarak bir bulut servisine gönderilmesi gizlilik
endişesi doğurur. Ayrıca sonucu açıklanamayan bir model, eğitim projesinin ilk
aşamasında test ve hata analizi yapmayı zorlaştırır.

## Çözüm

- Metin yerel API içinde işlenir.
- Ollama üzerinde çalışan açık ağırlıklı model kullanılır.
- Model kullanılamazsa açıklanabilir kural tabanlı fallback çalışır.
- Sonuçta analiz yöntemi, model ve iki ayrı skor kaynağı gösterilir.
- Riskli metin normal motivasyon akışından ayrılır.
- Sonuçlar Türkçe web demo ekranında gösterilir; API ayrıca testler ve Swagger UI ile doğrulanır.

## Dürüst Teknik Konumlandırma

Bu sürüm Ollama üzerinde gerçek bir yerel dil modeli çalıştırabilir. Ancak model
tek güvenlik kaynağı değildir: kriz tespiti ve kullanıcı mesajları deterministik
kodda kalır. Ollama kapalıysa sistem kural tabanlı fallback ile çalışmaya devam
eder.

## Kullanılan Teknolojiler

- C# ve .NET 10
- ASP.NET Core Minimal API
- Ollama ve Qwen3
- JSON Schema structured output
- Türkçe web demo ekranı
- OpenAPI ve Swagger UI
- xUnit ve WebApplicationFactory
- GitHub Actions

## 6 Dakikalık Sunum Akışı

1. Problemi ve gizlilik gerekçesini anlat.
2. Mimari diyagramı göster.
3. Kök adresteki Türkçe web demo ekranını aç.
4. Ekranın üstündeki model durumunun `ready` olduğunu göster.
5. Modelin kural listesinde olmayan bir metni sınıflandırmasını göster.
6. Ollama'yı kapatıp `rule-based-fallback` davranışını göster.
7. Kriz örneğinde modele gidilmeden güvenli mesaj üretildiğini göster.
8. Swagger UI ile API sözleşmesini kısaca göster.
9. `dotnet test` sonucunda 31 testin geçtiğini göster.
10. Foundry Local adaptörünü sonraki adım olarak anlat.

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

### Hangi model kullanılıyor?

Varsayılan olarak Ollama üzerindeki `qwen3:4b` kullanılır. Yapılandırmadaki model
adı değiştirilerek başka bir uyumlu yerel model seçilebilir.

### Confidence gerçek olasılık mı?

Model kullanıldığında modelin yapılandırılmış confidence değeri, fallback
kullanıldığında belgelenmiş kural tabanlı sezgisel değer döner. Hangi yöntemin
kullanıldığı `analysisMethod` alanından anlaşılır.

### Proje tıbbi tavsiye veriyor mu?

Hayır. Teşhis koymaz ve profesyonel desteğin yerine geçmez. Riskli ifadelerde
yalnızca güvenli yardım yönlendirmesi yapar.

### Veri nerede saklanıyor?

Bu MVP kullanıcı metnini veritabanına yazmaz ve loglamaz. Metin yalnızca istek
süresince bellekte işlenir.

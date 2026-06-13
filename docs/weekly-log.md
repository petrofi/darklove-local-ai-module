# Haftalık Geliştirme Günlüğü

## 1. Hafta: Proje Kurulumu

### Hedef

Problemi, teknik yönü ve depo yapısını belirlemek.

### Tamamlananlar

- Proje klasörleri oluşturuldu.
- README, mimari ve yol haritası belgeleri eklendi.
- Gizlilik odaklı ve yerel çalışma hedefi tanımlandı.

## 2. Hafta: Web API Temeli

### Hedef

Çalışan bir .NET Web API oluşturmak.

### Tamamlananlar

- .NET 10 Minimal API projesi oluşturuldu.
- İlk health endpointi eklendi.
- OpenAPI altyapısı kuruldu.

## 3. Hafta: İlk Duygu Analizi

### Hedef

Kural tabanlı ilk analiz sonucunu üretmek.

### Tamamlananlar

- Duygu analizi endpointi eklendi.
- Dört duygu kategorisi ve motivasyon mesajları tanımlandı.
- İlk kriz ifadesi kontrolü eklendi.

### Tespit Edilen Sorunlar

- Tüm kod `Program.cs` içindeydi.
- Alt metin eşleşmesi yanlış sonuçlar üretebiliyordu.
- Eşit skor davranışı açık değildi.
- Kriz durumunda normal motivasyon mesajı dönüyordu.
- Test projesi ve Swagger UI bulunmuyordu.
- README mevcut ilerlemeyi göstermiyordu.

## 4. Hafta: Sağlam MVP

### Hedef

Projeyi Microsoft Yaz Okulu demosunda güvenle anlatılabilecek, testli ve
açıklanabilir bir MVP haline getirmek.

### Tamamlananlar

- DTO, endpoint ve servis sorumlulukları ayrıldı.
- Türkçe kültür ve Unicode normalizasyonu eklendi.
- Tam kelime/ifade eşleşmesine geçildi.
- Tekrarlanan kuralların yalnızca bir kez puanlanması sağlandı.
- `neutral` ve `mixed` sonuçları tanımlandı.
- Belgelenmiş sezgisel güven formülü uygulandı.
- Kriz durumları için ayrı güvenli mesaj ve 112 yönlendirmesi eklendi.
- Boş ve uzun metin doğrulaması ProblemDetails formatına taşındı.
- Bozuk JSON isteklerinin 400 dönmesi sağlandı.
- HealthCheckService, Swagger UI ve OpenAPI metadata eklendi.
- Solution, xUnit test projesi ve GitHub Actions oluşturuldu.
- 20 birim ve entegrasyon testi başarıyla çalıştırıldı.
- README, mimari, yol haritası ve sunum notları güncellendi.
- Ayrıntılı Türkçe teknik rapor hazırlandı.

## 5. Hafta: Açık Model Entegrasyonu

### Hedef

Projeyi yerelde çalışan açık ağırlıklı modellerle gerçek sınıflandırma
yapabilecek hale getirmek.

### Tamamlananlar

- Ollama `/api/chat` entegrasyonu eklendi.
- Varsayılan model `qwen3:4b` olarak yapılandırıldı.
- JSON Schema structured output kullanıldı.
- Model yanıtı duygu adları ve 0-1 skor aralıklarıyla doğrulandı.
- Model çalışmadığında otomatik kural tabanlı fallback eklendi.
- Kriz metinlerinde modele hiç gidilmemesi sağlandı.
- Model endpointi güvenlik nedeniyle loopback ile sınırlandı.
- `GET /api/model/status` endpointi eklendi.
- API yanıtına analiz yöntemi, model, model skorları ve fallback nedeni eklendi.
- Sahte Ollama HTTP yanıtlarıyla model entegrasyon testleri yazıldı.
- Toplam 28 test başarıyla çalıştırıldı.

## 6. Hafta: Yerel Web Demo

### Hedef

API sözleşmesini değiştirmeden, jüri sunumunda teknik olmayan kişilerin de
kullanabileceği sade bir demo ekranı hazırlamak.

### Tamamlananlar

- `wwwroot` altında Türkçe, mobil uyumlu ve erişilebilir analiz ekranı eklendi.
- Model çalışma zamanı durumu sayfa açılışında görünür hale getirildi.
- Duygu, güven, analiz yöntemi, skorlar ve eşleşen ifadeler görselleştirildi.
- Kriz sonucu sıradan analizden farklı ve dikkat çekici biçimde gösterildi.
- Hazır demo metinleri ve 2.000 karakter sayacı eklendi.
- Statik dosyalara `nosniff` ve `no-referrer` güvenlik başlıkları eklendi.
- Uygulama profilleri Swagger yerine web demonun kök adresini açacak şekilde düzenlendi.
- Kök sayfa ve statik dosyalar için entegrasyon testleri eklendi.
- Toplam 31 test başarıyla çalıştırıldı.

## 7. Hafta: Web Model Yöneticisi

### Hedef

Bilgisayardaki mevcut modelleri web ekranında görünür yapmak ve terminal
komutu gerektirmeden model seçme ve indirme akışı sağlamak.

### Tamamlananlar

- Bilgisayardaki modellerin LM Studio altında olduğu tespit edildi.
- LM Studio REST API ve OpenAI uyumlu structured output entegrasyonu eklendi.
- LM Studio servisini mevcut `lms` aracıyla otomatik başlatma desteği eklendi.
- Yalnızca çalışabilir LLM'lerin listelenmesi sağlandı; embedding ve yardımcı
  görsel dosyaları ayrı model gibi gösterilmedi.
- Aktif model seçimi thread-safe uygulama durumuna taşındı.
- Model yükleme, indirme başlatma ve indirme ilerleme endpointleri eklendi.
- Web arayüzüne model kartları, boyut/quantization bilgileri ve ilerleme çubuğu eklendi.
- İndirme URL'leri güvenlik için yalnızca `huggingface.co` alan adıyla sınırlandı.
- Statik dosya önbelleğinin eski JavaScript göstermemesi için sürümleme eklendi.
- Toplam 38 test başarıyla çalıştırıldı.

## Sonraki Adım

Gerçek cihaz üzerinde farklı model boyutlarının hız ve doğruluk ölçümünü yapmak,
ardından Microsoft Foundry Local için ikinci bir adaptör eklemek.

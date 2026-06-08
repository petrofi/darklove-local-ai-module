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

## Sonraki Adım

Etiketli bir Türkçe değerlendirme veri kümesiyle kural tabanlı yaklaşımın
ölçülmesi ve ardından Microsoft Foundry Local prototipine geçilmesi.

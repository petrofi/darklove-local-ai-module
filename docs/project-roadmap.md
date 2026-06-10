# Proje Yol Haritası

## Tamamlanan Sağlam MVP

- [x] GitHub deposu ve dokümantasyon yapısı
- [x] .NET 10 Minimal API
- [x] Health check endpointi
- [x] Kural tabanlı Türkçe duygu analizi
- [x] Açıklanabilir skor ve eşleşen anahtar ifadeler
- [x] `neutral` ve `mixed` sonuçları
- [x] Kriz ifadesi kontrolü ve 112 yönlendirmesi
- [x] ProblemDetails tabanlı doğrulama
- [x] OpenAPI ve Swagger UI
- [x] Birim ve API entegrasyon testleri
- [x] GitHub Actions build/test iş akışı
- [x] Türkçe teknik rapor ve demo notları
- [x] Ollama açık model istemcisi
- [x] JSON şemalı model çıktısı ve sözleşme doğrulaması
- [x] Hibrit model/kural fallback mimarisi
- [x] Yerel model durum endpointi
- [x] Kriz ifadelerinde modeli devre dışı bırakan güvenlik politikası

## Sonraki Faz: Veri ve Ölçüm

- [ ] Küçük, anonim ve etiketli Türkçe değerlendirme veri kümesi hazırlamak
- [ ] Precision, recall ve F1 gibi ölçümleri tanımlamak
- [ ] Yanlış pozitif ve yanlış negatif örneklerini raporlamak
- [ ] Anahtar ifade listesini ölçüm sonuçlarına göre iyileştirmek

## Sonraki Faz: Python Deneyleri

- [ ] Aynı veri kümesinde Python tabanlı metin sınıflandırma deneyi yapmak
- [ ] Deney ortamını tekrar üretilebilir hale getirmek
- [ ] Kural tabanlı ve deneysel model sonuçlarını karşılaştırmak

## Sonraki Faz: Model Karşılaştırması

- [ ] Qwen3 1.7B, 4B ve uygun diğer açık modelleri karşılaştırmak
- [ ] Gecikme, kaynak tüketimi ve doğruluk karşılaştırması yapmak
- [ ] Microsoft Foundry Local için `IOpenSourceModelClient` adaptörü eklemek
- [ ] Ollama ve Foundry Local sonuçlarını aynı veri kümesinde karşılaştırmak

## Son Teslim

- [ ] Demo senaryolarını son kez doğrulamak
- [ ] Ekran görüntüleri ve kısa demo videosu hazırlamak
- [ ] Sunum slaytlarını teknik raporla uyumlu hale getirmek
- [ ] Bilinen sınırlamaları ve etik uyarıları açıkça sunmak

# Darklove Local AI Module

Darklove Local AI Module, Microsoft Yaz Okulu kapsamında geliştirilen, Türkçe
metinlerde duygusal işaretleri yerel olarak analiz eden gizlilik odaklı bir
.NET 10 Web API projesidir.

> Bu sürüm bir makine öğrenmesi modeli değildir. Açıklanabilir ve test
> edilebilir bir kural tabanlı MVP'dir. Tıbbi teşhis koymaz ve profesyonel
> psikolojik desteğin yerine geçmez.

## Özellikler

- `sadness`, `anxiety`, `hope` ve `anger` duygu işaretlerini analiz eder.
- Eşit en yüksek skorlarda `mixed`, eşleşme yoksa `neutral` döndürür.
- Türkçe büyük/küçük harf ve Unicode normalizasyonunu destekler.
- Alt metin yerine tam kelime ve ifade eşleşmesi kullanır.
- Aynı kural metinde tekrar edilse bile yalnızca bir puan verir.
- Skorları ve eşleşen anahtar ifadeleri açıklanabilir sonuç olarak döndürür.
- Kriz ifadelerini ayrı değerlendirir ve güvenli destek mesajı üretir.
- Boş veya 2.000 karakteri aşan metinleri `ProblemDetails` ile reddeder.
- Health check, OpenAPI, Swagger UI, birim testleri ve API testleri içerir.
- Kullanıcı metnini saklamaz veya loglamaz.

## Teknolojiler

- .NET 10 ve ASP.NET Core Minimal API
- Microsoft.AspNetCore.OpenApi
- Swagger UI
- xUnit
- Microsoft.AspNetCore.Mvc.Testing
- GitHub Actions

## Çalıştırma

Gereksinim: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```powershell
dotnet restore Darklove.LocalAI.slnx
dotnet run --project backend/Darklove.LocalAI.Api --launch-profile http
```

Uygulama başladıktan sonra:

- Swagger UI: `http://localhost:5019/swagger`
- OpenAPI belgesi: `http://localhost:5019/openapi/v1.json`
- Health check: `http://localhost:5019/api/health`

HTTPS profiliyle çalıştırmak için:

```powershell
dotnet run --project backend/Darklove.LocalAI.Api --launch-profile https
```

## Test

```powershell
dotnet build Darklove.LocalAI.slnx
dotnet test Darklove.LocalAI.slnx
```

Test paketi; duygu sonuçlarını, Türkçe karakterleri, skor formülünü, kriz
yanıtını, doğrulamayı, OpenAPI belgesini ve Swagger UI'ı kapsar.

## API Örneği

`POST /api/emotion/analyze`

```json
{
  "userText": "Bugün kendimi yalnız ve yorgun hissediyorum."
}
```

```json
{
  "detectedEmotion": "sadness",
  "confidence": 0.9,
  "scores": {
    "sadness": 2,
    "anxiety": 0,
    "hope": 0,
    "anger": 0
  },
  "matchedKeywords": {
    "sadness": [
      "yalnız",
      "yorgun"
    ]
  },
  "riskLevel": "none",
  "needsSupportWarning": false,
  "motivationMessage": "Bugün zor geçiyor olabilir..."
}
```

Makine tarafından kullanılan duygu ve risk kodları İngilizce, kullanıcıya
gösterilen mesajlar Türkçedir.

## Proje Yapısı

```text
backend/   ASP.NET Core API ve analiz servisi
tests/     Birim ve HTTP entegrasyon testleri
docs/      Mimari, yol haritası, günlük ve teknik rapor
.github/   GitHub Actions CI iş akışı
```

Projenin neden ve nasıl geliştirildiğini anlatan ayrıntılı belge:
[Türkçe Teknik Rapor](docs/technical-report-tr.md)

## Sonraki Aşamalar

- Etiketli Türkçe test veri kümesi hazırlamak
- Kural tabanlı sonuçlar için doğruluk ölçümü yapmak
- Python deneyleri geliştirmek
- Microsoft Foundry Local ile gerçek yerel model prototipi oluşturmak
- Kural tabanlı ve model tabanlı sonuçları karşılaştırmak

Bu maddeler mevcut sağlam MVP'nin dışında, sonraki araştırma ve geliştirme
fazlarıdır.

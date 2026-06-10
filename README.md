# Darklove Local AI Module

Darklove Local AI Module, Microsoft Yaz Okulu kapsamında geliştirilen, Türkçe
metinlerde duygusal işaretleri yerel olarak analiz eden gizlilik odaklı bir
.NET 10 Web API projesidir.

Uygulama, Ollama üzerinden cihazda çalışan açık ağırlıklı modelleri kullanır.
Varsayılan model `qwen3:4b` modelidir. Ollama veya model kullanılamıyorsa sistem
otomatik olarak açıklanabilir kural tabanlı analize geri döner.

> Proje tıbbi teşhis koymaz ve profesyonel psikolojik desteğin yerine geçmez.
> Kriz ifadeleri yapay zekâ modeline bırakılmaz; deterministik güvenlik
> kurallarıyla ele alınır.

## Özellikler

- Ollama üzerinde Qwen, Mistral ve benzeri yerel modellerle çalışabilir.
- Model yanıtını JSON şemasıyla sınırlar ve uygulama tarafında doğrular.
- Model kapalıysa veya hata verirse kural tabanlı fallback kullanır.
- Kriz ifadelerinde modeli çağırmadan güvenli destek ve `112` yönlendirmesi yapar.
- `sadness`, `anxiety`, `hope`, `anger`, `neutral` ve `mixed` sonuçlarını destekler.
- Hangi analiz yönteminin ve modelin kullanıldığını API yanıtında gösterir.
- Kural skorlarını, eşleşen ifadeleri ve model skorlarını ayrı alanlarda döndürür.
- Yalnızca loopback üzerindeki model endpointlerine izin verir.
- Kullanıcı metnini saklamaz veya loglamaz.
- ProblemDetails, health check, model status, OpenAPI ve Swagger UI içerir.
- Model, fallback, güvenlik ve HTTP davranışlarını kapsayan 28 test içerir.

## Teknolojiler

- .NET 10 ve ASP.NET Core Minimal API
- Ollama yerel model çalışma zamanı
- JSON Schema structured output
- `IHttpClientFactory`
- OpenAPI ve Swagger UI
- xUnit ve `WebApplicationFactory`
- GitHub Actions

## Açık Model Kurulumu

1. [Ollama'yı indirip kur](https://ollama.com/download).
2. Terminalde varsayılan modeli indir:

```powershell
ollama pull qwen3:4b
```

3. Ollama otomatik başlamadıysa çalıştır:

```powershell
ollama serve
```

4. Modelin hazır olduğunu kontrol et:

```powershell
ollama list
```

Model seçimi `appsettings.json` içindeki `LocalModel` bölümünden yapılır:

```json
{
  "LocalModel": {
    "Enabled": true,
    "Provider": "ollama",
    "Endpoint": "http://localhost:11434",
    "Model": "qwen3:4b",
    "TimeoutSeconds": 90
  }
}
```

Başka bir Ollama modeli kullanmak için yalnızca model adını değiştir:

```powershell
$env:LocalModel__Model = "qwen3:1.7b"
```

Kullanılacak modelin lisansını proje gereksinimlerine göre ayrıca kontrol et.

## API'yi Çalıştırma

Gereksinim: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```powershell
dotnet restore Darklove.LocalAI.slnx
dotnet run --project backend/Darklove.LocalAI.Api --launch-profile http
```

Uygulama başladıktan sonra:

- Swagger UI: `http://localhost:5019/swagger`
- Model durumu: `http://localhost:5019/api/model/status`
- Health check: `http://localhost:5019/api/health`
- OpenAPI: `http://localhost:5019/openapi/v1.json`

`/api/model/status` yanıtındaki durumlar:

- `ready`: Ollama ve seçilen model hazır.
- `model-not-found`: Ollama çalışıyor fakat model indirilmemiş.
- `runtime-unavailable`: Ollama çalışmıyor veya ulaşılamıyor.
- `disabled`: Model kullanımı yapılandırmada kapalı.

## Test

```powershell
dotnet build Darklove.LocalAI.slnx
dotnet test Darklove.LocalAI.slnx
```

Testler gerçek bir model indirmeden sahte Ollama HTTP yanıtlarıyla model
sözleşmesini doğrular. Gerçek model testi için Ollama ve seçilen model ayrıca
çalıştırılmalıdır.

## API Örneği

`POST /api/emotion/analyze`

```json
{
  "userText": "İçimde ağır bir hüzün var."
}
```

Model kullanıldığında örnek yanıt:

```json
{
  "detectedEmotion": "sadness",
  "confidence": 0.86,
  "scores": {
    "sadness": 0,
    "anxiety": 0,
    "hope": 0,
    "anger": 0
  },
  "matchedKeywords": {},
  "riskLevel": "none",
  "needsSupportWarning": false,
  "motivationMessage": "Bugün zor geçiyor olabilir...",
  "analysisMethod": "open-source-model",
  "model": "qwen3:4b",
  "modelScores": {
    "sadness": 0.86,
    "anxiety": 0.08,
    "hope": 0.02,
    "anger": 0.01,
    "neutral": 0.03
  }
}
```

Fallback kullanılırsa:

```json
{
  "analysisMethod": "rule-based-fallback",
  "model": "qwen3:4b",
  "fallbackReason": "model-unavailable"
}
```

`scores` mevcut kural eşleşme sayılarını, `modelScores` ise modelin 0-1
aralığındaki duygu skorlarını gösterir.

## Güvenlik Yaklaşımı

- Model endpointi yalnızca `localhost`, `127.0.0.1` veya `::1` olabilir.
- Kullanıcı metni buluta gönderilmez.
- Kullanıcı metni uygulama loglarına yazılmaz.
- Modelden motivasyon veya kriz tavsiyesi alınmaz.
- Model yalnızca yapılandırılmış duygu sınıflandırması üretir.
- Kriz tespiti ve kullanıcı mesajları uygulama kodunda kalır.

## Proje Yapısı

```text
backend/   API, hibrit analiz servisi, Ollama istemcisi ve güvenlik kuralları
tests/     Kural, model istemcisi, fallback ve HTTP entegrasyon testleri
docs/      Mimari, yol haritası, günlük ve teknik rapor
.github/   GitHub Actions CI iş akışı
```

Projenin neden ve nasıl geliştirildiğini anlatan ayrıntılı belge:
[Türkçe Teknik Rapor](docs/technical-report-tr.md)

## Sonraki Aşamalar

- Etiketli Türkçe değerlendirme veri kümesi hazırlamak
- Qwen model boyutlarını doğruluk ve hız açısından karşılaştırmak
- Microsoft Foundry Local için ikinci bir model adaptörü eklemek
- Kural ve model sonuçlarını precision, recall ve F1 ile ölçmek

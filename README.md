# Darklove Local AI Module

Darklove Local AI Module, Microsoft Yaz Okulu kapsamında geliştirilen, Türkçe
metinlerde duygusal işaretleri yerel olarak analiz eden gizlilik odaklı bir
.NET 10 Web API projesidir.

Uygulama, LM Studio veya Ollama üzerinden cihazda çalışan açık modelleri kullanır.
Geliştirme profili LM Studio'yu otomatik başlatabilir, bilgisayardaki modelleri
web ekranında listeler ve seçilen modeli analiz için yükler. Çalışma zamanı veya
model kullanılamıyorsa sistem açıklanabilir kural tabanlı analize geri döner.

> Proje tıbbi teşhis koymaz ve profesyonel psikolojik desteğin yerine geçmez.
> Kriz ifadeleri yapay zekâ modeline bırakılmaz; deterministik güvenlik
> kurallarıyla ele alınır.

## Özellikler

- LM Studio ve Ollama üzerinde Qwen, Granite, Mistral ve benzeri modellerle çalışabilir.
- Bilgisayardaki LLM'leri web ekranında listeler ve aktif modeli değiştirebilir.
- Model kataloğu kimliği veya Hugging Face bağlantısıyla indirme başlatabilir.
- LM Studio indirmelerinde boyut, hız ve yüzde ilerlemesini gösterir.
- Model yanıtını JSON şemasıyla sınırlar ve uygulama tarafında doğrular.
- Model kapalıysa veya hata verirse kural tabanlı fallback kullanır.
- Kriz ifadelerinde modeli çağırmadan güvenli destek ve `112` yönlendirmesi yapar.
- `sadness`, `anxiety`, `hope`, `anger`, `neutral` ve `mixed` sonuçlarını destekler.
- Hangi analiz yönteminin ve modelin kullanıldığını API yanıtında gösterir.
- Kural skorlarını, eşleşen ifadeleri ve model skorlarını ayrı alanlarda döndürür.
- Yalnızca loopback üzerindeki model endpointlerine izin verir.
- Kullanıcı metnini saklamaz veya loglamaz.
- Kurulum gerektirmeyen Türkçe web demo ekranı içerir.
- ProblemDetails, health check, model status, OpenAPI ve Swagger UI içerir.
- Model, fallback, güvenlik, web arayüzü ve HTTP davranışlarını kapsayan 38 test içerir.

## Teknolojiler

- .NET 10 ve ASP.NET Core Minimal API
- LM Studio ve Ollama yerel model çalışma zamanları
- JSON Schema structured output
- `IHttpClientFactory`
- HTML, CSS ve JavaScript ile aynı API içinde sunulan demo arayüzü
- OpenAPI ve Swagger UI
- xUnit ve `WebApplicationFactory`
- GitHub Actions

## Yerel Model Kullanımı

Bu bilgisayarda LM Studio zaten kurulu olduğu için ek model yöneticisi kurulumu
gerekmez. Uygulama geliştirme profilinde LM Studio arka plan servisini yerel
`lms` aracıyla başlatır. Ardından `http://localhost:5019` adresindeki **Yerel
model yöneticisi** bölümünden:

- Yüklü dil modellerini görebilir,
- **Yükle ve kullan** ile aktif modeli değiştirebilir,
- Katalog kimliği veya `huggingface.co` bağlantısıyla yeni model indirebilir,
- İndirme ilerlemesini izleyebilirsin.

> Başka bir bilgisayarda LM Studio veya Ollama çalışma zamanlarından en az biri
> kurulu olmalıdır. Web ekranı model indirmek için terminal komutu gerektirmez,
> ancak model dosyasını çalıştıracak yerel runtime'ın yerini tutmaz.

Geliştirme yapılandırması:

```json
{
  "LocalModel": {
    "Enabled": true,
    "Provider": "lmstudio",
    "Endpoint": "http://localhost:1234",
    "Model": "qwen/qwen3-vl-30b",
    "TimeoutSeconds": 300,
    "AutoStartRuntime": true
  }
}
```

Ollama kullanmak için:

```powershell
$env:LocalModel__Provider = "ollama"
$env:LocalModel__Endpoint = "http://localhost:11434"
$env:LocalModel__Model = "qwen3:4b"
```

Kullanılacak modelin lisansını proje gereksinimlerine göre ayrıca kontrol et.

## API'yi Çalıştırma

Gereksinim: [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```powershell
dotnet restore Darklove.LocalAI.slnx
dotnet run --project backend/Darklove.LocalAI.Api --launch-profile http
```

Uygulama başladıktan sonra:

- Web demo: `http://localhost:5019`
- Swagger UI: `http://localhost:5019/swagger`
- Model durumu: `http://localhost:5019/api/model/status`
- Model kataloğu: `http://localhost:5019/api/models/`
- Health check: `http://localhost:5019/api/health`
- OpenAPI: `http://localhost:5019/openapi/v1.json`

Web demo ekranında bir örnek metin seçebilir veya en fazla 2.000 karakterlik
kendi metnini yazıp **Metni analiz et** düğmesine basabilirsin. Ekran; bulunan
duyguyu, güven değerini, analiz yöntemini, kural/model skorlarını, eşleşen
ifadeleri ve güvenli kullanıcı mesajını gösterir.

`/api/model/status` yanıtındaki durumlar:

- `ready`: Seçilen model yüklü ve kullanıma hazır.
- `model-not-loaded`: Model diskte var fakat henüz belleğe yüklenmedi.
- `model-not-found`: Seçilen model yerel katalogda bulunamadı.
- `runtime-unavailable`: LM Studio veya Ollama çalışmıyor ya da ulaşılamıyor.
- `disabled`: Model kullanımı yapılandırmada kapalı.

## Test

```powershell
dotnet build Darklove.LocalAI.slnx
dotnet test Darklove.LocalAI.slnx
```

Testler gerçek model indirmeden sahte LM Studio ve Ollama HTTP yanıtlarıyla
listeleme, seçme, structured output ve indirme sözleşmelerini doğrular.

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
  "model": "qwen/qwen3-vl-30b",
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
  "model": "qwen/qwen3-vl-30b",
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
backend/   API, web demo, model yöneticisi, LM Studio/Ollama istemcileri ve güvenlik kuralları
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

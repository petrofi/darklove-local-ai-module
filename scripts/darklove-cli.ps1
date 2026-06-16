[CmdletBinding()]
param(
    [string]$Once,
    [switch]$NoStart,
    [switch]$Check
)

$ErrorActionPreference = "Stop"
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiBaseUrl = "http://localhost:5019"
$script:apiProcess = $null
$script:apiOutputTask = $null
$script:apiErrorTask = $null
$script:chatHistory = New-Object 'System.Collections.Generic.List[object]'

function Test-DarkloveApi {
    try {
        $null = Invoke-WebRequest `
            -Uri "$apiBaseUrl/api/health" `
            -UseBasicParsing `
            -TimeoutSec 2
        return $true
    }
    catch {
        return $false
    }
}

function Wait-DarkloveApi {
    param([int]$TimeoutSeconds = 45)

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-DarkloveApi) {
            return
        }

        if ($null -ne $script:apiProcess -and $script:apiProcess.HasExited) {
            $details = $script:apiErrorTask.GetAwaiter().GetResult()
            throw "API başlatılamadı. $details"
        }

        Start-Sleep -Milliseconds 500
    }

    throw "API $TimeoutSeconds saniye içinde hazır olmadı."
}

function Start-DarkloveApi {
    if (Test-DarkloveApi) {
        Write-Host "Yerel API hazır: $apiBaseUrl" -ForegroundColor DarkGreen
        return
    }

    if ($NoStart) {
        throw "API çalışmıyor. Önce uygulamayı başlatın veya -NoStart seçeneğini kaldırın."
    }

    if ($null -eq (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw ".NET 10 SDK bulunamadı. https://dotnet.microsoft.com/download/dotnet/10.0"
    }

    $projectDirectory = Join-Path $repoRoot "backend\Darklove.LocalAI.Api"
    $projectPath = Join-Path $projectDirectory "Darklove.LocalAI.Api.csproj"
    $executablePath = Join-Path $projectDirectory "bin\Release\net10.0\Darklove.LocalAI.Api.exe"

    if (-not (Test-Path -LiteralPath $executablePath)) {
        Write-Host "İlk hazırlık yapılıyor..." -ForegroundColor DarkCyan
        & dotnet build $projectPath --configuration Release --nologo
        if ($LASTEXITCODE -ne 0) {
            throw "API projesi derlenemedi."
        }
    }

    if (-not (Test-Path -LiteralPath $executablePath)) {
        throw "Derlenen API dosyası bulunamadı: $executablePath"
    }

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $executablePath
    $startInfo.Arguments = "--environment Development --urls `"$apiBaseUrl`""
    $startInfo.WorkingDirectory = $projectDirectory
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true

    $script:apiProcess = [System.Diagnostics.Process]::Start($startInfo)
    $script:apiOutputTask = $script:apiProcess.StandardOutput.ReadToEndAsync()
    $script:apiErrorTask = $script:apiProcess.StandardError.ReadToEndAsync()

    Wait-DarkloveApi
    Write-Host "Yerel API başlatıldı: $apiBaseUrl" -ForegroundColor Green
}

function Invoke-DarkloveRequest {
    param(
        [Parameter(Mandatory)]
        [string]$Path,
        [ValidateSet("GET", "POST")]
        [string]$Method = "GET",
        [object]$Body
    )

    $parameters = @{
        Uri = "$apiBaseUrl$Path"
        Method = $Method
        TimeoutSec = 120
    }

    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Depth 5 -Compress
        $parameters.ContentType = "application/json; charset=utf-8"
        $parameters.Body = [System.Text.Encoding]::UTF8.GetBytes($json)
    }

    return Invoke-RestMethod @parameters
}

function Get-EmotionLabel {
    param([string]$Emotion)

    $labels = @{
        sadness = "Üzüntü"
        anxiety = "Kaygı"
        hope = "Umut"
        anger = "Öfke"
        neutral = "Nötr"
        mixed = "Karma"
    }

    if ($labels.ContainsKey($Emotion)) {
        return $labels[$Emotion]
    }

    return $Emotion
}

function Show-Analysis {
    param(
        [string]$Text,
        [switch]$FromConversation
    )

    if ([string]::IsNullOrWhiteSpace($Text)) {
        Write-Host ""
        Write-Host "Analiz edilecek sohbet geçmişi veya metin bulunamadı." -ForegroundColor Yellow
        Write-Host ""
        return
    }

    try {
        $result = Invoke-DarkloveRequest `
            -Path "/api/emotion/analyze" `
            -Method POST `
            -Body @{ userText = $Text }

        $emotion = Get-EmotionLabel $result.detectedEmotion
        $confidence = [Math]::Round(([double]$result.confidence * 100), 0)
        $method = if ($result.analysisMethod -eq "open-source-model") {
            "Yerel açık kaynak model"
        }
        else {
            "Açıklanabilir kural tabanlı analiz"
        }

        Write-Host ""
        if ($FromConversation) {
            Write-Host "Sohbet geçmişi üzerinden analiz" -ForegroundColor DarkCyan
        }
        Write-Host "Duygu : $emotion" -ForegroundColor Cyan
        Write-Host "Güven : %$confidence"

        if ($result.riskLevel -eq "high") {
            Write-Host "Risk  : Yüksek" -ForegroundColor Red
        }

        Write-Host "Mesaj : $($result.motivationMessage)" -ForegroundColor Green
        Write-Host ""
    }
    catch {
        Write-Host ""
        Write-Host "Analiz yapılamadı: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
    }
}

function Get-AnalysisTranscript {
    param([string]$ExtraText)

    $lines = New-Object 'System.Collections.Generic.List[string]'

    foreach ($message in $script:chatHistory) {
        $label = if ($message.role -eq "assistant") { "Darklove" } else { "Kullanıcı" }
        $content = ([string]$message.content).Trim()

        if (-not [string]::IsNullOrWhiteSpace($content)) {
            $lines.Add("${label}: $content")
        }
    }

    if (-not [string]::IsNullOrWhiteSpace($ExtraText)) {
        $lines.Add("Kullanıcı: $($ExtraText.Trim())")
    }

    $transcript = ($lines -join "`n").Trim()

    if ($transcript.Length -le 2000) {
        return $transcript
    }

    return $transcript.Substring($transcript.Length - 2000)
}

function Show-Chat {
    param([Parameter(Mandatory)][string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
        return
    }

    try {
        $result = Invoke-DarkloveRequest `
            -Path "/api/chat" `
            -Method POST `
            -Body @{
                userText = $Text
                history = @($script:chatHistory.ToArray())
            }

        $message = [string]$result.assistantMessage

        Write-Host ""
        Write-Host "Darklove: " -NoNewline -ForegroundColor Magenta
        if ($result.needsSupportWarning) {
            Write-Host $message -ForegroundColor Yellow
        }
        else {
            Write-Host $message
        }
        Write-Host ""

        $script:chatHistory.Add([pscustomobject]@{
            role = "user"
            content = $Text
        })
        $script:chatHistory.Add([pscustomobject]@{
            role = "assistant"
            content = $message
        })

        while ($script:chatHistory.Count -gt 12) {
            $script:chatHistory.RemoveAt(0)
        }
    }
    catch {
        Write-Host ""
        Write-Host "Sohbet yanıtı alınamadı: $($_.Exception.Message)" -ForegroundColor Red
        Write-Host ""
    }
}

function Show-Models {
    try {
        $catalog = Invoke-DarkloveRequest -Path "/api/models/"
        Write-Host ""
        Write-Host "Bulunan modeller" -ForegroundColor Cyan

        if (@($catalog.models).Count -eq 0) {
            Write-Host "Henüz model bulunamadı. Web arayüzünden model indirebilirsiniz."
        }
        else {
            foreach ($model in $catalog.models) {
                $selected = if ($model.key -eq $catalog.selectedModel) { " [seçili]" } else { "" }
                $loaded = if ($model.isLoaded) { "yüklü" } else { "hazır" }
                Write-Host "- $($model.displayName)$selected"
                Write-Host "  $($model.provider) | $loaded | $($model.key)"
            }
        }

        Write-Host ""
    }
    catch {
        Write-Host "Model listesi alınamadı: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Show-Status {
    try {
        $status = Invoke-DarkloveRequest -Path "/api/model/status"
        Write-Host ""
        Write-Host "Sağlayıcı : $($status.provider)" -ForegroundColor Cyan
        Write-Host "Runtime   : $($status.runtimeAvailable)"
        Write-Host "Model     : $($status.model)"
        Write-Host "Hazır     : $($status.modelAvailable)"
        Write-Host "Durum     : $($status.status)"
        Write-Host ""
    }
    catch {
        Write-Host "Durum alınamadı: $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Show-Help {
    Write-Host ""
    Write-Host "Komutlar" -ForegroundColor Cyan
    Write-Host "  analiz          O ana kadarki sohbetin tamamını duygu açısından analiz eder."
    Write-Host "  analiz <metin>  Sohbet geçmişiyle birlikte ek metni analiz eder."
    Write-Host "  modeller  Bilgisayardaki yerel modelleri gösterir."
    Write-Host "  durum     Model sağlayıcısının durumunu gösterir."
    Write-Host "  şartlar   Kullanım şartları ve güvenlik notlarını gösterir."
    Write-Host "  yardım    Bu listeyi gösterir."
    Write-Host "  çıkış     Programı kapatır."
    Write-Host ""
    Write-Host "Normal kullanımda sadece mesajını yazman yeterli; Darklove sohbet eder." `
        -ForegroundColor DarkGray
    Write-Host ""
}

function Show-Terms {
    Write-Host ""
    Write-Host "Kullanım şartları" -ForegroundColor Cyan
    Write-Host "- Bu uygulama tıbbi teşhis veya profesyonel psikolojik destek yerine geçmez."
    Write-Host "- Model güven değerleri klinik olasılık değildir."
    Write-Host "- Kriz veya kendine zarar verme riski varsa güvendiğin birine ulaş ve acil durumda 112'yi ara."
    Write-Host "- Sohbet yerel API ve yerel model üzerinden çalışır; bu MVP kullanıcı metnini veritabanına kaydetmez."
    Write-Host "- Duygu analizi istiyorsan açıkça 'analiz' veya 'analiz <metin>' komutunu kullan."
    Write-Host ""
}

function Show-DarkloveBanner {
    $width = 76
    $border = "═" * $width
    $logo = @(
        "   ____  ___    ____  __ __ __    ____ _    ________",
        "  / __ \/   |  / __ \/ //_// /   / __ \ |  / / ____/",
        " / / / / /| | / /_/ / ,<  / /   / / / / | / / __/",
        "/ /_/ / ___ |/ _, _/ /| |/ /___/ /_/ /| |/ / /___",
        "\_____/_/  |_/_/ |_/_/ |_/_____/\____/ |___/_____/"
    )
    $tag = " LOCAL AI | PRIVATE | SAFE "
    $leftRule = "═" * [Math]::Floor(($width - $tag.Length) / 2)
    $rightRule = "═" * ($width - $tag.Length - $leftRule.Length)

    function Format-BannerLine {
        param([string]$Text)

        if ($Text.Length -ge $width) {
            return $Text.Substring(0, $width)
        }

        $leftPadding = [Math]::Floor(($width - $Text.Length) / 2)
        $rightPadding = $width - $Text.Length - $leftPadding

        return "$(" " * $leftPadding)$Text$(" " * $rightPadding)"
    }

    Write-Host "╔$border╗" -ForegroundColor DarkMagenta
    Write-Host "║$(" " * $width)║" -ForegroundColor DarkMagenta
    foreach ($line in $logo) {
        Write-Host "║$(Format-BannerLine $line)║" -ForegroundColor Magenta
    }
    Write-Host "║$(" " * $width)║" -ForegroundColor DarkMagenta
    Write-Host "╠$leftRule$tag$rightRule╣" -ForegroundColor DarkMagenta
    Write-Host "║$("Yerel ve güvenli yapay zekâ".PadLeft(51).PadRight($width))║" -ForegroundColor DarkCyan
    Write-Host "╚$border╝" -ForegroundColor DarkMagenta
    Write-Host ""
}

try {
    if ($Check) {
        if ($null -eq (Get-Command dotnet -ErrorAction SilentlyContinue)) {
            throw ".NET SDK bulunamadı."
        }

        Write-Host "Darklove CMD istemcisi hazır."
        exit 0
    }

    Clear-Host
    Show-DarkloveBanner
    Start-DarkloveApi

    if (-not [string]::IsNullOrWhiteSpace($Once)) {
        Show-Chat $Once
        exit 0
    }

    Write-Host "Mesajınızı yazın. Sohbet analizi için 'analiz', komutlar için 'yardım' yazın."
    Write-Host ""

    while ($true) {
        $inputText = Read-Host "Siz"
        $trimmedInput = $inputText.Trim()
        $normalizedCommand = $trimmedInput.ToLowerInvariant()

        switch ($normalizedCommand) {
            { $_ -in @("çıkış", "cikis", "exit", "q") } { return }
            { $_ -in @("yardım", "yardim", "help") } { Show-Help; continue }
            { $_ -in @("şartlar", "sartlar", "terms", "kullanım şartları", "kullanim sartlari") } {
                Show-Terms
                continue
            }
            "modeller" { Show-Models; continue }
            "durum" { Show-Status; continue }
            "analiz" {
                Show-Analysis (Get-AnalysisTranscript "") -FromConversation
                continue
            }
            "duygu" {
                Show-Analysis (Get-AnalysisTranscript "") -FromConversation
                continue
            }
            { $_.StartsWith("analiz ") } {
                Show-Analysis (Get-AnalysisTranscript $trimmedInput.Substring(7)) -FromConversation
                continue
            }
            { $_.StartsWith("duygu ") } {
                Show-Analysis (Get-AnalysisTranscript $trimmedInput.Substring(6)) -FromConversation
                continue
            }
            default { Show-Chat $inputText }
        }
    }
}
finally {
    if ($null -ne $script:apiProcess -and -not $script:apiProcess.HasExited) {
        Write-Host "Yerel API kapatılıyor..." -ForegroundColor DarkGray
        Stop-Process -Id $script:apiProcess.Id -Force -ErrorAction SilentlyContinue
        $null = $script:apiProcess.WaitForExit(5000)
    }
}

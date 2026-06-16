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
    param([Parameter(Mandatory)][string]$Text)

    if ([string]::IsNullOrWhiteSpace($Text)) {
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
        Write-Host "Duygu : $emotion" -ForegroundColor Cyan
        if ($result.analysisMethod -eq "open-source-model") {
            Write-Host "Güven : %$confidence (model çıktısı; klinik olasılık değildir)"
        }
        else {
            Write-Host "Güven : %$confidence (sezgisel değer)"
        }
        Write-Host "Yöntem: $method"

        if (-not [string]::IsNullOrWhiteSpace($result.model)) {
            Write-Host "Model : $($result.model)"
        }

        if ($result.analysisMethod -eq "rule-based-fallback") {
            Write-Host "Not   : Yerel model yanıt vermedi; güvenli yedek analiz kullanıldı." `
                -ForegroundColor Yellow
        }

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
    Write-Host "  modeller  Bilgisayardaki yerel modelleri gösterir."
    Write-Host "  durum     Model sağlayıcısının durumunu gösterir."
    Write-Host "  yardım    Bu listeyi gösterir."
    Write-Host "  çıkış     Programı kapatır."
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

    Write-Host "╔$border╗" -ForegroundColor DarkMagenta
    Write-Host "║$(" " * $width)║" -ForegroundColor DarkMagenta
    foreach ($line in $logo) {
        Write-Host "║$($line.PadRight($width))║" -ForegroundColor Magenta
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
        Show-Analysis $Once
        exit 0
    }

    Write-Host "Metninizi yazın. Komutlar için 'yardım', çıkmak için 'çıkış' yazın."
    Write-Host ""

    while ($true) {
        $inputText = Read-Host "Siz"
        switch ($inputText.Trim().ToLowerInvariant()) {
            { $_ -in @("çıkış", "cikis", "exit", "q") } { return }
            { $_ -in @("yardım", "yardim", "help") } { Show-Help; continue }
            "modeller" { Show-Models; continue }
            "durum" { Show-Status; continue }
            default { Show-Analysis $inputText }
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

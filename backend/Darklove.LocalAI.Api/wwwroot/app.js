const emotionLabels = {
  sadness: "Üzüntü",
  anxiety: "Kaygı",
  hope: "Umut",
  anger: "Öfke",
  neutral: "Nötr",
  mixed: "Karma"
};

const methodLabels = {
  "open-source-model": "Yerel açık model",
  "rule-based": "Kural tabanlı",
  "rule-based-fallback": "Güvenli fallback",
  "rule-based-safety": "Kriz güvenlik kuralı"
};

const fallbackLabels = {
  "model-unavailable": "Ollama çalışma zamanına ulaşılamadı.",
  "model-not-found": "Seçilen model Ollama içinde bulunamadı.",
  "model-timeout": "Yerel model zaman aşımına uğradı.",
  "invalid-model-response": "Model yanıtı beklenen JSON sözleşmesine uymadı.",
  "model-http-error": "Yerel model çalışma zamanı hata yanıtı döndürdü.",
  "model-error": "Model analizinde beklenmeyen bir hata oluştu."
};

const statusLabels = {
  ready: "Yerel model hazır",
  "model-not-loaded": "Model seçilmeyi bekliyor",
  "model-not-found": "Model indirilmemiş",
  "runtime-unavailable": "Fallback etkin",
  "runtime-error": "Model servisi hatalı",
  disabled: "Model kapalı"
};

const form = document.querySelector("#analysis-form");
const userText = document.querySelector("#user-text");
const characterCount = document.querySelector("#character-count");
const analyzeButton = document.querySelector("#analyze-button");
const clearButton = document.querySelector("#clear-button");
const modelStatus = document.querySelector("#model-status");
const modelStatusText = document.querySelector("#model-status-text");
const emptyState = document.querySelector("#empty-state");
const loadingState = document.querySelector("#loading-state");
const resultContent = document.querySelector("#result-content");
const errorState = document.querySelector("#error-state");
const errorMessage = document.querySelector("#error-message");
const emotionName = document.querySelector("#emotion-name");
const confidenceValue = document.querySelector("#confidence-value");
const supportMessage = document.querySelector("#support-message");
const analysisMethod = document.querySelector("#analysis-method");
const modelName = document.querySelector("#model-name");
const fallbackNote = document.querySelector("#fallback-note");
const scoreSource = document.querySelector("#score-source");
const scoreList = document.querySelector("#score-list");
const keywordSection = document.querySelector("#keyword-section");
const keywordList = document.querySelector("#keyword-list");
const refreshModelsButton = document.querySelector("#refresh-models-button");
const modelProviderLabel = document.querySelector("#model-provider-label");
const modelCount = document.querySelector("#model-count");
const modelList = document.querySelector("#model-list");
const modelDownloadForm = document.querySelector("#model-download-form");
const downloadModelId = document.querySelector("#download-model-id");
const downloadQuantization = document.querySelector("#download-quantization");
const downloadModelButton = document.querySelector("#download-model-button");
const downloadProgress = document.querySelector("#download-progress");
const downloadStatusText = document.querySelector("#download-status-text");
const downloadPercent = document.querySelector("#download-percent");
const downloadTrack = downloadProgress.querySelector(".download-track");
const downloadProgressFill = document.querySelector("#download-progress-fill");
const downloadDetail = document.querySelector("#download-detail");
const serialSupportNote = document.querySelector("#serial-support-note");
const ecgStatusPill = document.querySelector("#ecg-status-pill");
const ecgBaudRate = document.querySelector("#ecg-baud-rate");
const connectEcgButton = document.querySelector("#connect-ecg-button");
const disconnectEcgButton = document.querySelector("#disconnect-ecg-button");
const ecgCanvas = document.querySelector("#ecg-canvas");
const ecgHeartRate = document.querySelector("#ecg-heart-rate");
const ecgRhythmLabel = document.querySelector("#ecg-rhythm-label");
const ecgSignalQuality = document.querySelector("#ecg-signal-quality");
const ecgSampleCount = document.querySelector("#ecg-sample-count");
const ecgStatusText = document.querySelector("#ecg-status-text");
const useEcgContext = document.querySelector("#use-ecg-context");
const chatForm = document.querySelector("#chat-form");
const chatText = document.querySelector("#chat-text");
const chatLog = document.querySelector("#chat-log");
const sendChatButton = document.querySelector("#send-chat-button");
const clearChatButton = document.querySelector("#clear-chat-button");

let activeDownloadTimer = null;
let chatHistory = [];
let ecgPort = null;
let ecgReader = null;
let ecgReading = false;
let ecgLineBuffer = "";
let ecgRenderFrame = null;

const ecgState = {
  samples: [],
  peaks: [],
  bpm: null,
  rhythmCode: "waiting",
  rhythmLabel: "Ölçüm bekleniyor",
  signalQuality: "waiting",
  signalLabel: "Bekleniyor",
  leadOff: false,
  averageValue: null,
  lastAboveThreshold: false,
  lastPeakTime: 0
};

function setVisibleState(state) {
  emptyState.hidden = state !== "empty";
  loadingState.hidden = state !== "loading";
  resultContent.hidden = state !== "result";
  errorState.hidden = state !== "error";
}

function updateCharacterCount() {
  characterCount.textContent = `${userText.value.length} / 2000`;
}

function updateModelStatus(status) {
  modelStatus.classList.remove("status-loading", "status-ready", "status-fallback");
  modelStatus.classList.add(status.status === "ready" ? "status-ready" : "status-fallback");
  modelStatusText.textContent = statusLabels[status.status] ?? "Model durumu bilinmiyor";
  modelStatus.title = `${status.provider}: ${status.model}`;
}

function formatBytes(value) {
  if (!Number.isFinite(value) || value <= 0) {
    return "Boyut bilinmiyor";
  }

  const units = ["B", "KB", "MB", "GB", "TB"];
  const unitIndex = Math.min(
    Math.floor(Math.log(value) / Math.log(1024)),
    units.length - 1);
  const amount = value / (1024 ** unitIndex);

  return `${amount >= 10 || unitIndex === 0 ? amount.toFixed(0) : amount.toFixed(1)} ${units[unitIndex]}`;
}

function setEcgStatus(kind, label, detail) {
  ecgStatusPill.className = `ecg-status-pill ${kind}`;
  ecgStatusPill.textContent = label;

  if (detail) {
    ecgStatusText.textContent = detail;
  }
}

function resetEcgState() {
  ecgState.samples = [];
  ecgState.peaks = [];
  ecgState.bpm = null;
  ecgState.rhythmCode = "waiting";
  ecgState.rhythmLabel = "Ölçüm bekleniyor";
  ecgState.signalQuality = "waiting";
  ecgState.signalLabel = "Bekleniyor";
  ecgState.leadOff = false;
  ecgState.averageValue = null;
  ecgState.lastAboveThreshold = false;
  ecgState.lastPeakTime = 0;
  updateEcgUi();
  scheduleEcgRender();
}

function initializeEcgPanel() {
  drawEcgCanvas();

  if (!("serial" in navigator)) {
    serialSupportNote.textContent =
      "Bu tarayıcı Web Serial API desteklemiyor. Chrome veya Edge ile localhost üzerinden aç.";
    connectEcgButton.disabled = true;
    setEcgStatus("error", "Destek yok", "Tarayıcı seri port erişimi sunmuyor.");
    return;
  }

  serialSupportNote.textContent =
    "Web Serial destekleniyor. Arduino Uno genelde 9600 baud ile veri gönderir.";
  connectEcgButton.disabled = false;
  setEcgStatus("waiting", "Beklemede", "Bağlan düğmesine basınca Arduino portunu seçebilirsin.");
}

async function connectEcg() {
  if (!("serial" in navigator) || ecgReading) {
    return;
  }

  connectEcgButton.disabled = true;
  setEcgStatus("waiting", "Bağlanıyor", "Tarayıcı izin penceresinde Arduino portunu seç.");

  try {
    ecgPort = await navigator.serial.requestPort({
      filters: [
        { usbVendorId: 0x2341 },
        { usbVendorId: 0x2a03 },
        { usbVendorId: 0x1a86 }
      ]
    });

    await ecgPort.open({
      baudRate: Number(ecgBaudRate.value),
      bufferSize: 255
    });

    resetEcgState();
    ecgReading = true;
    ecgBaudRate.disabled = true;
    disconnectEcgButton.disabled = false;
    setEcgStatus("connected", "Bağlı", "Seri veri bekleniyor. Elektrotlar bağlı değilse ! uyarısı normaldir.");
    readEcgStream();
  } catch (error) {
    connectEcgButton.disabled = false;
    ecgBaudRate.disabled = false;

    const cancelled = error instanceof DOMException && error.name === "NotFoundError";
    setEcgStatus(
      cancelled ? "waiting" : "error",
      cancelled ? "Beklemede" : "Hata",
      cancelled
        ? "Port seçimi iptal edildi."
        : "Arduino bağlantısı açılamadı. Seri monitör açıksa kapatıp tekrar dene.");
  }
}

async function disconnectEcg(showMessage = true) {
  ecgReading = false;

  try {
    if (ecgReader) {
      await ecgReader.cancel();
    }
  } catch {
    // Port kapanırken reader zaten iptal edilmiş olabilir.
  }

  try {
    if (ecgPort) {
      await ecgPort.close();
    }
  } catch {
    // Bağlantı zaten kapanmış olabilir.
  }

  ecgReader = null;
  ecgPort = null;
  ecgLineBuffer = "";
  connectEcgButton.disabled = false;
  disconnectEcgButton.disabled = true;
  ecgBaudRate.disabled = false;

  if (showMessage) {
    setEcgStatus("waiting", "Beklemede", "EKG bağlantısı kapatıldı.");
  }
}

async function readEcgStream() {
  const decoder = new TextDecoder();

  try {
    while (ecgPort?.readable && ecgReading) {
      ecgReader = ecgPort.readable.getReader();

      try {
        while (ecgReading) {
          const { value, done } = await ecgReader.read();

          if (done) {
            break;
          }

          if (value) {
            processEcgText(decoder.decode(value, { stream: true }));
          }
        }
      } finally {
        ecgReader.releaseLock();
        ecgReader = null;
      }
    }
  } catch {
    if (ecgReading) {
      setEcgStatus("error", "Hata", "Seri bağlantı kesildi veya veri okunamadı.");
    }
  } finally {
    if (ecgReading) {
      await disconnectEcg(false);
    }
  }
}

function processEcgText(text) {
  ecgLineBuffer += text.replaceAll("\0", "");

  const lines = ecgLineBuffer.split(/\r?\n/);
  ecgLineBuffer = lines.pop() ?? "";

  lines.forEach(handleEcgLine);

  if (ecgLineBuffer.length > 80) {
    handleEcgLine(ecgLineBuffer);
    ecgLineBuffer = "";
  }
}

function handleEcgLine(line) {
  const normalized = line.trim();

  if (!normalized) {
    return;
  }

  if (normalized.includes("!") || /lead|lo\+|lo-/i.test(normalized)) {
    ecgState.leadOff = true;
    ecgState.bpm = null;
    ecgState.rhythmCode = "no-contact";
    ecgState.rhythmLabel = "Elektrot bekleniyor";
    ecgState.signalQuality = "no-contact";
    ecgState.signalLabel = "Temas yok";
    setEcgStatus("warning", "Elektrot yok", "AD8232 elektrot teması algılamıyor; modül insana bağlı değilse bu normal.");
    updateEcgUi();
    scheduleEcgRender();
    return;
  }

  const match = normalized.match(/-?\d+(?:[.,]\d+)?/);
  if (!match) {
    return;
  }

  const value = Number(match[0].replace(",", "."));
  if (!Number.isFinite(value)) {
    return;
  }

  ecgState.leadOff = false;
  addEcgSample(value);
}

function addEcgSample(value) {
  const now = performance.now();
  ecgState.samples.push({ value, time: now });
  ecgState.samples = ecgState.samples
    .filter((sample) => now - sample.time <= 15000)
    .slice(-900);

  const recent = ecgState.samples.filter((sample) => now - sample.time <= 10000);
  const values = recent.map((sample) => sample.value);
  const minimum = Math.min(...values);
  const maximum = Math.max(...values);
  const range = maximum - minimum;
  const average = values.reduce((sum, item) => sum + item, 0) / Math.max(1, values.length);
  ecgState.averageValue = average;

  if (recent.length < 20) {
    ecgState.signalQuality = "waiting";
    ecgState.signalLabel = "Örnek bekleniyor";
  } else if (range < 8) {
    ecgState.signalQuality = "weak";
    ecgState.signalLabel = "Zayıf";
  } else {
    ecgState.signalQuality = "good";
    ecgState.signalLabel = "İyi";
  }

  const threshold = minimum + range * 0.62;
  const aboveThreshold = range >= 12 && value >= threshold;

  if (aboveThreshold && !ecgState.lastAboveThreshold && now - ecgState.lastPeakTime > 320) {
    ecgState.peaks.push(now);
    ecgState.lastPeakTime = now;
  }

  ecgState.lastAboveThreshold = aboveThreshold;
  ecgState.peaks = ecgState.peaks.filter((peak) => now - peak <= 12000);
  updateHeartRateFromPeaks();
  updateEcgUi();
  scheduleEcgRender();
}

function updateHeartRateFromPeaks() {
  if (ecgState.peaks.length < 3) {
    ecgState.bpm = null;
    ecgState.rhythmCode = "waiting";
    ecgState.rhythmLabel = "Ölçüm bekleniyor";
    return;
  }

  const intervals = [];
  for (let index = 1; index < ecgState.peaks.length; index += 1) {
    intervals.push(ecgState.peaks[index] - ecgState.peaks[index - 1]);
  }

  const usableIntervals = intervals
    .filter((interval) => interval >= 300 && interval <= 1800)
    .slice(-8);

  if (usableIntervals.length < 2) {
    ecgState.bpm = null;
    ecgState.rhythmCode = "waiting";
    ecgState.rhythmLabel = "Ölçüm bekleniyor";
    return;
  }

  const averageInterval =
    usableIntervals.reduce((sum, interval) => sum + interval, 0) / usableIntervals.length;
  const bpm = Math.round(60000 / averageInterval);

  if (bpm < 35 || bpm > 220) {
    ecgState.bpm = null;
    ecgState.rhythmCode = "waiting";
    ecgState.rhythmLabel = "Ölçüm bekleniyor";
    return;
  }

  const variance = usableIntervals
    .reduce((sum, interval) => sum + ((interval - averageInterval) ** 2), 0) /
    usableIntervals.length;
  const variation = Math.sqrt(variance) / averageInterval;

  ecgState.bpm = bpm;

  if (variation > 0.18) {
    ecgState.rhythmCode = "irregular";
    ecgState.rhythmLabel = "Düzensiz olabilir";
  } else if (bpm < 55) {
    ecgState.rhythmCode = "low";
    ecgState.rhythmLabel = "Düşük ritim";
  } else if (bpm > 105) {
    ecgState.rhythmCode = "elevated";
    ecgState.rhythmLabel = "Yüksek ritim";
  } else {
    ecgState.rhythmCode = "stable";
    ecgState.rhythmLabel = "Dengeli";
  }

  setEcgStatus("connected", "Canlı", "EKG verisi okunuyor. BPM yaklaşık değerdir.");
}

function updateEcgUi() {
  ecgHeartRate.textContent = ecgState.bpm ? String(ecgState.bpm) : "--";
  ecgRhythmLabel.textContent = ecgState.rhythmLabel;
  ecgSignalQuality.textContent = ecgState.signalLabel;
  ecgSampleCount.textContent = String(ecgState.samples.length);
}

function scheduleEcgRender() {
  if (ecgRenderFrame) {
    return;
  }

  ecgRenderFrame = window.requestAnimationFrame(() => {
    ecgRenderFrame = null;
    drawEcgCanvas();
  });
}

function drawEcgCanvas() {
  const context = ecgCanvas.getContext("2d");
  const rect = ecgCanvas.getBoundingClientRect();
  const ratio = window.devicePixelRatio || 1;
  const width = Math.max(320, Math.round(rect.width || ecgCanvas.width));
  const height = Math.max(160, Math.round(rect.height || ecgCanvas.height));

  ecgCanvas.width = Math.round(width * ratio);
  ecgCanvas.height = Math.round(height * ratio);
  context.setTransform(ratio, 0, 0, ratio, 0, 0);
  context.clearRect(0, 0, width, height);
  context.fillStyle = "#06090d";
  context.fillRect(0, 0, width, height);

  context.strokeStyle = "rgba(103, 232, 179, 0.08)";
  context.lineWidth = 1;
  for (let x = 0; x < width; x += 32) {
    context.beginPath();
    context.moveTo(x, 0);
    context.lineTo(x, height);
    context.stroke();
  }
  for (let y = 0; y < height; y += 28) {
    context.beginPath();
    context.moveTo(0, y);
    context.lineTo(width, y);
    context.stroke();
  }

  const samples = ecgState.samples.slice(-420);
  if (samples.length < 2) {
    context.strokeStyle = "rgba(103, 232, 179, 0.35)";
    context.beginPath();
    context.moveTo(0, height / 2);
    context.lineTo(width, height / 2);
    context.stroke();
    return;
  }

  const values = samples.map((sample) => sample.value);
  const minimum = Math.min(...values);
  const maximum = Math.max(...values);
  const range = Math.max(1, maximum - minimum);

  context.strokeStyle = "#67e8b3";
  context.lineWidth = 2;
  context.beginPath();

  samples.forEach((sample, index) => {
    const x = (index / Math.max(1, samples.length - 1)) * width;
    const normalized = (sample.value - minimum) / range;
    const y = height - 18 - normalized * (height - 36);

    if (index === 0) {
      context.moveTo(x, y);
    } else {
      context.lineTo(x, y);
    }
  });

  context.stroke();
}

function buildHeartContext() {
  if (!useEcgContext.checked) {
    return null;
  }

  const recentSamples = ecgState.samples.filter(
    (sample) => performance.now() - sample.time <= 10000);

  if (recentSamples.length === 0 && !ecgState.leadOff) {
    return null;
  }

  return {
    bpm: ecgState.bpm,
    rhythm: ecgState.rhythmLabel,
    signalQuality: ecgState.signalLabel,
    leadOff: ecgState.leadOff,
    sampleCount: recentSamples.length,
    averageValue: ecgState.averageValue,
    measuredAt: new Date().toISOString()
  };
}

function appendChatMessage(role, text) {
  const message = document.createElement("div");
  message.className = `chat-message ${role}`;
  message.textContent = text;
  chatLog.append(message);
  chatLog.scrollTop = chatLog.scrollHeight;
  return message;
}

async function sendChatMessage(event) {
  event.preventDefault();
  const text = chatText.value.trim();

  if (!text) {
    chatText.focus();
    return;
  }

  appendChatMessage("user", text);
  chatText.value = "";
  sendChatButton.disabled = true;

  const pendingMessage = appendChatMessage("assistant pending", "Yanıt hazırlanıyor...");

  try {
    const response = await fetch("/api/chat", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json"
      },
      body: JSON.stringify({
        userText: text,
        history: chatHistory.slice(-12),
        heartContext: buildHeartContext()
      })
    });
    const body = await response.json();

    if (!response.ok) {
      throw new Error(extractError(body));
    }

    pendingMessage.classList.remove("pending");
    pendingMessage.textContent = body.assistantMessage;
    pendingMessage.classList.toggle("crisis", body.needsSupportWarning);

    chatHistory.push(
      { role: "user", content: text },
      { role: "assistant", content: body.assistantMessage });
    refreshModelStatus();
  } catch (error) {
    pendingMessage.classList.remove("pending");
    pendingMessage.classList.add("error");
    pendingMessage.textContent = error instanceof Error
      ? error.message
      : "Sohbet isteği tamamlanamadı.";
  } finally {
    sendChatButton.disabled = false;
    chatText.focus();
  }
}

function clearChat() {
  chatHistory = [];
  chatLog.replaceChildren();
  appendChatMessage(
    "assistant",
    "Sohbet temizlendi. İstersen Arduino bağlantısını açık bırakıp konuşmaya devam edebiliriz.");
  chatText.focus();
}

function createModelCard(model, selectedModel) {
  const card = document.createElement("article");
  card.className = "local-model-card";
  const isSelected = model.key === selectedModel;

  if (isSelected) {
    card.classList.add("selected-model");
  }

  const heading = document.createElement("div");
  heading.className = "local-model-heading";

  const titleGroup = document.createElement("div");
  const title = document.createElement("h4");
  title.textContent = model.displayName;
  const key = document.createElement("code");
  key.textContent = model.key;
  titleGroup.append(title, key);

  const status = document.createElement("span");
  status.className = model.isLoaded ? "model-state loaded" : "model-state";
  status.textContent = model.isLoaded
    ? isSelected ? "Aktif" : "Bellekte"
    : isSelected ? "Seçili" : "Yüklü";
  heading.append(titleGroup, status);

  const meta = document.createElement("div");
  meta.className = "local-model-meta";
  [formatBytes(model.sizeBytes), model.parameters, model.quantization]
    .filter(Boolean)
    .forEach((value) => {
      const item = document.createElement("span");
      item.textContent = value;
      meta.append(item);
    });

  if (model.supportsVision) {
    const vision = document.createElement("span");
    vision.textContent = "Görsel destekli";
    meta.append(vision);
  }

  const action = document.createElement("button");
  action.type = "button";
  action.className = "model-select-button";
  action.textContent = model.isLoaded && isSelected ? "Kullanılıyor" : "Yükle ve kullan";
  action.disabled = model.isLoaded && isSelected;
  action.addEventListener("click", () => selectModel(model.key, action));

  card.append(heading, meta, action);
  return card;
}

function renderModelCatalog(catalog) {
  modelList.replaceChildren();
  modelCount.textContent = `${catalog.models.length} model`;
  modelProviderLabel.textContent = catalog.runtimeAvailable
    ? `${catalog.provider} çalışma zamanı hazır.`
    : `${catalog.provider} çalışma zamanına ulaşılamıyor.`;

  updateModelStatus({
    status: catalog.status,
    provider: catalog.provider,
    model: catalog.selectedModel
  });

  if (catalog.models.length === 0) {
    const empty = document.createElement("div");
    empty.className = "model-list-empty";
    empty.textContent = catalog.runtimeAvailable
      ? "Henüz çalışabilir bir dil modeli bulunamadı."
      : "Yerel çalışma zamanı başlatılamadı. LM Studio veya Ollama kurulumunu kontrol et.";
    modelList.append(empty);
    return;
  }

  catalog.models.forEach((model) => {
    modelList.append(createModelCard(model, catalog.selectedModel));
  });
}

async function refreshModelCatalog() {
  refreshModelsButton.disabled = true;

  try {
    const response = await fetch("/api/models/", {
      headers: { Accept: "application/json" }
    });
    const body = await response.json();

    if (!response.ok) {
      throw new Error(extractError(body));
    }

    renderModelCatalog(body);
  } catch (error) {
    modelProviderLabel.textContent = "Model kataloğu alınamadı.";
    modelList.replaceChildren();
    const message = document.createElement("div");
    message.className = "model-list-empty";
    message.textContent = error instanceof Error
      ? error.message
      : "Model kataloğu alınamadı.";
    modelList.append(message);
  } finally {
    refreshModelsButton.disabled = false;
  }
}

async function selectModel(model, button) {
  const originalLabel = button.textContent;
  button.disabled = true;
  button.textContent = "Model yükleniyor...";

  try {
    const response = await fetch("/api/models/selected", {
      method: "PUT",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json"
      },
      body: JSON.stringify({ model })
    });
    const body = await response.json();

    if (!response.ok) {
      throw new Error(extractError(body));
    }

    await refreshModelCatalog();
  } catch (error) {
    button.disabled = false;
    button.textContent = originalLabel;
    modelProviderLabel.textContent = error instanceof Error
      ? error.message
      : "Model yüklenemedi.";
  }
}

function updateDownloadProgress(status) {
  downloadProgress.hidden = false;

  const total = status.totalSizeBytes ?? 0;
  const downloaded = status.downloadedBytes ?? 0;
  const percentage = total > 0
    ? Math.max(0, Math.min(100, Math.round((downloaded / total) * 100)))
    : status.status === "completed" || status.status === "already_downloaded" ? 100 : 0;

  downloadPercent.textContent = `${percentage}%`;
  downloadProgressFill.style.width = `${percentage}%`;
  downloadTrack.setAttribute("aria-valuenow", String(percentage));

  const statusMessages = {
    downloading: "Model indiriliyor.",
    paused: "İndirme duraklatıldı.",
    completed: "Model indirme tamamlandı.",
    already_downloaded: "Model zaten bilgisayarda yüklü.",
    failed: "Model indirilemedi."
  };
  downloadStatusText.textContent = statusMessages[status.status] ?? "İndirme hazırlanıyor.";

  const details = [];
  if (total > 0) {
    details.push(`${formatBytes(downloaded)} / ${formatBytes(total)}`);
  }
  if (status.bytesPerSecond) {
    details.push(`${formatBytes(status.bytesPerSecond)}/sn`);
  }
  if (status.error) {
    details.push(status.error);
  }
  downloadDetail.textContent = details.join(" · ");

  return status.status === "completed" ||
    status.status === "already_downloaded" ||
    status.status === "failed";
}

async function pollDownload(jobId) {
  try {
    const response = await fetch(`/api/models/downloads/${encodeURIComponent(jobId)}`, {
      headers: { Accept: "application/json" }
    });
    const body = await response.json();

    if (!response.ok) {
      throw new Error(extractError(body));
    }

    if (updateDownloadProgress(body)) {
      activeDownloadTimer = null;
      downloadModelButton.disabled = false;
      await refreshModelCatalog();
      return;
    }

    activeDownloadTimer = window.setTimeout(() => pollDownload(jobId), 1000);
  } catch (error) {
    activeDownloadTimer = null;
    downloadModelButton.disabled = false;
    downloadStatusText.textContent = "İndirme durumu alınamadı.";
    downloadDetail.textContent = error instanceof Error ? error.message : "";
  }
}

async function startModelDownload(event) {
  event.preventDefault();
  const model = downloadModelId.value.trim();

  if (!model) {
    downloadModelId.focus();
    return;
  }

  if (activeDownloadTimer) {
    window.clearTimeout(activeDownloadTimer);
    activeDownloadTimer = null;
  }

  downloadModelButton.disabled = true;
  updateDownloadProgress({ status: "preparing" });

  try {
    const response = await fetch("/api/models/downloads", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json"
      },
      body: JSON.stringify({
        model,
        quantization: downloadQuantization.value || null
      })
    });
    const body = await response.json();

    if (!response.ok) {
      throw new Error(extractError(body));
    }

    const finished = updateDownloadProgress(body);
    if (body.jobId && !finished) {
      activeDownloadTimer = window.setTimeout(() => pollDownload(body.jobId), 1000);
    } else {
      downloadModelButton.disabled = false;
      await refreshModelCatalog();
    }
  } catch (error) {
    downloadModelButton.disabled = false;
    downloadStatusText.textContent = "Model indirilemedi.";
    downloadDetail.textContent = error instanceof Error
      ? error.message
      : "İndirme isteği tamamlanamadı.";
  }
}

async function refreshModelStatus() {
  try {
    const response = await fetch("/api/model/status", {
      headers: { Accept: "application/json" }
    });

    if (!response.ok) {
      throw new Error("Model durumu alınamadı.");
    }

    updateModelStatus(await response.json());
  } catch {
    updateModelStatus({
      status: "runtime-unavailable",
      provider: "ollama",
      model: "bilinmiyor"
    });
  }
}

function renderScores(result) {
  const modelScores = result.modelScores;
  const source = modelScores ?? result.scores;
  const isModelScore = Boolean(modelScores);
  const maximumRuleScore = Math.max(1, ...Object.values(result.scores));

  scoreSource.textContent = isModelScore ? "Model skorları" : "Kural skorları";
  scoreList.replaceChildren();

  Object.entries(source).forEach(([emotion, score]) => {
    const normalizedScore = isModelScore ? score : score / maximumRuleScore;
    const row = document.createElement("div");
    row.className = "score-row";

    const label = document.createElement("span");
    label.className = "score-label";
    label.textContent = emotionLabels[emotion] ?? emotion;

    const track = document.createElement("span");
    track.className = "score-track";
    const fill = document.createElement("span");
    fill.className = "score-fill";
    fill.style.width = `${Math.max(0, Math.min(100, normalizedScore * 100))}%`;
    track.append(fill);

    const value = document.createElement("span");
    value.className = "score-value";
    value.textContent = isModelScore ? `%${Math.round(score * 100)}` : String(score);

    row.append(label, track, value);
    scoreList.append(row);
  });
}

function renderKeywords(matchedKeywords) {
  const keywords = Object.values(matchedKeywords ?? {}).flat();
  keywordList.replaceChildren();
  keywordSection.hidden = keywords.length === 0;

  keywords.forEach((keyword) => {
    const item = document.createElement("span");
    item.textContent = keyword;
    keywordList.append(item);
  });
}

function renderResult(result) {
  emotionName.textContent = emotionLabels[result.detectedEmotion] ?? result.detectedEmotion;
  confidenceValue.textContent = `%${Math.round(result.confidence * 100)}`;
  supportMessage.textContent = result.motivationMessage;
  supportMessage.classList.toggle("crisis", result.riskLevel === "high");
  analysisMethod.textContent = methodLabels[result.analysisMethod] ?? result.analysisMethod;
  modelName.textContent = result.model ?? "Kullanılmadı";

  const fallbackText = fallbackLabels[result.fallbackReason];
  fallbackNote.hidden = !fallbackText;
  fallbackNote.textContent = fallbackText
    ? `${fallbackText} Analiz kural tabanlı olarak tamamlandı.`
    : "";

  renderScores(result);
  renderKeywords(result.matchedKeywords);
  setVisibleState("result");
}

function extractError(problem) {
  const firstValidationError = problem?.errors
    ? Object.values(problem.errors).flat()[0]
    : null;

  return firstValidationError ?? problem?.detail ?? problem?.title ?? "Lütfen tekrar deneyin.";
}

async function analyzeText(event) {
  event.preventDefault();
  const text = userText.value.trim();

  if (!text) {
    errorMessage.textContent = "Analiz için bir metin yazmalısın.";
    setVisibleState("error");
    userText.focus();
    return;
  }

  analyzeButton.disabled = true;
  setVisibleState("loading");

  try {
    const response = await fetch("/api/emotion/analyze", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json"
      },
      body: JSON.stringify({ userText: text })
    });
    const body = await response.json();

    if (!response.ok) {
      throw new Error(extractError(body));
    }

    renderResult(body);
    refreshModelStatus();
  } catch (error) {
    errorMessage.textContent = error instanceof Error
      ? error.message
      : "İstek tamamlanamadı. Lütfen tekrar deneyin.";
    setVisibleState("error");
  } finally {
    analyzeButton.disabled = false;
  }
}

form.addEventListener("submit", analyzeText);
userText.addEventListener("input", updateCharacterCount);
userText.addEventListener("keydown", (event) => {
  if ((event.ctrlKey || event.metaKey) && event.key === "Enter") {
    form.requestSubmit();
  }
});

clearButton.addEventListener("click", () => {
  userText.value = "";
  updateCharacterCount();
  setVisibleState("empty");
  userText.focus();
});

refreshModelsButton.addEventListener("click", refreshModelCatalog);
modelDownloadForm.addEventListener("submit", startModelDownload);
connectEcgButton.addEventListener("click", connectEcg);
disconnectEcgButton.addEventListener("click", () => disconnectEcg());
chatForm.addEventListener("submit", sendChatMessage);
clearChatButton.addEventListener("click", clearChat);
chatText.addEventListener("keydown", (event) => {
  if ((event.ctrlKey || event.metaKey) && event.key === "Enter") {
    chatForm.requestSubmit();
  }
});
window.addEventListener("resize", scheduleEcgRender);

document.querySelectorAll("[data-model-suggestion]").forEach((button) => {
  button.addEventListener("click", () => {
    downloadModelId.value = button.dataset.modelSuggestion ?? "";
    downloadModelId.focus();
  });
});

document.querySelectorAll("[data-example]").forEach((button) => {
  button.addEventListener("click", () => {
    userText.value = button.dataset.example ?? "";
    updateCharacterCount();
    userText.focus();
  });
});

updateCharacterCount();
initializeEcgPanel();
refreshModelCatalog();

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

document.querySelectorAll("[data-example]").forEach((button) => {
  button.addEventListener("click", () => {
    userText.value = button.dataset.example ?? "";
    updateCharacterCount();
    userText.focus();
  });
});

updateCharacterCount();
refreshModelStatus();

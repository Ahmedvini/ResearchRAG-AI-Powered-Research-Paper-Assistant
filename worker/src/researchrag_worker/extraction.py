import json
import re
from dataclasses import dataclass

_MODEL_PATTERN = re.compile(
    r"\b(CNN|RNN|LSTM|GRU|Transformer|BERT|GPT-?\d*|ResNet[-\w]*|VGG[-\w]*|U-?Net|EEGNet|"
    r"SVM|Random Forest|XGBoost|LightGBM|Logistic Regression|k-?NN|Autoencoder|GAN|MLP)\b",
    re.IGNORECASE,
)

_DATASET_PATTERN = re.compile(
    r"\b((?:[A-Z][\w-]*\s+){0,3}[A-Z][\w-]*)\s+(?:dataset|corpus|benchmark)\b"
)

_METRIC_NAMES = [
    "accuracy",
    "precision",
    "recall",
    "f1",
    "auc",
    "bleu",
    "rouge",
    "rmse",
    "mae",
    "perplexity",
    "kappa",
]

_ACCURACY_PATTERN = re.compile(
    r"\baccuracy\s*(?:of|was|is|:|=|reached|achieves?d?)?\s*(\d{1,3}(?:\.\d+)?\s*%)",
    re.IGNORECASE,
)


@dataclass(frozen=True)
class PaperExtractionResult:
    dataset: str
    model: str
    metrics_json: str
    accuracy: str
    limitations_json: str
    future_work_json: str


def extract_paper_fields(full_text: str) -> PaperExtractionResult:
    sentences = _sentences(full_text)

    dataset_match = _DATASET_PATTERN.search(full_text)
    dataset = dataset_match.group(1).strip() if dataset_match else ""

    model_match = _MODEL_PATTERN.search(full_text)
    model = model_match.group(1) if model_match else ""

    lowered = full_text.lower()
    metrics = [name.upper() if name in {"auc", "rmse", "mae", "bleu", "rouge"} else name.capitalize()
               for name in _METRIC_NAMES if name in lowered]

    accuracy_match = _ACCURACY_PATTERN.search(full_text)
    accuracy = accuracy_match.group(1).replace(" ", "") if accuracy_match else ""

    limitations = _matching_sentences(sentences, ["limitation", "is limited", "drawback"])
    future_work = _matching_sentences(sentences, ["future work", "future research", "we plan to", "further study"])

    return PaperExtractionResult(
        dataset=dataset[:200],
        model=model[:200],
        metrics_json=json.dumps(metrics),
        accuracy=accuracy[:50],
        limitations_json=json.dumps(limitations),
        future_work_json=json.dumps(future_work),
    )


def _sentences(text: str) -> list[str]:
    normalized = " ".join(text.split())
    return [sentence.strip() for sentence in re.split(r"(?<=[.!?])\s+", normalized) if sentence.strip()]


def _matching_sentences(sentences: list[str], keywords: list[str], limit: int = 3, max_length: int = 300) -> list[str]:
    found: list[str] = []
    for sentence in sentences:
        lowered = sentence.lower()
        if any(keyword in lowered for keyword in keywords):
            trimmed = sentence if len(sentence) <= max_length else sentence[:max_length] + "..."
            if trimmed not in found:
                found.append(trimmed)
        if len(found) >= limit:
            break
    return found

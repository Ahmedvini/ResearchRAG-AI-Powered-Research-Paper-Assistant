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


_EXTRACTION_PROMPT = """You extract structured metadata from research papers.
Reply with a single JSON object only, no prose, using exactly these keys:
{"dataset": string, "model": string, "metrics": [string], "accuracy": string, "limitations": [string], "future_work": [string]}
Use "" or [] when the paper does not state a value.

Paper text:
"""


def extract_paper_fields_llm(client, full_text: str) -> PaperExtractionResult | None:
    """LLM-backed extraction. Returns None on any failure so callers can fall
    back to the regex heuristics in extract_paper_fields."""
    excerpt = full_text[:6000]
    if len(full_text) > 9000:
        # Limitations and future work usually live near the end of the paper.
        excerpt += "\n...\n" + full_text[-3000:]
    try:
        response = client.complete(_EXTRACTION_PROMPT + excerpt)
    except Exception:
        return None
    return parse_extraction_response(response)


def parse_extraction_response(response: str) -> PaperExtractionResult | None:
    # Take the outermost JSON object; this also strips markdown fences or any
    # prose the model wrapped around it.
    start, end = response.find("{"), response.rfind("}")
    if start == -1 or end <= start:
        return None
    try:
        data = json.loads(response[start : end + 1])
    except json.JSONDecodeError:
        return None
    if not isinstance(data, dict):
        return None

    def text_field(key: str, limit: int) -> str:
        value = data.get(key)
        return value.strip()[:limit] if isinstance(value, str) else ""

    def list_field(key: str) -> list[str]:
        value = data.get(key)
        if isinstance(value, str):
            value = [value]
        if not isinstance(value, list):
            return []
        return [item.strip()[:300] for item in value if isinstance(item, str) and item.strip()][:6]

    result = PaperExtractionResult(
        dataset=text_field("dataset", 200),
        model=text_field("model", 200),
        metrics_json=json.dumps(list_field("metrics")),
        accuracy=text_field("accuracy", 50),
        limitations_json=json.dumps(list_field("limitations")),
        future_work_json=json.dumps(list_field("future_work")),
    )

    if (
        not (result.dataset or result.model or result.accuracy)
        and result.metrics_json == "[]"
        and result.limitations_json == "[]"
        and result.future_work_json == "[]"
    ):
        return None  # the model extracted nothing; let the heuristics try
    return result


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

import json

from researchrag_worker.extraction import extract_paper_fields, extract_paper_fields_llm, parse_extraction_response


SAMPLE = """
We evaluate our approach on the BCI Competition IV dataset using a CNN classifier.
The model achieves accuracy of 87.5% and strong F1 and precision across subjects.
A limitation of this study is the small number of participants.
Future work will explore transfer learning across recording sessions.
"""


def test_extracts_dataset_and_model():
    result = extract_paper_fields(SAMPLE)

    assert "BCI Competition IV" in result.dataset
    assert result.model == "CNN"


def test_extracts_metrics_and_accuracy():
    result = extract_paper_fields(SAMPLE)

    metrics = json.loads(result.metrics_json)
    assert "Accuracy" in metrics
    assert "F1" in metrics
    assert result.accuracy == "87.5%"


def test_extracts_limitations_and_future_work():
    result = extract_paper_fields(SAMPLE)

    limitations = json.loads(result.limitations_json)
    future = json.loads(result.future_work_json)
    assert any("limitation" in item.lower() for item in limitations)
    assert any("future work" in item.lower() for item in future)


def test_handles_text_without_signals():
    result = extract_paper_fields("Nothing interesting here.")

    assert result.dataset == ""
    assert result.model == ""
    assert json.loads(result.metrics_json) == []
    assert result.accuracy == ""
    assert json.loads(result.limitations_json) == []
    assert json.loads(result.future_work_json) == []


def test_parse_extraction_response_reads_plain_json():
    response = json.dumps(
        {
            "dataset": " BCI Competition IV ",
            "model": "EEGNet",
            "metrics": ["Accuracy", "Kappa"],
            "accuracy": "87.5%",
            "limitations": ["Small cohort."],
            "future_work": ["Cross-session transfer."],
        }
    )

    result = parse_extraction_response(response)

    assert result is not None
    assert result.dataset == "BCI Competition IV"
    assert result.model == "EEGNet"
    assert json.loads(result.metrics_json) == ["Accuracy", "Kappa"]
    assert result.accuracy == "87.5%"


def test_parse_extraction_response_strips_fences_and_prose():
    response = 'Here you go:\n```json\n{"dataset": "TUH EEG", "model": "", "metrics": [], "accuracy": "", "limitations": [], "future_work": []}\n```'

    result = parse_extraction_response(response)

    assert result is not None
    assert result.dataset == "TUH EEG"


def test_parse_extraction_response_rejects_garbage_and_wrong_types():
    assert parse_extraction_response("I could not find anything.") is None
    assert parse_extraction_response("{not json") is None
    assert parse_extraction_response("[1, 2, 3]") is None

    # Wrong-typed fields are dropped rather than crashing.
    typed = parse_extraction_response(
        '{"dataset": 42, "model": null, "metrics": "Accuracy", "accuracy": "", "limitations": [1, "real"], "future_work": []}'
    )
    assert typed is not None
    assert typed.dataset == ""
    assert json.loads(typed.metrics_json) == ["Accuracy"]
    assert json.loads(typed.limitations_json) == ["real"]


def test_parse_extraction_response_returns_none_when_everything_is_empty():
    empty = json.dumps({"dataset": "", "model": "", "metrics": [], "accuracy": "", "limitations": [], "future_work": []})

    assert parse_extraction_response(empty) is None


def test_extract_paper_fields_llm_falls_back_to_none_on_client_error():
    class FailingClient:
        def complete(self, prompt: str) -> str:
            raise ConnectionError("provider down")

    assert extract_paper_fields_llm(FailingClient(), SAMPLE) is None


def test_extract_paper_fields_llm_uses_client_response():
    class FakeClient:
        def __init__(self):
            self.prompt = ""

        def complete(self, prompt: str) -> str:
            self.prompt = prompt
            return '{"dataset": "BCI Competition IV", "model": "CNN", "metrics": ["Accuracy"], "accuracy": "87.5%", "limitations": [], "future_work": []}'

    client = FakeClient()
    result = extract_paper_fields_llm(client, SAMPLE)

    assert result is not None
    assert result.model == "CNN"
    assert "BCI Competition IV" in client.prompt  # paper text made it into the prompt

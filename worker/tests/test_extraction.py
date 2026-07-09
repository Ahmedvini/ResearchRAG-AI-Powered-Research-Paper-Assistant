import json

from researchrag_worker.extraction import extract_paper_fields


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

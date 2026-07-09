from researchrag_worker.metadata import extract_metadata


def test_extract_metadata_from_common_paper_shape():
    text = """
    Neural Interfaces for Efficient Brain Computer Interaction
    Ahmed Example
    Abstract
    This paper studies brain computer interfaces with robust feature extraction.
    Keywords: BCI, EEG, classification
    1 Introduction
    The field is growing.
    Published in 2024.
    """

    metadata = extract_metadata(text)

    assert metadata.title == "Neural Interfaces for Efficient Brain Computer Interaction"
    assert metadata.authors == "Ahmed Example"
    assert metadata.publication_year == 2024
    assert "brain computer interfaces" in metadata.abstract
    assert metadata.keywords == "BCI, EEG, classification"


def test_publication_year_ignores_cited_references():
    text = """Deep Learning for EEG Analysis
Jane Doe; John Smith
Published 2024

Abstract
We study EEG classification building on LeCun (1998) and Hinton (2006).

Introduction
Prior work from 1997 established recurrent architectures.
"""

    metadata = extract_metadata(text)

    assert metadata.publication_year == 2024
    assert metadata.authors == "Jane Doe; John Smith"


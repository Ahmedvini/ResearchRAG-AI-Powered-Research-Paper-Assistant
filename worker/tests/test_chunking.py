from researchrag_worker.chunking import recursive_chunk


def test_recursive_chunk_keeps_short_text_together():
    chunks = recursive_chunk("A concise abstract about retrieval augmented generation.", 2, "Abstract", chunk_size=120)

    assert len(chunks) == 1
    assert chunks[0].page_number == 2
    assert chunks[0].section_name == "Abstract"


def test_recursive_chunk_overlaps_long_text():
    text = " ".join(f"sentence {i}." for i in range(120))
    chunks = recursive_chunk(text, 1, "Methodology", chunk_size=180, overlap=30)

    assert len(chunks) > 3
    assert all(chunk.section_name == "Methodology" for chunk in chunks)
    assert all(len(chunk.text) <= 220 for chunk in chunks)


def test_recursive_chunk_terminates_with_large_overlap():
    # Regression: overlap close to chunk_size used to loop forever because the
    # sentence-boundary split moved "end - overlap" back before "start".
    text = "aa bb cc dd ee ff gg hh ii jj kk ll mm nn oo pp"
    chunks = recursive_chunk(text, 1, "X", chunk_size=10, overlap=8)

    assert chunks
    assert all(chunk.text for chunk in chunks)


from researchrag_worker.sections import detect_section, split_page_into_sections


def test_detect_numbered_heading():
    assert detect_section("2.1 Methodology") == "Methodology"


def test_split_page_into_sections():
    sections = split_page_into_sections("Abstract\nA summary.\nIntroduction\nProblem setup.")

    assert sections[0] == ("Abstract", "A summary.")
    assert sections[1] == ("Introduction", "Problem setup.")


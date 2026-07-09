from researchrag_worker.sections import detect_section, split_page_into_sections


def test_detect_numbered_heading():
    assert detect_section("2.1 Methodology") == "Methodology"


def test_detect_heading_with_trailing_dot_numbering():
    assert detect_section("1. Introduction") == "Introduction"
    assert detect_section("2.1. Methods") == "Methodology"


def test_detect_roman_numeral_heading():
    assert detect_section("IV. Results") == "Results"


def test_detect_references_heading():
    # Without this alias, reference lists inherit the previous section name
    # (usually Conclusion) and pollute keyword-based analysis.
    assert detect_section("References") == "References"
    assert detect_section("Bibliography") == "References"


def test_detect_does_not_strip_normal_words():
    assert detect_section("I think this is fine", "Discussion") == "Discussion"


def test_numbered_headings_are_not_included_in_body():
    sections = split_page_into_sections("1. Introduction\nProblem setup.\n2. Results\nWe find things.")

    assert sections[0] == ("Introduction", "Problem setup.")
    assert sections[1] == ("Results", "We find things.")


def test_split_page_into_sections():
    sections = split_page_into_sections("Abstract\nA summary.\nIntroduction\nProblem setup.")

    assert sections[0] == ("Abstract", "A summary.")
    assert sections[1] == ("Introduction", "Problem setup.")


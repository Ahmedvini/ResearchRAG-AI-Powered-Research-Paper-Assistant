from pathlib import Path

import fitz


def extract_pages(path: Path) -> list[tuple[int, str]]:
    with fitz.open(path) as document:
        return [(index + 1, page.get_text("text")) for index, page in enumerate(document)]


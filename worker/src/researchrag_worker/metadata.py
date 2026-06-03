import re
from dataclasses import dataclass


@dataclass(frozen=True)
class PaperMetadata:
    title: str | None
    authors: str | None
    publication_year: int | None
    abstract: str | None
    keywords: str | None


def extract_metadata(full_text: str) -> PaperMetadata:
    lines = [line.strip() for line in full_text.splitlines() if line.strip()]
    title = _first_title_candidate(lines)
    year = _publication_year(full_text)
    abstract = _between_heading(full_text, "abstract", ["keywords", "introduction", "1 introduction"])
    keywords = _keywords(full_text)
    authors = _authors(lines, title)
    return PaperMetadata(title, authors, year, abstract, keywords)


def _first_title_candidate(lines: list[str]) -> str | None:
    for line in lines[:12]:
        if 8 <= len(line) <= 220 and not re.match(r"^(abstract|keywords|introduction)\b", line, re.I):
            return line
    return None


def _publication_year(text: str) -> int | None:
    matches = [int(value) for value in re.findall(r"\b(19[8-9]\d|20[0-3]\d)\b", text)]
    return min(matches) if matches else None


def _between_heading(text: str, heading: str, stops: list[str]) -> str | None:
    pattern = re.compile(rf"{heading}\s*(.+)", re.I | re.S)
    match = pattern.search(text)
    if not match:
        return None
    body = match.group(1)
    stop_positions = [body.lower().find(stop) for stop in stops if body.lower().find(stop) > 0]
    if stop_positions:
        body = body[: min(stop_positions)]
    body = " ".join(body.split())
    return body[:2500] or None


def _keywords(text: str) -> str | None:
    match = re.search(r"keywords?\s*[:\-]\s*(.+)", text, re.I)
    if not match:
        return None
    return match.group(1).splitlines()[0][:500].strip()


def _authors(lines: list[str], title: str | None) -> str | None:
    if title and title in lines:
        index = lines.index(title)
        authorish: list[str] = []
        for line in lines[index + 1 : index + 5]:
            if re.match(r"^(abstract|keywords|introduction|\d+\s+introduction)\b", line, re.I):
                break
            authorish.append(line)
        return "; ".join(authorish)[:500] or None
    return None

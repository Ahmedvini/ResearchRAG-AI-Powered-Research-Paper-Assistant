import re

SECTION_ALIASES = {
    "abstract": "Abstract",
    "introduction": "Introduction",
    "related work": "Related Work",
    "background": "Related Work",
    "methodology": "Methodology",
    "methods": "Methodology",
    "method": "Methodology",
    "experiments": "Experiments",
    "experimental setup": "Experiments",
    "results": "Results",
    "discussion": "Discussion",
    "conclusion": "Conclusion",
    "conclusions": "Conclusion",
}


# Strips heading numbering: "1 ", "1. ", "2.1 ", "2.1. ", and Roman "IV.".
_NUMBERING = re.compile(r"^\s*(?:\d+(?:\.\d+)*\.?|[IVXLCDM]+\.)\s*")


def _normalize_heading(line: str) -> str:
    normalized = _NUMBERING.sub("", line).strip().strip(":").lower()
    return re.sub(r"\s+", " ", normalized)


def detect_section(line: str, current: str = "Unknown") -> str:
    return SECTION_ALIASES.get(_normalize_heading(line), current)


def split_page_into_sections(text: str, current: str = "Unknown") -> list[tuple[str, str]]:
    sections: list[tuple[str, str]] = []
    buffer: list[str] = []
    active = current
    for line in text.splitlines():
        is_heading = _normalize_heading(line) in SECTION_ALIASES
        next_section = detect_section(line, active)
        if next_section != active and buffer:
            sections.append((active, "\n".join(buffer).strip()))
            buffer = []
        active = next_section
        # Keep heading lines out of the body whether or not they are numbered.
        if not is_heading:
            buffer.append(line)
    if buffer:
        sections.append((active, "\n".join(buffer).strip()))
    return [(name, body) for name, body in sections if body]


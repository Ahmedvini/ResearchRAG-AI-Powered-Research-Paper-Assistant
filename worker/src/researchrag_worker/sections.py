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


def detect_section(line: str, current: str = "Unknown") -> str:
    normalized = re.sub(r"^\s*\d+(\.\d+)*\s*", "", line).strip().strip(":").lower()
    normalized = re.sub(r"\s+", " ", normalized)
    return SECTION_ALIASES.get(normalized, current)


def split_page_into_sections(text: str, current: str = "Unknown") -> list[tuple[str, str]]:
    sections: list[tuple[str, str]] = []
    buffer: list[str] = []
    active = current
    for line in text.splitlines():
        next_section = detect_section(line, active)
        if next_section != active and buffer:
            sections.append((active, "\n".join(buffer).strip()))
            buffer = []
        active = next_section
        if line.strip().lower() not in SECTION_ALIASES:
            buffer.append(line)
    if buffer:
        sections.append((active, "\n".join(buffer).strip()))
    return [(name, body) for name, body in sections if body]


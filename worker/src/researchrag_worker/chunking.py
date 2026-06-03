from dataclasses import dataclass


@dataclass(frozen=True)
class TextChunk:
    text: str
    page_number: int
    section_name: str


def recursive_chunk(text: str, page_number: int, section_name: str, chunk_size: int = 1200, overlap: int = 180) -> list[TextChunk]:
    cleaned = " ".join(text.split())
    if not cleaned:
        return []
    if len(cleaned) <= chunk_size:
        return [TextChunk(cleaned, page_number, section_name)]

    chunks: list[TextChunk] = []
    start = 0
    while start < len(cleaned):
        end = min(start + chunk_size, len(cleaned))
        if end < len(cleaned):
            split_at = max(cleaned.rfind(". ", start, end), cleaned.rfind("; ", start, end), cleaned.rfind(" ", start, end))
            if split_at > start + chunk_size // 2:
                end = split_at + 1
        chunk = cleaned[start:end].strip()
        if chunk:
            chunks.append(TextChunk(chunk, page_number, section_name))
        if end >= len(cleaned):
            break
        start = max(0, end - overlap)
    return chunks


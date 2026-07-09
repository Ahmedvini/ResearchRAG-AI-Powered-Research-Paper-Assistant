import time
from pathlib import Path
from uuid import uuid4

from qdrant_client import QdrantClient
from qdrant_client.http.models import Distance, PointStruct, VectorParams
from sqlalchemy import create_engine, text

from researchrag_worker.chunking import TextChunk, recursive_chunk
from researchrag_worker.config import Settings
from researchrag_worker.embeddings import EmbeddingProvider, create_embedding_provider
from researchrag_worker.metadata import extract_metadata
from researchrag_worker.pdf import extract_pages
from researchrag_worker.sections import split_page_into_sections


def main() -> None:
    settings = Settings()
    engine = create_engine(settings.database_url, pool_pre_ping=True)
    qdrant = QdrantClient(url=settings.qdrant_url)
    embeddings = create_embedding_provider(
        settings.embedding_provider,
        settings.resolve_embedding_model(),
        settings.openai_api_key,
        settings.openai_base_url,
        settings.ollama_base_url,
    )
    ensure_collection(qdrant, settings.qdrant_collection, embeddings.dimension)

    while True:
        job = claim_next_job(engine, settings)
        if not job:
            time.sleep(settings.poll_seconds)
            continue
        process_job(engine, qdrant, embeddings, settings, job)


def claim_next_job(engine, settings: Settings):
    with engine.begin() as connection:
        # Requeue jobs whose worker died mid-run so they are not stuck in
        # 'Running' forever.
        connection.execute(
            text(
                """
                UPDATE ProcessingJobs j
                JOIN Documents d ON d.Id = j.DocumentId
                SET j.Status='Queued', j.StartedAt=NULL, d.Status='Queued'
                WHERE j.Status='Running'
                  AND j.StartedAt < (UTC_TIMESTAMP(6) - INTERVAL :minutes MINUTE)
                """
            ),
            {"minutes": settings.stale_running_minutes},
        )
        # Give up on jobs that exhausted their retries.
        connection.execute(
            text(
                """
                UPDATE ProcessingJobs j
                JOIN Documents d ON d.Id = j.DocumentId
                SET j.Status='Failed',
                    j.LastError=COALESCE(j.LastError, 'Retry limit reached.'),
                    j.CompletedAt=UTC_TIMESTAMP(6),
                    d.Status='Failed',
                    d.FailureReason=COALESCE(d.FailureReason, 'Retry limit reached.')
                WHERE j.Status='Queued' AND j.Attempts >= :max_attempts
                """
            ),
            {"max_attempts": settings.max_attempts},
        )
        row = connection.execute(
            text(
                """
                SELECT j.Id AS job_id, j.DocumentId AS document_id, j.Attempts AS attempts,
                       d.StoragePath AS storage_path, d.WorkspaceId AS workspace_id
                FROM ProcessingJobs j
                JOIN Documents d ON d.Id = j.DocumentId
                WHERE j.Status = 'Queued' AND j.Attempts < :max_attempts
                ORDER BY j.CreatedAt
                LIMIT 1
                FOR UPDATE OF j SKIP LOCKED
                """
            ),
            {"max_attempts": settings.max_attempts},
        ).mappings().first()
        if not row:
            return None
        connection.execute(
            text("UPDATE ProcessingJobs SET Status='Running', Attempts=Attempts+1, StartedAt=UTC_TIMESTAMP(6) WHERE Id=:id"),
            {"id": row["job_id"]},
        )
        connection.execute(text("UPDATE Documents SET Status='Extracting' WHERE Id=:id"), {"id": row["document_id"]})
        job = dict(row)
        job["attempts"] = row["attempts"] + 1  # value after the claim above
        return job


def process_job(engine, qdrant: QdrantClient, embeddings: EmbeddingProvider, settings: Settings, job: dict) -> None:
    try:
        path = Path(job["storage_path"])
        pages = extract_pages(path)
        full_text = "\n".join(text for _, text in pages)
        metadata = extract_metadata(full_text)

        chunks: list[TextChunk] = []
        active_section = "Unknown"
        for page_number, page_text in pages:
            for section_name, section_text in split_page_into_sections(page_text, active_section):
                active_section = section_name
                chunks.extend(recursive_chunk(section_text, page_number, section_name, settings.chunk_size, settings.chunk_overlap))

        with engine.begin() as connection:
            connection.execute(text("UPDATE Documents SET Status='Chunking', Title=:title, Authors=:authors, PublicationYear=:year, Abstract=:abstract, Keywords=:keywords WHERE Id=:id"), {
                "id": job["document_id"],
                "title": metadata.title,
                "authors": metadata.authors,
                "year": metadata.publication_year,
                "abstract": metadata.abstract,
                "keywords": metadata.keywords,
            })
            connection.execute(text("DELETE FROM DocumentChunks WHERE DocumentId=:id"), {"id": job["document_id"]})

            points: list[PointStruct] = []
            for chunk in chunks:
                chunk_id = str(uuid4())
                vector_id = str(uuid4())
                connection.execute(
                    text(
                        """
                        INSERT INTO DocumentChunks (Id, CreatedAt, DocumentId, WorkspaceId, Text, PageNumber, SectionName, VectorId)
                        VALUES (:id, UTC_TIMESTAMP(6), :document_id, :workspace_id, :text, :page, :section, :vector_id)
                        """
                    ),
                    {
                        "id": chunk_id,
                        "document_id": job["document_id"],
                        "workspace_id": job["workspace_id"],
                        "text": chunk.text,
                        "page": chunk.page_number,
                        "section": chunk.section_name,
                        "vector_id": vector_id,
                    },
                )
                points.append(
                    PointStruct(
                        id=vector_id,
                        vector=embeddings.embed(chunk.text),
                        payload={
                            "chunk_id": chunk_id,
                            "document_id": str(job["document_id"]),
                            "workspace_id": str(job["workspace_id"]),
                            "page_number": chunk.page_number,
                            "section": chunk.section_name,
                        },
                    )
                )

            connection.execute(text("UPDATE Documents SET Status='Embedding' WHERE Id=:id"), {"id": job["document_id"]})

        if points:
            qdrant.upsert(collection_name=settings.qdrant_collection, points=points)

        with engine.begin() as connection:
            connection.execute(text("UPDATE Documents SET Status='Ready' WHERE Id=:id"), {"id": job["document_id"]})
            connection.execute(
                text("UPDATE ProcessingJobs SET Status='Completed', CompletedAt=UTC_TIMESTAMP(6) WHERE Id=:id"),
                {"id": job["job_id"]},
            )
    except Exception as exc:
        error = str(exc)[:1000]
        exhausted = job.get("attempts", settings.max_attempts) >= settings.max_attempts
        with engine.begin() as connection:
            if exhausted:
                connection.execute(
                    text("UPDATE Documents SET Status='Failed', FailureReason=:error WHERE Id=:id"),
                    {"id": job["document_id"], "error": error},
                )
                connection.execute(
                    text("UPDATE ProcessingJobs SET Status='Failed', LastError=:error, CompletedAt=UTC_TIMESTAMP(6) WHERE Id=:id"),
                    {"id": job["job_id"], "error": error},
                )
            else:
                # Attempts remain: put the job back in the queue for a retry.
                connection.execute(
                    text("UPDATE Documents SET Status='Queued', FailureReason=:error WHERE Id=:id"),
                    {"id": job["document_id"], "error": error},
                )
                connection.execute(
                    text("UPDATE ProcessingJobs SET Status='Queued', LastError=:error, StartedAt=NULL WHERE Id=:id"),
                    {"id": job["job_id"], "error": error},
                )


def ensure_collection(qdrant: QdrantClient, collection: str, dimension: int) -> None:
    existing = {item.name for item in qdrant.get_collections().collections}
    if collection not in existing:
        qdrant.create_collection(collection, vectors_config=VectorParams(size=dimension, distance=Distance.COSINE))

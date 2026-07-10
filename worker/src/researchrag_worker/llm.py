import json
from abc import ABC, abstractmethod
from urllib import request


class LlmClient(ABC):
    @abstractmethod
    def complete(self, prompt: str) -> str:
        raise NotImplementedError


class OpenAiLlmClient(LlmClient):
    def __init__(self, api_key: str, model: str = "gpt-4.1-mini", base_url: str = "https://api.openai.com") -> None:
        self.api_key = api_key
        self.model = model
        self.base_url = base_url.rstrip("/")

    def complete(self, prompt: str) -> str:
        payload = json.dumps(
            {
                "model": self.model,
                "temperature": 0,
                "messages": [{"role": "user", "content": prompt}],
            }
        ).encode("utf-8")
        http_request = request.Request(
            f"{self.base_url}/v1/chat/completions",
            data=payload,
            headers={"Authorization": f"Bearer {self.api_key}", "Content-Type": "application/json"},
            method="POST",
        )
        with request.urlopen(http_request, timeout=120) as response:
            body = json.loads(response.read().decode("utf-8"))
        return body["choices"][0]["message"]["content"]


class OllamaLlmClient(LlmClient):
    def __init__(self, model: str = "llama3.1", base_url: str = "http://localhost:11434") -> None:
        self.model = model
        self.base_url = base_url.rstrip("/")

    def complete(self, prompt: str) -> str:
        payload = json.dumps(
            {"model": self.model, "prompt": prompt, "stream": False, "format": "json"}
        ).encode("utf-8")
        http_request = request.Request(
            f"{self.base_url}/api/generate",
            data=payload,
            headers={"Content-Type": "application/json"},
            method="POST",
        )
        with request.urlopen(http_request, timeout=180) as response:
            body = json.loads(response.read().decode("utf-8"))
        return body["response"]


def create_llm_client(
    provider: str,
    openai_api_key: str | None,
    openai_base_url: str,
    openai_model: str,
    ollama_base_url: str,
    ollama_model: str,
) -> LlmClient | None:
    """Returns None for 'echo' or unconfigured providers; callers then use the
    regex-heuristic extraction instead of an LLM."""
    normalized = provider.lower()
    if normalized == "openai" and openai_api_key:
        return OpenAiLlmClient(openai_api_key, openai_model, openai_base_url)
    if normalized == "ollama":
        return OllamaLlmClient(ollama_model, ollama_base_url)
    return None

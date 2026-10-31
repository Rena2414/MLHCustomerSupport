from abc import ABC, abstractmethod
from typing import Dict, List

from app.core.messages import Message


class ConversationStore(ABC):
    @abstractmethod
    def get(self, conversation_id: str) -> List[Message]:
        ...

    @abstractmethod
    def append(self, conversation_id: str, messages: List[Message]) -> None:
        ...


class InMemoryConversationStore(ConversationStore):
    def __init__(self) -> None:
        self._conversations: Dict[str, List[Message]] = {}

    def get(self, conversation_id: str) -> List[Message]:
        return list(self._conversations.get(conversation_id, []))

    def append(self, conversation_id: str, messages: List[Message]) -> None:
        self._conversations.setdefault(conversation_id, []).extend(messages)

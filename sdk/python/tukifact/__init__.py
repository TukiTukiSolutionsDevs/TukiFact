"""TukiFact Python SDK."""

from .client import TukiFactClient, TukiFactError
from .types import (
    Customer,
    Document,
    DocumentCreateRequest,
    DocumentItem,
    DocumentResponse,
    DocumentStatus,
    DocumentType,
    Pagination,
    PaginatedResponse,
    Series,
    SunatResponse,
)

__version__ = "0.1.0"

__all__ = [
    # Client
    "TukiFactClient",
    "TukiFactError",
    # Types
    "Customer",
    "Document",
    "DocumentCreateRequest",
    "DocumentItem",
    "DocumentResponse",
    "DocumentStatus",
    "DocumentType",
    "Pagination",
    "PaginatedResponse",
    "Series",
    "SunatResponse",
    # Meta
    "__version__",
]

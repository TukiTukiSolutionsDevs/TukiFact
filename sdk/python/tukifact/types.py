from __future__ import annotations

from dataclasses import dataclass, field
from typing import Generic, Literal, Optional, TypeVar

# ─── Enums (as literals) ──────────────────────────────────────────────────────

DocumentType = Literal["factura", "boleta", "nota_credito", "nota_debito"]
DocumentStatus = Literal["pending", "sent", "accepted", "rejected", "voided"]
CustomerDocumentType = Literal["RUC", "DNI", "CE", "PASAPORTE"]

T = TypeVar("T")

# ─── Documents ────────────────────────────────────────────────────────────────


@dataclass
class DocumentItem:
    description: str
    quantity: float
    unit_price: float
    unit_code: Optional[str] = None
    igv_rate: Optional[float] = None
    discount: Optional[float] = None


@dataclass
class DocumentCreateRequest:
    type: DocumentType
    series: str
    customer_id: str
    issue_date: str  # ISO 8601
    items: list[DocumentItem] = field(default_factory=list)
    due_date: Optional[str] = None
    currency: Optional[str] = None  # e.g. "PEN", "USD"
    notes: Optional[str] = None
    reference_document_id: Optional[str] = None


@dataclass
class Document:
    id: str
    type: DocumentType
    series: str
    correlative: int
    full_number: str  # e.g. "F001-00000001"
    customer_id: str
    issue_date: str
    currency: str
    subtotal: float
    igv: float
    total: float
    status: DocumentStatus
    items: list[DocumentItem]
    created_at: str
    updated_at: str
    due_date: Optional[str] = None
    notes: Optional[str] = None
    reference_document_id: Optional[str] = None


@dataclass
class SunatResponse:
    accepted: bool
    code: Optional[str] = None
    description: Optional[str] = None
    observations: list[str] = field(default_factory=list)


@dataclass
class DocumentResponse:
    document: Document
    sunat: Optional[SunatResponse] = None


# ─── Customers ────────────────────────────────────────────────────────────────


@dataclass
class Customer:
    id: str
    document_type: CustomerDocumentType
    document_number: str
    legal_name: str
    created_at: str
    updated_at: str
    trade_name: Optional[str] = None
    address: Optional[str] = None
    district: Optional[str] = None
    province: Optional[str] = None
    department: Optional[str] = None
    email: Optional[str] = None
    phone: Optional[str] = None


# ─── Series ───────────────────────────────────────────────────────────────────


@dataclass
class Series:
    id: str
    type: DocumentType
    prefix: str  # e.g. "F001", "B001"
    current_correlative: int
    is_active: bool


# ─── Pagination ───────────────────────────────────────────────────────────────


@dataclass
class Pagination:
    page: int
    page_size: int
    total_count: int
    total_pages: int


@dataclass
class PaginatedResponse(Generic[T]):
    data: list[T]
    page: int
    page_size: int
    total_count: int
    total_pages: int

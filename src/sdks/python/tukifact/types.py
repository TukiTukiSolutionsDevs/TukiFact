from dataclasses import dataclass, field
from typing import Optional


@dataclass
class CreateDocumentItem:
    description: str
    quantity: float
    unit_price: float
    igv_type: str = "10"
    product_code: Optional[str] = None
    unit_measure: str = "NIU"
    discount: float = 0


@dataclass
class CreateDocumentRequest:
    document_type: str  # "01" or "03"
    serie: str
    customer_doc_type: str
    customer_doc_number: str
    customer_name: str
    items: list[CreateDocumentItem]
    currency: str = "PEN"
    customer_address: Optional[str] = None
    customer_email: Optional[str] = None
    notes: Optional[str] = None


@dataclass
class CreateCreditNoteRequest:
    serie: str
    reference_document_id: str
    credit_note_reason: str
    items: list[CreateDocumentItem]
    description: Optional[str] = None
    currency: str = "PEN"


@dataclass
class DocumentItemResponse:
    sequence: int
    description: str
    quantity: float
    unit_measure: str
    unit_price: float
    unit_price_with_igv: float
    igv_type: str
    igv_amount: float
    subtotal: float
    total: float
    product_code: Optional[str] = None


@dataclass
class DocumentResponse:
    id: str
    document_type: str
    document_type_name: str
    serie: str
    correlative: int
    full_number: str
    issue_date: str
    currency: str
    customer_doc_type: str
    customer_doc_number: str
    customer_name: str
    operacion_gravada: float
    igv: float
    total: float
    status: str
    items: list[DocumentItemResponse] = field(default_factory=list)
    sunat_response_code: Optional[str] = None
    sunat_response_description: Optional[str] = None
    hash_code: Optional[str] = None
    xml_url: Optional[str] = None
    created_at: Optional[str] = None


@dataclass
class DashboardSummary:
    total_documents: int
    total_amount: float
    total_igv: float
    accepted: int
    rejected: int
    pending: int


@dataclass
class DashboardResponse:
    today: DashboardSummary
    this_month: DashboardSummary
    this_year: DashboardSummary


@dataclass
class SeriesResponse:
    id: str
    document_type: str
    serie: str
    current_correlative: int
    emission_point: str
    is_active: bool

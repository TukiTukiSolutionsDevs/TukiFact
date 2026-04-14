"""TukiFact SDK — synchronous HTTP client."""

from __future__ import annotations

import re
from typing import Any, Optional

import httpx

from .types import (
    Customer,
    Document,
    DocumentCreateRequest,
    DocumentItem,
    DocumentResponse,
    Pagination,
    PaginatedResponse,
    Series,
    SunatResponse,
)

SDK_NAME = "tukifact-python"
SDK_VERSION = "0.1.0"
DEFAULT_BASE_URL = "https://api.tukifact.net.pe"
SANDBOX_BASE_URL = "https://sandbox.tukifact.net.pe"


# ─── Errors ───────────────────────────────────────────────────────────────────


class TukiFactError(Exception):
    """Raised when the TukiFact API returns an error response."""

    def __init__(
        self,
        message: str,
        status_code: int,
        details: Optional[dict[str, Any]] = None,
    ) -> None:
        super().__init__(message)
        self.status_code = status_code
        self.details = details

    def __repr__(self) -> str:
        return f"TukiFactError(status_code={self.status_code}, message={str(self)!r})"


# ─── Key conversion helpers ───────────────────────────────────────────────────


def _to_camel(name: str) -> str:
    """Convert snake_case → camelCase."""
    parts = name.split("_")
    return parts[0] + "".join(p.title() for p in parts[1:])


def _to_snake(name: str) -> str:
    """Convert camelCase → snake_case."""
    s1 = re.sub(r"(.)([A-Z][a-z]+)", r"\1_\2", name)
    return re.sub(r"([a-z0-9])([A-Z])", r"\1_\2", s1).lower()


def _serialize(obj: Any) -> Any:
    """Recursively convert a dataclass (or nested structure) to a camelCase dict."""
    if hasattr(obj, "__dataclass_fields__"):
        return {
            _to_camel(k): _serialize(v)
            for k, v in obj.__dict__.items()
            if v is not None
        }
    if isinstance(obj, list):
        return [_serialize(i) for i in obj]
    return obj


# ─── Deserializers ────────────────────────────────────────────────────────────


def _deserialize_item(d: dict[str, Any]) -> DocumentItem:
    return DocumentItem(
        description=d["description"],
        quantity=d["quantity"],
        unit_price=d["unitPrice"],
        unit_code=d.get("unitCode"),
        igv_rate=d.get("igvRate"),
        discount=d.get("discount"),
    )


def _deserialize_document(d: dict[str, Any]) -> Document:
    return Document(
        id=d["id"],
        type=d["type"],
        series=d["series"],
        correlative=d["correlative"],
        full_number=d["fullNumber"],
        customer_id=d["customerId"],
        issue_date=d["issueDate"],
        currency=d["currency"],
        subtotal=d["subtotal"],
        igv=d["igv"],
        total=d["total"],
        status=d["status"],
        items=[_deserialize_item(i) for i in d.get("items", [])],
        created_at=d["createdAt"],
        updated_at=d["updatedAt"],
        due_date=d.get("dueDate"),
        notes=d.get("notes"),
        reference_document_id=d.get("referenceDocumentId"),
    )


def _deserialize_document_response(d: dict[str, Any]) -> DocumentResponse:
    sunat_raw = d.get("sunat")
    sunat = (
        SunatResponse(
            accepted=sunat_raw["accepted"],
            code=sunat_raw.get("code"),
            description=sunat_raw.get("description"),
            observations=sunat_raw.get("observations", []),
        )
        if sunat_raw
        else None
    )
    return DocumentResponse(document=_deserialize_document(d["document"]), sunat=sunat)


def _deserialize_customer(d: dict[str, Any]) -> Customer:
    return Customer(
        id=d["id"],
        document_type=d["documentType"],
        document_number=d["documentNumber"],
        legal_name=d["legalName"],
        created_at=d["createdAt"],
        updated_at=d["updatedAt"],
        trade_name=d.get("tradeName"),
        address=d.get("address"),
        district=d.get("district"),
        province=d.get("province"),
        department=d.get("department"),
        email=d.get("email"),
        phone=d.get("phone"),
    )


def _deserialize_series(d: dict[str, Any]) -> Series:
    return Series(
        id=d["id"],
        type=d["type"],
        prefix=d["prefix"],
        current_correlative=d["currentCorrelative"],
        is_active=d["isActive"],
    )


def _parse_paginated(raw: dict[str, Any]) -> Pagination:
    return Pagination(
        page=raw["page"],
        page_size=raw["pageSize"],
        total_count=raw["totalCount"],
        total_pages=raw["totalPages"],
    )


# ─── Client ───────────────────────────────────────────────────────────────────


class TukiFactClient:
    """
    Synchronous TukiFact API client.

    Usage::

        client = TukiFactClient(api_key="YOUR_KEY")
        response = client.create_document(...)

        # or as a context manager
        with TukiFactClient(api_key="YOUR_KEY") as client:
            docs = client.list_documents()
    """

    def __init__(
        self,
        api_key: str,
        base_url: Optional[str] = None,
        version: str = "v1",
        timeout: float = 30.0,
        sandbox: bool = False,
    ) -> None:
        resolved_base = SANDBOX_BASE_URL if sandbox else (base_url or DEFAULT_BASE_URL)
        self._client = httpx.Client(
            base_url=f"{resolved_base}/api/{version}",
            headers={
                "Authorization": f"Bearer {api_key}",
                "Content-Type": "application/json",
                "X-SDK": SDK_NAME,
                "X-SDK-Version": SDK_VERSION,
            },
            timeout=timeout,
        )

    def __enter__(self) -> "TukiFactClient":
        return self

    def __exit__(self, *_: Any) -> None:
        self.close()

    def close(self) -> None:
        """Close the underlying HTTP connection pool."""
        self._client.close()

    # ── Internal helpers ──────────────────────────────────────────────────────

    def _request(self, method: str, path: str, **kwargs: Any) -> Any:
        try:
            response = self._client.request(method, path, **kwargs)
        except httpx.TimeoutException as exc:
            raise TukiFactError("Request timed out", 408) from exc
        except httpx.RequestError as exc:
            raise TukiFactError(str(exc), 0) from exc

        if not response.is_success:
            details: Optional[dict[str, Any]] = None
            try:
                details = response.json()
            except Exception:
                pass
            message = (details or {}).get("message", f"HTTP {response.status_code}")
            raise TukiFactError(message, response.status_code, details)

        return response.json()

    def _request_raw(self, path: str) -> bytes:
        try:
            response = self._client.get(path)
        except httpx.TimeoutException as exc:
            raise TukiFactError("Request timed out", 408) from exc

        if not response.is_success:
            raise TukiFactError(f"HTTP {response.status_code}", response.status_code)

        return response.content

    # ── Documents ─────────────────────────────────────────────────────────────

    def create_document(self, data: DocumentCreateRequest) -> DocumentResponse:
        raw = self._request("POST", "/documents", json=_serialize(data))
        return _deserialize_document_response(raw)

    def get_document(self, document_id: str) -> DocumentResponse:
        raw = self._request("GET", f"/documents/{document_id}")
        return _deserialize_document_response(raw)

    def list_documents(
        self,
        page: int = 1,
        page_size: int = 20,
        type: Optional[str] = None,
        status: Optional[str] = None,
    ) -> PaginatedResponse[Document]:
        params: dict[str, Any] = {"page": page, "pageSize": page_size}
        if type:
            params["type"] = type
        if status:
            params["status"] = status
        raw = self._request("GET", "/documents", params=params)
        pagination = _parse_paginated(raw)
        return PaginatedResponse(
            data=[_deserialize_document(d) for d in raw["data"]],
            page=pagination.page,
            page_size=pagination.page_size,
            total_count=pagination.total_count,
            total_pages=pagination.total_pages,
        )

    def get_document_pdf(self, document_id: str) -> bytes:
        """Download the PDF for a document. Returns raw bytes."""
        return self._request_raw(f"/documents/{document_id}/pdf")

    def get_document_xml(self, document_id: str) -> bytes:
        """Download the signed XML (CDR) for a document. Returns raw bytes."""
        return self._request_raw(f"/documents/{document_id}/xml")

    def void_document(self, document_id: str, reason: str) -> DocumentResponse:
        raw = self._request(
            "POST", f"/documents/{document_id}/void", json={"reason": reason}
        )
        return _deserialize_document_response(raw)

    # ── Customers ─────────────────────────────────────────────────────────────

    def list_customers(
        self,
        page: int = 1,
        page_size: int = 20,
        search: Optional[str] = None,
    ) -> PaginatedResponse[Customer]:
        params: dict[str, Any] = {"page": page, "pageSize": page_size}
        if search:
            params["search"] = search
        raw = self._request("GET", "/customers", params=params)
        pagination = _parse_paginated(raw)
        return PaginatedResponse(
            data=[_deserialize_customer(c) for c in raw["data"]],
            page=pagination.page,
            page_size=pagination.page_size,
            total_count=pagination.total_count,
            total_pages=pagination.total_pages,
        )

    # ── Series ────────────────────────────────────────────────────────────────

    def list_series(self) -> list[Series]:
        raw = self._request("GET", "/series")
        return [_deserialize_series(s) for s in raw]

    # ── Health ────────────────────────────────────────────────────────────────

    def health(self) -> dict[str, str]:
        return self._request("GET", "/health")

"""TukiFact Python SDK — Facturación Electrónica para Perú."""

from typing import Any, Optional
import httpx

from .types import (
    CreateDocumentRequest, CreateDocumentItem, DocumentResponse,
    DocumentItemResponse, CreateCreditNoteRequest, DashboardResponse,
    DashboardSummary, SeriesResponse,
)


def _to_camel(s: str) -> str:
    parts = s.split("_")
    return parts[0] + "".join(p.capitalize() for p in parts[1:])


def _serialize(obj: Any) -> Any:
    if isinstance(obj, list):
        return [_serialize(i) for i in obj]
    if hasattr(obj, "__dataclass_fields__"):
        return {_to_camel(k): _serialize(v) for k, v in obj.__dict__.items() if v is not None}
    return obj


class TukiFactClient:
    """Client for TukiFact Electronic Invoicing API."""

    def __init__(
        self,
        base_url: str = "https://tukifact.net.pe",
        api_key: Optional[str] = None,
        access_token: Optional[str] = None,
        tenant_id: Optional[str] = None,
        timeout: float = 30.0,
    ):
        self.base_url = base_url.rstrip("/")
        self.api_key = api_key
        self.access_token = access_token
        self.tenant_id = tenant_id
        self._client = httpx.Client(base_url=self.base_url, timeout=timeout)

    def _headers(self) -> dict[str, str]:
        h: dict[str, str] = {"Content-Type": "application/json"}
        if self.access_token:
            h["Authorization"] = f"Bearer {self.access_token}"
        if self.api_key:
            h["X-Api-Key"] = self.api_key
        if self.tenant_id:
            h["X-Tenant-Id"] = self.tenant_id
        return h

    def _request(self, method: str, path: str, json: Any = None) -> Any:
        res = self._client.request(method, path, headers=self._headers(), json=json)
        if res.status_code >= 400:
            error = res.json() if res.headers.get("content-type", "").startswith("application/json") else {"error": f"HTTP {res.status_code}"}
            raise Exception(error.get("error", f"HTTP {res.status_code}"))
        if res.status_code == 204:
            return None
        return res.json()

    # === Auth ===
    def login(self, email: str, password: str, tenant_id: str) -> dict:
        data = self._request("POST", "/v1/auth/login", {"email": email, "password": password, "tenantId": tenant_id})
        self.access_token = data["accessToken"]
        self.tenant_id = data["user"]["tenantId"]
        return data

    # === Documents ===
    def emit_document(self, request: CreateDocumentRequest) -> DocumentResponse:
        data = self._request("POST", "/v1/documents", _serialize(request))
        return self._parse_document(data)

    def emit_credit_note(self, request: CreateCreditNoteRequest) -> DocumentResponse:
        data = self._request("POST", "/v1/documents/credit-note", _serialize(request))
        return self._parse_document(data)

    def get_document(self, doc_id: str) -> DocumentResponse:
        data = self._request("GET", f"/v1/documents/{doc_id}")
        return self._parse_document(data)

    def list_documents(self, page: int = 1, page_size: int = 20, **filters: Any) -> dict:
        params = {"page": page, "pageSize": page_size, **{k: v for k, v in filters.items() if v is not None}}
        qs = "&".join(f"{k}={v}" for k, v in params.items())
        return self._request("GET", f"/v1/documents?{qs}")

    def download_pdf(self, doc_id: str) -> bytes:
        res = self._client.get(f"/v1/documents/{doc_id}/pdf", headers=self._headers())
        res.raise_for_status()
        return res.content

    def download_xml(self, doc_id: str) -> bytes:
        res = self._client.get(f"/v1/documents/{doc_id}/xml", headers=self._headers())
        res.raise_for_status()
        return res.content

    # === Dashboard ===
    def get_dashboard(self) -> DashboardResponse:
        d = self._request("GET", "/v1/dashboard")
        return DashboardResponse(
            today=DashboardSummary(**{_to_snake(k): v for k, v in d["today"].items()}),
            this_month=DashboardSummary(**{_to_snake(k): v for k, v in d["thisMonth"].items()}),
            this_year=DashboardSummary(**{_to_snake(k): v for k, v in d["thisYear"].items()}),
        )

    # === Series ===
    def list_series(self) -> list[SeriesResponse]:
        data = self._request("GET", "/v1/series")
        return [SeriesResponse(**{_to_snake(k): v for k, v in s.items()}) for s in data]

    def create_series(self, document_type: str, serie: str) -> SeriesResponse:
        data = self._request("POST", "/v1/series", {"documentType": document_type, "serie": serie})
        return SeriesResponse(**{_to_snake(k): v for k, v in data.items()})

    # === Void ===
    def void_document(self, document_id: str, reason: str) -> dict:
        return self._request("POST", "/v1/voided-documents", {"documentId": document_id, "voidReason": reason})

    # === Plans ===
    def list_plans(self) -> list[dict]:
        return self._request("GET", "/v1/plans")

    # === Helpers ===
    def _parse_document(self, data: dict) -> DocumentResponse:
        items = [DocumentItemResponse(**{_to_snake(k): v for k, v in i.items()}) for i in data.get("items", [])]
        return DocumentResponse(
            id=data["id"], document_type=data["documentType"], document_type_name=data["documentTypeName"],
            serie=data["serie"], correlative=data["correlative"], full_number=data["fullNumber"],
            issue_date=data["issueDate"], currency=data["currency"],
            customer_doc_type=data["customerDocType"], customer_doc_number=data["customerDocNumber"],
            customer_name=data["customerName"], operacion_gravada=data["operacionGravada"],
            igv=data["igv"], total=data["total"], status=data["status"], items=items,
            sunat_response_code=data.get("sunatResponseCode"),
            sunat_response_description=data.get("sunatResponseDescription"),
            hash_code=data.get("hashCode"), xml_url=data.get("xmlUrl"),
            created_at=data.get("createdAt"),
        )

    def close(self):
        self._client.close()

    def __enter__(self): return self
    def __exit__(self, *_): self.close()


def _to_snake(s: str) -> str:
    import re
    return re.sub(r"(?<!^)(?=[A-Z])", "_", s).lower()

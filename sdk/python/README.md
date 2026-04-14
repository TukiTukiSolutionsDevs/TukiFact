# tukifact

Official Python SDK for the [TukiFact](https://tukifact.net.pe) electronic invoicing API (SUNAT, Peru).

## Install

```bash
pip install tukifact
```

## Quick Start

```python
from tukifact import TukiFactClient, DocumentCreateRequest, DocumentItem

client = TukiFactClient(api_key="YOUR_API_KEY")

response = client.create_document(
    DocumentCreateRequest(
        type="factura",
        series="F001",
        customer_id="cust_abc123",
        issue_date="2026-04-14",
        currency="PEN",
        items=[
            DocumentItem(
                description="Servicio de consultoría",
                quantity=1,
                unit_price=1000.0,
            )
        ],
    )
)

print(response.document.full_number)  # F001-00000001
```

## Context Manager

```python
with TukiFactClient(api_key="YOUR_API_KEY") as client:
    result = client.list_documents(page=1, page_size=10)
    print(result.total_count)
```

## Sandbox

```python
client = TukiFactClient(api_key="YOUR_SANDBOX_KEY", sandbox=True)
```

## Config Options

| Parameter  | Type    | Default                       | Description               |
|------------|---------|-------------------------------|---------------------------|
| `api_key`  | str     | required                      | Your TukiFact API key     |
| `base_url` | str     | `https://api.tukifact.net.pe` | Override the API base URL |
| `version`  | str     | `"v1"`                        | API version               |
| `timeout`  | float   | `30.0`                        | Request timeout (seconds) |
| `sandbox`  | bool    | `False`                       | Use sandbox environment   |

## Error Handling

```python
from tukifact import TukiFactClient, TukiFactError

try:
    client.get_document("nonexistent-id")
except TukiFactError as e:
    print(e.status_code)   # e.g. 404
    print(str(e))          # error message
    print(e.details)       # raw API error dict, if available
```

## Download PDF / XML

```python
pdf_bytes = client.get_document_pdf("doc_abc123")
with open("factura.pdf", "wb") as f:
    f.write(pdf_bytes)

xml_bytes = client.get_document_xml("doc_abc123")
```

## Async (coming soon)

An `AsyncTukiFactClient` built on `httpx.AsyncClient` is planned for a future release.

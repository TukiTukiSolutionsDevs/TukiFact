# tukifact

SDK oficial de TukiFact para Python — Facturación Electrónica para Perú.

## Instalación

```bash
pip install tukifact
```

## Uso Rápido

```python
from tukifact import TukiFactClient, CreateDocumentRequest, CreateDocumentItem

client = TukiFactClient(
    base_url="https://tukifact.net.pe",
    api_key="tk_your_api_key_here",
    tenant_id="your-tenant-uuid",
)

# Emitir factura
factura = client.emit_document(CreateDocumentRequest(
    document_type="01",
    serie="F001",
    customer_doc_type="6",
    customer_doc_number="20100047218",
    customer_name="CLIENTE SAC",
    items=[CreateDocumentItem(
        description="Servicio de consultoría",
        quantity=1,
        unit_price=1000,
    )],
))

print(f"{factura.full_number} — {factura.status} — S/ {factura.total}")
```

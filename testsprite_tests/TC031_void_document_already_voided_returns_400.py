import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
VOIDED_DOCUMENTS_ENDPOINT = "/v1/voided-documents"

TENANT_AUTH_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}

TIMEOUT = 30

def test_void_document_already_voided_returns_400():
    # Step 1: Authenticate as tenant and get access token
    login_resp = requests.post(
        BASE_URL + LOGIN_ENDPOINT,
        json=TENANT_AUTH_PAYLOAD,
        timeout=TIMEOUT
    )
    assert login_resp.status_code == 200, f"Login failed: {login_resp.text}"
    login_json = login_resp.json()
    access_token = login_json.get("accessToken")
    assert access_token and access_token.startswith("eyJ"), "Invalid accessToken"
    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: Emit a new factura document to later void it (to replicate TC030 resource creation)
    # Create customer
    customer_payload = {
        "docType": "6",
        "docNumber": f"20888999{int(time.time()) % 10000:04}",
        "name": f"Cliente Void Test {int(time.time())}",
        "email": f"clientevoid{int(time.time())}@test.pe",
        "phone": "011234567",
        "address": "Av Test Void 123",
        "ubigeo": "150101",
        "departamento": "Lima",
        "provincia": "Lima",
        "distrito": "Lima",
        "category": "frecuente",
        "notes": "Cliente para prueba de anulación"
    }
    customer_resp = requests.post(
        BASE_URL + "/v1/customers",
        headers=headers,
        json=customer_payload,
        timeout=TIMEOUT
    )
    assert customer_resp.status_code == 201, f"Customer creation failed: {customer_resp.text}"
    customer_id = customer_resp.json().get("id")
    assert customer_id, "Customer ID missing"

    # Create product
    product_payload = {
        "code": f"PRDVOID{int(time.time()) % 10000:04}",
        "description": f"Producto Void Test {int(time.time())}",
        "unitMeasure": "NIU",
        "price": 100.00,
        "taxType": "10"
    }
    product_resp = requests.post(
        BASE_URL + "/v1/products",
        headers=headers,
        json=product_payload,
        timeout=TIMEOUT
    )
    assert product_resp.status_code == 201, f"Product creation failed: {product_resp.text}"
    product_id = product_resp.json().get("id")
    assert product_id, "Product ID missing"

    # Get series for Factura "01" - expected series F001 with id = 56d175cb-df4d-46b7-a762-02a8e01a0adf in PRD but will query and pick F001 series
    series_resp = requests.get(BASE_URL + "/v1/series", headers=headers, timeout=TIMEOUT)
    assert series_resp.status_code == 200, f"Failed to get series: {series_resp.text}"
    series_list = series_resp.json()
    factura_serie = None
    for s in series_list:
        if s.get("serie") == "F001" and s.get("documentType") == "01":
            factura_serie = s.get("serie")
            break
    assert factura_serie == "F001", "Factura serie F001 not found"

    # Emit factura document
    doc_payload = {
        "documentType": "01",
        "serie": "F001",
        "currency": "PEN",
        "customerDocType": "6",
        "customerDocNumber": customer_payload["docNumber"],
        "customerName": customer_payload["name"],
        "customerAddress": customer_payload["address"],
        "customerEmail": customer_payload["email"],
        "notes": "Documento para prueba de anulación",
        "items": [
            {
                "description": product_payload["description"],
                "quantity": 1,
                "unitPrice": product_payload["price"],
                "unitMeasure": product_payload["unitMeasure"],
                "igvType": product_payload["taxType"]
            }
        ]
    }
    doc_resp = requests.post(
        BASE_URL + "/v1/documents",
        headers=headers,
        json=doc_payload,
        timeout=TIMEOUT
    )
    assert doc_resp.status_code == 201, f"Document creation failed: {doc_resp.text}"
    doc_json = doc_resp.json()
    doc_id = doc_json.get("id")
    assert doc_id, "Document ID missing"

    voided_doc_id = None
    ticket_number = None

    try:
        # Step 3: Void the emitted document - first time (TC030)
        void_payload = {
            "documentId": doc_id,
            "voidReason": "Error en la emisión del comprobante"
        }
        void_resp = requests.post(
            BASE_URL + VOIDED_DOCUMENTS_ENDPOINT,
            headers=headers,
            json=void_payload,
            timeout=TIMEOUT
        )
        assert void_resp.status_code == 201, f"Void document initial failed: {void_resp.text}"
        void_json = void_resp.json()
        ticket_number = void_json.get("ticketNumber")
        voided_doc_id = doc_id
        assert ticket_number and ticket_number.startswith("RA-"), "Invalid ticketNumber"
        assert void_json.get("status") == "pending", "Unexpected status on void document"

        # Step 4: Attempt to void the same document again (TC031 test focus)
        void_payload_second = {
            "documentId": voided_doc_id,
            "voidReason": "Segundo intento"
        }
        void_resp_second = requests.post(
            BASE_URL + VOIDED_DOCUMENTS_ENDPOINT,
            headers=headers,
            json=void_payload_second,
            timeout=TIMEOUT
        )
        assert void_resp_second.status_code == 400, "Expected 400 Bad Request when voiding already voided document"
        error_json = void_resp_second.json()
        error_msg = error_json.get("error", "")
        assert "solo se pueden anular documentos aceptados" in error_msg.lower(), \
            f"Expected error message with 'solo se pueden anular documentos aceptados', got: {error_msg}"

    finally:
        # Cleanup: Attempt to delete the document if API supports (best effort, ignoring failure)
        # Usually deleting documents not supported, so we skip or pass
        pass

test_void_document_already_voided_returns_400()

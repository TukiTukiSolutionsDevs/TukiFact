import requests
import uuid

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
DOCUMENTS_URL = f"{BASE_URL}/v1/documents"
VOIDED_DOCUMENTS_URL = f"{BASE_URL}/v1/voided-documents"

AUTH_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}

HEADERS_JSON = {
    "Content-Type": "application/json"
}

def test_post_v1_voided_documents_void_accepted_document():
    timeout = 30
    # Authenticate and get access token
    auth_resp = requests.post(LOGIN_URL, json=AUTH_PAYLOAD, timeout=timeout)
    assert auth_resp.status_code == 200, f"Authentication failed: {auth_resp.text}"
    access_token = auth_resp.json().get("accessToken")
    assert access_token, "No accessToken received"

    headers_auth = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Create a new accepted document (Factura type 01)
    # Use unique serie and unique customerDocNumber to avoid conflicts
    unique_suffix = uuid.uuid4().hex[:8].upper()
    document_payload = {
        "request": {
            "documentType": "01",
            "serie": "F001",
            "currency": "PEN",
            "customerDocType": "6",
            # Use a valid unique customerDocNumber (e.g. RUC)
            "customerDocNumber": f"2010007{unique_suffix[:5]}",  
            "customerName": "Cliente Test SAC",
            "customerEmail": f"cliente{unique_suffix}@test.com",
            "items": [
                {
                    "description": "Servicio de consultoria",
                    "quantity": 1,
                    "unitPrice": 100.00,
                    "igvType": "gravado",
                    "unitMeasure": "NIU"
                }
            ]
        }
    }

    document_resp = requests.post(DOCUMENTS_URL, json=document_payload, headers=headers_auth, timeout=timeout)
    assert document_resp.status_code == 201, f"Document creation failed: {document_resp.text}"
    document_data = document_resp.json()
    document_id = document_data.get("documentId")
    status = document_data.get("status")
    assert document_id and status == "accepted", "Document not accepted or documentId missing"

    # Attempt to void the accepted document
    void_payload = {
        "documentId": document_id,
        "voidReason": "Error en documento"
    }
    try:
        void_resp = requests.post(VOIDED_DOCUMENTS_URL, json=void_payload, headers=headers_auth, timeout=timeout)
        assert void_resp.status_code == 201, f"Voiding document failed: {void_resp.text}"
        void_data = void_resp.json()
        voided_document_id = void_data.get("voidedDocumentId")
        ticket = void_data.get("ticket")
        assert voided_document_id, "voidedDocumentId missing in response"
        assert ticket, "ticket missing in response"
    finally:
        pass

test_post_v1_voided_documents_void_accepted_document()
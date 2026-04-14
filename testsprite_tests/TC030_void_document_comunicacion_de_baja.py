import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
DOCUMENTS_ENDPOINT = "/v1/documents"
VOIDED_DOCUMENTS_ENDPOINT = "/v1/voided-documents"
TIMEOUT = 30


def test_void_document_comunicacion_de_baja():
    # Step 1: Authenticate and get access token
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        login_resp = requests.post(
            f"{BASE_URL}{LOGIN_ENDPOINT}",
            json=login_payload,
            timeout=TIMEOUT
        )
        login_resp.raise_for_status()
        login_data = login_resp.json()
        assert "accessToken" in login_data and login_data["accessToken"].startswith("eyJ"), "accessToken missing or invalid"
        access_token = login_data["accessToken"]
    except Exception as e:
        assert False, f"Login failed: {e}"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: Emit a new factura document
    document_payload = {
        "documentType": "01",
        "serie": "F001",
        "currency": "PEN",
        "customerDocType": "6",
        "customerDocNumber": "20100070970",
        "customerName": "Para Anular SAC",
        "items": [
            {
                "description": "Item para anular",
                "quantity": 1,
                "unitPrice": 100.00,
                "unitMeasure": "ZZ",
                "igvType": "10"
            }
        ]
    }

    document_id = None
    ticket_number = None
    try:
        emit_resp = requests.post(
            f"{BASE_URL}{DOCUMENTS_ENDPOINT}",
            json=document_payload,
            headers=headers,
            timeout=TIMEOUT
        )
        emit_resp.raise_for_status()
        emit_data = emit_resp.json()
        # Validate response document id and documentType
        document_id = emit_data.get("id", None)
        assert document_id is not None, "Document ID not returned"
        assert emit_data.get("documentType") == "01", "Document type mismatch"
    except Exception as e:
        assert False, f"Failed to emit factura document: {e}"

    assert document_id is not None, "Document ID not set, cannot continue"

    # Step 3: Post to voided-documents with documentId and voidReason
    void_payload = {
        "documentId": document_id,
        "voidReason": "Error en la emisión del comprobante"
    }

    try:
        void_resp = requests.post(
            f"{BASE_URL}{VOIDED_DOCUMENTS_ENDPOINT}",
            json=void_payload,
            headers=headers,
            timeout=TIMEOUT
        )
        void_resp.raise_for_status()
        # Expected status 201 Created
        assert void_resp.status_code == 201, f"Expected 201 Created but got {void_resp.status_code}"
        void_data = void_resp.json()
        ticket_number = void_data.get("ticketNumber", "")
        status = void_data.get("status", "")
        assert ticket_number.startswith("RA-"), f"ticketNumber does not start with 'RA-': {ticket_number}"
        assert status == "pending", f"Status is not 'pending': {status}"
    except Exception as e:
        assert False, f"Voided document creation failed: {e}"
    finally:
        # Cleanup: Attempt to delete document to keep environment clean if API supports deletion
        # The PRD doesn't mention delete document endpoint explicitly, so skip cleanup.
        pass

    # Optionally print or return ticket_number for further use
    return ticket_number


test_void_document_comunicacion_de_baja()
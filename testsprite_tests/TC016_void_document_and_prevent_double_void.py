import requests
import uuid

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
DOCUMENTS_URL = f"{BASE_URL}/v1/documents"
VOIDED_DOCUMENTS_URL = f"{BASE_URL}/v1/voided-documents"

LOGIN_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}

TIMEOUT = 30


def test_void_document_and_prevent_double_void():
    # Login first to get the Bearer token
    login_resp = requests.post(LOGIN_URL, json=LOGIN_PAYLOAD, timeout=TIMEOUT)
    assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
    login_data = login_resp.json()
    access_token = login_data.get("accessToken")
    assert access_token and isinstance(access_token, str) and access_token.strip() != "", "Invalid accessToken"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Create a unique "customerDocNumber" using UUID to prevent conflicts
    unique_suffix = str(uuid.uuid4())[:8]
    doc_body = {
        "documentType": "01",
        "serie": "F001",
        "currency": "PEN",
        "customerDocType": "6",
        "customerDocNumber": f"2010007097{unique_suffix[-1]}",  # Keep length realistic if needed
        "customerName": "Para Anular",
        "items": [
            {
                "description": "Item",
                "quantity": 1,
                "unitPrice": 10.00,
                "unitMeasure": "ZZ",
                "igvType": "10"
            }
        ]
    }

    document_id = None
    try:
        # Emit new document
        emit_resp = requests.post(DOCUMENTS_URL, json=doc_body, headers=headers, timeout=TIMEOUT)
        assert emit_resp.status_code == 201, f"Document emission failed: HTTP {emit_resp.status_code} {emit_resp.text}"
        emit_data = emit_resp.json()
        document_id = emit_data.get("id")
        assert document_id and isinstance(document_id, str), "Missing or invalid document id"

        # Void the document - first attempt
        void_body = {
            "documentId": document_id,
            "voidReason": "Error en emision"
        }
        void_resp_1 = requests.post(VOIDED_DOCUMENTS_URL, json=void_body, headers=headers, timeout=TIMEOUT)
        assert void_resp_1.status_code == 201, f"First void attempt failed: HTTP {void_resp_1.status_code} {void_resp_1.text}"
        void_data_1 = void_resp_1.json()
        ticket_number = void_data_1.get("ticketNumber")
        assert ticket_number and isinstance(ticket_number, str) and ticket_number.strip() != "", "Missing or invalid ticketNumber"

        # Void the same document again - should fail
        void_resp_2 = requests.post(VOIDED_DOCUMENTS_URL, json=void_body, headers=headers, timeout=TIMEOUT)
        assert void_resp_2.status_code == 400, f"Second void attempt did not fail as expected: HTTP {void_resp_2.status_code} {void_resp_2.text}"

    finally:
        # Cleanup: Try deleting the document if API supports DELETE (not specified in PRD)
        # Since no delete endpoint was specified, skip cleanup.
        # If delete endpoint existed, use it here.
        pass


test_void_document_and_prevent_double_void()
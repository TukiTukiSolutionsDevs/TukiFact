import requests

BASE_URL = "http://localhost:5186"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
CREDIT_NOTE_URL = f"{BASE_URL}/v1/documents/credit-note"
FACTURA_URL = f"{BASE_URL}/v1/documents"

LOGIN_PAYLOAD = {
    "email": "admin@tukitest.pe",
    "password": "TestSprite2026!",
    "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
}

def test_emit_credit_note_against_factura():
    timeout = 30

    # Step 1: Login to get Bearer token
    login_resp = requests.post(LOGIN_URL, json=LOGIN_PAYLOAD, timeout=timeout)
    assert login_resp.status_code == 200, f"Login failed: {login_resp.text}"
    login_data = login_resp.json()
    access_token = login_data.get("accessToken")
    assert access_token and isinstance(access_token, str), "No accessToken in login response"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Step 2: Emit a factura with multiple items to get the document ID (simulate TC010)
    factura_payload = {
        "documentType": "01",
        "serie": "F001",
        "currency": "PEN",
        "customerDocType": "6",
        "customerDocNumber": "20100070970",
        "customerName": "Cliente Test SAC",
        "items": [
            {
                "description": "Servicio A",
                "quantity": 2,
                "unitPrice": 50.00,
                "unitMeasure": "ZZ",
                "igvType": "10"
            },
            {
                "description": "Producto B",
                "quantity": 1,
                "unitPrice": 100.00,
                "unitMeasure": "NIU",
                "igvType": "10"
            }
        ]
    }

    factura_resp = requests.post(FACTURA_URL, headers=headers, json=factura_payload, timeout=timeout)
    assert factura_resp.status_code == 201, f"Emit Factura failed: {factura_resp.text}"
    factura_data = factura_resp.json()
    document_id = factura_data.get("id")
    assert document_id and isinstance(document_id, str), "Factura id not present"
    full_number = factura_data.get("fullNumber")
    assert full_number and full_number.startswith("F001-"), "Factura fullNumber invalid"
    items = factura_data.get("items")
    assert isinstance(items, list) and len(items) == 2, "Factura items count mismatch"
    total = factura_data.get("total")
    assert total == 236.0, f"Factura total incorrect, got {total}"

    try:
        # Step 3: Emit credit note against the saved factura document ID
        credit_note_payload = {
            "serie": "FC01",
            "referenceDocumentId": document_id,
            "creditNoteReason": "01",
            "description": "Anulacion de operacion",
            "currency": "PEN",
            "items": [
                {
                    "description": "Servicio A",
                    "quantity": 2,
                    "unitPrice": 50.00,
                    "unitMeasure": "ZZ",
                    "igvType": "10"
                }
            ]
        }

        credit_note_resp = requests.post(CREDIT_NOTE_URL, headers=headers, json=credit_note_payload, timeout=timeout)
        assert credit_note_resp.status_code == 201, f"Credit note emission failed: {credit_note_resp.text}"
        credit_note_data = credit_note_resp.json()
        cn_full_number = credit_note_data.get("fullNumber")
        assert cn_full_number and cn_full_number.startswith("FC01-"), "Credit note fullNumber invalid"

    finally:
        # Cleanup: Delete created factura to keep the environment clean
        if document_id:
            del_headers = headers.copy()
            del_url = f"{FACTURA_URL}/{document_id}"
            try:
                requests.delete(del_url, headers=del_headers, timeout=timeout)
            except Exception:
                pass

test_emit_credit_note_against_factura()
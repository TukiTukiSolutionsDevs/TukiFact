import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_URL = f"{BASE_URL}/v1/auth/login"
DOCUMENTS_URL = f"{BASE_URL}/v1/documents"
DEBIT_NOTE_URL = f"{BASE_URL}/v1/documents/debit-note"
TIMEOUT = 30

def test_emit_debit_note_08_penalidad():
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }

    # Authenticate to obtain accessToken
    response = requests.post(LOGIN_URL, json=login_payload, timeout=TIMEOUT)
    assert response.status_code == 200, f"Login failed with status {response.status_code}"
    token_data = response.json()
    access_token = token_data.get("accessToken")
    assert access_token and access_token.startswith("eyJ"), "Invalid accessToken received"

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Emit a fresh factura first
    factura_payload = {
        "documentType": "01",
        "serie": "F001",
        "currency": "PEN",
        "customerDocType": "6",
        "customerDocNumber": "20100070970",
        "customerName": "Debit Test SAC",
        "items": [
            {
                "description": "Servicio base",
                "quantity": 1,
                "unitPrice": 1000.00,
                "unitMeasure": "ZZ",
                "igvType": "10"
            }
        ]
    }

    new_doc_id = None
    try:
        factura_resp = requests.post(DOCUMENTS_URL, json=factura_payload, headers=headers, timeout=TIMEOUT)
        assert factura_resp.status_code == 201, f"Factura creation failed with status {factura_resp.status_code}"
        factura_json = factura_resp.json()
        new_doc_id = factura_json.get("id")
        assert new_doc_id, "Factura response missing 'id' field"

        # Emit debit note referencing the new factura
        debit_note_payload = {
            "serie": "FD01",
            "referenceDocumentId": new_doc_id,
            "debitNoteReason": "02",
            "description": "Penalidad por atraso",
            "currency": "PEN",
            "items": [
                {
                    "description": "Penalidad contractual",
                    "quantity": 1,
                    "unitPrice": 200.00,
                    "unitMeasure": "ZZ",
                    "igvType": "10"
                }
            ]
        }

        debit_resp = requests.post(DEBIT_NOTE_URL, json=debit_note_payload, headers=headers, timeout=TIMEOUT)
        assert debit_resp.status_code == 201, f"Debit note creation failed with status {debit_resp.status_code}"
        debit_json = debit_resp.json()

        full_number = debit_json.get("fullNumber")
        document_type = debit_json.get("documentType")

        assert full_number and full_number.startswith("FD01-"), f"'fullNumber' invalid or missing: {full_number}"
        assert document_type == "08", f"'documentType' expected '08', got '{document_type}'"
    finally:
        # Cleanup: delete the created factura document if possible
        if new_doc_id:
            try:
                del_resp = requests.delete(f"{DOCUMENTS_URL}/{new_doc_id}", headers=headers, timeout=TIMEOUT)
                # Accept 204 as success; if 400 due to associated documents, that's OK per instructions
                assert del_resp.status_code in (204, 400), f"Failed to delete factura doc, status {del_resp.status_code}"
            except Exception:
                pass

test_emit_debit_note_08_penalidad()
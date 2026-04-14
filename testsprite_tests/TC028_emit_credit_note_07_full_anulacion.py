import requests

BASE_URL = "http://localhost:80"
LOGIN_PATH = "/v1/auth/login"
DOCUMENTS_PATH = "/v1/documents"
CREDIT_NOTE_PATH = "/v1/documents/credit-note"

LOGIN_PAYLOAD = {
    "email": "prdtest@test.pe",
    "password": "PrdTest2026!",
    "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
}

DOCUMENT_ID_FROM_TC017 = None  # This should be set to the actual document ID from TC017 before running the test

def test_emit_credit_note_07_full_anulacion():
    # Step 1: Authenticate tenant user to get access token
    try:
        login_resp = requests.post(
            f"{BASE_URL}{LOGIN_PATH}",
            json=LOGIN_PAYLOAD,
            timeout=30
        )
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}, body: {login_resp.text}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert isinstance(access_token, str) and access_token.startswith("eyJ"), "Invalid accessToken format"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Ensure DOCUMENT_ID_FROM_TC017 is provided
        assert DOCUMENT_ID_FROM_TC017 is not None, "Document ID from TC017 must be provided"

        credit_note_body = {
            "serie": "FC01",
            "referenceDocumentId": DOCUMENT_ID_FROM_TC017,
            "creditNoteReason": "01",
            "description": "Anulación de la operación completa",
            "currency": "PEN",
            "items": [
                {
                    "description": "Servicio de consultoría TI",
                    "quantity": 2,
                    "unitPrice": 500.00,
                    "unitMeasure": "ZZ",
                    "igvType": "10"
                },
                {
                    "description": "Licencia de software anual",
                    "quantity": 1,
                    "unitPrice": 1200.00,
                    "unitMeasure": "NIU",
                    "igvType": "10"
                },
                {
                    "description": "Capacitación técnica",
                    "quantity": 3,
                    "unitPrice": 150.00,
                    "unitMeasure": "ZZ",
                    "igvType": "10",
                    "discount": 50.00
                }
            ]
        }

        resp = requests.post(
            f"{BASE_URL}{CREDIT_NOTE_PATH}",
            headers=headers,
            json=credit_note_body,
            timeout=30
        )
        assert resp.status_code == 201, f"Expected 201 Created but got {resp.status_code}, body: {resp.text}"
        resp_json = resp.json()

        full_number = resp_json.get("fullNumber")
        document_type = resp_json.get("documentType")

        assert full_number is not None and full_number.startswith("FC01-"), f"fullNumber invalid or missing: {full_number}"
        assert document_type == "07", f"documentType expected '07', got {document_type}"

    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Test failed: {e}")

# Example to set the DOCUMENT_ID_FROM_TC017 before running the test
# This value must be replaced with the actual document id saved from TC017

DOCUMENT_ID_FROM_TC017 = "replace-with-actual-document-id-from-TC017"

test_emit_credit_note_07_full_anulacion()
import requests
import time

BASE_URL = "http://localhost:80"
LOGIN_ENDPOINT = "/v1/auth/login"
DOCUMENTS_ENDPOINT = "/v1/documents"
TIMEOUT = 30

def test_emit_boleta_03_with_dni_customer():
    # Step 1: Authenticate and get bearer token
    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    try:
        login_resp = requests.post(BASE_URL + LOGIN_ENDPOINT, json=login_payload, timeout=TIMEOUT)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}, body: {login_resp.text}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "Invalid accessToken"
    except Exception as e:
        raise AssertionError(f"Authentication request failed: {e}")

    headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Prepare document payload as per test case
    document_payload = {
        "documentType": "03",
        "serie": "B001",
        "currency": "PEN",
        "customerDocType": "1",
        "customerDocNumber": "71234567",
        "customerName": "Juan Pérez López",
        "items": [
            {
                "description": "Producto retail A",
                "quantity": 5,
                "unitPrice": 25.00,
                "unitMeasure": "NIU",
                "igvType": "10"
            },
            {
                "description": "Producto retail B",
                "quantity": 2,
                "unitPrice": 75.00,
                "unitMeasure": "NIU",
                "igvType": "10"
            }
        ]
    }

    doc_id = None
    try:
        # Step 2: Emit document (POST /v1/documents)
        resp = requests.post(BASE_URL + DOCUMENTS_ENDPOINT, headers=headers, json=document_payload, timeout=TIMEOUT)
        assert resp.status_code == 201, f"Document creation failed with status {resp.status_code}, body: {resp.text}"
        json_resp = resp.json()
        doc_id = json_resp.get("id")
        full_number = json_resp.get("fullNumber")
        doc_type = json_resp.get("documentType")
        total = json_resp.get("total")

        # Validate required fields
        assert doc_id, "Document id is missing"
        assert full_number and full_number.startswith("B001-"), f"fullNumber invalid: {full_number}"
        assert doc_type == "03", f"documentType expected '03', got '{doc_type}'"
        assert isinstance(total, (int, float)), "total must be a number"
        # Calculate expected total: (5*25 + 2*75) * 1.18 = (125 + 150) * 1.18 = 324.50
        expected_subtotal = 125 + 150
        expected_total = round(expected_subtotal * 1.18, 2)
        # Use ±0.01 tolerance for floating point calculation
        assert abs(total - expected_total) < 0.01, f"Total {total} differs from expected {expected_total}"
    finally:
        # Cleanup: delete the created document if possible
        if doc_id:
            try:
                # There is no DELETE /v1/documents/{id} documented, so skip deletion
                pass
            except Exception:
                pass

test_emit_boleta_03_with_dni_customer()
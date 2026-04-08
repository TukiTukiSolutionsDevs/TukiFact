import requests

BASE_URL = "http://localhost:5186"
LOGIN_ENDPOINT = "/v1/auth/login"
DOCUMENTS_ENDPOINT = "/v1/documents"
TIMEOUT = 30

def test_TC005_post_v1_documents_emit_new_factura_document():
    login_url = BASE_URL + LOGIN_ENDPOINT
    documents_url = BASE_URL + DOCUMENTS_ENDPOINT

    # Login payload and headers
    login_payload = {
        "email": "admin@tukitest.pe",
        "password": "TestSprite2026!",
        "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
    }
    headers = {
        "Content-Type": "application/json"
    }
    try:
        login_resp = requests.post(login_url, json=login_payload, headers=headers, timeout=TIMEOUT)
        login_resp.raise_for_status()
    except Exception as e:
        assert False, f"Login failed: {e}"
    login_data = login_resp.json()
    assert "accessToken" in login_data, "No accessToken in login response"
    access_token = login_data["accessToken"]

    # Prepare Authorization header
    auth_headers = {
        "Authorization": f"Bearer {access_token}",
        "Content-Type": "application/json"
    }

    # Prepare factura payload corrected igvType to 'gravado' string
    factura_payload = {
        "documentType": "01",
        "serie": "F001",
        "currency": "PEN",
        "customerDocType": "6",  # RUC customer doc type
        "customerDocNumber": "20100070970",
        "customerName": "Cliente Test SAC",
        "customerEmail": "cliente@test.com",
        "items": [
            {
                "description": "Servicio de consultoria",
                "quantity": 1,
                "unitPrice": 100.00,
                "igvType": "gravado",
                "unitMeasure": "ZZ"
            }
        ]
    }

    doc_id = None
    try:
        resp = requests.post(documents_url, json=factura_payload, headers=auth_headers, timeout=TIMEOUT)
        # Validate response status code
        assert resp.status_code == 201, f"Expected 201 Created but got {resp.status_code}"
        resp_json = resp.json()
        assert "documentId" in resp_json, "Response JSON missing 'documentId'"
        assert "status" in resp_json, "Response JSON missing 'status'"
        assert resp_json["status"].lower() == "accepted", f"Expected status 'accepted' but got {resp_json['status']}"
        doc_id = resp_json["documentId"]
    finally:
        # Cleanup: delete emitted document if doc_id present to keep test environment clean
        if doc_id:
            try:
                delete_url = f"{documents_url}/{doc_id}"
                del_resp = requests.delete(delete_url, headers=auth_headers, timeout=TIMEOUT)
                # No assertion on delete response (may not be allowed), just attempt cleanup
            except Exception:
                pass

test_TC005_post_v1_documents_emit_new_factura_document()

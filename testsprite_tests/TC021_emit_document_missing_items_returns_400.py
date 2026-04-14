import requests

def test_emit_document_missing_items_returns_400():
    base_url = "http://localhost:80"
    login_url = f"{base_url}/v1/auth/login"
    documents_url = f"{base_url}/v1/documents"

    login_payload = {
        "email": "prdtest@test.pe",
        "password": "PrdTest2026!",
        "tenantId": "b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }

    try:
        login_resp = requests.post(login_url, json=login_payload, timeout=30)
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_json = login_resp.json()
        access_token = login_json.get("accessToken")
        assert access_token is not None and access_token.startswith("eyJ"), "Invalid accessToken format"

        headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        document_payload = {
            "documentType": "01",
            "serie": "F001",
            "currency": "PEN",
            "customerDocType": "6",
            "customerDocNumber": "20100070970",
            "customerName": "Sin Items",
            "items": []
        }

        doc_resp = requests.post(documents_url, json=document_payload, headers=headers, timeout=30)
        assert doc_resp.status_code == 400, f"Expected 400 Bad Request but got {doc_resp.status_code}"
        resp_json = doc_resp.json()
        # Optionally check error message presence
        assert "error" in resp_json or "message" in resp_json, "Error message not found in response"

    except requests.RequestException as e:
        assert False, f"Request failed: {str(e)}"

test_emit_document_missing_items_returns_400()
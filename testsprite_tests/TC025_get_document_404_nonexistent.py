import requests

def test_get_document_404_nonexistent():
    base_url = "http://localhost:80"
    login_url = f"{base_url}/v1/auth/login"
    document_id = "00000000-0000-0000-0000-000000000000"
    document_url = f"{base_url}/v1/documents/{document_id}"
    login_payload = {
        "email":"prdtest@test.pe",
        "password":"PrdTest2026!",
        "tenantId":"b2ba4cad-45c9-4a98-96f9-bf2eb6c96572"
    }
    timeout = 30

    try:
        # Step 1: Authenticate and get access token
        login_resp = requests.post(login_url, json=login_payload, timeout=timeout)
        login_resp.raise_for_status()
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert access_token and access_token.startswith("eyJ"), "accessToken missing or invalid"

        # Step 2: GET document by non-existent ID with Bearer token
        headers = {"Authorization": f"Bearer {access_token}"}
        doc_resp = requests.get(document_url, headers=headers, timeout=timeout)
        assert doc_resp.status_code == 404, f"Expected 404 Not Found but got {doc_resp.status_code}"
    except requests.RequestException as e:
        raise AssertionError(f"HTTP request failed: {e}")

test_get_document_404_nonexistent()
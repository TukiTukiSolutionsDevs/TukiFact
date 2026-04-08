import requests

BASE_URL = "http://localhost:5186"
LOGIN_ENDPOINT = "/v1/auth/login"
VOIDED_DOCUMENTS_ENDPOINT = "/v1/voided-documents"
TIMEOUT = 30

def test_list_voided_documents():
    # Login credentials as per instruction
    login_payload = {
        "email": "admin@tukitest.pe",
        "password": "TestSprite2026!",
        "tenantId": "0c700fd7-1fdb-43d5-a2a8-d20fe4bcab84"
    }
    headers = {
        "Content-Type": "application/json"
    }

    try:
        # Authenticate and get Bearer token
        login_resp = requests.post(
            f"{BASE_URL}{LOGIN_ENDPOINT}",
            json=login_payload,
            headers=headers,
            timeout=TIMEOUT
        )
        assert login_resp.status_code == 200, f"Login failed with status {login_resp.status_code}"
        login_data = login_resp.json()
        access_token = login_data.get("accessToken")
        assert access_token and isinstance(access_token, str), "accessToken missing or invalid"

        auth_headers = {
            "Authorization": f"Bearer {access_token}",
            "Content-Type": "application/json"
        }

        # Call the list voided documents endpoint
        resp = requests.get(f"{BASE_URL}{VOIDED_DOCUMENTS_ENDPOINT}", headers=auth_headers, timeout=TIMEOUT)

        assert resp.status_code == 200, f"Expected 200 OK, got {resp.status_code}"
        resp_json = resp.json()
        assert isinstance(resp_json, list), f"Response is not a JSON array but {type(resp_json)}"

        for idx, item in enumerate(resp_json):
            assert isinstance(item, dict), f"Element at index {idx} is not an object/dict"
            for field in ["ticketNumber", "status", "createdAt"]:
                assert field in item, f"Field '{field}' missing in element at index {idx}"
                assert item[field] is not None, f"Field '{field}' is None in element at index {idx}"

    except (requests.RequestException, AssertionError) as e:
        raise AssertionError(f"Test list_voided_documents failed: {e}")

test_list_voided_documents()